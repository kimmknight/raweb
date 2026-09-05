declare global {
  interface Window {
    __pinia: import('pinia').Pinia;

    addEventListener(
      event: 'RAWebReady',
      listener: (this: Window, event: RAWebReadyEvent) => any,
      options?: boolean | AddEventListenerOptions
    ): void;
    removeEventListener(
      event: 'RAWebReady',
      listener: (this: Window, event: RAWebReadyEvent) => any,
      options?: boolean | EventListenerOptions
    ): void;

    addEventListener(
      event: 'RAWebAppMounted',
      listener: (this: Window, event: RAWebAppMountedEvent) => any,
      options?: boolean | AddEventListenerOptions
    ): void;
    removeEventListener(
      event: 'RAWebAppMounted',
      listener: (this: Window, event: RAWebAppMountedEvent) => any,
      options?: boolean | EventListenerOptions
    ): void;
  }

  interface RAWebReadyEventData {
    app: import('vue').App;
    router: import('vue-router').Router;
    stores: typeof import('./stores/index.mjs');
    components: { RouterLink: typeof import('vue-router').RouterLink } & typeof import('$components');
    vue: typeof import('vue');
  }

  interface RAWebReadyEvent extends CustomEvent<RAWebReadyEventData> {}
  interface RAWebAppMountedEvent extends CustomEvent<RAWebReadyEventData> {}
}

export {};
