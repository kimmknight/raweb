<script setup lang="ts">
  import {
    Button,
    ContentDialog,
    IconButton,
    MenuFlyout,
    MenuFlyoutDivider,
    MenuFlyoutItem,
    ProgressRing,
    TextBlock,
  } from '$components';
  import { useCoreDataStore } from '$stores';
  import { restoreSplashScreen, simpleModeEnabled, useUpdateDetails } from '$utils';
  import { computed, nextTick, onMounted, ref, type UnwrapRef, useTemplateRef } from 'vue';
  import { useRoute, useRouter } from 'vue-router';

  const {
    forceVisible = false,
    loading = false,
    hideProfileMenu = false,
    withBorder = false,
    update,
  } = defineProps<{
    forceVisible?: boolean;
    loading?: boolean;
    hideProfileMenu?: boolean;
    withBorder?: boolean;
    update?: UnwrapRef<ReturnType<typeof useUpdateDetails>['updateDetails']>;
  }>();

  const { policies, appBase: base, authUser, iisBase } = useCoreDataStore();
  const hidePasswordChange = policies.passwordChangeEnabled === false;

  const route = useRoute();
  const router = useRouter();

  // TODO [Anchors]: Remove this when all major browsers support CSS Anchor Positioning
  const supportsAnchorPositions = CSS.supports('position-area', 'center center');

  const titlebarElem = useTemplateRef<HTMLDivElement>('titlebarElem');

  const isPopup = computed(() => typeof window !== 'undefined' && window.opener && window.opener !== window);

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
        // in window controls overlay mode, show the header
        if (titlebarElem.value) titlebarElem.value.style.display = 'flex';
        document.body.style.setProperty('--header-height', 'env(titlebar-area-height, 30px)');
      } else {
        // not in window controls overlay mode, show the header
        if (titlebarElem.value) titlebarElem.value.style.display = 'none';
        document.body.style.setProperty('--header-height', '0px');
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

  async function signOut() {
    // redirect to the logout URL
    restoreSplashScreen().then(() => {
      window.location.href = `${window.location.origin}${base}logoff`;
    });
  }

  /**
   * Redirects the user to the password change page with the current username and return URL specified.
   */
  function changePassword() {
    restoreSplashScreen().then(() => {
      const currentLocation = window.location.href;
      const passwordChangeUrl = `${iisBase}password?username=${
        authUser.username
      }&returnUrl=${encodeURIComponent(currentLocation)}`;
      window.location.href = passwordChangeUrl;
    });
  }

  // listen for Alt + L keyboard shortcut to trigger sign out
  window.addEventListener('keydown', (event) => {
    if (event.altKey && event.key === 'l') {
      event.preventDefault(); // prevent default action to avoid any conflicts with other shortcuts
      signOut();
    }
  });

  function goBack() {
    route.meta.isTitlebarBackButton = true;

    if (route.path === '/policies') {
      router.push('/settings');
    } else if (route.name === 'webGuacd') {
      router.back();
    } else {
      router.push('/simple');
    }
  }

  // set the app title
  const appTitle = ref(document.title);
  router.afterEach(() => {
    nextTick(() => {
      appTitle.value = document.title;
    });
  });
</script>

<template>
  <div :class="`app-header ${withBorder ? 'with-border' : ''}`" ref="titlebarElem">
    <div class="left">
      <IconButton
        :onclick="goBack"
        class="profile-menu-button"
        title="Go back"
        v-if="
          (simpleModeEnabled && (route.path === '/settings' || route.path === '/policies')) ||
          (route.name === 'webGuacd' && !isPopup)
        "
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
      <ContentDialog size="max" v-if="update?.details" :title="update.details.name">
        <template #opener="{ open }">
          <Button
            :class="['profile-menu-button']"
            style="color: var(--wui-system-attention)"
            v-if="update?.details"
            @click="() => open()"
          >
            <template #icon>
              <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                <path
                  d="M16 8.25a.75.75 0 0 1 1.5 0v3.25a.75.75 0 0 1-.75.75H14a.75.75 0 0 1 0-1.5h1.27A3.502 3.502 0 0 0 12 8.5c-1.093 0-2.037.464-2.673 1.23a.75.75 0 1 1-1.154-.96C9.096 7.66 10.463 7 12 7c1.636 0 3.088.785 4 2v-.75ZM8 15v.75a.75.75 0 0 1-1.5 0v-3a.75.75 0 0 1 .75-.75H10a.75.75 0 0 1 0 1.5H8.837a3.513 3.513 0 0 0 5.842.765.75.75 0 1 1 1.142.972A5.013 5.013 0 0 1 8 15Zm4-13C6.477 2 2 6.477 2 12s4.477 10 10 10 10-4.477 10-10S17.523 2 12 2Zm8.5 10a8.5 8.5 0 1 1-17 0 8.5 8.5 0 0 1 17 0Z"
                  fill="currentColor"
                />
              </svg>
            </template>
            {{ $t('titlebar.updateAvailable') }}
          </Button>
        </template>
        <div class="gfm" v-html="update.details.notes"></div>
        <template v-slot:footer="{ close }">
          <Button :href="update.details.html_url" target="_blank">View on GitHub</Button>
          <Button @click="close">Close</Button>
        </template>
      </ContentDialog>
      <RouterLink to="/settings" custom v-slot="settingsLinkProps">
        <IconButton
          :href="settingsLinkProps.href"
          class="profile-menu-button"
          title="Open settings"
          @click="settingsLinkProps.navigate"
          v-if="
            settingsLinkProps &&
            !hideProfileMenu &&
            simpleModeEnabled &&
            route.path !== '/settings' &&
            route.path !== '/policies'
          "
        >
          <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path
              d="M12.012 2.25c.734.008 1.465.093 2.182.253a.75.75 0 0 1 .582.649l.17 1.527a1.384 1.384 0 0 0 1.927 1.116l1.401-.615a.75.75 0 0 1 .85.174 9.792 9.792 0 0 1 2.204 3.792.75.75 0 0 1-.271.825l-1.242.916a1.381 1.381 0 0 0 0 2.226l1.243.915a.75.75 0 0 1 .272.826 9.797 9.797 0 0 1-2.204 3.792.75.75 0 0 1-.848.175l-1.407-.617a1.38 1.38 0 0 0-1.926 1.114l-.169 1.526a.75.75 0 0 1-.572.647 9.518 9.518 0 0 1-4.406 0 .75.75 0 0 1-.572-.647l-.168-1.524a1.382 1.382 0 0 0-1.926-1.11l-1.406.616a.75.75 0 0 1-.849-.175 9.798 9.798 0 0 1-2.204-3.796.75.75 0 0 1 .272-.826l1.243-.916a1.38 1.38 0 0 0 0-2.226l-1.243-.914a.75.75 0 0 1-.271-.826 9.793 9.793 0 0 1 2.204-3.792.75.75 0 0 1 .85-.174l1.4.615a1.387 1.387 0 0 0 1.93-1.118l.17-1.526a.75.75 0 0 1 .583-.65c.717-.159 1.45-.243 2.201-.252Zm0 1.5a9.135 9.135 0 0 0-1.354.117l-.109.977A2.886 2.886 0 0 1 6.525 7.17l-.898-.394a8.293 8.293 0 0 0-1.348 2.317l.798.587a2.881 2.881 0 0 1 0 4.643l-.799.588c.32.842.776 1.626 1.348 2.322l.905-.397a2.882 2.882 0 0 1 4.017 2.318l.11.984c.889.15 1.798.15 2.687 0l.11-.984a2.881 2.881 0 0 1 4.018-2.322l.905.396a8.296 8.296 0 0 0 1.347-2.318l-.798-.588a2.881 2.881 0 0 1 0-4.643l.796-.587a8.293 8.293 0 0 0-1.348-2.317l-.896.393a2.884 2.884 0 0 1-4.023-2.324l-.11-.976a8.988 8.988 0 0 0-1.333-.117ZM12 8.25a3.75 3.75 0 1 1 0 7.5 3.75 3.75 0 0 1 0-7.5Zm0 1.5a2.25 2.25 0 1 0 0 4.5 2.25 2.25 0 0 0 0-4.5Z"
              fill="currentColor"
            />
          </svg>
        </IconButton>
      </RouterLink>
      <MenuFlyout
        placement="bottom"
        anchor="end"
        v-if="!hideProfileMenu && authUser && policies.anonymousAuthentication !== 'always'"
      >
        <template v-slot="{ popoverId }">
          <Button
            :popovertarget="popoverId"
            :class="['profile-menu-button', supportsAnchorPositions ? '' : 'manual-anchor']"
          >
            {{ authUser.fullName || authUser.username }}
          </Button>
        </template>
        <template v-slot:menu>
          <MenuFlyoutItem
            v-if="!hidePasswordChange"
            hint="&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"
            @click="changePassword"
          >
            {{ $t('profile.changePassword') }}
            <template v-slot:icon>
              <svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                <path d="M18.25 7a1.25 1.25 0 1 1-2.5 0 1.25 1.25 0 0 1 2.5 0Z" fill="currentColor" />
                <path
                  d="M15.5 2.05A6.554 6.554 0 0 0 8.95 8.6c0 .387.05.76.11 1.104a.275.275 0 0 1-.07.244l-6.235 6.236a2.75 2.75 0 0 0-.806 1.944V20.3c0 .966.784 1.75 1.75 1.75h2.5a1.75 1.75 0 0 0 1.75-1.75v-1.25H9.7c.69 0 1.25-.56 1.25-1.25v-1.75h1.75a1.25 1.25 0 0 0 1.25-1.204c.496.128 1.02.204 1.55.204a6.554 6.554 0 0 0 6.55-6.55c0-3.631-2.953-6.45-6.55-6.45ZM10.45 8.6a5.054 5.054 0 0 1 5.05-5.05c2.802 0 5.05 2.181 5.05 4.95a5.054 5.054 0 0 1-5.05 5.05c-.68 0-1.38-.171-2.005-.44a.75.75 0 0 0-1.046.69v.75H10.7c-.69 0-1.25.56-1.25 1.25v1.75H7.7c-.69 0-1.25.56-1.25 1.25v1.5a.25.25 0 0 1-.25.25H3.7a.25.25 0 0 1-.25-.25v-2.172c0-.331.132-.65.366-.884l6.236-6.235a1.774 1.774 0 0 0 .486-1.564 4.917 4.917 0 0 1-.088-.845Z"
                  fill="currentColor"
                />
              </svg>
            </template>
          </MenuFlyoutItem>
          <MenuFlyoutDivider v-if="!hidePasswordChange" />
          <MenuFlyoutItem hint="Alt + L" @click="signOut">
            {{ $t('profile.signOut') }}
            <template v-slot:icon>
              <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                <path
                  d="M8.502 11.5a1.002 1.002 0 1 1 0 2.004 1.002 1.002 0 0 1 0-2.004Z"
                  fill="currentColor"
                />
                <path
                  d="M12 4.354v6.651l7.442-.001L17.72 9.28a.75.75 0 0 1-.073-.976l.073-.084a.75.75 0 0 1 .976-.073l.084.073 2.997 2.997a.75.75 0 0 1 .073.976l-.073.084-2.996 3.004a.75.75 0 0 1-1.134-.975l.072-.085 1.713-1.717-7.431.001L12 19.25a.75.75 0 0 1-.88.739l-8.5-1.502A.75.75 0 0 1 2 17.75V5.75a.75.75 0 0 1 .628-.74l8.5-1.396a.75.75 0 0 1 .872.74Zm-1.5.883-7 1.15V17.12l7 1.236V5.237Z"
                  fill="currentColor"
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
    --background-color: var(--wui-solid-background-base);
    background-color: var(--background-color);
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

  .app-header.with-border::after {
    content: '';
    width: 100vw;
    position: absolute;
    height: 0;
    top: 100%;
    left: 0;
    box-shadow: 0 1px 0 0.5px
      light-dark(var(--wui-control-stroke-default), var(--wui-solid-background-tertiary));
  }
  @media (prefers-color-scheme: light) {
    .app-header.with-border::before {
      content: '';
      width: 100vw;
      position: absolute;
      height: var(--height);
      top: 0;
      left: 0;
      box-shadow: 0 1px 50px 1px hsl(0deg 0% 0% / 12%);
      z-index: -1;
      background-color: var(--background-color);
    }
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
