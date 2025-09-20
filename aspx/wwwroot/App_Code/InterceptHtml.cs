using System;
using System.IO;
using System.Text;
using System.Web;
public class InterceptHtml : IHttpModule
{
    public void Init(HttpApplication context)
    {
        context.PostResolveRequestCache += (sender, e) =>
        {
            var app = (HttpApplication)sender;
            var ctx = app.Context;

            string fullHtmlPath = ResolveFullPath(ctx);
            if (fullHtmlPath != null && File.Exists(fullHtmlPath))
            {
                ctx.RemapHandler(new HtmlHandler(fullHtmlPath));
            }
        };
    }

    /// <summary>
    /// Resolve the full path to the requested HTML file.
    /// If the file does not exist or is not an HTML file, returns null.
    /// </summary>
    private string ResolveFullPath(HttpContext context)
    {
        // if there is no file name, assume it is a request for index.html
        string requested = context.Request.AppRelativeCurrentExecutionFilePath.TrimStart('~', '/');
        if (string.IsNullOrEmpty(requested))
        {
            requested = "index.html";
        }

        // if there is no extension, assume .html
        if (string.IsNullOrEmpty(Path.GetExtension(requested)))
        {
            requested += ".html";
        }

        // if the file is not an HTML file, do not resolve the path
        if (!requested.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // return the full path
        return context.Server.MapPath("~/" + requested);
    }

    /// <summary>
    /// Custom HTTP handler to serve HTML files with token replacement.
    /// </summary>
    private class HtmlHandler : IHttpHandler
    {
        private readonly string _fullPath;

        public HtmlHandler(string fullPath)
        {
            _fullPath = fullPath;
        }

        /// <summary>
        /// Reads a file and replaces specific tokens before writing it to the response.
        /// </summary>
        /// <param name="context"></param>
        public void ProcessRequest(HttpContext context)
        {
            // read file
            string html = File.ReadAllText(_fullPath, Encoding.UTF8);

            // token replacement
            string machineDisplayName =
                new AliasUtilities.AliasResolver().Resolve(Environment.MachineName);
            html = html.Replace("%raweb.servername%", machineDisplayName);

            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.Write(html);
        }

        public bool IsReusable
        {
            get { return true; }
        }

    }


    public void Dispose() { }
}
