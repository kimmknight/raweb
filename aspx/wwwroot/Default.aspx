<%-- This page should only be used in the version of the app built with vite. --%>
<%@ Page Language="C#" AutoEventWireup="true" %>
<%@ Register Src="~/lib/controls/AppRoot.ascx" TagName="AppRoot" TagPrefix="raweb" %>

<raweb:AppRoot runat="server" />

<script type="module">
    const mainScript = document.createElement('script');
    mainScript.type = 'module';
    mainScript.src = '<%= ResolveUrl("~/lib/assets/main.js") %>';
    mainScript.crossOrigin = 'use-credentials';
    document.body.appendChild(mainScript);

    const mainStylesheet = document.createElement('link');
    mainStylesheet.rel = 'stylesheet';
    mainStylesheet.href = '<%= ResolveUrl("~/lib/assets/main.css") %>';
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