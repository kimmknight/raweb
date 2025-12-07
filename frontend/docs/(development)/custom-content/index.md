---
title: Inject custom content into RAWeb
nav_title: Content injection
---

RAWeb allows administrators to inject custom CSS and JavaScript into the web application by placing files in the `App_Data/inject` folder of the RAWeb server installation. Any static file placed in this folder will be served by RAWeb, and specific files can be used to customize the appearance and behavior of the web application.

<InfoBar title="Unsupported feature" severity="caution">
  Custom content injection is made available for advanced users and administrators as way to test potential new features for RAWeb or implement customizatations that are not otherwise provided by RAWeb. It is not officially supported and may break in future releases. Use at your own risk.
</InfoBar>

To inject custom content, create the following files in the `App_Data/inject` folder:
- `index.css`: This file can contain custom CSS styles that will be applied to the RAWeb web application.
- `index.js`: This file can contain custom JavaScript code that will be executed in the context of the RAWeb web application.

Once these files are placed in the `App_Data/inject` folder, RAWeb will automatically include them in the web application. This allows administrators to customize the appearance and behavior of RAWeb to better fit their organization's needs.

For example, you could use `index.css` to change the color scheme of the RAWeb interface, or use `index.js` to add custom functionality such as additional logging or user interface enhancements.

## Example: Hide the wiki button from the navigation bar

To hide the "Docs" button from the navigation bar, you can create an `index.css` file in the `App_Data/inject` folder with the following content:

```css
.nav-rail a[href="/docs"] {
  display: none;
}
```

This CSS rule targets the anchor element that links to the documentation page and sets its display property to `none`, effectively hiding it from view.


## Example: Use iris spring accent color

To change the accent color of RAWeb to match the iris spring theme, create an `index.css` file in the `App_Data/inject` folder with the following content:

```css
@media (prefers-color-scheme: light) {
  :root {
    --wui-accent-default: hsl(265, 40%, 43%);
    --wui-accent-secondary: hsl(265, 33%, 48%);
    --wui-accent-tertiary: hsl(266, 29%, 53%);
    --wui-accent-selected-text-background: hsl(265, 37%, 48%);
    --wui-accent-text-primary: hsl(259, 50%, 31%);
    --wui-accent-text-secondary: hsl(255, 71%, 19%);
    --wui-accent-text-tertiary: hsl(265, 40%, 43%);
  }
}

@media (prefers-color-scheme: dark) {
  :root {
    --wui-accent-default: hsl(272, 39%, 72%);
    --wui-accent-secondary: hsl(272, 29%, 66%);
    --wui-accent-tertiary: hsl(272, 22%, 60%);
    --wui-accent-selected-text-background: hsl(265, 37%, 48%);
    --wui-accent-text-primary: hsl(282, 53%, 87%);
    --wui-accent-text-secondary: hsl(282, 53%, 87%);
    --wui-accent-text-tertiary: hsl(272, 39%, 72%);
  }
}
```

## Example: Add Google Analytics tracking

To add Google Analytics tracking to RAWeb, create an `index.js` file in the `App_Data/inject` folder with the following content, replacing `G-XXXXXXXXXX` with your actual Google Analytics tag ID:

```javascript
/**
 * Inject Google Analytics tracking script
 * @param {string} tag - The Google Analytics tag ID
 */
function injectGoogleAnalytics(tag) {
  if (!tag) {
    console.error('Google Analytics tag ID is not provided.');
    alert('Google Analytics tag ID is not provided.');
    return;
  }

  // create and inject the gtag script
  const gtagScript = document.createElement('script');
  gtagScript.async = true;
  gtagScript.src = `https://www.googletagmanager.com/gtag/js?id=${tag}`;
  document.head.appendChild(gtagScript);

  // initialize dataLayer and gtag function
  window.dataLayer = window.dataLayer || [];
  function gtag() {
    dataLayer.push(arguments);
  }
  gtag('js', new Date());
  gtag('config', tag);
}

injectGoogleAnalytics('G-XXXXXXXXXX');
```

## Example: Replace the titlebar logo

To replace the RAWeb logo in the titlebar with a custom logo, create an `index.css` file in the `App_Data/inject` folder with the following content, replacing the URL with the path to your custom logo image:

```css
:root {
  --logo-url: url("/lib/assets/default.ico");
}

/* replace header logo */
.app-header img.logo {
  display: none;
}
.app-header .left::before {
  content: "";
  display: inline-block;
  width: 16px;
  height: 16px;
  padding: 0 8px;
  background-image: var(--logo-url);
  background-size: contain;
  background-repeat: no-repeat;
  background-position: center;
}

/* replace splash screen logo */
:where(svg.root-splash-app-logo, svg.splash-app-logo) > * {
  display: none;
}
:where(svg.root-splash-app-logo, svg.splash-app-logo) {
  background-image: var(--logo-url);
  background-size: contain;
  background-repeat: no-repeat;
  background-position: center;
}

```
