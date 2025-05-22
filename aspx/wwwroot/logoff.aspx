<%@ Page Language="C#" AutoEventWireup="true" %>
<%@ Register Src="./lib/controls/Header.ascx" TagName="Header" TagPrefix="raweb" %>
<%@ Register Src="./lib/controls/head.ascx" TagName="head" TagPrefix="raweb" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">

<raweb:head runat="server" title="RemoteApps - Signing out" additional='<meta cache-control="no-cache, no-store, must-revalidate" />'/>

<body>
    <raweb:Header runat="server" forceVisible="true"></raweb:Header>

    <form id="form1" runat="server">
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

                // optionally, expire auth cookie too (if using FormsAuthentication)
                if (Request.Cookies[".ASPXAUTH"] != null)
                {
                    Response.Cookies[".ASPXAUTH"].Expires = DateTime.Now.AddDays(-1);
                }
            }
        </script>
    </form>
    <script>
        (async () => {
            // clear localStorage data keys
            Object.keys(localStorage)
                .filter((key) => key.includes(':data'))
                .forEach((key) => {
                    localStorage.removeItem(key);
                });

            // clear service worker cache
            await caches.keys().then((cacheNames) => {
                cacheNames.forEach((cacheName) => {
                    caches.delete(cacheName);
                });
            });

            const redirectUrl = '<%= ResolveUrl("~/login.aspx") %>';
            window.location.href = redirectUrl;
        })()
    </script>

    <div>
        <h1>Signing out</h1>
        <p>Please wait...</p>
    </div>

    <style>
        div {
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            height: calc(100% - var(--header-height) / 2);
        }
        h1 {
            color: var(--wui-text-primary);
            display: block;
            margin-bottom: 12px;
            font-size: var(--wui-font-size-subtitle);
            line-height: 28px;
            font-family: var(--wui-font-family-display);
            font-weight: 600;
            white-space: pre-wrap;
            margin: 0;
            padding: 0;
            text-align: center;
        }
        p {
            color: var(--wui-text-primary);
            font-family: var(--wui-font-family-text);
            font-size: var(--wui-font-size-body);
            font-weight: 400;
            line-height: 20px;
            margin: 4px 0;
            text-align: center;
        }
    </style>

</body>
</html>