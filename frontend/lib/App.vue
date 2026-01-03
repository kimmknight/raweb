<!-- Note: Install vue with npm if you want vscode to not show import errors. -->
<!-- First, set your working directiry to the /app folder. Then, run: -->
<!-- npm install vue@3.5.13 vue-router@4.5.0 devalue@5.1.1 -->
<!-- Generated files will be ignored by Git. -->

<script setup lang="ts">
  import { Button, InfoBar, NavigationRail, ProgressRing, TextBlock, Titlebar } from '$components';
  import { useCoreDataStore } from '$stores';
  import {
    combineTerminalServersModeEnabled,
    registerServiceWorker,
    removeSplashScreen,
    simpleModeEnabled,
    useUpdateDetails,
    useWebfeedData,
  } from '$utils';
  import { hidePortsEnabled } from '$utils/hidePorts';
  import { useTranslation } from 'i18next-vue';
  import { computed, onMounted, ref, watch, watchEffect } from 'vue';
  import { useRouter } from 'vue-router';
  import { i18nextPromise } from './i18n';

  // TODO: requestClose: remove this logic once all browsers have supported this for some time
  const canUseDialogs = HTMLDialogElement.prototype.requestClose !== undefined;
  const falseWritableComputedRef = computed({
    get: () => false,
    set: () => {},
  });

  const router = useRouter();
  const coreAppData = useCoreDataStore();
  const { t } = useTranslation();

  const supportsCentralizedPublishing = computed(() => {
    return coreAppData.capabilities.supportsCentralizedPublishing;
  });

  const webfeedOptions = {
    mergeTerminalServers:
      canUseDialogs === false ? falseWritableComputedRef : combineTerminalServersModeEnabled,
    hidePortsWhenPossible: hidePortsEnabled,
    supportsCentralizedPublishing,
  };
  const { data, loading, error, refresh } = useWebfeedData(coreAppData.iisBase, webfeedOptions);

  // refresh the webfeed when combineTerminalServersModeEnabled changes,
  // but revert the change if there is an error (e.g., if the server is unreachable)
  let combineTerminalServersModeEnabledRevertValue: boolean | null = null;
  watch(combineTerminalServersModeEnabled, async (newValue, oldValue) => {
    // do not refresh if the value has been reverted
    if (newValue === combineTerminalServersModeEnabledRevertValue) {
      return;
    }

    // refresh the webfeed with the new value
    const { error } = await refresh(webfeedOptions);

    // if there is an error, revert the value back to the old value
    // and set the revert value to the old value to prevent an infinite loop
    if (error.value !== null) {
      if (combineTerminalServersModeEnabledRevertValue === null) {
        combineTerminalServersModeEnabledRevertValue = oldValue;
      }
      combineTerminalServersModeEnabled.value = combineTerminalServersModeEnabledRevertValue;
    }

    // if there is no error, set the revert value to null
    // because the value has been successfully changed
    // and we do not need to revert it anymore
    else {
      combineTerminalServersModeEnabledRevertValue = null;
    }
  });

  // refresh the webfeed when hidePortsEnabled changes
  let hidePortsEnabledRevertValue: boolean | null = null;
  watch(hidePortsEnabled, async (newValue, oldValue) => {
    // do not refresh if the value has been reverted
    if (newValue === hidePortsEnabledRevertValue) {
      return;
    }

    // refresh the webfeed with the new value
    const { error } = await refresh(webfeedOptions);

    // if there is an error, revert the value back to the old value
    // and set the revert value to the old value to prevent an infinite loop
    if (error.value !== null) {
      if (hidePortsEnabledRevertValue === null) {
        hidePortsEnabledRevertValue = oldValue;
      }
      hidePortsEnabled.value = hidePortsEnabledRevertValue;
    }

    // if there is no error, set the revert value to null
    // because the value has been successfully changed
    // and we do not need to revert it anymore
    else {
      hidePortsEnabledRevertValue = null;
    }
  });

  const sslError = ref(false);

  const titlebarLoading = ref(false);
  async function listenToServiceWorker(event: any) {
    if (event.data.type === 'fetch-queue') {
      const fetching = event.data.backgroundFetchQueueLength > 0;
      titlebarLoading.value = fetching;
    }
  }

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

  // track whether the component has been mounted
  const mounted = ref(false);
  onMounted(() => {
    mounted.value = true;
  });

  const canRemoveSplashScreen = computed(() => {
    return mounted.value && data.value && !error.value && i18nReady.value;
  });
  watchEffect(() => {
    if (canRemoveSplashScreen.value) {
      removeSplashScreen();
    }
  });

  // track whether the app should show animations, including view transitions
  let prefersReducedMotion = true;
  onMounted(() => {
    const prefersReducedMotionMediaQueryList = window.matchMedia('(prefers-reduced-motion: reduce)');

    function updatePrefersReducedMotion() {
      prefersReducedMotion = prefersReducedMotionMediaQueryList.matches;
    }

    prefersReducedMotionMediaQueryList.addEventListener('change', updatePrefersReducedMotion);
    prefersReducedMotion = prefersReducedMotionMediaQueryList.matches;

    return () => {
      prefersReducedMotionMediaQueryList.removeEventListener('change', updatePrefersReducedMotion);
    };
  });

  router.beforeResolve((to, from, next) => {
    const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    if (!document.startViewTransition || prefersReducedMotion) {
      return next();
    }

    // if the splash screen is visible, we should not start a view transition
    const splashScreen = document.querySelector<HTMLDivElement>('.root-splash-wrapper');
    const splashScreenVisible = splashScreen && splashScreen.style.display !== 'none';
    if (splashScreenVisible) {
      return;
    }

    const mainElem = document.querySelector('main');
    const mainChildElem = mainElem ? mainElem.querySelector('div') : null;

    // hide overflow so the view transition does not fade between the scroll heights
    if (mainChildElem) {
      mainChildElem.style.overflow = 'hidden';
    }

    const transition = document.startViewTransition(() => {
      // navigate to the new route during the view transition
      next();
    });

    // scroll to top between the before transition and the after transition
    transition.ready.then(() => {
      setTimeout(() => {
        if (mainElem && mainChildElem) {
          mainElem.scrollTo({ top: 0, left: 0, behavior: 'instant' });
          mainChildElem.style.overflow = 'unset';
        }
      }, 130);

      requestAnimationFrame(() => {
        // now everything is ready and scroll has happened
        // browser will continue with "after" animations
      });
    });
  });

  const { updateDetails, populateUpdateDetails } = useUpdateDetails();
  onMounted(() => {
    populateUpdateDetails();
  });

  const signedInUserGlobalAlerts = (() => {
    const alertsJson = coreAppData.policies.signedInUserGlobalAlerts;
    if (!alertsJson) {
      return [];
    }

    try {
      const alerts = JSON.parse(alertsJson);
      if (!Array.isArray(alerts)) {
        return [];
      }
      return alerts.filter(
        (
          alert
        ): alert is {
          title?: string;
          message?: string;
          linkText?: string;
          linkHref?: string;
          type?: 'information' | 'attention';
        } =>
          (alert && typeof alert === 'object' && typeof alert.title === 'string') ||
          (alert.title === undefined && typeof alert.message === 'string') ||
          (alert.message === undefined &&
            (typeof alert.linkText === 'string' || alert.linkText === undefined) &&
            (typeof alert.linkHref === 'string' || alert.linkHref === undefined) &&
            (alert.type === 'information' || alert.type === 'attention' || alert.type === undefined))
      );
    } catch {
      return [];
    }
  })();

  const securityErrorHelpHref = 'https://kimmknight.github.io/raweb/docs/security/error-5003/';

  function openInfoBarPopup(href: string, target: string) {
    const popup = window.open(href, target, 'width=1000,height=600,menubar=0,status=0');
    if (popup) {
      popup.focus();
    } else {
      alert('Please allow popups for this application');
    }
  }
