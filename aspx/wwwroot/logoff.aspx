<%-- This page should only be used in the version of the app built with vite. --%>
<%@ Page Language="C#" AutoEventWireup="true"%>
<%@ Register Src="~/lib/controls/AppRoot.ascx" TagName="AppRoot" TagPrefix="raweb" %>

<script runat="server">
    protected void Page_Load(object sender, EventArgs e)
    {
        // clear server-side session
        Session.Clear();
        Session.Abandon();

        // expire session cookie on client
        if (Request.Cookies["ASP.NET_SessionId"] != null)
        {
            Response.Cookies["ASP.NET_SessionId"].Expires = DateTime.Now.AddDays(-1);
        }

        // expire auth cookie too
        if (Request.Cookies[".ASPXAUTH"] != null)
        {
            Response.Cookies[".ASPXAUTH"].Expires = DateTime.Now.AddDays(-1);
        }
    }
</script>

<raweb:AppRoot runat="server" />

<script type="module">
    const mainScript = document.createElement('script');
    mainScript.type = 'module';
    mainScript.src = '<%= ResolveUrl("~/lib/assets/logoff.js") %>';
    mainScript.crossOrigin = 'use-credentials';
    document.body.appendChild(mainScript);

    const mainStylesheet = document.createElement('link');
    mainStylesheet.rel = 'stylesheet';
    mainStylesheet.href = '<%= ResolveUrl("~/lib/assets/logoff.css") %>';
    mainStylesheet.crossOrigin = 'use-credentials';
    document.head.appendChild(mainStylesheet);

    const sharedScript = document.createElement('script');
    sharedScript.type = 'module';
    sharedScript.src = '<%= ResolveUrl("~/lib/assets/shared.js") %>';
    sharedScript.crossOrigin = 'use-credentials';
    document.body.appendChild(sharedScript);

    const sharedStylesheet = document.createElement('link');
    sharedStylesheet.rel = 'stylesheet';
    sharedStylesheet.href = '<%= ResolveUrl("~/lib/assets/shared.css") %>';
    sharedStylesheet.crossOrigin = 'use-credentials';
    document.head.appendChild(sharedStylesheet);
</script>