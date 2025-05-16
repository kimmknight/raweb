<script setup lang="ts">
  import Button from '$components/Button/Button.vue';
  import IconButton from '$components/IconButton/IconButton.vue';
  import { MenuFlyout, MenuFlyoutItem } from '$components/MenuFlyout/index.mjs';
  import ProgressRing from '$components/ProgressRing/ProgressRing.vue';
  import TextBlock from '$components/TextBlock/TextBlock.vue';
  import { simpleModeEnabled } from '$utils';
  import { onMounted, ref, useTemplateRef } from 'vue';

  const { forceVisible = false, loading = false } = defineProps<{
    forceVisible?: boolean;
    loading?: boolean;
  }>();

  // TODO [Anchors]: Remove this when all major browsers support CSS Anchor Positioning
  const supportsAnchorPositions = CSS.supports('position-area', 'center center');

  const titlebarElem = useTemplateRef<HTMLDivElement>('titlebarElem');

  onMounted(() => {
    // hide the header if the display mode is not window-controls-overlay
    const isWindowControlsOverlayMode = window.matchMedia('(display-mode: window-controls-overlay)').matches;
    const isStandaloneMode = window.matchMedia('(display-mode: standalone)').matches;
    if ((!isWindowControlsOverlayMode || isStandaloneMode) && !forceVisible) {
      if (titlebarElem.value) titlebarElem.value.style.display = 'none';
      document.body.style.setProperty('--header-height', '0px');
    }

    const isWindowControlsOverlayMediaQueryList = window.matchMedia('(display-mode: window-controls-overlay)');
    function handleWindowControlsOverlayChange(event: MediaQueryListEvent) {
      if (event.matches) {
        // in standalone mode, hide the header
        if (titlebarElem.value) titlebarElem.value.style.display = 'none';
        document.body.style.setProperty('--header-height', '0px');
      } else {
        // not in standalone mode, show the header
        if (titlebarElem.value) titlebarElem.value.style.display = 'flex';
        document.body.style.setProperty('--header-height', 'env(titlebar-area-height, 30px)');
      }
    }

    const isStandaloneMediaQueryList = window.matchMedia('(display-mode: standalone)');
    function handleStandaloneChange(event: MediaQueryListEvent) {
      if (event.matches) {
        // in standalone mode, hide the header
        if (titlebarElem.value) titlebarElem.value.style.display = 'none';
        document.body.style.setProperty('--header-height', '0px');
      } else {
        // not in standalone mode, show the header
        if (titlebarElem.value) titlebarElem.value.style.display = 'flex';
        document.body.style.setProperty('--header-height', 'env(titlebar-area-height, 30px)');
      }
    }

    // watch for changes to the display mode and hide/show the header accordingly
    if (!forceVisible) {
      // only show the header when in window-controls-overlay mode
      isWindowControlsOverlayMediaQueryList.addEventListener('change', handleWindowControlsOverlayChange);
    } else {
      // only show the header when not in standalone mode or when window controls overlay mode is enabled
      isStandaloneMediaQueryList.addEventListener('change', handleStandaloneChange);
    }

    // track whether the window/tab is in focus and adjust the header visibility accordingly
    function handleFocus() {
      document.body.setAttribute('data-window-focused', 'true');
    }
    function handleBlur() {
      document.body.setAttribute('data-window-focused', 'false');
    }
    window.addEventListener('focus', handleFocus);
    window.addEventListener('blur', handleBlur);

    return () => {
      // clean up event listeners on component unmount
      isWindowControlsOverlayMediaQueryList.removeEventListener('change', handleWindowControlsOverlayChange);
      isStandaloneMediaQueryList.removeEventListener('change', handleStandaloneChange);
      window.removeEventListener('focus', handleFocus);
      window.removeEventListener('blur', handleBlur);
    };
  });

  const base = window.__base;
  const authUser = window.__authUser;

  async function signOut() {
    // redirect to the logout URL
    window.location.href = `${window.location.origin}${window.__base}logoff.aspx`; // redirect to the logout URL
  }

  // listen for Alt + L keyboard shortcut to trigger sign out
  window.addEventListener('keydown', (event) => {
    if (event.altKey && event.key === 'l') {
      event.preventDefault(); // prevent default action to avoid any conflicts with other shortcuts
      signOut();
    }
  });

  // watch for changes to the URL hash and update the app state accordingly
  const hash = ref(window.location.hash);
  window.addEventListener('hashchange', () => {
    hash.value = window.location.hash;
  });

  function goBack() {
    window.location.hash = '#simple';
  }

  // set the app title
  const appTitle = ref(document.title);
