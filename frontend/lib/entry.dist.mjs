import { confirmDialogPlugin, securityDialogPlugin } from '$dialogs';
import { redirectToFqdn } from '$utils';
import { vDropZone, vSwap } from '$utils/directives/index.mjs';
import { VueQueryPlugin } from '@tanstack/vue-query';
import { createPinia } from 'pinia';
import { createApp } from 'vue';
import App from './App.vue';
import { router } from './appRouter.ts';
import i18n from './i18n.ts';
import { useCoreDataStore } from './stores/index.mjs';

const pinia = createPinia();
await useCoreDataStore(pinia).fetchData(); // fetch core data before mounting the app

if (typeof window !== 'undefined') {
  redirectToFqdn();
}

const app = i18n(createApp(App));
app.use(pinia);
app.use(router);
app.use(VueQueryPlugin);
app.use(confirmDialogPlugin);
app.use(securityDialogPlugin);

app.directive('swap', vSwap);
app.directive('drop-zone', vDropZone);

await router.isReady();

// expose globals for use by injected scripts
if (typeof window !== 'undefined') {
  const rawebReadyEvent = new CustomEvent('RAWebReady', {
    detail: {
      app,
      router,
      components: {
        RouterLink: (await import('vue-router')).RouterLink,
        ...(await import('$components')),
      },
      vue: await import('vue'),
      stores: await import('./stores/index.mjs'),
    },
  });
  window.dispatchEvent(rawebReadyEvent);
}

const appInstance = app.mount('#app');

if (typeof window !== 'undefined') {
  appInstance.$nextTick(async () => {
    const event = new CustomEvent('RAWebAppMounted', {
      detail: {
        app,
        router,
        components: {
          RouterLink: (await import('vue-router')).RouterLink,
          ...(await import('$components')),
        },
        vue: await import('vue'),
        stores: await import('./stores/index.mjs'),
      },
    });
    window.dispatchEvent(event);
  });
}
