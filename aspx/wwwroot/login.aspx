<%@ Page Language="C#" AutoEventWireup="true" CodeFile="login.aspx.cs" Inherits="Login" Debug="true" %>
<%@ Register Src="./lib/controls/Header.ascx" TagName="Header" TagPrefix="raweb" %>
<%@ Register Src="./lib/controls/head.ascx" TagName="head" TagPrefix="raweb" %>
<%@ Register Src="./lib/controls/InfoBarCritical.ascx" TagName="InfoBarCritical" TagPrefix="winui" %>

<!DOCTYPE html>
<html lang="en">

<raweb:head runat="server" title="RemoteApps - Sign in" additional='<meta cache-control="no-cache, no-store, must-revalidate" />'/>

<style>
    .input {
        display: flex;
        flex-direction: column;
        gap: 3px;
        margin-bottom: 10px;
    }

    strong {
        font-weight: 600;
    }

    .unindent {
        margin-left: -11px;
    }

    .button-row {
        display: flex;
        flex-direction: row-reverse;
    }

    .access {
        margin-top: 1.25rem;
        opacity: 0.9;
    }

    .access :global(.unindent) {
        margin-left: -11px;
    }


    @media (max-width: 600px) or (max-height: 600px) {
        .dialog {
            box-sizing: border-box !important;
            inline-size: 100% !important;
            max-inline-size: unset !important;
            border-radius: 0 !important;
            border: none !important;
            box-shadow: none !important;
            inset: calc(var(--header-height) + env(safe-area-inset-top, 0px)) env(safe-area-inset-right, 0px) env(safe-area-inset-bottom, 0px) env(safe-area-inset-left, 0px);
        }

        .dialog-body {
            height: 100%;
            box-sizing: border-box;
            display: flex;
            flex-direction: column;
            overflow: auto;
        }

        .dialog-body>* {
            max-inline-size: 600px;
            inline-size: 100%;
            margin: 0 auto;
        }

        .content-wrapper {
            flex-grow: 1;
            display: flex;
            flex-direction: column;
            justify-content: space-between;
        }
    }
</style>

<body>
    <raweb:Header runat="server" forceVisible="true"></raweb:Header>

    <div class="dialog-wrapper">
        <div class="dialog">
            <div class="dialog-body">
                <h1 class="dialog-title">Sign in</h1>
                <form action="login.aspx" runat="server"
                    class="content-wrapper">
                    <div class="content">
                        <p>
                            To continue to
                            <strong>
                                RemoteApp and Desktop Connections
                            </strong>
                            <span style="white-space: nowrap;">
                                on
                                <strong>
                                    <%= resolver.Resolve(Environment.MachineName) %>
                                </strong>
                            </span>
                        </p>

                        <winui:InfoBarCritical runat="server" id="InfoBarCritical1" Visible="false" style="margin-bottom: 16px;"
                             />

                        <div class="input" id="username-group">
                            <div class="textblock">Domain\Username</div>
                            <div class="text-box-container">
                                <input class="text-box" name="username" autocomplete="username" required id="Username" runat="server" autofocus />
                                <div class="text-box-underline"></div>
                            </div>
                        </div>

                        <div class="input">
                            <div class="textblock">Password</div>
                            <div class="text-box-container">
                                <input class="text-box" name="password" type="password"
                                    autocomplete="current-password" required id="password" runat="server" />
                                <div class="text-box-underline"></div>
                            </div>
                        </div>

                        <p class="access">
                            Powered by RAWeb
                            <br />
                            <a href="https://github.com/kimmknight/raweb" class="button style-hyperlink unindent">
                                Learn more
                            </a>
                        </p>
                    </div>

                    <div class="button-row">
                        <button class="button style-accent" type="submit">Continue</button>
                    </div>
                </form>
            </div>
        </div>
    </div>

