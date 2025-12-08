using System;
using System.IO;
using System.Text;
using System.Web;
using RAWeb.Server.Utilities;

namespace RAWebServer.Modules {
    public class InterceptHtml : IHttpModule {
        public void Init(HttpApplication context) {
            context.BeginRequest += (sender, e) => {
                var app = (HttpApplication)sender;
                var ctx = app.Context;

                // normalize requests to the root or to index.html to "/"
                // so that relative paths in our HTML templates work correctly
                if (!ctx.Request.Path.EndsWith("/") && string.Equals(ctx.Request.AppRelativeCurrentExecutionFilePath, "~/", StringComparison.OrdinalIgnoreCase)) {
                    ctx.Response.Redirect("~/");
                    return;
                }
            };

            context.PostResolveRequestCache += (sender, e) => {
                var app = (HttpApplication)sender;
                var ctx = app.Context;

                // let IIS handle serving static files normally
                var fullPath = ctx.Server.MapPath(ctx.Request.AppRelativeCurrentExecutionFilePath);
                if (File.Exists(fullPath)) {
                    return;
                }

                // capture Windows Server- and Azure-style webfeed URLs
                if (
                    ctx.Request.Path.EndsWith("/Feed/webfeed.aspx", StringComparison.OrdinalIgnoreCase) || // RDWeb default location
                    ctx.Request.Path.EndsWith("/RDWeb/Feed/webfeed.aspx", StringComparison.OrdinalIgnoreCase) || // RDWeb default location
                    ctx.Request.Path.EndsWith("/api/feeddiscovery/webfeeddiscovery.aspx", StringComparison.OrdinalIgnoreCase) || // Azure Virtual Desktop (classic) default location
                    ctx.Request.Path.EndsWith("/api/arm/feeddiscovery", StringComparison.OrdinalIgnoreCase) // Azure Virtual Desktop default location
                ) {
                    // redirect to our workspace/webfeed endpoint
                    ctx.Response.Redirect("~/api/workspace");
                    return;
                }

                // do not interfere with requests to the API
                var relativePath = ctx.Request.AppRelativeCurrentExecutionFilePath;
                if (
                    relativePath.StartsWith("~/api/", StringComparison.OrdinalIgnoreCase) ||
                    relativePath.StartsWith("~/auth/", StringComparison.OrdinalIgnoreCase) ||
                    relativePath.Equals("~/webfeed.aspx", StringComparison.OrdinalIgnoreCase)
                ) {
                    return;
                }

                // [Workspace Discovery - Part 1]
                // The macOS, iOS and Android clients use their own user agents when
                // testing whether the workspace URL is valid. In effect, they are testing
                // whether they receive a 401 Unauthorized response with a WWW-Authenticate
                // header for NTLM or Negotiate.
                var hasAspxAuthCookie = ctx.Request.Cookies[".ASPXAUTH"] != null;
                if (ctx.Request.UserAgent != null && !hasAspxAuthCookie) {
                    var isMacosAddWorkspaceDialog = ctx.Request.UserAgent.StartsWith("com.microsoft.rdc.macos") && ctx.Request.UserAgent.Contains("RdCore/");
                    var isIosAddWorkspaceDialog = ctx.Request.UserAgent.StartsWith("com.microsoft.rdc.ios") && ctx.Request.UserAgent.Contains("RdCore/");
                    var isAndroidAddWorkspaceDialog = ctx.Request.UserAgent.StartsWith("com.microsoft.rdc.androidx") && ctx.Request.UserAgent.Contains("RdCore/");

                    if (isMacosAddWorkspaceDialog || isIosAddWorkspaceDialog || isAndroidAddWorkspaceDialog) {
                        ctx.Response.StatusCode = 401; // Unauthorized - IIS will add the WWW-Authenticate header as long as Windows Authentication is enabled
                        ctx.Response.End();
                        return;
                    }
                }

                // [Workspace Discovery - Part 2]
                // If it is a workspace client (e.g. Windows RADC or Windows App),
                // serve the workspace XML when no file or endpoint matches (excludes HTML files)
                // This is allows the client to handle cases where the workspace
                // URL entered by the user is not teh exact correct URL.
                var isWorkspaceClient = ctx.Request.UserAgent.StartsWith("TSWorkspace/2.0");
                if (isWorkspaceClient) {
                    ctx.Response.Redirect("~/api/workspace");
                    return;
                }

                // if the request resolves to an HTML file, serve it with token replacement
                var htmlPath = ResolveFullPath(ctx);
                if (htmlPath != null && File.Exists(htmlPath)) {
                    ctx.RemapHandler(new HtmlHandler(htmlPath));
                    return;
                }

                // directly serve any static file in the App_Data/inject folder
                if (relativePath.StartsWith("~/inject/", StringComparison.OrdinalIgnoreCase)) {
                    Console.WriteLine("Serving injected file: " + relativePath);
                    var rootedInjectPath = ctx.Server.MapPath("~/App_Data/inject/" + relativePath.Substring("~/inject/".Length));
                    ctx.RemapHandler(new StaticFileHandler(rootedInjectPath));
                    return;
                }

                // if the request starts with ~/docs, serve docs/index.html
                if (relativePath.StartsWith("~/docs", StringComparison.OrdinalIgnoreCase)) {
                    var docsIndexPath = ctx.Server.MapPath("~/docs.html");
                    if (File.Exists(docsIndexPath)) {
                        ctx.RemapHandler(new HtmlHandler(docsIndexPath));
                        return;
                    }
                }

                // otherwise, always serve index.html
                var indexPath = ctx.Server.MapPath("~/index.html");
                if (File.Exists(indexPath)) {
                    ctx.RemapHandler(new HtmlHandler(indexPath));
                }
            };
        }

