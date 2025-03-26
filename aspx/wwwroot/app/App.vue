<!-- Note: Install vue with npm if you want vscode to not show import errors. -->
<!-- First, set your working directiry to the /app folder. Then, run: -->
<!-- npm install vue@3.5.13 vue-router@4.5.0 devalue@5.1.1 -->
<!-- Generated files will be ignored by Git. -->

<script setup lang="ts">
  import { NavigationRail, TextBlock, Titlebar } from '$components';
  import { favoritesEnabled, simpleModeEnabled, useWebfeedData } from '$utils';
  import { getCurrentInstance, onMounted, ref, watchEffect } from 'vue';
  import Apps from './lib/pages/Apps.vue';
  import Devices from './lib/pages/Devices.vue';
  import Favorites from './lib/pages/Favorites.vue';
  import Settings from './lib/pages/Settings.vue';
  import Simple from './lib/pages/Simple.vue';

  const app = getCurrentInstance();
  const base = app?.appContext.config.globalProperties.base;
  const iisBase = app?.appContext.config.globalProperties.iisBase;

  const { data, loading, error, refresh } = useWebfeedData(iisBase);

  function removeSplashScreen() {
    const splashWrapperElem: HTMLDivElement | null = document.querySelector('.root-splash-wrapper');
    if (splashWrapperElem) {
      splashWrapperElem.style.transition = 'opacity 300ms cubic-bezier(0.16, 1, 0.3, 1)';
      splashWrapperElem.style.opacity = '0';
      setTimeout(() => {
        splashWrapperElem.remove();
      }, 300); // wait for the transition to finish before removing the element
    }

    // and update the theme color to match the app's background color instead of the splash screen color
    const themeColorMetaTags = document.querySelectorAll('meta[name="theme-color"]');
    const color = getComputedStyle(document.documentElement)
      .getPropertyValue('--wui-solid-background-base')
      .trim();
    themeColorMetaTags.forEach((metaTag) => {
      metaTag.setAttribute('content', color);
    });
  }

  async function registerServiceWorker() {
    if ('serviceWorker' in navigator) {
      try {
        const registration = await navigator.serviceWorker.register(base + 'service-worker.js', {
          scope: base,
        });
        if (registration.installing) {
          console.debug('Service worker installing');
        } else if (registration.waiting) {
          console.debug('Service worker installed');
        } else if (registration.active) {
          console.debug('Service worker active');
        }
      } catch (error) {
        console.error('Service worker registration registration failed: ', error);
      }
    }
  }

  onMounted(() => {
    registerServiceWorker();
  });

  // watch for changes to the URL hash and update the app state accordingly
  const hash = ref(window.location.hash);
  window.addEventListener('hashchange', () => {
    hash.value = window.location.hash;
  });
  watchEffect(() => {
    const allowedRoutes = ['#settings'];

    if (simpleModeEnabled.value) {
      allowedRoutes.unshift('#simple');
    } else {
      allowedRoutes.unshift('#apps', '#devices');
    }

    // add favorites to the allowed routes if enabled
    if (favoritesEnabled.value && !simpleModeEnabled.value) {
      allowedRoutes.unshift('#favorites');
    }

    // if the hash is not recognized, default to the first allowed route
    if (!hash.value || !allowedRoutes.includes(hash.value)) {
      window.location.hash = allowedRoutes[0];
    }
  });

  // track whether the component has been mounted
  const mounted = ref(false);
  onMounted(() => {
    mounted.value = true;
  });

  // hide the splash screen once data is loaded and the component is mounted
  watchEffect(() => {
    if (mounted.value && data.value && !error.value) {
      removeSplashScreen();
    }
  });

  // @ts-expect-error window.navigation exists when view transitions are supported
  const navigation = window.navigation;
  if (navigation) {
    // @ts-expect-error navigate event should be typed
    navigation.addEventListener('navigate', (event) => {
      if (event.canTransition && document.startViewTransition) {
        const mainElem = document.querySelector('main');
        const mainChildElem = mainElem ? mainElem.querySelector('div') : null;

        // hide overflow so the view transition does not fade between the scroll heights
        if (mainChildElem) {
          mainChildElem.style.overflow = 'hidden';
        }

        const transition = document.startViewTransition();

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
      }
    });
  }
</script>

<template>
  <Titlebar forceVisible />
  <div id="appContent">
    <NavigationRail v-if="!simpleModeEnabled" />
    <main :class="{ simple: simpleModeEnabled }">
      <div>
        <Favorites :data v-if="hash === '#favorites'" />
        <Devices :data v-else-if="hash === '#devices'" />
        <Apps :data v-else-if="hash === '#apps'" />
        <Simple :data v-else-if="hash === '#simple'" />
        <Settings :data v-else-if="hash === '#settings'" />
        <div v-else>
          <TextBlock variant="title">404</TextBlock>
          <br />
          <br />
          <TextBlock>Not found</TextBlock>
        </div>
      </div>
    </main>
  </div>
</template>

<style scoped>
  main {
    flex: 1;
    height: var(--content-height);
    overflow: auto;
    background-color: var(--wui-solid-background-tertiary);
    box-sizing: border-box;
    border-radius: var(--wui-overlay-corner-radius) 0 0 0;
  }
  main.simple {
    border-radius: 0;
  }

  main > div {
    --padding: 36px;
    padding: var(--padding);
    width: 100%;
    height: var(--content-height);
    box-sizing: border-box;
    view-transition-name: main;
  }
</style>

<style>
  ::view-transition-group(disabled),
  ::view-transition-old(disabled),
  ::view-transition-new(disabled) {
    animation-duration: 0s !important;
  }

  @keyframes fade-in {
    from {
      opacity: 0;
    }
  }

  @keyframes fade-out {
    to {
      opacity: 0;
    }
  }

  @keyframes entrance {
    from {
      transform: translateY(120px);
      opacity: 0;
    }
  }

  ::view-transition-old(main) {
    animation: 130ms cubic-bezier(0.16, 1, 0.3, 1) both fade-out;
  }

  ::view-transition-new(main) {
    animation: 210ms cubic-bezier(0.16, 1, 0.3, 1) 130ms both fade-in,
      380ms cubic-bezier(0.16, 1, 0.3, 1) both entrance;
  }
</style>
