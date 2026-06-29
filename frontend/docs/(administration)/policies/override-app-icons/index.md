---
title: $t{{ policies.App.Icon.title }}
nav_title: Override app icons
redirects:
  - policies/App.Icon
---

By default, RAWeb uses its own built-in icons for the web app. Icons include the titlebar icon, the splash screen icon, the default resource icon, the default desktop wallpapers, and the PWA icons that are used by browsers and operating systems when the web app is installed on a device.

This policy lets you replace the icons with custom images. You can override any combination of the available icon slots; slots that are not overridden continue to use the built-in icons.

<PolicyDetails translationKeyPrefix="policies.App.Icon" />

To change the titlebar icon, override the 72x72 icon.

To change the splash screen icon, override the 192x192 icon.

## Supported icon formats

Any image format that browsers can render is accepted (PNG, JPEG, WebP, SVG, ICO). The uploaded image will be resized to fit the target dimensions using contain scaling, centered on a transparent background, and re-encoded as PNG.
