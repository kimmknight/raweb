import { createApp } from 'vue';
import Logoff from './Logoff.vue';
import i18n from './i18n.ts';

const app = i18n(createApp(Logoff));

app.directive('swap', (el, binding) => {
  if (el.parentNode) {
    el.outerHTML = binding.value;
  }
});

app.mount('#app');
