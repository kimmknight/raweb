<script setup lang="ts">
  import { NavigationPane, TextBlock, Titlebar } from '$components';
  import { TreeItem } from '$components/NavigationView/NavigationTypes';
  import {
    arrowRouting,
    bug,
    building,
    globeShield,
    home,
    server,
    shield,
    tetrisApp,
    uninstallApp,
  } from '$icons';
  import { notEmpty, registerServiceWorker, removeSplashScreen } from '$utils';
  import { computed, onMounted, onUnmounted, ref, watchEffect } from 'vue';
  import { RouteRecordNormalized, useRouter } from 'vue-router';
  import { i18nextPromise } from './i18n';

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

  // remove the splash screen once everything is ready
  const canRemoveSplashScreen = computed(() => {
    return i18nReady.value;
  });
  watchEffect(() => {
    if (canRemoveSplashScreen.value) {
      setTimeout(() => {
        removeSplashScreen();
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

  /**
   * Converts a route tree into menu items for the navigation pane.
   */
  function convertRouteTreeToMenuItems(routeTree: TreeNode[]): TreeItem[] {
    return routeTree
      .map((node) => {
        return {
          name: categories[node.label]?.label || node.label,
          type: 'category',
          children: node.children.map((child) => {
            const family = [
              {
                name: String(child.meta?.nav_title || child.meta?.title || child.name),
                href: child.path,
              },
              ...child.children
                .map((grandchild) => ({
                  name: String(grandchild.meta?.nav_title || grandchild.meta?.title || grandchild.name),
                  href: grandchild.path,
                }))
                .filter(notEmpty),
            ].filter((route) => route.name !== 'undefined');

            // if there is only one family member, return a simple navigation item
            if (family.length === 1) {
              return {
                name: family[0].name,
                icon: categories[child.label]?.icon,
                href: family[0].href,
              };
            }

            // otherwise, return an expander with the child and grandchildren as navigation items
            return {
              name: categories[child.label]?.label || child.label,
              icon: categories[child.label]?.icon,
              type: 'expander',
              children: family,
            };
          }),
        } satisfies TreeItem;
      })
      .sort((a, b) => a.name.localeCompare(b.name) * -1);
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
    workspaces: { label: 'Workspaces', icon: building },
    'publish-resources': { label: 'Publishing Resources', icon: tetrisApp },
    policies: { label: 'Policies', icon: globeShield },
    security: { label: 'Security', icon: shield },
    'reverse-proxy': { label: 'Reverse proxies', icon: arrowRouting },
    uninstall: { label: 'Uninstall RAWeb', icon: uninstallApp },
    'supported-environments': { label: 'Supported Environments', icon: server },
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

  const windowWidth = ref(window.innerWidth);
  function handleResize() {
    windowWidth.value = window.innerWidth;
  }
  onMounted(() => {
    window.addEventListener('resize', handleResize);
  });
  onUnmounted(() => {
    window.removeEventListener('resize', handleResize);
  });
</script>

<template>
  <Titlebar forceVisible :loading="titlebarLoading" hideProfileMenu />
  <div id="appContent">
    <NavigationPane
      :show-back-arrow="false"
      :variant="windowWidth < 800 ? 'leftCompact' : 'left'"
      stateId="docs-nav"
      :menu-items="menuItems"
    />
    <main>
      <div id="page">
        <router-view v-slot="{ Component }">
          <TextBlock variant="title" tag="h1" class="page-title">{{
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

  #page :deep(:where(h1, h2):not(.page-title)) {
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

  #page :deep(*:target) {
    animation: flicker 2.2s ease-in-out 1;
    position: relative;
  }
  #page :deep(*:not(a):target::before) {
    content: '';
    position: absolute;
    left: -24px;
    top: calc(50% - 6px);
    background-color: var(--wui-system-attention);
    width: 12px;
    height: 12px;
    border-radius: 50%;
  }
  #page :deep(li:target::before) {
    top: 6px;
    left: -38px;
  }
</style>
