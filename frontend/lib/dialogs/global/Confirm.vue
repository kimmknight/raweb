<script setup lang="ts">
  import { Button, ContentDialog, InfoBar, TextBlock } from '$components';
  import { unproxify } from '$utils/unproxify';
  import { useTranslation } from 'i18next-vue';
  import { onUnmounted, ref, useTemplateRef } from 'vue';
  import { useRouter } from 'vue-router';

  const { t } = useTranslation();
  const router = useRouter();

  const title = ref<string>();
  const message = ref<string>();
  const confirmButtonText = ref<string>();
  const cancelButtonText = ref<string>();

  type DoneFunction = (status?: true | Error) => void;

  let resolvePromise = ref<((value: DoneFunction | PromiseLike<DoneFunction>) => void) | null>(null);
  let rejectPromise = ref<((reason?: any) => void) | null>(null);

  let size = ref<'min' | 'standard' | 'max' | 'maxer' | 'maxest'>('standard');
  let titlebar = ref<string>();
  let severity = ref<'attention' | 'caution' | 'critical'>();
  let emphasizeCancelButton = ref(false);
  let titlebarIcon = ref<{ light: string | null; dark: string | null }>();
  let helpAction = ref<(() => void) | undefined>(undefined);

  /**
   * Triggers the confirm dialog to be shown with the specified parameters.
   *
   * Returns a Promise that resolves if the user confirms or rejects if the user cancels.
   */
  function show(
    dialogTitle: string,
    dialogMessage: string,
    confirmText = 'OK',
    cancelText = 'Cancel',
    opts?: {
      size?: typeof size.value;
      titlebar?: typeof titlebar.value;
      severity?: typeof severity.value;
      emphasizeCancelButton?: typeof emphasizeCancelButton.value;
      titlebarIcon?: typeof titlebarIcon.value;
      helpAction?: typeof helpAction.value;
    }
  ): Promise<DoneFunction> {
    if (resolvePromise.value) {
      cancel('ALREADY_OPEN');
    }

    title.value = dialogTitle;
    message.value = dialogMessage;
    confirmButtonText.value = confirmText;
    cancelButtonText.value = cancelText;
    size.value = opts?.size ?? 'standard';
    titlebar.value = opts?.titlebar;
    severity.value = opts?.severity;
    emphasizeCancelButton.value = opts?.emphasizeCancelButton ?? false;
    titlebarIcon.value = opts?.titlebarIcon;
    helpAction.value = opts?.helpAction ?? undefined;

    return new Promise<DoneFunction>((resolve, reject) => {
      resolvePromise.value = resolve;
      rejectPromise.value = reject;
      open();
    });
  }

  const confirming = ref(false);
  const confirmError = ref<Error | null>(null);
  function confirm(close: () => void) {
    confirming.value = true;
    resolvePromise.value?.((status) => {
      if (status instanceof Error) {
        confirmError.value = status;
      } else {
        close();
      }
      confirming.value = false;
    });
  }

  function cancel(reason: string | undefined = undefined) {
    confirming.value = false;
    confirmError.value = null;
    resolvePromise.value = null;
    rejectPromise.value?.(reason);
    rejectPromise.value = null;
  }

  defineExpose({
    show,
  });

  const unregister = router.beforeEach((to, from, next) => {
    // if navigating away, close the dialog
    cancel('NAVIGATE_AWAY');
    unstable_close();
    next();
  });
  onUnmounted(() => {
    unregister();
  });

  const dialogRef = useTemplateRef('dialog');
  const open = () => unproxify(dialogRef.value)?.open();
  const unstable_close = () => unproxify(dialogRef.value)?.close();
</script>

<template>
  <ContentDialog
    :close-on-backdrop-click="false"
    @close="() => cancel()"
    ref="dialog"
    :title
    :size
    :titlebar
    :severity
    :titlebarIcon
    :help-action="helpAction"
  >
    <template #default>
      <InfoBar v-if="confirmError" severity="critical">
        <TextBlock>{{ confirmError.message }}</TextBlock>
      </InfoBar>
      <TextBlock v-else style="white-space: pre-wrap">{{ message }}</TextBlock>
    </template>
    <template #footer="{ close }">
      <Button @click="confirm(close)" :loading="confirming" v-if="!confirmError && confirmButtonText">{{
        confirmButtonText === 'Yes'
          ? t('dialog.yes')
          : confirmButtonText === 'OK'
            ? t('dialog.ok')
            : confirmButtonText
      }}</Button>
      <Button
        :variant="emphasizeCancelButton ? 'accent' : 'standard'"
        @click="
          () => {
            cancel();
            close();
          }
        "
        >{{
          confirmError
            ? t('dialog.close')
            : cancelButtonText === 'No'
              ? t('dialog.no')
              : cancelButtonText === 'Cancel'
                ? t('dialog.cancel')
                : cancelButtonText
        }}</Button
      >
    </template>
  </ContentDialog>
</template>
