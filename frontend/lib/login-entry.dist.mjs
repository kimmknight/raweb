import { createApp } from 'vue';
import Login from './Login.vue';
import i18n from './i18n.ts';

const app = i18n(createApp(Login));

app.directive('swap', (el, binding) => {
  if (el.parentNode) {
    el.outerHTML = binding.value;
  }
});

app.mount('#app');
