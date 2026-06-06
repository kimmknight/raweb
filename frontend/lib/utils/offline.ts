import { ref } from 'vue';
import { isBrowser } from './environment';

export const offline = ref(isBrowser && 'navigator' in window ? !navigator.onLine : false);

if (isBrowser && 'navigator' in window) {
  window.addEventListener('offline', () => {
    offline.value = true;
  });

  window.addEventListener('online', () => {
    offline.value = false;
  });
}
