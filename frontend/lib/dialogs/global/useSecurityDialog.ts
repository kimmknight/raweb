import { useCoreDataStore } from '$stores';
import { isBrowser } from '$utils/environment';
import { t } from 'i18next';
import { h, markRaw, ref, render } from 'vue';
import SecurityDialog from './Security.vue';

// track the singe instance of the security dialog
const securityDialogComponentInstance = ref<InstanceType<typeof SecurityDialog> | null>(null);

export const requestCredentials: InstanceType<typeof SecurityDialog>['show'] =
  /**
   * Triggers the security dialog to be shown with the specified parameters.
   *
   * Returns a Promise that resolves if the user submits credentials or rejects if the user cancels.
   */
  (title, message, submitButtonText, cancelButtonText, initialErrorMessage, passwordOnlyPromptCredentials) => {
    if (!securityDialogComponentInstance.value) {
      console.error('Security dialog not initialized! Call app.use(securityDialogPlugin) first.');
      return Promise.reject('Security dialog not initialized');
    }

    return securityDialogComponentInstance.value.show(
      title,
      message,
      submitButtonText,
      cancelButtonText,
      initialErrorMessage,
      passwordOnlyPromptCredentials
    );
  };

type InitialErrorMessage = Parameters<InstanceType<typeof SecurityDialog>['show']>[4];
type PasswordOnlyPromptCredentials = Parameters<InstanceType<typeof SecurityDialog>['show']>[5];

type InvalidCredentialsResponse = {
  success: false;
  error?: string;
  domain?: string;
};
type ValidCredentialsResponse = {
  success: true;
  username: string;
  domain: string;
};
type CredentialsResponse = InvalidCredentialsResponse | ValidCredentialsResponse;

export const retryWithSudo =
  /**
   * Triggers the sudo security dialog to be shown with the specified parameters.
   *
   * Returns a Promise that resolves if the user submits credentials or rejects if the user cancels.
   */
  async (
    retryCallback: () => Promise<unknown>,
    passwordOnlyPromptCredentials: PasswordOnlyPromptCredentials,
    initialErrorMessage?: InitialErrorMessage | number
  ) => {
    if (!securityDialogComponentInstance.value) {
      console.error('Security dialog not initialized! Call app.use(securityDialogPlugin) first.');
      return Promise.reject('Security dialog not initialized');
    }

    const { iisBase } = useCoreDataStore();

    async function authenticateSudoUser(username: string, password: string) {
      return fetch(iisBase + 'api/auth/authenticate/sudo', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          username: username,
          password: password,
        }),
      })
        .then((res): Promise<CredentialsResponse> => res.json())
        .catch((): InvalidCredentialsResponse => ({ success: false }));
    }

    return new Promise<void | unknown>((resolve, reject) => {
      securityDialogComponentInstance
        .value!.show(
          t('security.sudoTitle'),
          t('security.sudoMessage', {
            time: new Date(new Date().getTime() + 1000 * 60 * 60).toLocaleTimeString([], {
              hour: 'numeric',
              minute: '2-digit',
            }),
          }),
          t('dialog.ok'),
          t('dialog.cancel'),
          typeof initialErrorMessage === 'number'
            ? initialErrorMessage === 0
              ? undefined
              : t('security.sudoGenericError')
            : initialErrorMessage,
          passwordOnlyPromptCredentials,
          async (credentials) => {
            const response = await authenticateSudoUser(credentials.username, credentials.password);
            if (!response.success) {
              throw new Error(response.error ? t(response.error) : t('security.sudoGenericError'));
            }
            return true;
          }
        )
        .then(async ({ done, credentials }) => {
          const response = await authenticateSudoUser(credentials.username, credentials.password);

          if (!response.success) {
            done(new Error(response.error ? t(response.error) : t('security.sudoGenericError')));
          } else {
            done();
            resolve(await retryCallback());
          }
        })
        .catch(() => {
          console.log('Sudo authentication canceled by user.');
          reject();
        });
    });
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

    if (isBrowser) {
      let container = document.querySelector('div#securityDialogPlugin');
      if (!container) {
        container = document.createElement('div');
        container.id = 'securityDialogPlugin';
        document.body.appendChild(container);
      }

      // render the virtual node into the container
      // and append it to the document body
      render(vnode, container);

      // store the component instance for later use
      securityDialogComponentInstance.value = markRaw(vnode.component?.exposed || {}) as InstanceType<
        typeof SecurityDialog
      >;
    } else {
      securityDialogComponentInstance.value = null;
    }

    // provide the requestCredentials function globally
    app.provide('requestCredentials', requestCredentials);
    app.config.globalProperties.requestCredentials = requestCredentials;
  },
};
