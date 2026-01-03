import { h, markRaw, ref, render } from 'vue';
import CustomConfirmDialog from './Confirm.vue';

// track the singe instance of the confirm dialog
const confirmComponentInstance = ref<InstanceType<typeof CustomConfirmDialog> | null>(null);
const container = document.createElement('div');

export const showConfirm: InstanceType<typeof CustomConfirmDialog>['show'] =
  /**
   * Triggers the confirm dialog to be shown with the specified parameters.
   *
   * Returns a Promise that resolves if the user confirms or rejects if the user cancels.
   */
  (title, message, confirmButtonText, cancelButtonText, opts) => {
    if (!confirmComponentInstance.value) {
      console.error('Confirm dialog not initialized! Call initConfirmDialog() first.');
      return Promise.reject('Confirm dialog not initialized');
    }

    return confirmComponentInstance.value.show(title, message, confirmButtonText, cancelButtonText, opts);
  };

/**
 * The Vue Plugin definition for our Confirmation Dialog Service.
 * This will mount the CustomConfirmDialog and make `showConfirm` globally available.
 */
export const confirmDialogPlugin = {
  install(app: ReturnType<typeof import('vue').createApp>) {
    // create a virtual node for the CustomConfirmDialog component
    // and render it into our container
    const vnode = h(CustomConfirmDialog);
    vnode.appContext = app._context;

    // render the virtual node into the container
    // and append it to the document body
    render(vnode, container);
    document.body.appendChild(container);

    // store the component instance for later use
    confirmComponentInstance.value = markRaw(vnode.component?.exposed || {}) as InstanceType<
      typeof CustomConfirmDialog
    >;

    // provide the showConfirm function globally
    app.provide('showConfirm', showConfirm);
    app.config.globalProperties.showConfirm = showConfirm;
  },
};
