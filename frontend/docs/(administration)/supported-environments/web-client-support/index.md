---
title: Supported web clients
nav_title: Web clients
---

RAWeb's web interface utilizes modern web technologies to provide a responsive and user-friendly experience. To ensure optimal performance and compatibility, it is important to use a supported web browser. RAWeb hides or disables certain features when accessed from unsupported or partially-supported browsers.

| Required web technology | Description | Supported browsers |
| ---------------------- | ----------- | ------------------ |
| [`prefers-color-scheme`](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/At-rules/@media/prefers-color-scheme#browser_compatibility)  media query | Used to detect dark mode preference and adjust the interface accordingly. |  Edge 79+, Chrome 76+, Firefox 67+, Safari 12.1+ |
| [Subtle crypto](https://developer.mozilla.org/en-US/docs/Web/API/Crypto/subtle#browser_compatibility) | Used for generating hashes for the resource manager. | Edge 12+, Chrome 37+, Firefox 34+, Safari 11+ |
| [CSS anchor positioning](https://caniuse.com/css-anchor-positioning) | Used for all dropdown and context menus in the web interface. | Edge 125+, Chrome 125+, Safari 26+ |
| [`dialog.requestClose()`](https://developer.mozilla.org/en-US/docs/Web/API/HTMLDialogElement/requestClose#browser_compatibility) | Required for dialogs in the web interface. | Edge 134+, Chrome 134+, Firefox 139+, Safari 18.4+ |
| [Service Workers](https://developer.mozilla.org/en-US/docs/Web/API/Service_Worker_API) | Used for stale-while-revalidate caching logic, which allows RAWeb to load quickly after the first page load. The cache is discarded whenever a user signs out. | Edge 17+, Chrome 45+, Firefox 138+, Safari 11.1+ |
| [Window Controls Overlay](https://caniuse.com/?search=window+controls+overlay) | Allows RAWeb's web interface to combine its titlebar with the OS titlebar when in PWA mode | Edge 105+, Chrome 105+ |

## CSS anchor positioning

Firefox does not currently support CSS anchor positioning.

The version of Safari released Fall 2025 is the first version to support CSS anchor positioning. As a result, users accessing RAWeb with Firefox or older versions of Safari will experience limited functionality in dropdown and context menus. Specifically, these menus will not be displayed.

- Menus for apps and devices will not appear.
- It will not be possible to add or remove favorites.
- The connection method dialog will not show an option to remember the selected method.
- The menus in the resource manager will appear at the top-left corner of the screen instead of next to the menu source.

## dialog.requestClose()

Support for `<dialog>` and `dialog.requestClose()` are new features that were added to web browsers in 2025. As a result, users accessing RAWeb with older browsers will be unable to use dialogs in the web interface.

- The option to view properties for apps and devices will not appear.
- The connection method dialog will not appear. Users will be connected using the default method without being prompted (usually RDP file download).
- Dialogs on the policy page may not open correctly.
- The resource manager functionality may be broken.

## Window Controls Overlay

On chromium-based browsers (e.g., Edge, Chrome), RAWeb can utilize the Window Controls Overlay feature when installed as a Progressive Web App (PWA). This allows RAWeb to integrate its titlebar with the operating system's titlebar, providing a more seamless user experience.

To take advantage of this feature:
1. Install RAWeb as a PWA by clicking the install icon in the address bar of your browser.
2. Launch RAWeb from your desktop or start menu.
3. Click the up chevron icon in the titlebar to toggle between integrated and standard titlebar modes.

_Note the RAWeb-provided buttons and browser-provided buttons in the shared titlebar:_

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="./window-controls.dark.webp">
  <source media="(prefers-color-scheme: light)" srcset="./window-controls.webp">
  <img width="900" src="./window-controls.webp" alt="A screenshot of the RAWeb sign in page when anonymous authentication is allowed" border="1">
</picture>
