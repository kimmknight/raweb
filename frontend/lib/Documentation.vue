<script setup lang="ts">
  import {
    ListItem,
    MenuFlyoutItem,
    NavigationPane,
    ProgressBar,
    ProgressRing,
    TextBlock,
    TextBox,
    Titlebar,
  } from '$components';
  import { TreeItem } from '$components/NavigationView/NavigationTypes.ts';
  import {
    animalRabbit,
    arrowRouting,
    bug,
    building,
    globeShield,
    home,
    installApp,
    lightning,
    server,
    shield,
    tetrisApp,
    uninstallApp,
  } from '$icons';
  import { notEmpty, PreventableEvent, registerServiceWorker, removeSplashScreen } from '$utils';
  import { isBrowser } from '$utils/environment.ts';
  import { entranceIn, fadeOut } from '$utils/transitions';
  import { useTranslation } from 'i18next-vue';
  import { computed, onMounted, onUnmounted, ref, watch, watchEffect } from 'vue';
  import { RouteRecordNormalized, useRouter } from 'vue-router';
  import { i18nextPromise } from './i18n.ts';

  const titlebarLoading = ref(false);
  async function listenToServiceWorker(event: any) {
    if (event.data.type === 'fetch-queue') {
      const fetching = event.data.backgroundFetchQueueLength > 0;
      titlebarLoading.value = fetching;
    }
  }

  const sslError = ref(false);
  onMounted(() => {
    registerServiceWorker(listenToServiceWorker).then((response) => {
      if (response === 'SSL_ERROR') {
        sslError.value = true;
      }
    });
  });

  // track whether i18n is ready
  const i18nReady = ref(false);
  i18nextPromise.then(() => {
    i18nReady.value = true;
  });

  const { t } = useTranslation();

  // remove the splash screen once everything is ready
  const canRemoveSplashScreen = computed(() => {
    return i18nReady.value;
  });
  const splashScreenRemoved = ref(false);
  watchEffect(() => {
    if (canRemoveSplashScreen.value) {
      setTimeout(() => {
        removeSplashScreen().then(() => {
          splashScreenRemoved.value = true;
        });
      }, 300);
    }
  });

  type TreeNode = {
    label: string;
    children: TreeNode[];
  } & Partial<RouteRecordNormalized>;

  /**
   * Builds a tree structure from the given routes based on their names.
   *
   * Each segment of the route name (split by '/') becomes a node in the tree.
   */
  function buildRouteTree(routes: RouteRecordNormalized[]): TreeNode[] {
    const root: TreeNode[] = [];

    function insert(parts: string[], nodes: TreeNode[], route: RouteRecordNormalized) {
      const [head, ...tail] = parts;
      if (!head) return;

      let node = nodes.find((n) => n.label === head);

      if (!node) {
        node = { label: head, children: [] };
        nodes.push(node);
      }

      if (tail.length === 0) {
        // Merge route properties into the node
        Object.assign(node, route, { label: head, children: node.children });
      } else {
        insert(tail, node.children, route);
      }
    }

    for (const route of routes) {
      if (!route.name) continue;

      const parts = String(route.name)
        .replace(/\/+$/, '') // remove trailing slashes
        .split('/') // split into segments
        .filter(Boolean);

      insert(parts, root, route);
    }

    return root;
  }

  function navigate(evt: PreventableEvent<MouseEvent | KeyboardEvent>, href?: string) {
    if (!href) {
      return;
    }

    // prevent the default link behavior
    evt.preventDefault();

    // if the destination is the same as the current URL,
    // we need to manually scroll to the top and animate
    // because vue-router will not trigger a navigation
    // in response to only changing the hash or
    // navigating to the same URL
    const destinationUrl = new URL(href, window.location.origin);
    const currentUrl = new URL(window.location.href);
    if (destinationUrl.pathname === currentUrl.pathname) {
      const contentElem = document.querySelector('#app main > #page');
      fadeOut(contentElem).then(async () => {
        // make the browser update the :target css selectors
        document.location.hash = destinationUrl.hash;

        // scroll to the top
        const container = document.querySelector('#app main');
        container?.scrollTo(0, 0);

        // use the entrance animation to reveal the new content
        await entranceIn(contentElem);
      });
    } else {
      router.push(href);
    }
  }

  /**
   * Converts a route tree into menu items for the navigation pane.
   */
  function convertRouteTreeToMenuItems(routeTree: TreeNode[]): TreeItem[] {
    return routeTree
      .map((node) => {
        return {
          name: categories[node.label]?.label || node.label,
          type: 'category',
          children: node.children
            .map((child) => {
              const family = [
                {
                  name: String(child.meta?.nav_title || child.meta?.title || child.name),
                  href: child.path,
                  onClick: (evt) => navigate(evt, child.path),
                } satisfies TreeItem,
                ...child.children
                  .map(
                    (grandchild) =>
                      ({
                        name: String(grandchild.meta?.nav_title || grandchild.meta?.title || grandchild.name),
                        href: grandchild.path,
                        onClick: (evt) => navigate(evt, grandchild.path),
                      } satisfies TreeItem)
                  )
                  .filter(notEmpty),
              ].filter((route) => route.name !== 'undefined');

              // if there is only one family member, return a simple navigation item
              if (family.length === 1) {
                return {
                  name: family[0].name,
                  icon: categories[child.label]?.icon,
                  href: family[0].href,
                  onClick: family[0].onClick,
                } satisfies TreeItem;
              }

              // otherwise, return an expander with the child and grandchildren as navigation items
              return {
                name: categories[child.label]?.label || child.label,
                icon: categories[child.label]?.icon,
                type: 'expander',
                children: family,
              } satisfies TreeItem;
            })
            .sort((a, b) => a.name.localeCompare(b.name)),
        } satisfies TreeItem;
      })
      .sort((a, b) => a.name.localeCompare(b.name))
      .sort((a) => (a.name === 'User Guide' ? -1 : 1)); // User Guide first
  }

  const router = useRouter();
  const routes = router.getRoutes();
  const homeRoute = routes.find((r) => r.name === 'index');
  const welcomeRoutes = routes.filter((r) => r.name?.toString().startsWith('(welcome)/'));
  const extractedRoutes = [homeRoute, ...welcomeRoutes].filter(notEmpty);
  const restRoutes = routes.filter((route) => !extractedRoutes.some((r) => r.name === route.name));
  const docsRouteTree = buildRouteTree(restRoutes) || [];

  // labels and icons for non-leaf (not last in tree) nodes
  const categories: Record<string, { label: string; icon?: string }> = {
    '(user-guide)': { label: 'User Guide' },
    '(administration)': { label: 'Administration' },
    '(development)': { label: 'Development' },
    workspaces: { label: 'Workspaces', icon: building },
    'publish-resources': { label: 'Publishing Resources', icon: tetrisApp },
    policies: { label: 'Policies', icon: globeShield },
    security: { label: 'Security', icon: shield },
    'reverse-proxy': { label: 'Reverse proxies', icon: arrowRouting },
    deployment: { label: 'Deployment', icon: building },
    uninstall: { label: 'Uninstall RAWeb', icon: uninstallApp },
    installation: { label: 'Install RAWeb', icon: installApp },
    'get-started': { label: 'Get started', icon: lightning },
    'supported-environments': { label: 'Supported Environments', icon: server },
    'custom-content': {
      label: 'Custom Content',
      icon: animalRabbit,
    },
  };

  const generateWelcomeRoutesTree = convertRouteTreeToMenuItems(buildRouteTree(welcomeRoutes));
  const generatedRestRoutesTree = convertRouteTreeToMenuItems(docsRouteTree);

  const menuItems = [
    {
      name: 'Home',
      href: homeRoute?.path,
      icon: home,
    },
    ...(generateWelcomeRoutesTree[0]?.children || []),
    {
      name: 'hr',
    },
    ...generatedRestRoutesTree,

    {
      name: 'footer',
      type: 'navigation',
      children: [
        {
          name: 'hr',
        },
        {
          name: 'View on GitHub',
          href: 'https://github.com/kimmknight/raweb',
          icon: '<svg width="98" height="96" viewBox="0 0 96 96" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M48.854 0C21.839 0 0 22 0 49.217c0 21.756 13.993 40.172 33.405 46.69 2.427.49 3.316-1.059 3.316-2.362 0-1.141-.08-5.052-.08-9.127-13.59 2.934-16.42-5.867-16.42-5.867-2.184-5.704-5.42-7.17-5.42-7.17-4.448-3.015.324-3.015.324-3.015 4.934.326 7.523 5.052 7.523 5.052 4.367 7.496 11.404 5.378 14.235 4.074.404-3.178 1.699-5.378 3.074-6.6-10.839-1.141-22.243-5.378-22.243-24.283 0-5.378 1.94-9.778 5.014-13.2-.485-1.222-2.184-6.275.486-13.038 0 0 4.125-1.304 13.426 5.052a46.97 46.97 0 0 1 12.214-1.63c4.125 0 8.33.571 12.213 1.63 9.302-6.356 13.427-5.052 13.427-5.052 2.67 6.763.97 11.816.485 13.038 3.155 3.422 5.015 7.822 5.015 13.2 0 18.905-11.404 23.06-22.324 24.283 1.78 1.548 3.316 4.481 3.316 9.126 0 6.6-.08 11.897-.08 13.526 0 1.304.89 2.853 3.316 2.364 19.412-6.52 33.405-24.935 33.405-46.691C97.707 22 75.788 0 48.854 0z" fill="currentColor"/></svg>',
        },
        {
          name: 'Submit a bug report',
          href: 'https://github.com/kimmknight/raweb/issues',
          icon: bug,
        },
      ],
    },
  ] satisfies TreeItem[];

  const windowWidth = ref(isBrowser ? window.innerWidth : 0);
  function handleResize() {
    windowWidth.value = window.innerWidth;
  }
  onMounted(() => {
    window.addEventListener('resize', handleResize);
  });
  onUnmounted(() => {
    window.removeEventListener('resize', handleResize);
  });

  // on initial mount, check for a hash in the URL and scroll to it
  onMounted(() => {
    const initialHash = window.location.hash;
    if (!initialHash) {
      return;
    }

    const targetElem = document.querySelector(window.location.hash);
    if (targetElem && targetElem instanceof HTMLElement) {
      const container = document.querySelector('#app main');
      container?.scrollTo(0, targetElem.offsetTop - 32);
    }
  });

  // once the splash screen is removed, add the router-target class
  // to the element(s) targed by the hash in the URL
  // Note: the router afterEach hook will handle adding/removing
  // the class upon subsequent navigations
  watch(
    () => splashScreenRemoved.value,
    () => {
      const initialHash = window.location.hash;
      if (!initialHash) {
        return;
      }

      requestAnimationFrame(() => {
        const targetElems = document.querySelectorAll(initialHash);
        targetElems.forEach((targetElem) => {
          targetElem.classList.add('router-target');
        });
      });
    }
  );

  // get new search results when the search value changes
  const searchValue = ref('');
  const searchResults = ref<PagefindSearchFragment[]>([]);
  const searching = ref(false);
  watchEffect(() => {
    searchValue.value;
    if (!isBrowser || !window.pagefind) {
      return;
    }

    searching.value = true;
    window.pagefind.debouncedSearch(searchValue.value).then(async (results) => {
      const topResults = await Promise.all((results?.results || []).slice(0, 5).map((res) => res.data()));
      searchResults.value = topResults;
      searching.value = false;
    });
  });

  // only show the search results when the search box is focused
  const searchBoxIsFocused = ref(false);
  function handleSearchBoxFocus() {
    searchBoxIsFocused.value = true;
  }
  function handleSearchBoxBlur(event: FocusEvent) {
    // only change the state if the new focus target is outside the search area
    if (
      !event.relatedTarget ||
      !(event.relatedTarget instanceof HTMLElement) ||
      !event.currentTarget ||
      !(event.currentTarget instanceof HTMLElement) ||
      !event.currentTarget.contains(event.relatedTarget)
    ) {
      searchBoxIsFocused.value = false;
    }
  }

  /**
   * Moves focus to the search result listed above the currently focused search result.
   * If there is no previous search result, moves focus back to the search box.
   */
  function moveFocusUp() {
    const focusedElement = document.activeElement as HTMLElement;
    if (focusedElement) {
      const previousElement = focusedElement.previousElementSibling as HTMLElement | undefined;
      if (previousElement) {
        focusedElement.setAttribute('tabindex', '-1');
        previousElement.focus();
        previousElement.setAttribute('tabindex', '0');
      } else {
        // move focus back to the search box
        focusSearchBox();
      }
    }
  }

  /**
   * Moves focus to the search result listed below the currently focused search result.
   */
  function moveFocusDown() {
    const focusedElement = document.activeElement as HTMLElement;
    if (focusedElement) {
      const nextElement = focusedElement.nextElementSibling as HTMLElement | undefined;
      if (nextElement) {
        focusedElement.setAttribute('tabindex', '-1');
        nextElement.focus();
        nextElement.setAttribute('tabindex', '0');
      }
    }
  }

  /**
   * Focuses the first search result in the search results list.
   */
  function focusFirstResult() {
    const firstResultElem = document.querySelector('.search-box-result') as HTMLElement | undefined;
    if (firstResultElem) {
      firstResultElem.focus();
      firstResultElem.setAttribute('tabindex', '0');
    }
  }

  /**
   * Focuses the search box input element.
   */
  function focusSearchBox() {
    (document.querySelector('.search-box-container input') as HTMLInputElement | undefined)?.focus();
  }

  /**
   * Goes to the search results page for the given search value.
   */
  function handleSearchSubmit(value: string) {
    if (value.length > 0) {
      router.push('/docs/search?q=' + encodeURIComponent(value));
      searchBoxIsFocused.value = false;
      searchValue.value = '';
    }
  }

  /**
   * Expands the search box in the navigation pane and focuses the input.
   */
  function expandSearchBox(toggleCollapse: () => void) {
    toggleCollapse();
    setTimeout(() => {
      searchBoxIsFocused.value = true;
      focusSearchBox();
    }, 120);
  }
