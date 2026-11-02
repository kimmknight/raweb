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
  import { CertificateViewerDialog, ServerLayoutDialog } from '$dialogs';
  import { certificate } from '$icons';
  import { useCoreDataStore } from '$stores';
  import {
    activeGuacdSessionCertificate,
    activeGuacdSessionLayout,
    guacdKeyboardLayoutLabel,
    loadServerKeyboardLayouts,
    restoreSplashScreen,
    simpleModeEnabled,
    useElementSize,
    useUpdateDetails,
  } from '$utils';
  import { isBrowser } from '$utils/environment.ts';
  import {
    computed,
    defineProps,
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
      const passwordChangeUrl = new URL(`${iisBase}password`, window.location.origin);
      passwordChangeUrl.searchParams.set('username', authUser.username);
      passwordChangeUrl.searchParams.set('ReturnUrl', window.location.href);
      window.location.href = passwordChangeUrl.href;
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

  const detectedServerLayout = ref<string | null>(null);
  function handleAutoDetectedDialogOpen() {
    loadServerKeyboardLayouts(iisBase)
      .then((result) => {
        detectedServerLayout.value = result.layout;
      })
      .catch(() => {});
  }

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
        <img :src="`${base}lib/assets/icon-72x72.webp`" alt="" class="logo" />
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
      <CertificateViewerDialog
        v-if="activeGuacdSessionCertificate && route.name === 'webGuacd' && !isPopup"
        :certificate-value="activeGuacdSessionCertificate.value"
      >
        <template #opener="{ open }">
          <IconButton
            class="profile-menu-button"
            :title="
              $t(
                activeGuacdSessionCertificate.isGateway
                  ? 'client.serverCertificate.gatewayTitlebarButton'
                  : 'client.serverCertificate.titlebarButton'
              )
            "
            @click="open"
          >
            <span v-html="certificate" style="display: flex; width: 24px; height: 24px"></span>
          </IconButton>
        </template>
      </CertificateViewerDialog>
      <template v-if="activeGuacdSessionLayout && route.name === 'webGuacd' && !isPopup">
        <ContentDialog
          v-if="activeGuacdSessionLayout.isAutoDetected"
          :title="$t('client.serverLayout.panelTitle')"
          @open="handleAutoDetectedDialogOpen"
        >
          <template #opener="{ open }">
            <IconButton
              class="profile-menu-button"
              :title="$t('client.serverLayout.titlebarButton')"
              @click="open"
            >
              <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                <path
                  d="M19.7454 5C20.988 5 21.9954 6.00736 21.9954 7.25V16.7546C21.9954 17.9972 20.988 19.0046 19.7454 19.0046H4.25C3.00736 19.0046 2 17.9972 2 16.7546V7.25C2 6.00736 3.00736 5 4.25 5H19.7454ZM19.7454 6.5H4.25C3.83579 6.5 3.5 6.83579 3.5 7.25V16.7546C3.5 17.1688 3.83579 17.5046 4.25 17.5046H19.7454C20.1596 17.5046 20.4954 17.1688 20.4954 16.7546V7.25C20.4954 6.83579 20.1596 6.5 19.7454 6.5ZM6.75 14.5H17.25C17.6642 14.5 18 14.8358 18 15.25C18 15.6297 17.7178 15.9435 17.3518 15.9932L17.25 16H6.75C6.33579 16 6 15.6642 6 15.25C6 14.8703 6.28215 14.5565 6.64823 14.5068L6.75 14.5H17.25H6.75ZM16.5 11C17.0523 11 17.5 11.4477 17.5 12C17.5 12.5523 17.0523 13 16.5 13C15.9477 13 15.5 12.5523 15.5 12C15.5 11.4477 15.9477 11 16.5 11ZM10.5049 11C11.0572 11 11.5049 11.4477 11.5049 12C11.5049 12.5523 11.0572 13 10.5049 13C9.95259 13 9.50488 12.5523 9.50488 12C9.50488 11.4477 9.95259 11 10.5049 11ZM7.50488 11C8.05716 11 8.50488 11.4477 8.50488 12C8.50488 12.5523 8.05716 13 7.50488 13C6.95259 13 6.50488 12.5523 6.50488 12C6.50488 11.4477 6.95259 11 7.50488 11ZM13.5049 11C14.0572 11 14.5049 11.4477 14.5049 12C14.5049 12.5523 14.0572 13 13.5049 13C12.9526 13 12.5049 12.5523 12.5049 12C12.5049 11.4477 12.9526 11 13.5049 11ZM6 8C6.55228 8 7 8.44772 7 9C7 9.55228 6.55228 10 6 10C5.44772 10 5 9.55228 5 9C5 8.44772 5.44772 8 6 8ZM8.99512 8C9.54741 8 9.99512 8.44772 9.99512 9C9.99512 9.55228 9.54741 10 8.99512 10C8.44284 10 7.99512 9.55228 7.99512 9C7.99512 8.44772 8.44284 8 8.99512 8ZM11.9951 8C12.5474 8 12.9951 8.44772 12.9951 9C12.9951 9.55228 12.5474 10 11.9951 10C11.4428 10 10.9951 9.55228 10.9951 9C10.9951 8.44772 11.4428 8 11.9951 8ZM14.9951 8C15.5474 8 15.9951 8.44772 15.9951 9C15.9951 9.55228 15.5474 10 14.9951 10C14.4428 10 13.9951 9.55228 13.9951 9C13.9951 8.44772 14.4428 8 14.9951 8ZM17.9951 8C18.5474 8 18.9951 8.44772 18.9951 9C18.9951 9.55228 18.5474 10 17.9951 10C17.4428 10 16.9951 9.55228 16.9951 9C16.9951 8.44772 17.4428 8 17.9951 8Z"
                  fill="currentColor"
                />
              </svg>
            </IconButton>
          </template>
          <TextBlock block>{{ $t('client.serverLayout.panelAutoDetected') }}</TextBlock>
          <p class="server-layout-detected-value">
            {{ guacdKeyboardLayoutLabel(detectedServerLayout || undefined) || '…' }}
          </p>
          <template v-slot:footer="{ close }">
            <Button @click="close">{{ $t('dialog.close') }}</Button>
          </template>
        </ContentDialog>
        <ServerLayoutDialog
          v-else
          :host-id="activeGuacdSessionLayout.hostId"
          :current-layout="activeGuacdSessionLayout.current"
          :title="$t('client.serverLayout.panelTitle')"
          warn-reconnect
          @confirm="(layout) => activeGuacdSessionLayout?.applyLayout(layout)"
          :close-on-backdrop-click="true"
          :close-on-escape="true"
        >
          <template #opener="{ open }">
            <IconButton
              class="profile-menu-button"
              :title="$t('client.serverLayout.titlebarButton')"
              @click="open"
            >
              <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                <path
                  d="M19.7454 5C20.988 5 21.9954 6.00736 21.9954 7.25V16.7546C21.9954 17.9972 20.988 19.0046 19.7454 19.0046H4.25C3.00736 19.0046 2 17.9972 2 16.7546V7.25C2 6.00736 3.00736 5 4.25 5H19.7454ZM19.7454 6.5H4.25C3.83579 6.5 3.5 6.83579 3.5 7.25V16.7546C3.5 17.1688 3.83579 17.5046 4.25 17.5046H19.7454C20.1596 17.5046 20.4954 17.1688 20.4954 16.7546V7.25C20.4954 6.83579 20.1596 6.5 19.7454 6.5ZM6.75 14.5H17.25C17.6642 14.5 18 14.8358 18 15.25C18 15.6297 17.7178 15.9435 17.3518 15.9932L17.25 16H6.75C6.33579 16 6 15.6642 6 15.25C6 14.8703 6.28215 14.5565 6.64823 14.5068L6.75 14.5H17.25H6.75ZM16.5 11C17.0523 11 17.5 11.4477 17.5 12C17.5 12.5523 17.0523 13 16.5 13C15.9477 13 15.5 12.5523 15.5 12C15.5 11.4477 15.9477 11 16.5 11ZM10.5049 11C11.0572 11 11.5049 11.4477 11.5049 12C11.5049 12.5523 11.0572 13 10.5049 13C9.95259 13 9.50488 12.5523 9.50488 12C9.50488 11.4477 9.95259 11 10.5049 11ZM7.50488 11C8.05716 11 8.50488 11.4477 8.50488 12C8.50488 12.5523 8.05716 13 7.50488 13C6.95259 13 6.50488 12.5523 6.50488 12C6.50488 11.4477 6.95259 11 7.50488 11ZM13.5049 11C14.0572 11 14.5049 11.4477 14.5049 12C14.5049 12.5523 14.0572 13 13.5049 13C12.9526 13 12.5049 12.5523 12.5049 12C12.5049 11.4477 12.9526 11 13.5049 11ZM6 8C6.55228 8 7 8.44772 7 9C7 9.55228 6.55228 10 6 10C5.44772 10 5 9.55228 5 9C5 8.44772 5.44772 8 6 8ZM8.99512 8C9.54741 8 9.99512 8.44772 9.99512 9C9.99512 9.55228 9.54741 10 8.99512 10C8.44284 10 7.99512 9.55228 7.99512 9C7.99512 8.44772 8.44284 8 8.99512 8ZM11.9951 8C12.5474 8 12.9951 8.44772 12.9951 9C12.9951 9.55228 12.5474 10 11.9951 10C11.4428 10 10.9951 9.55228 10.9951 9C10.9951 8.44772 11.4428 8 11.9951 8ZM14.9951 8C15.5474 8 15.9951 8.44772 15.9951 9C15.9951 9.55228 15.5474 10 14.9951 10C14.4428 10 13.9951 9.55228 13.9951 9C13.9951 8.44772 14.4428 8 14.9951 8ZM17.9951 8C18.5474 8 18.9951 8.44772 18.9951 9C18.9951 9.55228 18.5474 10 17.9951 10C17.4428 10 16.9951 9.55228 16.9951 9C16.9951 8.44772 17.4428 8 17.9951 8Z"
                  fill="currentColor"
                />
              </svg>
            </IconButton>
          </template>
          <TextBlock block>{{ $t('client.serverLayout.panelMessage') }}</TextBlock>
        </ServerLayoutDialog>
      </template>
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

  .server-layout-detected-value {
    color: var(--wui-text-primary);
    font-family: var(--wui-font-family-text);
    font-size: 13px;
    margin: 4px 0 0;
    opacity: 0.84;
  }
</style>
