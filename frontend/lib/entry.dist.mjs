import { confirmDialogPlugin, securityDialogPlugin } from '$dialogs';
import { redirectToFqdn } from '$utils';
import { VueQueryPlugin } from '@tanstack/vue-query';
import { createPinia } from 'pinia';
import { createApp } from 'vue';
import App from './App.vue';
import { router } from './appRouter.ts';
import i18n from './i18n.ts';
import { useCoreDataStore } from './stores/index.mjs';

const pinia = createPinia();
await useCoreDataStore(pinia).fetchData(); // fetch core data before mounting the app

redirectToFqdn();

const app = i18n(createApp(App));
app.use(pinia);
app.use(router);
app.use(VueQueryPlugin);
app.use(confirmDialogPlugin);
app.use(securityDialogPlugin);

app.directive('swap', (el, binding) => {
  if (el.parentNode) {
    el.outerHTML = binding.value;
  }
});

// showConfirm('hi', 'history', 'hi', 'hi');

await router.isReady();
app.mount('#app');
