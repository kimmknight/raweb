using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Windows.Storage;

namespace RAWeb.DesktopApp.InternalServer;

/// <summary>
/// Persists the internal RAWeb origin's localStorage to disk and restores it on
/// the next launch.
/// <br/><br/>
/// The internal server binds to a random free port on every launch (see
/// ServerUtils.StartServer), so the WebView2 origin is different every time
/// the app starts. This means that the browser's built-in localStorage
/// persistence is useless in our case.
/// <br/><br/>
/// This middleware closes that gap entirely from the server side: it injects a
/// script into every HTML response (via UseHeadInjection) that restores
/// previously-persisted values into localStorage and reports every subsequent
/// change to API endpoints that this middleware also maps.
/// </summary>
internal static class UseLocalStoragePersistenceMiddleware {
  private const string FileName = "localStorage.json";
  private const string SetItemEndpointPath = "/api/_internal/local-storage/set-item";
  private const string RemoveItemEndpointPath = "/api/_internal/local-storage/remove-item";
  private const string ClearEndpointPath = "/api/_internal/local-storage/clear";

  private const string InjectedScriptTemplate = """
    (function () {
      var persisted = __PERSISTED__;
      for (var key in persisted) {
        if (Object.prototype.hasOwnProperty.call(persisted, key)) {
          try { localStorage.setItem(key, persisted[key]); } catch (e) {}
        }
      }

      var nativeSetItem = Storage.prototype.setItem;
      var nativeRemoveItem = Storage.prototype.removeItem;
      var nativeClear = Storage.prototype.clear;

      function post(url, body) {
        try {
          fetch(url, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            credentials: "same-origin",
            keepalive: true,
            body: JSON.stringify(body),
          });
        } catch (e) {}
      }

      Storage.prototype.setItem = function (key, value) {
        nativeSetItem.call(this, key, value);
        if (this === window.localStorage) {
          post(__SET_ITEM_ENDPOINT__, { key: String(key), value: String(value) });
        }
      };

      Storage.prototype.removeItem = function (key) {
        nativeRemoveItem.call(this, key);
        if (this === window.localStorage) {
          post(__REMOVE_ITEM_ENDPOINT__, { key: String(key) });
        }
      };

      Storage.prototype.clear = function () {
        nativeClear.call(this);
        if (this === window.localStorage) {
          post(__CLEAR_ENDPOINT__, {});
        }
      };
    })();
  """;

  /// <summary>
  /// Loads any previously-persisted localStorage values from <paramref name="storageFolder"/>,
  /// injects a script that restores them (and mirrors future changes back to disk),
  /// and maps the API endpoints that receive those changes.
  /// </summary>
  internal static void UseLocalStoragePersistence(this WebApplication app, StorageFolder storageFolder) {
    var filePath = Path.Combine(storageFolder.Path, FileName);
    var values = LoadValues(filePath);
    var valuesDictReadWriteLock = new Lock();
    var valuesFileWriteLock = new SemaphoreSlim(1, 1);

    var scriptTemplate = InjectedScriptTemplate
      .Replace("__SET_ITEM_ENDPOINT__", JsonSerializer.Serialize(SetItemEndpointPath, LocalStorageJsonContext.Default.String))
      .Replace("__REMOVE_ITEM_ENDPOINT__", JsonSerializer.Serialize(RemoveItemEndpointPath, LocalStorageJsonContext.Default.String))
      .Replace("__CLEAR_ENDPOINT__", JsonSerializer.Serialize(ClearEndpointPath, LocalStorageJsonContext.Default.String));
    app.UseHeadInjection(
      UseHeadInjectionMiddleware.InjectionType.Script,
      () => {
        string persisted;
        lock (valuesDictReadWriteLock) {
          persisted = JsonSerializer.Serialize(values, LocalStorageJsonContext.Default.DictionaryStringString);
        }
        return scriptTemplate.Replace("__PERSISTED__", persisted);
      },
      "local-storage-persistence-script"
    );

    app.MapPost(SetItemEndpointPath, async (HttpContext context) => {
      var body = await DeserializeAsync(context, LocalStorageJsonContext.Default.SetItemRequestBody);
      if (body?.Key is not { } key || body.Value is not { } value) {
        return Results.BadRequest();
      }

      await MutateAsync(valuesDictReadWriteLock, valuesFileWriteLock, filePath, values, () => values[key] = value);
      return Results.NoContent();
    });

    app.MapPost(RemoveItemEndpointPath, async (HttpContext context) => {
      var body = await DeserializeAsync(context, LocalStorageJsonContext.Default.RemoveItemRequestBody);
      if (body?.Key is not { } key) {
        return Results.BadRequest();
      }

      await MutateAsync(valuesDictReadWriteLock, valuesFileWriteLock, filePath, values, () => values.Remove(key));
      return Results.NoContent();
    });

    app.MapPost(ClearEndpointPath, async (HttpContext _) => {
      await MutateAsync(valuesDictReadWriteLock, valuesFileWriteLock, filePath, values, values.Clear);
      return Results.NoContent();
    });
  }

  /// <summary>
  /// Deserializes the request body using the supplied source-generated type info,
  /// returning <see langword="null"/> if the body is missing or malformed.
  /// </summary>
  private static async Task<TBody?> DeserializeAsync<TBody>(HttpContext context, JsonTypeInfo<TBody> typeInfo) {
    try {
      return await JsonSerializer.DeserializeAsync(context.Request.Body, typeInfo);
    }
    catch (JsonException) {
      return default;
    }
  }

  private static Dictionary<string, string> LoadValues(string filePath) {
    if (!File.Exists(filePath)) {
      return [];
    }

    try {
      using var stream = File.OpenRead(filePath);
      return JsonSerializer.Deserialize(stream, LocalStorageJsonContext.Default.DictionaryStringString) ?? [];
    }
    catch (Exception ex) {
      Console.Error.WriteLine($"[LocalStoragePersistence] Failed to load persisted localStorage from {filePath}: {ex.Message}");
      return [];
    }
  }

  private static async Task MutateAsync(Lock valuesDictReadWriteLock, SemaphoreSlim valuesFileWriteLock, string filePath, Dictionary<string, string> values, Action mutate) {
    await valuesFileWriteLock.WaitAsync();
    try {
      string json;
      lock (valuesDictReadWriteLock) {
        mutate();
        json = JsonSerializer.Serialize(values, LocalStorageJsonContext.Default.DictionaryStringString);
      }
      await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
    }
    catch (Exception ex) {
      Console.Error.WriteLine($"[LocalStoragePersistence] Failed to persist localStorage to {filePath}: {ex.Message}");
    }
    finally {
      valuesFileWriteLock.Release();
    }
  }
}

internal sealed record SetItemRequestBody(string? Key, string? Value);

internal sealed record RemoveItemRequestBody(string? Key);

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(SetItemRequestBody))]
[JsonSerializable(typeof(RemoveItemRequestBody))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(string))]
internal partial class LocalStorageJsonContext : JsonSerializerContext { }
