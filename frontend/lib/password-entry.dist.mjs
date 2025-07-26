import { createApp } from 'vue';
import ChangePassword from './ChangePassword.vue';
import i18n from './i18n.ts';

const app = i18n(createApp(ChangePassword));

app.directive('swap', (el, binding) => {
  if (el.parentNode) {
    el.outerHTML = binding.value;
  }
});

app.mount('#app');
