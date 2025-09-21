<script setup lang="ts">
  import { ProgressRing, TextBlock, Titlebar } from '$components';
  import { useCoreDataStore } from '$stores';
  import { removeSplashScreen } from '$utils';
  import { computed, onMounted, ref, watchEffect } from 'vue';
  import { i18nextPromise } from './i18n';

  const { iisBase } = useCoreDataStore();

  const failed = ref(false);

  onMounted(async () => {
    // clear localStorage data keys
    Object.keys(localStorage)
      .filter((key) => key.includes(':data'))
      .forEach((key) => {
        localStorage.removeItem(key);
      });

    // clear service worker cache
    if ('caches' in window) {
      const cacheNames = await caches.keys();
      await Promise.all(
        cacheNames.map((cacheName) => {
          return caches.delete(cacheName);
        })
      );
    }

    await fetch(iisBase + 'api/auth/clear', {
      method: 'GET',
      credentials: 'include',
    }).then((response) => {
      if (!response.ok) {
        failed.value = true;
        throw new Error(`Failed to clear authentication: ${response.statusText}`);
      }
    });

    const redirectHref = iisBase + 'login';
    const returnUrl = new URLSearchParams(window.location.search).get('ReturnUrl');
    const redirectUrl = new URL(redirectHref, window.location.origin);
    if (returnUrl) {
      redirectUrl.searchParams.set('ReturnUrl', returnUrl);
    }
    window.location.href = redirectUrl.href;
  });

  // track whether i18n is ready
  const i18nReady = ref(false);
  i18nextPromise.then(() => {
    i18nReady.value = true;
  });

  // do not close splash screen unless it has been at least 1 second
  const minSplashScreenDuration = 700; // the sign out button waits 300ms for the splash screen to appear
  const durationPassed = ref(false);
  setTimeout(() => {
    durationPassed.value = true;
  }, minSplashScreenDuration);

  // remove the splash screen once everything is ready
  const canRemoveSplashScreen = computed(() => {
    return i18nReady.value && durationPassed.value;
  });
  watchEffect(() => {
    if (canRemoveSplashScreen.value) {
      removeSplashScreen();
    }
  });
</script>

<template>
  <Titlebar :title="$t('signOut.title')" forceVisible hideProfileMenu withBorder />
  <div v-if="failed">
    <TextBlock variant="subtitle" tag="h1">{{
      $t('signOut.failed', { defaultValue: 'Failed to sign out. Please try again later.' })
    }}</TextBlock>
  </div>
  <div v-else>
    <ProgressRing :size="48" />
    <TextBlock variant="subtitle" tag="h1">{{
      $t('signOut.title', { defaultValue: 'Signing out' })
    }}</TextBlock>
  </div>
</template>

<style scoped>
  div {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 24px;
    height: calc(100% - var(--header-height) / 2);
  }
</style>
