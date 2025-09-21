import { createPinia } from 'pinia';
import { createApp } from 'vue';
import ChangePassword from './ChangePassword.vue';
import i18n from './i18n.ts';
import { useCoreDataStore } from './stores';

const app = i18n(createApp(ChangePassword));

const pinia = createPinia();
app.use(pinia);
await useCoreDataStore(pinia).fetchData(); // fetch core data before mounting the app

app.directive('swap', (el, binding) => {
  if (el.parentNode) {
    el.outerHTML = binding.value;
  }
});

app.mount('#app');
