import { h, markRaw, ref, render } from 'vue';
import SecurityDialog from './Security.vue';

// track the singe instance of the security dialog
const securityDialogComponentInstance = ref<InstanceType<typeof SecurityDialog> | null>(null);
const container = document.createElement('div');

export const requestCredentials: InstanceType<typeof SecurityDialog>['show'] =
  /**
   * Triggers the security dialog to be shown with the specified parameters.
   *
   * Returns a Promise that resolves if the user submits credentials or rejects if the user cancels.
   */
  (title, message, submitButtonText, cancelButtonText, initialErrorMessage) => {
    if (!securityDialogComponentInstance.value) {
      console.error('Security dialog not initialized! Call initSecurityDialog() first.');
      return Promise.reject('Security dialog not initialized');
    }

    return securityDialogComponentInstance.value.show(
      title,
      message,
      submitButtonText,
      cancelButtonText,
      initialErrorMessage
    );
  };

/**
 * The Vue Plugin definition for our Security Dialog Service.
 * This will mount the SecurityDialog and make `requestCredentials` globally available.
 */
export const securityDialogPlugin = {
  install(app: ReturnType<typeof import('vue').createApp>) {
    // create a virtual node for the SecurityDialog component
    // and render it into our container
    const vnode = h(SecurityDialog);
    vnode.appContext = app._context;

    // render the virtual node into the container
    // and append it to the document body
    render(vnode, container);
    document.body.appendChild(container);

    // store the component instance for later use
    securityDialogComponentInstance.value = markRaw(vnode.component?.exposed || {}) as InstanceType<
      typeof SecurityDialog
    >;

    // provide the requestCredentials function globally
    app.provide('requestCredentials', requestCredentials);
    app.config.globalProperties.requestCredentials = requestCredentials;
  },
};