<style>
    .dialog-wrapper {
        display: flex;
        flex-direction: column;
        inline-size: 100%;
        block-size: 100%;
        justify-content: center;
        position: fixed;
        align-items: center;
    }

    .dialog {
        inline-size: 448px;
    }

    .dialog {
        background-clip: padding-box;
        background-color: var(--wui-solid-background-base);
        border: 1px solid var(--wui-surface-stroke-default);
        border-radius: var(--wui-overlay-corner-radius);
        box-shadow: var(--wui-dialog-shadow);
        box-sizing: border-box;
        max-inline-size: calc(100% - 24px);
        overflow: hidden;
        position: fixed;
    }

    .dialog-title {
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
    }

    .dialog-body {
        background-color: var(--wui-layer-default);
        color: var(--wui-text-primary);
        font-family: var(--wui-font-family-text);
        font-size: var(--wui-font-size-body);
        font-weight: 400;
        line-height: 20px;
        -webkit-user-select: none;
        -moz-user-select: none;
        -ms-user-select: none;
        user-select: none;
        padding: 24px;
    }

    input.text-box {
        background: none;
        border: none;
        width: 100%;
        background-color: transparent;
        border: none;
        border-radius: var(--wui-control-corner-radius);
        box-sizing: border-box;
        color: var(--wui-text-primary);
        cursor: unset;
        flex: 1 1 auto;
        font-family: var(--wui-font-family-text);
        font-size: var(--wui-font-size-body);
        font-weight: 400;
        inline-size: 100%;
        margin: 0;
        min-block-size: 30px;
        outline: none;
        padding-inline: 10px;
        padding-block: 5px;
        -webkit-user-select: none;
        -moz-user-select: none;
        -ms-user-select: none;
        user-select: none;
        resize: none;
        overflow-y: hidden;
    }

    input.text-box:focus {
        outline: none;
    }

    .text-box-container {
        align-items: center;
        background-clip: padding-box;
        background-color: var(--wui-control-fill-default);
        border: 1px solid var(--wui-control-stroke-default);
        border-radius: var(--wui-control-corner-radius);
        cursor: text;
        display: flex;
        inline-size: 100%;
        position: relative;
    }

    .text-box-container:hover {
        background-color: var(--wui-control-fill-secondary);
    }

    .text-box-container.disabled {
        background-color: var(--wui-control-fill-disabled);
        cursor: default;
    }

    .text-box-container.disabled .text-box-underline {
        display: none;
    }

    .text-box-container:focus-within {
        background-color: var(--wui-control-fill-input-active);
    }

    .text-box-container:focus-within div :global(.ProseMirror)::-moz-placeholder {
        color: var(--wui-text-tertiary);
    }

    .text-box-container:focus-within div :global(.ProseMirror):-ms-input-placeholder {
        color: var(--wui-text-tertiary);
    }

    .text-box-container:focus-within div :global(.ProseMirror)::placeholder {
        color: var(--wui-text-tertiary);
    }

    .text-box-container:focus-within .text-box-underline:after {
        border-bottom: 2px solid var(--wui-accent-default);
    }

    .text-box-container:focus-within :global(.text-box-clear-button) {
        display: flex;
    }

    .text-box-underline {
        block-size: calc(100% + 2px);
        border-radius: var(--wui-control-corner-radius);
        inline-size: calc(100% + 2px);
        inset-block-start: -1px;
        inset-inline-start: -1px;
        overflow: hidden;
        pointer-events: none;
        position: absolute;
    }

    .text-box-underline::after {
        block-size: 100%;
        border-bottom: 1px solid var(--wui-control-strong-stroke-default);
        box-sizing: border-box;
        content: '';
        inline-size: 100%;
        inset-block-end: 0;
        inset-inline-start: 0;
        position: absolute;
    }

    .button {
        align-items: center;
        border: none;
        border-radius: var(--wui-control-corner-radius);
        box-sizing: border-box;
        cursor: default;
        display: inline-flex;
        font-family: var(--wui-font-family-text);
        font-size: var(--wui-body-font-size);
        font-weight: 400;
        justify-content: center;
        line-height: 20px;
        padding-block: 4px 6px;
        padding-inline: 11px;
        position: relative;
        text-decoration: none;
        transition: var(--wui-control-faster-duration) ease background;
        -webkit-user-select: none;
        -moz-user-select: none;
        -ms-user-select: none;
        user-select: none;
    }

    .button.style-hyperlink {
        background-color: var(--wui-subtle-transparent);
        color: var(--wui-accent-text-primary);
        cursor: pointer;
    }

    .button.style-hyperlink:hover {
        background-color: var(--wui-subtle-secondary);
    }

    .button.style-hyperlink:active {
        background-color: var(--wui-subtle-tertiary);
        color: var(--wui-accent-text-tertiary);
    }

    .button.style-accent {
        background-color: var(--wui-accent-default);
        border: 1px solid var(--wui-control-stroke-on-accent-default);
        border-bottom-color: var(--wui-control-stroke-on-accent-secondary);
        color: var(--wui-text-on-accent-primary);
    }

    .button.style-accent:hover {
        background-color: var(--wui-accent-secondary);
    }

    .button.style-accent:active {
        background-color: var(--wui-accent-tertiary);
        border-color: transparent;
        color: var(--wui-text-on-accent-secondary);
    }
</style>

</body>

<script lang="javascript">
    async function authenticateUser(username, password) {
        // remove machine name from username if present
        const machineName = '<%= Environment.MachineName %>';
        if (username.startsWith(machineName)) {
            username = username.substring(machineName.length);
        }

        // Base64 encode the credentials
        const credentials = btoa(username + ":" + password); 

        // clear the HTML form values
        document.querySelector('form').reset();

        // authenticate
        return fetch('<%= ResolveUrl("~/auth/login.aspx") %>', {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded",
                "Authorization": "Basic " + credentials,
                "x-requested-with": "XMLHttpRequest",
            },
            credentials: 'include',
        })
            .then(response => {
                if (response.ok) {
                    // Redirect to the main application page on successful login
                    const returnUrl = new URLSearchParams(window.location.search).get('ReturnUrl');
                    const redirectUrl = returnUrl ? decodeURIComponent(returnUrl) : '<%= ResolveUrl("~/") %>';
                    window.location.href = redirectUrl;
                    return true
                } else {
                    // Handle login failure (e.g., show an error message)
                    alert("Login failed. Please check your credentials.");
                    return false;
                }
            })
            .catch(error => {
                console.error("Error during login:", error);
                alert("An error occurred. Please try again.");
                return false
            });
    }
</script>

</html>