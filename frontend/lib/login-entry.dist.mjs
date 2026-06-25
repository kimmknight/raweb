import { redirectToFqdn } from '$utils';
import i18next from 'i18next';
import { createPinia } from 'pinia';
import { createApp } from 'vue';
import { createRouter, createWebHistory } from 'vue-router';
import i18n, { i18nextPromise } from './i18n.ts';
import Login from './Login.vue';
import { useCoreDataStore } from './stores/index.mjs';

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/:pathMatch(.*)*', redirect: '/login' },
    { path: '/login', component: Login },
  ],
});

const pinia = createPinia();
await useCoreDataStore(pinia).fetchData(); // fetch core data before mounting the app

const forcedLanguage = useCoreDataStore(pinia).policies?.forcedLanguage;
if (typeof forcedLanguage === 'string' && forcedLanguage) {
  await i18nextPromise;
  await i18next.changeLanguage(forcedLanguage);
}

if (typeof window !== 'undefined') {
  redirectToFqdn();
}

const app = i18n(createApp(Login));
app.use(pinia);
app.use(router);

app.directive('swap', (el, binding) => {
  if (el.parentNode) {
    el.outerHTML = binding.value;
  }
});

await router.isReady();
app.mount('#app');
