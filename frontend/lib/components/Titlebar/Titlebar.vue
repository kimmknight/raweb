<script setup lang="ts">
  import {
    AnimatedIcon,
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
  import { restoreSplashScreen, simpleModeEnabled, useElementSize, useUpdateDetails } from '$utils';
  import { isBrowser } from '$utils/environment.ts';
  import {
    computed,
    nextTick,
    onMounted,
    onUnmounted,
    ref,
    type UnwrapRef,
    useTemplateRef,
    watch,
    watchEffect,
  } from 'vue';
  import { useRoute, useRouter } from 'vue-router';
  import { DraggableWindowAreas } from './DraggableWindowAreas';
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
  const supportsAnchorPositions = isBrowser && CSS.supports('position-area', 'center center');

  const isPopup = computed(() => isBrowser && window.opener && window.opener !== window);

  const needsCustomTitlebar = ref(true);
  const customTitlebarCleanupFunction = ref<() => void>();
  onMounted(() => {
    // hide the header if the display mode is not window-controls-overlay
    const isWindowControlsOverlayMode = window.matchMedia('(display-mode: window-controls-overlay)').matches;
    const isStandaloneMode = window.matchMedia('(display-mode: standalone)').matches;
    if ((!isWindowControlsOverlayMode || isStandaloneMode) && !forceVisible) {
      needsCustomTitlebar.value = false;
    } else {
      needsCustomTitlebar.value = true;
    }

    const isWindowControlsOverlayMediaQueryList = window.matchMedia('(display-mode: window-controls-overlay)');
    function handleWindowControlsOverlayChange(event: MediaQueryListEvent) {
      if (event.matches) {
        // in window controls overlay mode, show the header
        needsCustomTitlebar.value = true;
      } else {
        // not in window controls overlay mode, show the header
        needsCustomTitlebar.value = false;
      }
    }

    const isStandaloneMediaQueryList = window.matchMedia('(display-mode: standalone)');
    function handleStandaloneChange(event: MediaQueryListEvent) {
      if (event.matches) {
        // in standalone mode, hide the header
        needsCustomTitlebar.value = false;
      } else {
        // not in standalone mode, show the header
        needsCustomTitlebar.value = true;
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

    customTitlebarCleanupFunction.value = () => {
      // clean up event listeners on component unmount
      isWindowControlsOverlayMediaQueryList.removeEventListener('change', handleWindowControlsOverlayChange);
      isStandaloneMediaQueryList.removeEventListener('change', handleStandaloneChange);
      window.removeEventListener('focus', handleFocus);
      window.removeEventListener('blur', handleBlur);
    };
  });
  onUnmounted(() => {
    if (customTitlebarCleanupFunction.value) {
      customTitlebarCleanupFunction.value();
    }
  });

  // track whether the window is full screen
  const isFullScreen = ref(false);
  function checkFullscreen() {
    const cssScreenWidth = parseInt((screen.width / window.devicePixelRatio).toFixed(0));
    const cssScreenHeight = parseInt((screen.height / window.devicePixelRatio).toFixed(0));

    const innerWidth = parseInt(window.innerWidth.toFixed(0));
    const innerHeight = parseInt(window.innerHeight.toFixed(0));

    isFullScreen.value =
      (innerHeight === cssScreenHeight && innerWidth === cssScreenWidth) ||
      (innerHeight === cssScreenWidth && innerWidth === cssScreenHeight);
  }
  onMounted(() => {
    checkFullscreen();
    window.addEventListener('resize', checkFullscreen);
  });
  onUnmounted(() => {
    window.removeEventListener('resize', checkFullscreen);
  });

  const shouldShowTitlebar = computed(() => {
    return (
      (needsCustomTitlebar.value || DraggableWindowAreas.isWebview2Available(window)) && !isFullScreen.value
    );
  });
  function setTitlebarHeight() {
    if (shouldShowTitlebar.value) {
      document.body.style.setProperty(
        '--header-height',
        'max(env(titlebar-area-height, 32px), var(--titlebar-area-height, 32px))'
      );
    } else {
      document.body.style.setProperty('--header-height', '0px');
    }
  }
  onMounted(() => {
    setTitlebarHeight();
  });
  watch(shouldShowTitlebar, (s) => {
    setTitlebarHeight();
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
  function addKeyboardListeners(event: KeyboardEvent) {
    if (event.altKey && event.key === 'l') {
      event.preventDefault(); // prevent default action to avoid any conflicts with other shortcuts
      signOut();
    }
  }
  onMounted(() => {
    window.addEventListener('keydown', addKeyboardListeners);
  });
  onUnmounted(() => {
    window.removeEventListener('keydown', addKeyboardListeners);
  });

  function goBack() {
    route.meta.isTitlebarBackButton = true;

    if (route.path.startsWith('/settings') || route.name === 'webGuacd') {
      router.back();
    } else {
      router.push('/simple');
    }
  }

  // set the app title
  const appTitle = ref(isBrowser ? document.title : 'RAWeb');
  if (isBrowser) {
    router.afterEach(() => {
      nextTick(() => {
        appTitle.value = document.title;
      });
    });
  }

  const draggableWindowAreas = ref<DraggableWindowAreas>();
  onMounted(() => {
    if (!DraggableWindowAreas.isWebview2Available(window)) {
      return;
    }

    draggableWindowAreas.value = new DraggableWindowAreas(false);
  });
  function restoreStandardTitlebarDragArea() {
    if (draggableWindowAreas.value) {
      // restore the standard titlebar drag area before we remove the titlebar vue component
      draggableWindowAreas.value.set('base', {
        x: 0,
        y: 0,
        w: window.innerWidth,
        h: appHeaderSize.value.height,
        singleUse: false,
      });
    }
  }
  onUnmounted(() => {
    if (draggableWindowAreas.value) {
      // restore the standard titlebar drag area before we remove the titlebar vue component
      restoreStandardTitlebarDragArea();

      draggableWindowAreas.value.dispose();
    }
  });

  var appHeaderElement = useTemplateRef('appHeader');
  var rightActionsElement = useTemplateRef('rightActions');
  var backButtonElement = useTemplateRef('backButton');
  var appHeaderSize = useElementSize(appHeaderElement);
  var rightActionsSize = useElementSize(rightActionsElement);
  var backButtonSize = useElementSize(backButtonElement);
  watchEffect(() => {
    if (!draggableWindowAreas.value) {
      return;
    }

    // maintain a draggable area that covers the entire titlebar noninteractive titlebar
    draggableWindowAreas.value.set('base', {
      x: backButtonSize.value.width,
      y: 0,
      w: appHeaderSize.value.width - rightActionsSize.value.width - backButtonSize.value.width,
      h: appHeaderSize.value.height,
      singleUse: false,
    });
  });

  var showBackButton = computed(() => {
    return (
      (simpleModeEnabled.value && route.path.startsWith('/settings')) ||
      (route.name === 'webGuacd' && !isPopup.value)
    );
  });

  // when the back button is shown, we need to move the location of the icon menu
  watchEffect(() => {
    if (!draggableWindowAreas.value) {
      return;
    }

    if (showBackButton.value) {
      draggableWindowAreas.value.offsetSysMenuPosition(backButtonSize.value.width - 8);
    } else {
      draggableWindowAreas.value.offsetSysMenuPosition(0);
    }
  });

  // when the appHeader element is not visible (e.g. due to a parent's display: none,
  // or because v-if removed it), we need to restore the standard drag area so that
  // the user can still drag the window
  watchEffect((onCleanup) => {
    const element = appHeaderElement.value;
    const areas = draggableWindowAreas.value;
    if (!element || !areas) {
      return;
    }

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.target !== element) {
            return;
          }

          if (entry.intersectionRatio === 0) {
            // titlebar is not visible; restore the standard titlebar drag area
            restoreStandardTitlebarDragArea();
          } else {
            // titlebar is visible; set the custom titlebar drag area
            areas.set('base', {
              x: backButtonSize.value.width,
              y: 0,
              w: appHeaderSize.value.width - rightActionsSize.value.width - backButtonSize.value.width,
              h: appHeaderSize.value.height,
              singleUse: false,
            });
          }
        });
      },
      { threshold: [0, 1] }
    );

    observer.observe(element);
    onCleanup(() => observer.disconnect());
  });
