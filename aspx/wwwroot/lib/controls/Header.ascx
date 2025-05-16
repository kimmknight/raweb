<%@ Control Language="C#" AutoEventWireup="true" %>

<script runat="server">
    public bool forceVisible { get; set; }
</script>


<header class="app-header">
    <div class="left">
        <img src="<%= ResolveUrl("~/lib/assets/icon.svg") %>" alt="" class="logo" />
        <span class="title">RemoteApps</span>
    </div>
</header>

<script type="text/javascript">
    const forceVisible = '<%= forceVisible.ToString().ToLower() %>' === 'true';

    // hide the header if the display mode is not window-controls-overlay
    const isWindowControlsOverlayMode = window.matchMedia('(display-mode: window-controls-overlay)').matches;
    const isStandaloneMode = window.matchMedia('(display-mode: standalone)').matches;
    if ((!isWindowControlsOverlayMode || isStandaloneMode) && !forceVisible) {
        document.querySelector('.app-header').style.display = 'none';
        document.body.style.setProperty('--header-height', '0px');
    }

    // watch for changes to the display mode and hide/show the header accordingly
    if (!forceVisible) {
        // only show the header when in window-controls-overlay mode
        const mediaQueryList = window.matchMedia('(display-mode: window-controls-overlay)');
        mediaQueryList.addEventListener('change', (event) => {
            if (event.matches) {
                document.querySelector('.app-header').style.display = 'flex';
                document.body.style.setProperty('--header-height', 'env(titlebar-area-height, 30px)');
            } else {
                document.querySelector('.app-header').style.display = 'none';
                document.body.style.setProperty('--header-height', '0px');
            }
        });
    } else {
        // only show the header when not in standalone mode or when window controls overlay mode is enabled
        const mediaQueryList = window.matchMedia('(display-mode: standalone)');
        mediaQueryList.addEventListener('change', (event) => {
            if (event.matches) {
                document.querySelector('.app-header').style.display = 'none';
                document.body.style.setProperty('--header-height', '0px');
            } else {
                document.querySelector('.app-header').style.display = 'flex';
                document.body.style.setProperty('--header-height', 'env(titlebar-area-height, 30px)');
            }
        });

    }

    // track whether the window/tab is in focus and adjust the header visibility accordingly
    window.addEventListener('focus', () => {
        document.body.setAttribute('data-window-focused', 'true');
    });
    window.addEventListener('blur', () => {
        document.body.setAttribute('data-window-focused', 'false');
    });
</script>

<style>
    body {
        --header-height: max(env(titlebar-area-height, 33px), 33px);
        --content-height: calc(
            100vh - var(--header-height) - env(safe-area-inset-top, 0px) - env(safe-area-inset-bottom, 0px)
        );
        margin: 0;
    }

    .app-header {
        --height: var(--header-height);
        background-color: #000;
        color: var(--wui-text-primary);
        height: var(--height);
        display: flex;
        flex-direction: row;
        align-items: center;
        justify-content: space-between;
        padding: 0 10px 0 0;
        font-size: 13px;
        flex-grow: 0;
        flex-shrink: 0;
        user-select: none;
        app-region: drag;
    }

    .app-header .left {
        display: flex;
        flex-direction: row;
        gap: 0;
        flex-wrap: nowrap;
        align-items: center;
        justify-content: flex-start;
        height: 100%;
    }

    .app-header img.logo {
        block-size: 16px;
        padding: 0 8px 0 14px;
        object-fit: cover;
        -webkit-user-drag: none;
    }

    .app-header .title {
        padding: 0 8px;
        font-family: var(--wui-font-family-text);
        font-size: var(--wui-font-size-caption);
    }

    body[data-window-focused="false"] .app-header .title {
        opacity: 0.5;
    }
</style>