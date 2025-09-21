import { useCoreDataStore } from '$stores';

export async function registerServiceWorker(
  onMessage?: (this: ServiceWorkerContainer, ev: MessageEvent<any>) => any
) {
  if ('serviceWorker' in navigator) {
    try {
      const { appBase, iisBase } = useCoreDataStore();
      const registration = await navigator.serviceWorker.register(appBase + 'service-worker.js', {
        scope: appBase,
      });
      if (registration.installing) {
        console.debug('Service worker installing');
      } else if (registration.waiting) {
        console.debug('Service worker installed');
      } else if (registration.active) {
        console.debug('Service worker active');
        registration.active.postMessage({ type: 'variable', key: '__iisBase', value: iisBase });
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
