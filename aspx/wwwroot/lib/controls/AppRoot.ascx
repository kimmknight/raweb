<%@ Import Namespace="AliasUtilities" %>
<%@ Import Namespace="System" %>
<%@ Import Namespace="System.Web" %>
<%@ Import Namespace="System.Web.UI" %>
<%@ Import Namespace="System.Web.Security" %>
<%@ Import Namespace="System.Xml" %>

<script runat="server">
    protected void Page_Load(object sender, EventArgs e)
    {
        // get the authenticated user from the cookie
        // and check if the user is authenticated
        string authUser = getAuthenticatedUser();
        if (string.IsNullOrEmpty(authUser))
        {
            // redirect to login page if not authenticated
            Response.Redirect("~/login.aspx" + "?ReturnUrl=" + Uri.EscapeUriString(HttpContext.Current.Request.Url.AbsolutePath));
        }

        if (!IsPostBack)
        {
            // Code to execute on initial page load
        }

    }

    // make the alias resolver available
    public AliasUtilities.AliasResolver resolver = new AliasUtilities.AliasResolver();

    // get the current authenticated user from the cookie
    public string getAuthenticatedUser()
    {
        HttpCookie authCookie = HttpContext.Current.Request.Cookies[".ASPXAUTH"];
        if (authCookie == null || authCookie.Value == "") return "";
        try
        {
            // decrypt may throw an exception if authCookie.Value is total garbage
            FormsAuthenticationTicket authTicket = FormsAuthentication.Decrypt(authCookie.Value);
            if (authTicket == null)
            {
                return "";
            }
            return authTicket.Name;
        }
        catch
        {
            return "";
        }
    }
</script>

<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, viewport-fit=cover">
    <title><%= resolver.Resolve(System.Net.Dns.GetHostName()) %> - RemoteApps</title>
    <link rel="stylesheet" href='<%= ResolveUrl("~/lib/winui.css") %>'>
    <meta name="theme-color" content="hsl(0, 0%, 16%)" media="(prefers-color-scheme: dark)">
    <meta name="theme-color" content="hsl(122, 39%, 40%)">
    <link rel="icon" href="<%= ResolveUrl("~/icon.svg") %>" type="image/svg+xml">
    <link rel="manifest" href="<%= ResolveUrl("~/manifest.aspx") %>">
</head>

