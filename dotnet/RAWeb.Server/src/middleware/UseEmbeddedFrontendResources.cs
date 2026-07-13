using System.Reflection;
using System.Text;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Middleware;

public static class UseEmbeddedFrontendResourcesMiddleware {
  public static void UseEmbeddedFrontendResources(this WebApplication app, bool showServerNameInTitle = true) {
    var assembly = Assembly.GetExecutingAssembly();

    app.Use(async (context, next) => {
      // if the request path would interfere with other endpoints, we
      // should not serve the embedded resource and instead pass the request
      // to the next middleware
      if (IsExcludedPath(context.Request.Path.Value ?? string.Empty, context.Request.PathBase.Value ?? string.Empty)) {
        await next(context);
        return;
      }

      var path = context.Request.Path.Value?.TrimStart('/');
      if (string.IsNullOrEmpty(path)) {
        path = "index.html";
      }
      if (path.EndsWith('/')) {
        path = $"{path}index.html";
      }

      // get the names of all resources that are embedded into the assembly
      var allResourceNames = assembly.GetManifestResourceNames();

      // the built frontend assets are embedded into the assembly as resources with the prefix "static/"
      var resourceName = $"static/{path}";
      var ext = Path.GetExtension(path);

      // If there is no matching resource, try treating the request as an
      // extensionless route by appending ".html".
      // For example: /page should resolve to /page.html if it exists
      // NOTE: We check for the .html variant directly rather than relying on
      // Path.GetExtension because route segments may include dots.
      if (!allResourceNames.Contains(resourceName) && allResourceNames.Contains($"{resourceName}.html")) {
        ext = ".html";
        resourceName = $"{resourceName}.html";
      }

      // determine which frontend shell to use for client-side routing
      var frontendShellResourceName = path.StartsWith("docs/") || path == "docs"
        ? "static/docs.html"
        : "static/index.html";

      // if the requested resource is an html file but it does not exist,
      // we should serve the fallback shell instead and allow
      // client-side routing to handle showing the correct page.
      if (ext == ".html" && !allResourceNames.Contains(resourceName)) {
        resourceName = frontendShellResourceName;
      }

      // if there is still no matching resource, but the request accepts
      // html, then we should also try serving the fallback shell
      var acceptsHtml = context.Request.Headers.Accept.Any(header => header?.Contains("text/html") ?? false);
      if (acceptsHtml && !allResourceNames.Contains(resourceName)) {
        ext = ".html";
        resourceName = frontendShellResourceName;
      }

      // if there is still no matching resource, then we should pass the request
      // to the next middleware (which will likely return a 404)
      if (!allResourceNames.Contains(resourceName)) {
        await next(context);
        return;
      }

      // check for an icon override policy (App.Icon.<filename>) before serving the embedded resource
      var ignoreOverride = context.Request.Query.TryGetValue("ignoreOverride", out var ignoreOverrideValue) &&
                           ignoreOverrideValue == "true";
      if (!ignoreOverride && path.StartsWith("lib/assets/")) {
        var fileName = Path.GetFileName(path);
        var iconPolicyValue = PoliciesManager.RawPolicies[$"App.Icon.{fileName}"];
        if (!string.IsNullOrEmpty(iconPolicyValue) && iconPolicyValue.StartsWith("data:")) {
          var semicolonIndex = iconPolicyValue.IndexOf(';');
          var commaIndex = iconPolicyValue.IndexOf(',');
          if (semicolonIndex > 5 && commaIndex > semicolonIndex) {
            var mimeType = iconPolicyValue.Substring(5, semicolonIndex - 5);
            var base64Data = iconPolicyValue.Substring(commaIndex + 1);
            try {
              var bytes = Convert.FromBase64String(base64Data);
              context.Response.ContentType = mimeType;
              await context.Response.Body.WriteAsync(bytes);
              return;
            } catch {
              // invalid base64 — fall through to serve the embedded resource
            }
          }
        }
      }

      // read the resource stream
      using var stream = assembly.GetManifestResourceStream(resourceName);
      if (stream is null) {
        await next(context);
        return;
      }

      // write the stream to the response with the correct content type
      context.Response.ContentType = GetContentType(ext);
      if (ext == ".html") {
        await StreamHtmlAsync(stream, context, showServerNameInTitle);
        return;
      }
      await stream.CopyToAsync(context.Response.Body);
      return;
    });
  }

  private static string GetContentType(string ext) => ext switch {
    ".html" => "text/html",
    ".js" => "application/javascript",
    ".css" => "text/css",
    ".png" => "image/png",
    ".jpg" => "image/jpeg",
    ".jpeg" => "image/jpeg",
    ".gif" => "image/gif",
    ".svg" => "image/svg+xml",
    ".webp" => "image/webp",
    ".ico" => "image/x-icon",
    ".webmanifest" => "application/manifest+json",
    ".timestamp" => "text/plain",
    _ => "application/octet-stream"
  };

  private static bool IsExcludedPath(string path, string rootPath) {

    var excludedPrefixes = new[] {
    "/api/",
    "/auth/",
  };

    var excludedPaths = new[] {
    "/guacd-tunnel",
    "/webfeed.aspx",
    "/resources",
  };

    return excludedPaths.Contains(path) || excludedPrefixes.Any(prefix => path.StartsWith(prefix));
  }

  private static async Task StreamHtmlAsync(Stream stream, HttpContext context, bool showServerNameInTitle) {
    var rootPath = context.Request.PathBase.Value ?? string.Empty;

    // check whether a policy-provided icon should replace the default SVG on the splash screen
    var icon192Policy = PoliciesManager.RawPolicies["App.Icon.icon-192x192.webp"];
    var splashLogoImg = string.IsNullOrEmpty(icon192Policy)
      ? ""
      : $"""<img src="{rootPath}/lib/assets/icon-192x192.webp" class="root-splash-app-logo" alt="" />""";

    // check for inject/index.css and inject/index.js
    var injectDir = Path.Combine(Constants.AppDataFolderPath, "inject");
    var cssPath = Path.Combine(injectDir, "index.css");
    var jsPath = Path.Combine(injectDir, "index.js");
    var injectBuilder = new StringBuilder();
    if (File.Exists(cssPath)) {
      injectBuilder.AppendLine($"""<link rel="stylesheet" type="text/css" href="{rootPath}/api/inject/file/index.css">""");
    }
    if (File.Exists(jsPath)) {
      injectBuilder.AppendLine($"""<script type="text/javascript" src="{rootPath}/api/inject/file/index.js"></script>""");
    }

    // values for token replacement
    var machineDisplayName = new AliasResolver().Resolve(Environment.MachineName);
    var baseTag = $"""<base href="{rootPath}/">""";
    var overrides = injectBuilder.ToString();

    // read and perform token replacement line by line
    // to avoid loading the entire html file into memory
    using var reader = new StreamReader(stream);
    await using var writer = new StreamWriter(
      context.Response.Body,
      // leaveOpen: middleware does not own context.Response.Body and must not dispose it
      leaveOpen: true
    );
    string? line;
    while ((line = await reader.ReadLineAsync()) is not null) {
      if (showServerNameInTitle) {
        line = line.Replace("%raweb.servername%", machineDisplayName);
      }
      line = line.Replace("%raweb.basetag%", baseTag);
      line = line.Replace("%raweb.overrides%", overrides);
      line = line.Replace("%raweb.splashlogoimg%", splashLogoImg);
      await writer.WriteLineAsync(line);
    }

    await writer.FlushAsync();
    return;
  }
}
