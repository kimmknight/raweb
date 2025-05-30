import { createApp } from 'vue';
import App from './App.vue';
import i18n from './i18n.ts';

const app = i18n(createApp(App));

app.directive('swap', (el, binding) => {
  if (el.parentNode) {
    el.outerHTML = binding.value;
  }
});

app.mount('#app');
