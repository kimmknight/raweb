using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace RAWeb.DesktopApp.InternalServer;

internal static class UseHeadInjectionMiddleware {
  internal enum InjectionType {
    Style,
    Script
  }

  /// <summary>
  /// This middleware allows you to inject content into the head of HTML responses.
  /// </summary>
  /// <param name="app"></param>
  /// <param name="type"></param>
  /// <param name="content"></param>
  /// <exception cref="NotImplementedException"></exception>
  internal static void UseHeadInjection(this WebApplication app, InjectionType type, string content, string id) {
    app.UseHeadInjection(type, () => content, id);
  }

  /// <summary>
  /// This middleware allows you to inject content into the head of HTML responses.
  /// </summary>
  /// <param name="app"></param>
  /// <param name="type"></param>
  /// <param name="content"></param>
  /// <exception cref="NotImplementedException"></exception>
  internal static void UseHeadInjection(this WebApplication app, InjectionType type, Func<string> getContent, string id) {
    app.Use(async (context, next) => {
      var originalBodyStream = context.Response.Body;

      // buffer the response body so it can be read and rewritten once the
      // pipeline has finished writing to it (the live response stream is
      // write-only and cannot be read back)
      using var newBodyStream = new MemoryStream();
      context.Response.Body = newBodyStream;

      try {
        await next(context);
      }
      finally {
        context.Response.Body = originalBodyStream;
      }

      var contentType = context.Response.ContentType ?? "";
      var isHtml = contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase);
      var isStatusOk = context.Response.StatusCode == StatusCodes.Status200OK;

      newBodyStream.Seek(0, SeekOrigin.Begin);

      if (!isHtml || !isStatusOk) {
        await newBodyStream.CopyToAsync(originalBodyStream);
        return;
      }

      var content = getContent();
      var injectionString = type switch {
        InjectionType.Style => $"<style id=\"{id}\">{content}</style>",
        InjectionType.Script => $"<script id=\"{id}\">{content}</script>",
        _ => throw new NotImplementedException()
      };

      try {
        // read the buffered stream as text, inject the content into the head,
        // and write the result to the original response stream
        using var reader = new StreamReader(newBodyStream, Encoding.UTF8, leaveOpen: true);
        var html = await reader.ReadToEndAsync();
        var injectedHtml = html.Replace("</head>", $"{injectionString}</head>", StringComparison.OrdinalIgnoreCase);
        var injectedBytes = Encoding.UTF8.GetBytes(injectedHtml);
        context.Response.ContentLength = injectedBytes.Length;
        await originalBodyStream.WriteAsync(injectedBytes);
      }
      catch (Exception ex) {
        // if anything goes wrong during the injection process, we want to fail
        // gracefully by writing the original response body to the output without
        // modification
        Console.Error.WriteLine($"Error during head injection: {ex}");
        newBodyStream.Seek(0, SeekOrigin.Begin);
        await newBodyStream.CopyToAsync(originalBodyStream);
      }
    });
  }
}