</script>

<template>
  <Titlebar forceVisible :loading="titlebarLoading" hideProfileMenu />
  <div id="appContent">
    <NavigationPane
      :show-back-arrow="false"
      :variant="windowWidth < 800 ? 'leftCompact' : 'left'"
      stateId="docs-nav"
      :menu-items="menuItems"
    >
      <template #custom="{ collapsed, toggleCollapse }">
        <div
          v-if="!collapsed"
          class="search-area"
          @focusin="handleSearchBoxFocus"
          @focusout="handleSearchBoxBlur"
        >
          <div class="search-box-container">
            <TextBox
              v-model:value="searchValue"
              :placeholder="t('docs.search.placeholder')"
              @keydown.down="focusFirstResult()"
              @submit="handleSearchSubmit"
              @keydown.enter="() => handleSearchSubmit(searchValue)"
              showSubmitButton
            />
          </div>

          <div class="search-box-results" v-if="searchBoxIsFocused">
            <MenuFlyoutItem
              v-if="searchResults.length > 0"
              v-for="(result, index) in searchResults"
              class="search-box-result"
              :href="result.raw_url"
              @click.prevent="
                router.push(result.raw_url || '/docs/');
                searchValue = '';
                searchBoxIsFocused = false;
              "
              :tabindex="index === 0 ? '0' : '-1'"
              @keydown.up="() => moveFocusUp()"
              @keydown.down="() => moveFocusDown()"
            >
              {{ result.meta.nav_title || result.meta.title }}
            </MenuFlyoutItem>
            <div class="search-box-result center" v-if="searching && searchResults.length === 0">
              <ProgressRing :size="16" />
              {{ t('docs.search.searching') }}
            </div>
            <div class="search-box-result center" v-else-if="searchResults.length === 0 && !searchValue">
              {{ t('docs.search.typeToSearch') }}
            </div>
            <div class="search-box-result center" v-else-if="searchResults.length === 0">
              {{ t('docs.search.noResults') }}
            </div>
            <ProgressBar v-if="searching" />
            <div v-else style="height: 3px"></div>
          </div>
        </div>
        <ListItem v-else @click="expandSearchBox(toggleCollapse)">
          <template #icon>
            <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path
                d="M10 2.75a7.25 7.25 0 0 1 5.63 11.819l4.9 4.9a.75.75 0 0 1-.976 1.134l-.084-.073-4.901-4.9A7.25 7.25 0 1 1 10 2.75Zm0 1.5a5.75 5.75 0 1 0 0 11.5 5.75 5.75 0 0 0 0-11.5Z"
                fill="#ffffff"
              />
            </svg>
          </template>
        </ListItem>
      </template>
    </NavigationPane>

    <main>
      <div id="page" data-pagefind-body>
        <router-view v-slot="{ Component }">
          <TextBlock variant="title" tag="h1" class="page-title" data-pagefind-meta="title">{{
            router.currentRoute.value.meta.title
          }}</TextBlock>
          <component :is="Component" />
        </router-view>
      </div>
    </main>
  </div>
