<script setup lang="ts">
  import { Button, ContentDialog, TextBlock, TextBox } from '$components';
  import { useCoreDataStore } from '$stores';
  import { unproxify } from '$utils/unproxify';
  import { useTranslation } from 'i18next-vue';
  import { computed, onUnmounted, ref, useTemplateRef, watchEffect } from 'vue';
  import { useRouter } from 'vue-router';

  const { t } = useTranslation();
  const router = useRouter();
  const { appBase } = useCoreDataStore();

  const title = ref<string>();
  const message = ref<string>();
  const submitButtonText = ref<string>();
  const cancelButtonText = ref<string>();
  const passwordOnlyPromptCredentials = ref<PasswordOnlyPromptCredentials | null>(null);
  const useAcrylicBackdrop = ref<boolean>(false);

  const username = ref<string>('');
  const password = ref<string>('');
  const rememberCredentials = ref<boolean>(false);

  type DoneFunction = (status?: true | Error) => void;
  interface Credentials {
    domain: string;
    username: string;
    password: string;
    remember: boolean;
  }

  interface PasswordOnlyPromptCredentials {
    displayName: string;
    domain: string;
    username: string;
  }

  type ResolveValue = { done: DoneFunction; credentials: Credentials };
  let resolvePromise = ref<((value: ResolveValue | PromiseLike<ResolveValue>) => void) | null>(null);
  let rejectPromise = ref<((reason?: any) => void) | null>(null);
  let beforeResolve = ref<((credentials: Credentials) => Promise<boolean>) | undefined>(undefined);

  /**
   * Triggers the confirm dialog to be shown with the specified parameters.
   *
   * Returns a Promise that resolves if the user confirms or rejects if the user cancels.
   */
  function show(
    dialogTitle: string,
    dialogMessage: string,
    submitText = 'OK',
    cancelText = 'Cancel',
    errorMessage = '',
    /**
     * When provided, the dialog will only prompt for a password.
     */
    _passwordOnlyPromptCredentials: PasswordOnlyPromptCredentials | null = null,
    /**
     * When provided, this function will be called before the dialog resolves.
     *
     * If it returns false, the dialog will not resolve and will remain open.
     *
     * If it returns true, the dialog will resolve and close.
     */
    _beforeResolve?: (credentials: Credentials) => Promise<boolean>,
    _useAcrylicBackdrop?: boolean
  ): Promise<{ done: DoneFunction; credentials: Credentials }> {
    if (resolvePromise.value) {
      cancel('ALREADY_OPEN');
    }

    title.value = dialogTitle;
    message.value = dialogMessage;
    submitButtonText.value = submitText;
    cancelButtonText.value = cancelText;
    passwordOnlyPromptCredentials.value = _passwordOnlyPromptCredentials;
    if (_passwordOnlyPromptCredentials) {
      username.value = `${_passwordOnlyPromptCredentials.domain}\\${_passwordOnlyPromptCredentials.username}`;
    }
    submitError.value = errorMessage ? new Error(errorMessage) : null;
    beforeResolve.value = _beforeResolve;
    useAcrylicBackdrop.value = _useAcrylicBackdrop ?? false;

    return new Promise<{ done: DoneFunction; credentials: Credentials }>((resolve, reject) => {
      resolvePromise.value = resolve;
      rejectPromise.value = reject;
      open();
    });
  }

  const formFieldKey = ref<number>(0);

  const submitting = ref(false);
  const submitError = ref<Error | null>(null);
  async function submit(close: () => void) {
    if (submitting.value) {
      return;
    }

    submitting.value = true;

    const usernameContainsDomain = username.value.includes('\\') || username.value.includes('@');

    // extract domain if included in username
    let domain = '.';
    let pureUsername = username.value;
    if (usernameContainsDomain) {
      if (username.value.includes('\\')) {
        [domain, pureUsername] = username.value.split('\\', 2);
      } else if (username.value.includes('@')) {
        [pureUsername, domain] = username.value.split('@', 2);
      }
    }
    pureUsername = pureUsername.trim();
    domain = domain.trim();

    if (!domain) {
      domain = '.';
    }
    if (!pureUsername) {
      submitError.value = new Error(t('usernameRequired'));
      submitting.value = false;
      return;
    }
    if (!password.value) {
      submitError.value = new Error(t('passwordRequired'));
      submitting.value = false;
      return;
    }

    const credentials: Credentials = {
      domain,
      username: pureUsername,
      password: password.value,
      remember: rememberCredentials.value,
    };

    const shouldResolveCallbackPromise = beforeResolve.value
      ? beforeResolve.value(credentials)
      : Promise.resolve(true);
    shouldResolveCallbackPromise
      .then((shouldResolve) => {
        if (!shouldResolve) {
          submitting.value = false;
          return;
        }

        resolvePromise.value?.({
          done: (status) => {
            if (status instanceof Error) {
              submitError.value = status;
            } else {
              close();
              cleanup();
            }
            submitting.value = false;
          },
          credentials,
        });
      })
      .catch((error) => {
        submitError.value = error instanceof Error ? error : new Error(String(error));
        submitting.value = false;
      });
  }

  function cancel(reason: string | undefined = undefined) {
    submitting.value = false;
    submitError.value = null;
    rejectPromise.value?.(reason);
    cleanup();
  }

  function cleanup() {
    resolvePromise.value = null;
    rejectPromise.value = null;
    username.value = '';
    password.value = '';
    rememberCredentials.value = false;
    formFieldKey.value += 1; // incrementing the key tells Vue to recreate the input fields, clearing the browser's autofill state
  }

  const unregister = router.beforeEach((to, from) => {
    // if navigating away, close the dialog
    cancel('NAVIGATE_AWAY');
    unstable_close();
  });
  onUnmounted(() => {
    unregister();
  });

  defineExpose({
    show,
  });

  const dialogRef = useTemplateRef('dialog');
  const open = () => unproxify(dialogRef.value)?.open();
  const isOpen = computed(() => unproxify(dialogRef.value)?.isOpen);
  const unstable_close = () => unproxify(dialogRef.value)?.close();

  const delayedIsOpen = ref(isOpen.value);
  watchEffect(() => {
    if (isOpen.value) {
      delayedIsOpen.value = true;
    } else {
      // wait for close animation to finish
      setTimeout(() => {
        if (!isOpen.value) {
          delayedIsOpen.value = false;
        }
      }, 500);
    }
  });
