<script setup lang="ts">
  import { Button, ContentDialog, TextBlock, TextBox } from '$components';
  import { useCoreDataStore } from '$stores';
  import { unproxify } from '$utils/unproxify';
  import { useTranslation } from 'i18next-vue';
  import { computed, onUnmounted, ref, useTemplateRef } from 'vue';
  import { useRouter } from 'vue-router';

  const { t } = useTranslation();
  const router = useRouter();
  const { appBase } = useCoreDataStore();

  const title = ref<string>();
  const message = ref<string>();
  const submitButtonText = ref<string>();
  const cancelButtonText = ref<string>();

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

  type ResolveValue = { done: DoneFunction; credentials: Credentials };
  let resolvePromise = ref<((value: ResolveValue | PromiseLike<ResolveValue>) => void) | null>(null);
  let rejectPromise = ref<((reason?: any) => void) | null>(null);

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
    errorMessage = ''
  ): Promise<{ done: DoneFunction; credentials: Credentials }> {
    title.value = dialogTitle;
    message.value = dialogMessage;
    submitButtonText.value = submitText;
    cancelButtonText.value = cancelText;
    submitError.value = errorMessage ? new Error(errorMessage) : null;

    return new Promise<{ done: DoneFunction; credentials: Credentials }>((resolve, reject) => {
      resolvePromise.value = resolve;
      rejectPromise.value = reject;
      open();
    });
  }

  const formFieldKey = ref<number>(0);

  const submitting = ref(false);
  const submitError = ref<Error | null>(null);
  function submit(close: () => void) {
    submitting.value = true;

    const usernameContainsDomain = username.value.includes('\\') || username.value.includes('@');

    // extract domain if included in username
    let domain = '.';
    let pureUsername = username.value;
    if (usernameContainsDomain) {
      if (username.value.includes('\\')) {
        [domain, pureUsername] = username.value.split('\\', 2);
      } else if (username.value.includes('@')) {
        [pureUsername] = username.value.split('@', 2);
      }
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
      credentials: {
        domain: domain,
        username: pureUsername,
        password: password.value,
        remember: rememberCredentials.value,
      },
    });
  }

  function cancel(reason: string | undefined = undefined) {
    rejectPromise.value?.(reason);
    submitting.value = false;
    submitError.value = null;
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

  const unregister = router.beforeEach((to, from, next) => {
    // if navigating away, close the dialog
    cancel('NAVIGATE_AWAY');
    unstable_close();
    next();
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
</script>

<template>
  <ContentDialog
    titlebar="RAWeb Security"
    :close-on-backdrop-click="false"
    :close-on-escape="false"
    :title="title"
    @close="() => cancel()"
    ref="dialog"
    :titlebar-icon="{
      light: `${appBase}lib/assets/security-icon.svg`,
      dark: `${appBase}lib/assets/security-icon-dark.svg`,
    }"
  >
    <template #default="{ close }">
      <TextBlock>{{ message }}</TextBlock>

      <form v-if="isOpen" action="" class="security-form" @keydown.enter.prevent="submit(close)">
        <TextBox
          :key="formFieldKey"
          v-model:value="username"
          type="text"
          required
          autocomplete="username"
          placeholder="User name"
        />

        <TextBox
          :key="formFieldKey"
          v-model:value="password"
          type="password"
          required
          autocomplete="current-password"
          placeholder="Password"
        />

        <!-- <CheckBox v-model:checked="rememberCredentials" style="margin-top: 4px" disabled>Remember me</CheckBox> -->
        <div class="remember-credentials-placeholder" style="margin-top: -4px"></div>

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