        /// <summary>
        /// Resolve the full path to the requested HTML file.
        /// If the file does not exist or is not an HTML file, returns null.
        /// </summary>
        private string ResolveFullPath(HttpContext context) {
            // if there is no file name, assume it is a request for index.html
            var requested = context.Request.AppRelativeCurrentExecutionFilePath.TrimStart('~', '/');
            if (string.IsNullOrEmpty(requested)) {
                requested = "index.html";
            }

            // if there is no extension, assume .html
            if (string.IsNullOrEmpty(Path.GetExtension(requested))) {
                requested += ".html";
            }

            // if the file is not an HTML file, do not resolve the path
            if (!requested.EndsWith(".html", StringComparison.OrdinalIgnoreCase)) {
                return null;
            }

            // return the full path
            return context.Server.MapPath("~/" + requested);
        }

        /// <summary>
        /// Custom HTTP handler to serve HTML files with token replacement.
        /// </summary>
        private class HtmlHandler : IHttpHandler {
            private readonly string _fullPath;

            public HtmlHandler(string fullPath) {
                _fullPath = fullPath;
            }

            /// <summary>
            /// Reads a file and replaces specific tokens before writing it to the response.
            /// </summary>
            /// <param name="context"></param>
            public void ProcessRequest(HttpContext context) {
                // read file
                var html = File.ReadAllText(_fullPath, Encoding.UTF8);

                // check for inject/index.css and inject/index.js
                var injectDir = context.Server.MapPath("~/App_Data/inject/");
                var cssPath = Path.Combine(injectDir, "index.css");
                var jsPath = Path.Combine(injectDir, "index.js");
                var injectBuilder = new StringBuilder();
                if (File.Exists(cssPath)) {
                    injectBuilder.AppendLine("<link rel=\"stylesheet\" type=\"text/css\" href=\"" + VirtualPathUtility.ToAbsolute("~/inject/index.css") + "\" />");
                }
                if (File.Exists(jsPath)) {
                    injectBuilder.AppendLine("<script type=\"text/javascript\" src=\"" + VirtualPathUtility.ToAbsolute("~/inject/index.js") + "\"></script>");
                }

                // token replacement
                var machineDisplayName =
                    new AliasResolver().Resolve(Environment.MachineName);
                html = html.Replace("%raweb.servername%", machineDisplayName);
                html = html.Replace("%raweb.basetag%", "<base href=\"" + VirtualPathUtility.ToAbsolute("~/") + "\" />");
                html = html.Replace("%raweb.overrides%", injectBuilder.ToString());

                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.Write(html);
            }

            public bool IsReusable {
                get { return true; }
            }

        }

        private class StaticFileHandler : IHttpHandler {
            private readonly string _fullPath;

            public StaticFileHandler(string fullPath) {
                _fullPath = fullPath;
            }

            public void ProcessRequest(HttpContext context) {
                var extension = Path.GetExtension(_fullPath).ToLowerInvariant();
                switch (extension) {
                    case ".css":
                        context.Response.ContentType = "text/css";
                        context.Response.Write(File.ReadAllText(_fullPath));
                        break;
                    case ".js":
                        context.Response.ContentType = "application/javascript";
                        context.Response.Write(File.ReadAllText(_fullPath));
                        break;
                    default:
                        var contentType = MimeMapping.GetMimeMapping(Path.GetFileName(_fullPath));
                        context.Response.ContentType = contentType;
                        var bytes = File.ReadAllBytes(_fullPath);
                        context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                        break;
                }
            }

            public bool IsReusable {
                get { return true; }
            }
        }

        public void Dispose() { }
    }
}