</script>

<template>
  <ContentDialog
    :titlebar="t('security.dialogTitle')"
    :close-on-backdrop-click="false"
    :close-on-escape="false"
    :title="title"
    @close="() => cancel()"
    ref="dialog"
    :titlebar-icon="{
      light: `${appBase}lib/assets/security-icon.svg`,
      dark: `${appBase}lib/assets/security-icon-dark.svg`,
    }"
    :acrylic-backdrop="useAcrylicBackdrop"
  >
    <template #default="{ close }">
      <TextBlock>{{ message }}</TextBlock>

      <form v-if="delayedIsOpen" action="" class="security-form" @keydown.enter.prevent="submit(close)">
        <TextBlock v-if="passwordOnlyPromptCredentials">
          {{ passwordOnlyPromptCredentials.displayName }}
        </TextBlock>
        <TextBox
          v-else
          :key="`username-${formFieldKey}`"
          v-model:value="username"
          type="text"
          required
          autocomplete="username"
          :placeholder="t('security.username')"
        />

        <label>
          <TextBlock v-if="passwordOnlyPromptCredentials" style="margin-bottom: 0.375rem">{{
            t('security.password')
          }}</TextBlock>
          <TextBox
            :key="`password-${formFieldKey}`"
            v-model:value="password"
            type="password"
            required
            autocomplete="current-password"
            :placeholder="passwordOnlyPromptCredentials ? '' : t('security.password')"
          />
        </label>

        <TextBlock v-if="passwordOnlyPromptCredentials">
          {{ passwordOnlyPromptCredentials.domain }}\{{ passwordOnlyPromptCredentials.username }}
        </TextBlock>

        <!-- <CheckBox v-model:checked="rememberCredentials" style="margin-top: 4px" disabled>Remember me</CheckBox> -->
        <div v-if="!submitError" class="remember-credentials-placeholder" style="margin-top: -4px"></div>

        <TextBlock v-if="submitError" style="margin-top: 4px; color: var(--wui-text-error)">{{
          submitError.message
        }}</TextBlock>
      </form>
    </template>

    <template #footer="{ close }">
      <Button variant="accent" @click="submit(close)" :loading="submitting">
        {{
          submitButtonText === 'Yes'
            ? t('dialog.yes')
            : submitButtonText === 'OK'
              ? t('dialog.ok')
              : submitButtonText
        }}
      </Button>
      <Button
        @click="
          cancel();
          close();
        "
      >
        {{
          cancelButtonText === 'No'
            ? t('dialog.no')
            : cancelButtonText === 'Cancel'
              ? t('dialog.cancel')
              : cancelButtonText
        }}
      </Button>
    </template>
  </ContentDialog>
</template>

<style scoped>
  .security-form {
    display: flex;
    flex-direction: column;
    gap: 12px;
    margin: 28px 0 0 0;
  }
</style>
