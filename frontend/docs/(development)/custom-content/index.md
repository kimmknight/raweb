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

### Jump to an example

- [Hide the wiki button from the navigation bar](#ex-hide-wiki-button)
- [Use iris spring accent color](#ex-accent-color)
- [Add Google Analytics tracking](#ex-analytics)
- [Replace the titlebar logo](#ex-titlebar-logo)
- [Add a list of files and links that change based on user permissions](#ex-filestore)

## Example: Hide the wiki button from the navigation bar {#ex-hide-wiki-button}

To hide the "Docs" button from the navigation bar, you can create an `index.css` file in the `App_Data/inject` folder with the following content:

```css
.nav-rail a[data-id='wiki'] {
  display: none;
}
```

This CSS rule targets the anchor element that links to the documentation page and sets its display property to `none`, effectively hiding it from view.

## Example: Use iris spring accent color {#ex-accent-color}

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

## Example: Add Google Analytics tracking {#ex-analytics}

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

## Example: Replace the titlebar logo {#ex-titlebar-logo}

To replace the RAWeb logo in the titlebar with a custom logo, create an `index.css` file in the `App_Data/inject` folder with the following content, replacing the URL with the path to your custom logo image. If you need to store
the logo image on the server, you can place it in the `App_Data/inject/public` folder and reference it as `url("public/your-logo-file-name.extension")`.

```css
:root {
  --logo-url: url('public/custom-icon.svg');
}

/* replace header logo */
.app-header .left img.logo {
  content: var(--logo-url);
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

## Example: Add a list of files and links that change based on user permissions {#ex-filestore}

To add a page that shows a list of files and links that change based on user permissions, you can create an `index.js` file in the `App_Data/inject` folder with the following code snippet.

This example uses RAWeb's component library and router to create a new page at the `/filestore` route and adds a link to that page in the navigation rail. It uses the same header, filter & search UI, and grid item components that are used in the built-in **Apps** and **Devices** pages to ensure a consistent user experience.

The files and links shown on the page are fetched from the filestore API endpoint (`/api/inject/list-files`) that lists files from the **App_Data/inject/filestore** folder on the server. You can customize the files shown to different users by controlling Windows file system permissions on the **filestore** folder and its contents.

```javascript
window.addEventListener('RAWebReady', async ({ detail: raweb }) => {
  // Add a new route for out custom page.
  const FilesAndShortcuts = createFilesAndShortcutsPageComponent(raweb);
  raweb.router.addRoute('/', {
    path: '/filestore',
    component: FilesAndShortcuts,
  });

  // Re-evaluate the current route to ensure the new route is recognized if we're already on the page
  raweb.router.replace(raweb.router.currentRoute.value.fullPath);
});

window.addEventListener('RAWebAppMounted', async ({ detail: raweb }) => {
  // Inject a link in the navigation rail
  const navRailAppsElement = document.querySelector(".nav-rail > nav > ul > li[data-id='apps']");
  if (navRailAppsElement) {
    const container = document.createElement('li');
    const Component = createFilesAndShortcutsNavRailLinkComponent(raweb);
    const subApp = raweb.vue.createApp(Component);
    subApp.use(raweb.router);
    subApp.mount(container);
    navRailAppsElement.after(container);
  }
});

function createFilesAndShortcutsPageComponent(raweb) {
  const { createHeaderActionModelRefs, GenericCard, HeaderActions, ResourceGrid, TextBlock } = raweb.components;
  const { h, ref, computed } = raweb.vue;
  const { useCoreDataStore, usePopupWindow } = raweb.stores;

  return {
    setup() {
      const {
        mode, // the view mode (list, card, etc)
        sortName, // Name or Date modified
        sortOrder, // asc or desc
        query, // the search query
      } = createHeaderActionModelRefs({
        defaults: { mode: 'tile' },
        persist: 'files', // preferences for this page will be stored under "files" key in localStorage
      });

      const coreAppData = useCoreDataStore();
      const { openWindow } = usePopupWindow();

      // fetch a list of files from the server and store in state
      const files = ref([]);
      const fetchError = ref(null);
      const fetching = ref(true);
      fetch(coreAppData.iisBase + 'api/inject/list-files')
        .then((res) => res.json())
        .then((data) => {
          files.value = data;
        })
        .catch((error) => {
          fetchError.value = 'Failed to fetch files';
          console.error('Error fetching files:', error);
        })
        .finally(() => {
          fetching.value = false;
        });

      // adjusted list of files based on the current sort and search query
      const sortedAndFilteredFiles = computed(() => {
        let result = [...files.value];
        if (query.value) {
          const lowerQuery = query.value.toLowerCase();
          result = result.filter(
            (file) =>
              file.name.toLowerCase().includes(lowerQuery) || file.fileType.toLowerCase().includes(lowerQuery)
          );
        }
        if (sortName.value === 'Name' || sortName.value === '') {
          result.sort((a, b) => {
            const aValue = a.name || '';
            const bValue = b.name || '';
            if (aValue < bValue) return sortOrder.value === 'asc' ? -1 : 1;
            if (aValue > bValue) return sortOrder.value === 'asc' ? 1 : -1;
            return 0;
          });
        }
        if (sortName.value === 'Date modified') {
          result.sort((a, b) => {
            const aValue = new Date(a.dateModified).getTime();
            const bValue = new Date(b.dateModified).getTime();
            if (aValue < bValue) return sortOrder.value === 'asc' ? -1 : 1;
            if (aValue > bValue) return sortOrder.value === 'asc' ? 1 : -1;
            return 0;
          });
        }
        return result;
      });

      return {
        mode,
        sortName,
        sortOrder,
        query,
        openWindow,
        files: {
          data: sortedAndFilteredFiles,
          loading: fetching,
          error: fetchError,
        },
      };
    },
    render() {
      return h('div', { style: { userSelect: 'none' } }, [
        h('div', [
          h(TextBlock, { variant: 'title', tag: 'h1' }, 'Files & shortcuts'),
          h(HeaderActions, {
            mode: this.mode,
            'onUpdate:mode': (val) => (this.mode = val),
            sortName: this.sortName,
            'onUpdate:sortName': (val) => (this.sortName = val),
            sortOrder: this.sortOrder,
            'onUpdate:sortOrder': (val) => (this.sortOrder = val),
            query: this.query,
            'onUpdate:query': (val) => (this.query = val),
            searchPlaceholder: 'Search files and shortcuts',
          }),
        ]),
        h('section', { style: { margin: '24px 0 8px 0', paddingBottom: '36px' } }, [
          this.files.loading.value
            ? h(TextBlock, 'Loading files...')
            : this.files.error.value
              ? h(TextBlock, { style: { color: 'var(--color-error)' } }, this.files.error)
              : h(ResourceGrid, { mode: this.mode, style: { gap: '16px' } }, [
                  this.files.data.value?.map((file) => {
                    const isExternal = file.url.startsWith('http://') || file.url.startsWith('https://');

                    return h(
                      'a',
                      {
                        href: file.url,
                        target: isExternal ? '_blank' : '_self',
                        style: {
                          textDecoration: 'none',
                          borderRadius: 'var(--wui-control-corner-radius)',
                        },
                        onclick: (evt) => {
                          if (!isExternal) {
                            evt.preventDefault();
                            this.openWindow(file.url, file.name, 'width=1200,height=1000,menubar=0,status=0');
                          }
                        },
                      },
                      h(GenericCard, {
                        mode: this.mode,
                        title: file.name,
                        caption: file.fileType,
                        icon: file.icon,
                        style: { height: '100%' },
                      })
                    );
                  }),
                ]),
        ]),
      ]);
    },
  };
}

function createFilesAndShortcutsNavRailLinkComponent(raweb) {
  const { RailButton, RouterLink, AnimatedNavigationItemIndicator } = raweb.components;
  const { h } = raweb.vue;
  const { useNavigationRailStore } = raweb.stores;

  return {
    render() {
      return h(
        RouterLink,
        {
          to: '/filestore',
          custom: true,
        },

        ({ href, isActive, navigate }) =>
          h(
            AnimatedNavigationItemIndicator.Selectable,
            {
              selected: isActive,
              indicatorSize: 24,
              trackHandle: useNavigationRailStore().trackHandle,
            },
            h(
              RailButton,
              {
                href,
                active: isActive,
                onClick: navigate,
              },
              {
                icon: () =>
                  h(
                    'svg',
                    {
                      width: '24',
                      height: '24',
                      fill: 'none',
                      viewBox: '0 0 24 24',
                      xmlns: 'http://www.w3.org/2000/svg',
                    },
                    h('path', {
                      d: 'M12.04 6.017a4.75 4.75 0 1 0 .335-.012h-.01a1.35 1.35 0 0 0-.326.012Zm-1.622 1.835c-.226.677-.368 1.506-.407 2.398h-1.1a3.5 3.5 0 0 1 1.507-2.398Zm-.374 3.898a8.43 8.43 0 0 0 .379 1.91 3.507 3.507 0 0 1-1.405-1.91h1.026Zm3.966 2.1.003-.008c.22-.587.373-1.306.443-2.092h1.276a3.51 3.51 0 0 1-1.722 2.1Zm-1.061-2.1c-.065.618-.187 1.154-.34 1.565-.118.313-.24.514-.336.623a.914.914 0 0 1-.023.025.914.914 0 0 1-.023-.025c-.097-.11-.218-.31-.335-.623-.154-.41-.276-.947-.341-1.565h1.398Zm.039-1.5h-1.476c.042-.828.185-1.547.38-2.065.117-.313.238-.514.335-.623a.79.79 0 0 1 .023-.025.79.79 0 0 1 .023.025c.097.11.218.31.335.623.195.518.338 1.237.38 2.065Zm1.501 0c-.043-.978-.21-1.88-.475-2.588a3.503 3.503 0 0 1 1.825 2.588h-1.35Zm-2.182-2.76-.004.002.004-.003Zm-.113 0 .003.002a.014.014 0 0 0-.004-.003l.001.001Z',
                      fill: 'currentColor',
                    }),
                    h('path', {
                      d: 'M6.5 2A2.5 2.5 0 0 0 4 4.5v15A2.5 2.5 0 0 0 6.5 22h13.25a.75.75 0 0 0 0-1.5H6.5a1 1 0 0 1-1-1h14.25a.75.75 0 0 0 .75-.75V4.5A2.5 2.5 0 0 0 18 2H6.5ZM19 4.5V18H5.5V4.5a1 1 0 0 1 1-1H18a1 1 0 0 1 1 1Z',
                      fill: 'currentColor',
                    })
                  ),
                'icon-active': () =>
                  h(
                    'svg',
                    {
                      width: '24',
                      height: '24',
                      fill: 'none',
                      viewBox: '0 0 24 24',
                      xmlns: 'http://www.w3.org/2000/svg',
                    },
                    h('path', {
                      d: 'M12.04 6.017a4.75 4.75 0 1 0 .335-.012h-.01a1.35 1.35 0 0 0-.326.012Zm-1.622 1.835c-.226.677-.368 1.506-.407 2.398h-1.1a3.5 3.5 0 0 1 1.507-2.398Zm-.374 3.898a8.43 8.43 0 0 0 .379 1.91 3.507 3.507 0 0 1-1.405-1.91h1.026Zm3.966 2.1.003-.008c.22-.587.373-1.306.443-2.092h1.276a3.51 3.51 0 0 1-1.722 2.1Zm-1.061-2.1c-.065.618-.187 1.154-.34 1.565-.118.313-.24.514-.336.623a.914.914 0 0 1-.023.025.914.914 0 0 1-.023-.025c-.097-.11-.218-.31-.335-.623-.154-.41-.276-.947-.341-1.565h1.398Zm.039-1.5h-1.476c.042-.828.185-1.547.38-2.065.117-.313.238-.514.335-.623a.79.79 0 0 1 .023-.025.79.79 0 0 1 .023.025c.097.11.218.31.335.623.195.518.338 1.237.38 2.065Zm1.501 0c-.043-.978-.21-1.88-.475-2.588a3.503 3.503 0 0 1 1.825 2.588h-1.35Zm-2.182-2.76-.004.002.004-.003Zm-.113 0 .003.002a.014.014 0 0 0-.004-.003l.001.001Z',
                      fill: 'currentColor',
                    }),
                    h('path', {
                      d: 'M6.5 2A2.5 2.5 0 0 0 4 4.5v15A2.5 2.5 0 0 0 6.5 22h13.25a.75.75 0 0 0 0-1.5H6.5a1 1 0 0 1-1-1h14.25a.75.75 0 0 0 .75-.75V4.5A2.5 2.5 0 0 0 18 2H6.5ZM19 4.5V18H5.5V4.5a1 1 0 0 1 1-1H18a1 1 0 0 1 1 1Z',
                      fill: 'currentColor',
                    })
                  ),
                default: () => 'Files & shortcuts',
              }
            )
          )
      );
    },
  };
}
```