</script>

<template>
  <div
    ref="appHeader"
    :class="`app-header ${withBorder ? 'with-border' : ''}`"
    v-if="shouldShowTitlebar"
    @pointerdown="draggableWindowAreas?.setAroundJsDrag"
  >
    <div class="left">
      <span class="back-wrapper" v-if="showBackButton" ref="backButton" @pointerdown.stop>
        <IconButton :onclick="goBack" class="profile-menu-button" title="Go back">
          <AnimatedIcon.Back />
        </IconButton>
      </span>
      <span class="logo-wrapper" @pointerdown.stop>
        <img :src="`${base}lib/assets/icon.svg`" alt="" class="logo" />
      </span>
      <span class="title">
        <TextBlock variant="caption">{{ appTitle }}</TextBlock>
      </span>
      <ProgressRing :size="16" style="padding: 0 8px" v-if="loading" />
    </div>
    <div class="right" ref="rightActions">
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
      <RouterLink
        to="/settings"
        custom
        v-slot="settingsLinkProps"
        v-if="!hideProfileMenu && simpleModeEnabled && !route.path.startsWith('/settings')"
      >
        <IconButton
          v-if="settingsLinkProps"
          :href="settingsLinkProps.href"
          class="profile-menu-button"
          title="Open settings"
          @click="settingsLinkProps.navigate"
        >
          <AnimatedIcon.Settings />
        </IconButton>
      </RouterLink>
      <RouterLink
        to="/simple"
        custom
        v-slot="simpleLinkProps"
        v-else-if="!hideProfileMenu && simpleModeEnabled"
      >
        <IconButton
          v-if="simpleLinkProps"
          :href="simpleLinkProps.href"
          class="profile-menu-button"
          title="Back to apps and devices list"
          @click="simpleLinkProps.navigate"
        >
          <AnimatedIcon.Home />
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
    --header-height: max(env(titlebar-area-height, 32px), var(--titlebar-area-height, 32px));
    margin: 0;
  }
  #app {
    --content-height: calc(
      100vh - var(--header-height) - env(safe-area-inset-top, 0px) - env(safe-area-inset-bottom, 0px)
    );
  }
