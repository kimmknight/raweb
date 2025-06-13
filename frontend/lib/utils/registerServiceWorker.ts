export async function registerServiceWorker(
  onMessage?: (this: ServiceWorkerContainer, ev: MessageEvent<any>) => any
) {
  if ('serviceWorker' in navigator) {
    try {
      const registration = await navigator.serviceWorker.register(window.__base + 'service-worker.js', {
        scope: window.__base,
      });
      if (registration.installing) {
        console.debug('Service worker installing');
      } else if (registration.waiting) {
        console.debug('Service worker installed');
      } else if (registration.active) {
        console.debug('Service worker active');
        registration.active.postMessage({ type: 'variable', key: '__iisBase', value: window.__iisBase });
      }

      if (onMessage) {
        navigator.serviceWorker.addEventListener('message', onMessage);
      }

      return registration.active;
    } catch (error) {
      console.error('Service worker registration registration failed: ', error);

      if (
        error instanceof Error &&
        error.name === 'SecurityError' &&
        error.message.includes('SSL certificate error')
      ) {
        return 'SSL_ERROR';
      }
    }
  }

  return null;
}