</script>

<template>
  <div class="app-header" ref="titlebarElem">
    <div class="left">
      <IconButton
        :onclick="goBack"
        class="profile-menu-button"
        title="Open settings"
        v-if="simpleModeEnabled && hash === '#settings'"
      >
        <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
          <path
            d="M10.733 19.79a.75.75 0 0 0 1.034-1.086L5.516 12.75H20.25a.75.75 0 0 0 0-1.5H5.516l6.251-5.955a.75.75 0 0 0-1.034-1.086l-7.42 7.067a.995.995 0 0 0-.3.58.754.754 0 0 0 .001.289.995.995 0 0 0 .3.579l7.419 7.067Z"
            fill="currentColor"
          />
        </svg>
      </IconButton>
      <img :src="`${base}lib/assets/icon.svg`" alt="" class="logo" />
      <span class="title">
        <TextBlock variant="caption">{{ appTitle }}</TextBlock>
      </span>
      <ProgressRing :size="16" style="padding: 0 8px" v-if="loading" />
    </div>
    <div class="right">
      <IconButton
        href="#settings"
        class="profile-menu-button"
        title="Open settings"
        v-if="simpleModeEnabled && hash !== '#settings'"
      >
        <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
          <path
            d="M12.012 2.25c.734.008 1.465.093 2.182.253a.75.75 0 0 1 .582.649l.17 1.527a1.384 1.384 0 0 0 1.927 1.116l1.401-.615a.75.75 0 0 1 .85.174 9.792 9.792 0 0 1 2.204 3.792.75.75 0 0 1-.271.825l-1.242.916a1.381 1.381 0 0 0 0 2.226l1.243.915a.75.75 0 0 1 .272.826 9.797 9.797 0 0 1-2.204 3.792.75.75 0 0 1-.848.175l-1.407-.617a1.38 1.38 0 0 0-1.926 1.114l-.169 1.526a.75.75 0 0 1-.572.647 9.518 9.518 0 0 1-4.406 0 .75.75 0 0 1-.572-.647l-.168-1.524a1.382 1.382 0 0 0-1.926-1.11l-1.406.616a.75.75 0 0 1-.849-.175 9.798 9.798 0 0 1-2.204-3.796.75.75 0 0 1 .272-.826l1.243-.916a1.38 1.38 0 0 0 0-2.226l-1.243-.914a.75.75 0 0 1-.271-.826 9.793 9.793 0 0 1 2.204-3.792.75.75 0 0 1 .85-.174l1.4.615a1.387 1.387 0 0 0 1.93-1.118l.17-1.526a.75.75 0 0 1 .583-.65c.717-.159 1.45-.243 2.201-.252Zm0 1.5a9.135 9.135 0 0 0-1.354.117l-.109.977A2.886 2.886 0 0 1 6.525 7.17l-.898-.394a8.293 8.293 0 0 0-1.348 2.317l.798.587a2.881 2.881 0 0 1 0 4.643l-.799.588c.32.842.776 1.626 1.348 2.322l.905-.397a2.882 2.882 0 0 1 4.017 2.318l.11.984c.889.15 1.798.15 2.687 0l.11-.984a2.881 2.881 0 0 1 4.018-2.322l.905.396a8.296 8.296 0 0 0 1.347-2.318l-.798-.588a2.881 2.881 0 0 1 0-4.643l.796-.587a8.293 8.293 0 0 0-1.348-2.317l-.896.393a2.884 2.884 0 0 1-4.023-2.324l-.11-.976a8.988 8.988 0 0 0-1.333-.117ZM12 8.25a3.75 3.75 0 1 1 0 7.5 3.75 3.75 0 0 1 0-7.5Zm0 1.5a2.25 2.25 0 1 0 0 4.5 2.25 2.25 0 0 0 0-4.5Z"
            fill="currentColor"
          />
        </svg>
      </IconButton>
      <MenuFlyout placement="bottom" anchor="end">
        <template v-slot="{ popoverId }">
          <Button
            :popovertarget="popoverId"
            :class="['profile-menu-button', supportsAnchorPositions ? '' : 'manual-anchor']"
          >
            {{ authUser.username }}
          </Button>
        </template>
        <template v-slot:menu>
          <MenuFlyoutItem hint="Alt + L" @click="signOut">
            Sign out
            <template v-slot:icon>
              <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                <path d="M8.502 11.5a1.002 1.002 0 1 1 0 2.004 1.002 1.002 0 0 1 0-2.004Z" fill="#ffffff" />
                <path
                  d="M12 4.354v6.651l7.442-.001L17.72 9.28a.75.75 0 0 1-.073-.976l.073-.084a.75.75 0 0 1 .976-.073l.084.073 2.997 2.997a.75.75 0 0 1 .073.976l-.073.084-2.996 3.004a.75.75 0 0 1-1.134-.975l.072-.085 1.713-1.717-7.431.001L12 19.25a.75.75 0 0 1-.88.739l-8.5-1.502A.75.75 0 0 1 2 17.75V5.75a.75.75 0 0 1 .628-.74l8.5-1.396a.75.75 0 0 1 .872.74Zm-1.5.883-7 1.15V17.12l7 1.236V5.237Z"
                  fill="#ffffff"
                />
                <path
                  d="M13 18.501h.765l.102-.006a.75.75 0 0 0 .648-.745l-.007-4.25H13v5.001ZM13.002 10 13 8.725V5h.745a.75.75 0 0 1 .743.647l.007.102.007 4.251h-1.5Z"
                  fill="currentColor"
                />
              </svg>
            </template>
          </MenuFlyoutItem>
        </template>
      </MenuFlyout>
    </div>
  </div>
