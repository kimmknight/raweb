import { createApp } from 'vue';
import App from './App.vue';

const app = createApp(App);

app.directive('swap', (el, binding) => {
  if (el.parentNode) {
    el.outerHTML = binding.value;
  }
});

app.mount('#app');