</template>

<style scoped>
  main {
    flex-grow: 1;
    flex-shrink: 1;
    flex-basis: 0%;

    height: var(--content-height);
    overflow: auto;
    background-color: var(--wui-solid-background-tertiary);
    box-sizing: border-box;
    border-radius: var(--wui-overlay-corner-radius) 0 0 0;

    display: flex;
    flex-direction: column;
  }

  .search-area {
    position: relative;
  }

  .search-box-container {
    animation: search-box-container-open 130ms var(--wui-control-fast-out-slow-in-easing);
    padding: 6px 16px 12px 16px;
  }

  @keyframes search-box-container-open {
    0% {
      opacity: 0;
      padding-top: 0;
      padding-bottom: 0;
    }
    100% {
      opacity: 1;
      padding-top: 6px;
      padding-bottom: 12px;
    }
  }

  .search-box-results {
    --menu-flyout-transition-offset: -10px;
    top: 38px;
    left: 16px;
    right: 16px;
    position: absolute;
    z-index: 10;
    user-select: none;
    font-family: var(--wui-font-family-text);
    font-size: var(--wui-font-size-body);
    font-weight: normal;
    line-height: 20px;
    display: flex;
    flex-direction: column;
    animation: menu-open var(--wui-control-normal-duration) var(--wui-control-fast-out-slow-in-easing);
    min-inline-size: 120px;
    max-inline-size: 100%;
    max-block-size: 100vh;
    padding-block: 2px;
    box-sizing: border-box;
    color: var(--wui-text-primary);
    border-radius: var(--wui-overlay-corner-radius);
    border: 1px solid var(--wui-surface-stroke-flyout);
    background-color: var(--wui-solid-background-quarternary);
    background-clip: padding-box;
    overflow: hidden;
  }

  .search-box-result {
    min-block-size: 28px;
    margin-top: 6px;
  }
  .search-box-result.center {
    text-align: center;
    color: var(--wui-text-secondary);
    font-size: 14px;
    cursor: default;
  }

  main > div#page {
    --padding: 36px;
    padding: var(--padding);
    width: 100%;
    box-sizing: border-box;
    view-transition-name: main;
    flex-grow: 1;
    flex-shrink: 1;
    max-width: 896px;
    margin: 0 auto;
  }

  #page :deep(.markdown-body) {
    font-family: var(--wui-font-family-text);
    line-height: 1.5;
    font-size: 14px;
  }

  #page :deep(:where(h1, h2):not(.page-title):not(.type-subtitle)) {
    font-family: var(--wui-font-family-display);
    font-weight: 600;
    font-size: 24px;
    line-height: 34px;
  }

  #page :deep(h3) {
    font-family: var(--wui-font-family-display);
    font-weight: 600;
    font-size: var(--wui-font-size-subtitle);
    line-height: 28px;
  }

  #page :deep(h4) {
    font-weight: 600;
    font-size: 16px;
    line-height: 24px;
  }

  #page :deep(:where(h1, h2, h3, h4):not(:first-child)) {
    margin-top: 32px;
    margin-bottom: 16px;
  }
  #page :deep(:where(h2 + h3), :where(h3 + h4)) {
    margin-top: 0 !important;
  }

  #page :deep(code:not(pre code)) {
    font-family: var(---wui-font-family-monospace);
    font-size: 13px;
    background-color: var(--wui-card-background-default);
    box-shadow: inset 0 0 0 1px var(--wui-surface-stroke-default);
    padding: 2px 4px;
    border-radius: 4px;
  }

  #page :deep(img) {
    max-width: 100%;
    margin: 4px 0;
    height: auto; /* maintain aspect ratio when width of page is smaller than the image */
  }

  :deep(a) {
    color: var(--wui-accent-text-primary);
  }

  #page :deep(details) {
    margin: 16px 0;
    border: 1px solid var(--wui-surface-stroke-default);
    border-radius: var(--wui-overlay-corner-radius);
    padding: 12px 16px;
  }

  #page :deep(details > summary) {
    font-weight: 600;
    cursor: default;
    user-select: none;
    padding: 12px 16px;
    margin: -12px -16px;
    border-radius: var(--wui-control-corner-radius);
    transition: var(--wui-control-faster-duration) ease;
    list-style-type: none;
  }
  #page :deep(details > summary::-webkit-details-marker) {
    display: none;
  }
  #page :deep(details > summary::before) {
    content: url('data:image/svg+xml; utf8, <svg width="16" height="16" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path d="M8.47 4.22a.75.75 0 0 0 0 1.06L15.19 12l-6.72 6.72a.75.75 0 1 0 1.06 1.06l7.25-7.25a.75.75 0 0 0 0-1.06L9.53 4.22a.75.75 0 0 0-1.06 0Z" fill="currentColor"/></svg>');
    filter: invert(1);
    margin-right: 8px;
    position: relative;
    top: 2px;
  }
  #page :deep(details[open] > summary::before) {
    content: url('data:image/svg+xml; utf8, <svg width="16" height="16" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path d="M4.22 8.47a.75.75 0 0 1 1.06 0L12 15.19l6.72-6.72a.75.75 0 1 1 1.06 1.06l-7.25 7.25a.75.75 0 0 1-1.06 0L4.22 9.53a.75.75 0 0 1 0-1.06Z" fill="currentColor"/></svg>');
  }
  #page :deep(details > summary):hover {
    background-color: var(--wui-subtle-secondary);
  }
  #page :deep(details > summary):active {
    background-color: var(--wui-subtle-tertiary);
    color: var(--wui-text-tertiary);
  }
  #page :deep(table) {
    border-collapse: separate;
    border-spacing: 0;
    width: 100%;
    margin: 16px 0;
    border-radius: var(--wui-overlay-corner-radius);
    border: 1px solid var(--wui-surface-stroke-default);
    overflow: hidden;
    font-family: var(--wui-font-family-text);
    font-size: 14px;
  }
  #page :deep(th) {
    padding: 6px 8px;
    text-align: left;
    background-color: var(--wui-layer-default);
  }
  #page :deep(td) {
    padding: 4px 8px;
    text-align: left;
  }
  #page :deep(table tr :where(th, td)) {
    border-bottom: 1px solid var(--wui-surface-stroke-default);
  }
  #page :deep(table tbody tr:last-child :where(th, td)) {
    border-bottom: none;
  }
  #page :deep(table th:not(:last-child)),
  #page :deep(table td:not(:last-child)) {
    border-right: 1px solid var(--wui-surface-stroke-default);
  }

  #page :deep(.info-bar + .info-bar) {
    margin-top: 8px;
  }

  @keyframes flicker {
    /* off states */
    0%,
    10%,
    20%,
    30%,
    100% {
      background-color: transparent;
    }

    /* flashes */
    5%,
    15%,
    25%,
    35% {
      background-color: color-mix(in srgb, var(--wui-system-attention) 24%, transparent);
    }
  }

  #page :deep(*:target),
  #page :deep(*.router-target) {
    animation: flicker 2.2s ease-in-out 1;
    position: relative;
    scroll-margin-top: 72px;
  }
  #page :deep(*:not(a):target::before),
  #page :deep(*.router-target:not(a)::before) {
    content: '';
    position: absolute;
    left: -24px;
    top: calc(50% - 6px);
    background-color: var(--wui-system-attention);
    width: 12px;
    height: 12px;
    border-radius: 50%;
  }
  #page :deep(li:target::before),
  #page :deep(li.router-target::before) {
    top: 6px;
    left: -38px;
  }
</style>
