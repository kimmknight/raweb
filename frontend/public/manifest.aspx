<%@ Page Language="C#" debug="true" %>

<script runat="server">
    protected void Page_Load(object sender, EventArgs e)
    {

        // This page is used to serve the manifest.json file for the PWA
        Response.Clear();
        Response.ContentType = "application/json";

        // Define the webmanifest
        var manifest = new
        {
            name = "RemoteApps",
            //short_name = "RAWeb",
            start_url = ResolveUrl("~/app/#favorites"),
            display = "standalone",
            display_override = new[] { "window-controls-overlay" }, // Corrected array initialization
            background_color = "#ffffff",
            theme_color = "#000000",
            icons = new[]
            {
                new
                {
                    src = ResolveUrl("~/app/lib/assets/icon.svg"),
                    sizes = "144x144",
                    type = "image/svg+xml",
                    purpose = "any"
                },
                new
                {
                    src = ResolveUrl("~/app/lib/assets/maskable-icon.svg"),
                    sizes = "144x144",
                    type = "image/svg+xml",
                    purpose = "maskable"
                },
                new
                {
                    src = ResolveUrl("~/app/lib/assets/monochrome-icon.svg"),
                    sizes = "144x144",
                    type = "image/svg+xml",
                    purpose = "monochrome"
                }
            },
            screenshots = new[]
            {
                new
                {
                    src = ResolveUrl("~/app/lib/assets/favorites_dark.png"),
                    sizes = "2570x1660",
                    type = "image/png",
                    // NOTE: Allow the wide screenshots used 1285x730 pixels in browser dev tools
                    form_factor = "wide"
                },
                new
                {
                    src = ResolveUrl("~/app/lib/assets/devices_dark.png"),
                    sizes = "2570x1660",
                    type = "image/png",
                    form_factor = "wide"
                },
                new
                {
                    src = ResolveUrl("~/app/lib/assets/app-properties_dark.png"),
                    sizes = "2570x1660",
                    type = "image/png",
                    form_factor = "wide"
                },
                new
                {
                    src = ResolveUrl("~/app/lib/assets/favorites_light.png"),
                    sizes = "2570x1660",
                    type = "image/png",
                    form_factor = "wide"
                },
                new
                {
                    src = ResolveUrl("~/app/lib/assets/devices_light.png"),
                    sizes = "2570x1660",
                    type = "image/png",
                    form_factor = "wide"
                },
                new
                {
                    src = ResolveUrl("~/app/lib/assets/app-properties_light.png"),
                    sizes = "2570x1660",
                    type = "image/png",
                    form_factor = "wide"
                },
                new
                {
                    src = ResolveUrl("~/app/lib/assets/favorites_mobile_dark.png"),
                    sizes = "880x1680",
                    type = "image/png",
                    // NOTE: Allow the wide screenshots used 440x840 pixels in browser dev tools
                    form_factor = "narrow"
                },
                new
                {
                    src = ResolveUrl("~/app/lib/assets/favorites_mobile_light.png"),
                    sizes = "880x1680",
                    type = "image/png",
                    // NOTE: Allow the wide screenshots used 440x840 pixels in browser dev tools
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