</template>

<style>
  body {
    --header-height: max(env(titlebar-area-height, 33px), 33px);
    --content-height: calc(
      100vh - var(--header-height) - env(safe-area-inset-top, 0px) - env(safe-area-inset-bottom, 0px)
    );
    margin: 0;
  }
</style>

<style scoped>
  .app-header {
    --height: var(--header-height);
    background-color: var(--wui-solid-background-base);
    color: var(--wui-text-primary);
    height: var(--height);
    display: flex;
    flex-direction: row;
    align-items: center;
    justify-content: space-between;
    padding: 0 2px 0 6px;
    font-size: 13px;
    flex-grow: 0;
    flex-shrink: 0;
    user-select: none;
    -webkit-app-region: drag;
    position: relative;
    font-family: var(--wui-font-family-text);
    font-size: var(--wui-font-size-caption);
    width: env(titlebar-area-width, 100%);
    box-sizing: border-box;
  }

  .app-header :where(.left, .right) {
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
    padding: 0 8px;
    object-fit: cover;
    -webkit-user-drag: none;
  }

  .app-header .title {
    padding: 0 8px;
  }
</style>

<style>
  body[data-window-focused='false'] .app-header .title,
  body[data-window-focused='false'] .profile-menu-button > span {
    opacity: 0.5;
  }

  .profile-menu-button:not(:hover):not(:active) {
    background-color: transparent !important;
  }
  .profile-menu-button {
    box-shadow: none !important;
    font-size: var(--wui-font-size-caption) !important;
    line-height: var(--wui-font-size-caption) !important;
    font-family: var(--wui-font-family-small) !important;
    padding-block: 4px !important;
    height: min(var(--header-height), 30px) !important;
    -webkit-app-region: no-drag;
    cursor: default;
  }

  .profile-menu-button.manual-anchor + .menu-flyout {
    position: absolute;
    top: env(titlebar-area-height, 30px);
    left: calc(100vw - 172px);
  }
</style>