</style>

<style scoped>
  .app-header {
    --height: var(--header-height);
    --background-color: var(--window-background-color);
    /* background-color: var(--background-color); */
    color: var(--wui-text-primary);
    height: var(--height);
    display: flex;
    flex-direction: row;
    align-items: center;
    justify-content: space-between;
    padding: 0 2px 0 0;
    font-size: 13px;
    flex-grow: 0;
    flex-shrink: 0;
    user-select: none;
    -webkit-app-region: drag;
    position: relative;
    font-family: var(--wui-font-family-text);
    font-size: var(--wui-font-size-caption);
    width: env(titlebar-area-width, calc(100% - var(--titlebar-caption-buttons-width, 0px)));
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
      /* background-color: var(--background-color); */
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

  .app-header .logo-wrapper {
    width: var(--titlebar-icon-area-width, 48px);
    display: flex;
    align-items: center;
    justify-content: center;
  }

  .app-header .back-wrapper {
    display: flex;
    align-items: center;
    margin-right: -8px;
    width: 38px;
    justify-content: right;
  }

  .app-header img.logo {
    block-size: 16px;
    object-fit: cover;
    -webkit-user-drag: none;
  }

  .app-header .title {
    padding: 0 8px 0 0;
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
    top: max(env(titlebar-area-height, 32px), var(--titlebar-area-height, 32px));
    left: calc(100vw - 172px);
  }
</style>