<body>
    <div id="app">
    </div>

    <div class="root-splash-wrapper">
        <div class="root-titlebar">
            <div class="left"></div>
            <div class="right"></div>
        </div>
        <style>
            :root {
                --titlebar-bg: #f3f3f3;
            }

            @media (prefers-color-scheme: dark) {
                :root {
                    --titlebar-bg: #202020;
                }
            }

            .root-splash-wrapper {
                top: 0;
                right: 0;
                bottom: 0;
                left: 0;
                position: fixed;
                z-index: 9998;
                display: flex;
                flex-direction: column;
                gap: 20px;
                align-items: center;
                justify-content: center;
                animation: none;
                animation-fill-mode: forwards;
                -webkit-app-region: drag;
            }

            .root-splash-wrapper {
                background: var(--wui-accent-text-secondary, #ffffff);
            }

            @media (prefers-color-scheme: dark) {
                .root-splash-wrapper {
                    background: #242424;
                }
            }

            .root-titlebar {
                display: none;
                flex-direction: row;
                align-items: center;
                position: fixed;
                left: env(titlebar-area-x, 0);
                top: env(titlebar-area-y, 0);
                width: env(titlebar-area-width, 100%);
                height: env(titlebar-area-height, 33px);
                -webkit-app-region: drag;
                app-region: drag;
                user-select: none;
                justify-content: space-between;
                background-color: var(--titlebar-bg);
                background-color: transparent;
                padding: 0 0 0 16px;
                box-sizing: border-box;
                color: #ffffff;
                font-family: var(--wui-font-family-small);
                font-size: var(--wui-caption-font-size);
                font-weight: 400;
                line-height: 16px;
            }

            .root-titlebar svg {
                width: 16px;
                height: 16px;
                margin: 0 16px 0 0;
                fill: var(--wui-accent-default);
            }

            .root-titlebar .left {
                display: flex;
                flex-direction: row;
                align-items: center;
                justify-content: flex-start;
            }

            .root-titlebar .right {
                display: flex;
                flex-direction: row;
                align-items: center;
                justify-content: flex-end;
            }

            .root-splash-app-name {
                height: 40px;
                margin: 8px;
                color: rgb(250, 249, 248);
                filter: drop-shadow(rgb(210 208 206 / 60%) 0px 0px 8px) drop-shadow(rgb(11 4 36 / 60%) 0px 0px 4px);
            }

            .root-splash-app-logo {
                fill: rgb(250, 249, 248);
                height: 100px;
                width: auto;
                filter: drop-shadow(rgb(11 4 36 / 60%) 0px 0px 4px);
            }

            .root-splash-note {
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell,
                    'Open Sans', 'Helvetica Neue', sans-serif;
                color: rgba(250, 249, 248, 0.3);
                display: flex;
                flex-direction: column;
                gap: 10px;
                position: absolute;
                bottom: 100px;
            }

            .root-splash-note circle {
                fill: none;
                stroke: var(--wui-text-on-accent-primary);
                stroke-width: 1.5;
                stroke-linecap: round;
                stroke-dasharray: 43.97;
                transform: rotate(-90deg);
                transform-origin: 50% 50%;
                transition: all var(--wui-control-normal-duration) linear;
                animation: root-splash-progress-ring-indeterminate 2s linear infinite;
            }

            @media (prefers-color-scheme: dark) {
                .root-splash-note circle {
                    stroke: var(--wui-accent-default);
                }
            }

            @keyframes root-splash-progress-ring-indeterminate {
                0% {
                    stroke-dasharray: 0.01px 43.97px;
                    transform: rotate(0);
                }

                50% {
                    stroke-dasharray: 21.99px 21.99px;
                    transform: rotate(450deg);
                }

                100% {
                    stroke-dasharray: 0.01px 43.97px;
                    transform: rotate(3turn);
                }
            }

            /* progress bar styles */
            #nprogress {
                position: absolute;
                top: 0;
                height: 2px;
                left: 0;
                width: 100%;
                z-index: 998;
                background-color: rgba(84, 56, 185, 0.1);
            }

            #nprogress .peg {
                height: 2px;
                background-color: hsl(var(--wui-accent-dark-1));
            }

            @media (prefers-color-scheme: dark) {
                #nprogress {
                    background-color: rgba(191, 180, 229, 0.1);
                }

                #nprogress .peg {
                    background-color: hsl(var(--wui-accent-light-2));
                }
            }
        </style>
        <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 64 64" class="root-splash-app-logo">
            <!-- Transparent background -->
            <rect width="64" height="64" fill="none" />

            <!-- Grid of apps -->
            <rect x="8" y="8" width="20" height="20" rx="4" fill="#42A5F5" />
            <rect x="36" y="8" width="20" height="20" rx="4" fill="#FFCA28" />
            <rect x="8" y="36" width="20" height="20" rx="4" fill="#EF5350" />
            <circle cx="46" cy="46" r="10" fill="#66BB6A" />
        </svg>


        <span class="root-splash-note">Powered by RAWeb</span>
        <span class="root-splash-note" style="bottom: 150px">
            <svg tabindex="-1" class="progress-ring indeterminate" width="32" height="32" viewBox="0 0 16 16"
                role="status">
                <circle cx="50%" cy="50%" r="7" stroke-dasharray="3" stroke-dashoffset="NaN" class="svelte-32f9k0">
                </circle>
            </svg>
        </span>
    </div>
</body>

<script type="module">
    window.__iisBase = '<%= ResolveUrl("~/") %>'
    window.__base = window.__iisBase + '';
    window.__authUser = {
        username: '<%= getAuthenticatedUser().Split('\\')[1] %>',
        domain: '<%= getAuthenticatedUser().Split('\\')[0] %>',
    }
    window.__terminalServerAliases = '<%= System.Configuration.ConfigurationManager.AppSettings["TerminalServerAliases"] ?? "" %>'.split(';')
        .map(pair => pair.split('=').map(part => part.trim()))
        .reduce((acc, [key, value]) => {
            acc[key] = value;
            return acc;
        }, {});
    window.__policies = {
        combineTerminalServersModeEnabled: '<%= System.Configuration.ConfigurationManager.AppSettings["App.CombineTerminalServersModeEnabled"] %>',
        favoritesEnabled: '<%= System.Configuration.ConfigurationManager.AppSettings["App.FavoritesEnabled"] %>',
        flatModeEnabled: '<%= System.Configuration.ConfigurationManager.AppSettings["App.FlatModeEnabled"] %>',
        iconBackgroundsEnabled: '<%= System.Configuration.ConfigurationManager.AppSettings["App.IconBackgroundsEnabled"] %>',
        simpleModeEnabled: '<%= System.Configuration.ConfigurationManager.AppSettings["App.SimpleModeEnabled"] %>',
    }
    window.__namespace = '<%= getAuthenticatedUser().Split('\\')[0] %>:<%= getAuthenticatedUser().Split('\\')[1] %>';
</script>