</script>

<template>
  <Titlebar forceVisible :loading="titlebarLoading || loading" :update="updateDetails" />
  <div id="appContent">
    <NavigationRail v-if="!simpleModeEnabled" />
    <main :class="{ simple: simpleModeEnabled }">
      <InfoBar severity="caution" v-if="sslError" :title="t('securityError503.title')" style="border-radius: 0">
        {{ t('securityError503.message') }}
        <br />
        <Button
          variant="hyperlink"
          :href="securityErrorHelpHref"
          style="margin-left: -11px; margin-bottom: -6px"
          target="_blank"
          @click.prevent="openInfoBarPopup(securityErrorHelpHref, 'help')"
        >
          {{ t('securityError503.action') }}
        </Button>
      </InfoBar>

      <InfoBar
        v-for="(alert, index) in signedInUserGlobalAlerts"
        :key="index"
        :severity="alert.type || 'attention'"
        :title="alert.title"
        class="global-alert"
      >
        {{ alert.message }}
        <template v-if="alert.linkText && alert.linkHref">
          <br />
          <Button
            variant="hyperlink"
            :href="alert.linkHref"
            style="margin-left: -11px; margin-bottom: -6px"
            target="_blank"
            @click.prevent="openInfoBarPopup(alert.linkHref, alert.title || `alert-link-${index}`)"
          >
            {{ alert.linkText }}
          </Button>
        </template>
      </InfoBar>

      <div id="page">
        <router-view v-slot="{ Component }" v-if="data">
          <component
            :is="Component"
            :data="data"
            :update="updateDetails"
            :workspace="data"
            :refresh-workspace="refresh"
          />
        </router-view>
        <div v-else>
          <TextBlock variant="title">Loading</TextBlock>
          <br />
          <br />
          <div style="display: flex; gap: 8px; align-items: center">
            <ProgressRing :size="24" />
            <TextBlock style="font-weight: 500">Please wait...</TextBlock>
          </div>
        </div>
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
  main.simple {
    border-radius: 0;
  }

  main > div#page {
    --padding: 36px;
    padding: var(--padding);
    width: 100%;
    box-sizing: border-box;
    view-transition-name: main;
    flex-grow: 1;
    flex-shrink: 1;
  }

  :deep(.global-alert) {
    border-radius: 0 !important;
  }
  :deep(.global-alert .info-bar-content p) {
    flex-basis: 100%;
  }
</style>

<style>
  ::view-transition-group(disabled),
  ::view-transition-old(disabled),
  ::view-transition-new(disabled) {
    animation-duration: 0s !important;
  }

  @keyframes entrance {
    from {
      transform: translateY(120px);
      opacity: 0;
    }
  }

  ::view-transition-old(main) {
    animation: var(--wui-view-transition-fade-out) cubic-bezier(0.16, 1, 0.3, 1) both fade-out;
  }

  ::view-transition-new(main) {
    animation: var(--wui-view-transition-fade-in) cubic-bezier(0.16, 1, 0.3, 1)
        var(--wui-view-transition-fade-out) both fade-in,
      var(--wui-view-transition-slide-in) cubic-bezier(0.16, 1, 0.3, 1) both entrance;
  }
</style>
