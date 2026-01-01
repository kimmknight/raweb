using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace RAWeb.Server.Utilities;

/// <summary>
/// Manages application policies stored in the appSettings.config file.
/// <br /><br />
/// The appSettings.config file is located in the App_Data folder by default.
/// Specify a different path via the constructor parameter if needed.
/// </summary>
/// <param name="appSettingsPath"></param>
public sealed class PoliciesManager {
  private static string s_appSettingsPath = Path.Combine(Constants.AppDataFolderPath, "appSettings.config");

  /// <summary>
  /// Constructor for PoliciesManager.
  /// Unless you need to specify a different path for appSettings.config,
  /// you do not need to instantiate this class. Instead, use the static
  /// methods.
  /// </summary>
  /// <param name="appSettingsPath"></param>
  public PoliciesManager(string? appSettingsPath = null) {
    if (appSettingsPath != null) {
      s_appSettingsPath = appSettingsPath;
    }
  }

  /// <summary>
  /// Exposes the key-value pairs from the appSettings.config file as a dictionary.
  /// </summary>
  public static PoliciesDictionary RawPolicies {
    get {
      // read the XML content of appSettings
      if (!File.Exists(s_appSettingsPath)) {
        return new PoliciesDictionary();
      }

      var appSettingsXml = File.ReadAllText(s_appSettingsPath);

      // parse the XML to extract key-value pairs
      var appSettings = XDocument.Parse(appSettingsXml)
        .Descendants("appSettings")
        .Descendants("add")
        .ToDictionary(
          x => x.Attribute("key")?.Value ?? "",
          x => x.Attribute("value")?.Value ?? ""
        );

      return appSettings;
    }
  }

  /// <summary>
  /// Sets or updates the specified key-value pair in the appSettings.config file.
  /// </summary>
  /// <param name="key"></param>
  /// <param name="value"></param>
  public static void Set(string key, string value) {
    if (string.IsNullOrEmpty(value)) {
      Remove(key);
      return;
    }

    var policies = RawPolicies.Value;
    policies[key] = value;
    WriteFile(policies);
  }

  /// <summary>
  /// Removes the specified key from the appSettings.config file.
  /// </summary>
  /// <param name="key"></param>
  public static void Remove(string key) {
    var policies = RawPolicies.Value;
    if (policies.ContainsKey(key)) {
      policies.Remove(key);
      WriteFile(policies);
    }
  }

  /// <summary>
  /// Writes the provided policies dictionary back to the appSettings.config file.
  /// </summary>
  /// <param name="policies"></param>
  private static void WriteFile(Dictionary<string, string> policies) {
    var appSettingsElement = new XElement("appSettings",
      policies
        // order keys alphabetically
        .OrderBy(kv => kv.Key)
        // convert to XML elements
        .Select(kv => new XElement("add",
          new XAttribute("key", kv.Key),
          new XAttribute("value", kv.Value)
        ))
    );

    var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), appSettingsElement);
    doc.Save(s_appSettingsPath);
  }

  /// <summary>
  /// A custom dictionary-like class that returns null for missing keys.
  /// </summary>
  public sealed class PoliciesDictionary(Dictionary<string, string>? innerDictionary = null) {
    private readonly Dictionary<string, string> _innerDictionary = innerDictionary ?? [];

    public static implicit operator PoliciesDictionary(Dictionary<string, string> dictionary) {
      return new PoliciesDictionary(dictionary);
    }

    /// <summary>
    /// Gets the value associated with the specified key, or null if the key does not exist. [indexer]
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public string? this[string key] {
      get {
        _innerDictionary.TryGetValue(key, out var value);
        return value;
      }
    }

    /// <summary>
    /// Gets the Duo MFA policy configuration if it exists; otherwise, returns null.
    /// The policy value, if present, is expected to be in the format:
    /// clientId:clientSecret@hostname. This method parses that string and returns
    /// a DuoMfaPolicyResult object containing the individual components.
    /// </summary>
    public DuoMfaPolicyResult? DuoMfa {
      get {
        var rawEnablementValue = this["App.Auth.MFA.Duo.Enabled"];
        var rawConfigValue = this["App.Auth.MFA.Duo"];

        // check if Duo MFA is enabled
        if (string.IsNullOrEmpty(rawEnablementValue) ||
            !bool.TryParse(rawEnablementValue, out var isEnabled) ||
            !isEnabled) {
          return null;
        }

        // parse the raw value as clientId:clientSecret@hostname
        if (string.IsNullOrEmpty(rawConfigValue)) {
          return null;
        }

        // part 1: clientId:clientSecret; part 2: hostname
        var parts = rawConfigValue.Split('@');
        if (parts.Length != 2) {
          return null;
        }
        var credentialsPart = parts[0];
        var hostnamePart = parts[1];

        // part 1a: clientId; part 1b: clientSecret
        var credentialsParts = credentialsPart.Split(':');
        if (credentialsParts.Length != 2) {
          return null;
        }
        var clientId = credentialsParts[0];
        var clientSecret = credentialsParts[1];

        return new DuoMfaPolicyResult(
          Hostname: hostnamePart,
          ClientId: clientId,
          SecretKey: clientSecret,
          RedirectPath: "/api/auth/duo/callback"
        );
      }
    }

    /// <summary>
    /// Exposes the internal dictionary for direct serialization to a JSON object.
    /// </summary>
    public Dictionary<string, string> Value => _innerDictionary;
  }

  public record DuoMfaPolicyResult(
    string Hostname,
    string ClientId,
    string SecretKey,
    string RedirectPath
  );
}
