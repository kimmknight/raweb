<%@ Page Language="C#" debug="true" %>

<script runat="server">
    protected void Page_Load(object sender, EventArgs e)
    {

        // prepare to serve the manifest.json file for the PWA
        Response.Clear();
        Response.ContentType = "application/json";

        // define the webmanifest
        var manifest = new
        {
            name = "RemoteApps",
            start_url = ResolveUrl("~/#favorites"),
            display = "standalone",
            display_override = new[] { "window-controls-overlay" },
            background_color = "#ffffff",
            theme_color = "#000000",
            icons = new[]
            {
                new
                {
                    src = ResolveUrl("~/lib/assets/icon.svg"),
                    sizes = "144x144",
                    type = "image/svg+xml",
                    purpose = "any"
                },
                new
                {
                    src = ResolveUrl("~/lib/assets/maskable-icon.svg"),
                    sizes = "144x144",
                    type = "image/svg+xml",
                    purpose = "maskable"
                },
                new
                {
                    src = ResolveUrl("~/lib/assets/monochrome-icon.svg"),
                    sizes = "144x144",
                    type = "image/svg+xml",
                    purpose = "monochrome"
                }
            },
            screenshots = new[]
            {
                new
                {
                    src = ResolveUrl("~/lib/assets/favorites_dark.png"),
                    sizes = "2570x1660", // 1285x730 pixels in browser dev tools
                    type = "image/png",
                    form_factor = "wide"
                },
                new
                {
                    src = ResolveUrl("~/lib/assets/devices_dark.png"),
                    sizes = "2570x1660",
                    type = "image/png",
                    form_factor = "wide"
                },
                new
                {
                    src = ResolveUrl("~/lib/assets/app-properties_dark.png"),
                    sizes = "2570x1660",
                    type = "image/png",
                    form_factor = "wide"
                },
                new
                {
                    src = ResolveUrl("~/lib/assets/favorites_light.png"),
                    sizes = "2570x1660",
                    type = "image/png",
                    form_factor = "wide"
                },
                new
                {
                    src = ResolveUrl("~/lib/assets/devices_light.png"),
                    sizes = "2570x1660",
                    type = "image/png",
                    form_factor = "wide"
                },
                new
                {
                    src = ResolveUrl("~/lib/assets/app-properties_light.png"),
                    sizes = "2570x1660",
                    type = "image/png",
                    form_factor = "wide"
                },
                new
                {
                    src = ResolveUrl("~/lib/assets/favorites_mobile_dark.png"),
                    sizes = "880x1680", // 440x840 pixels in browser dev tools
                    type = "image/png",
                    form_factor = "narrow"
                },
                new
                {
                    src = ResolveUrl("~/lib/assets/favorites_mobile_light.png"),
                    sizes = "880x1680",
                    type = "image/png",
                    form_factor = "narrow"
                }
            }
        };

        // serialize the manifest to JSON and write it to the response
        var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
        string json = serializer.Serialize(manifest);
        Response.Write(json);
        Response.End();
    }
</script>

