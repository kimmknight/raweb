<script setup lang="ts">
  import { Button, ContentDialog, InfoBar, TextBlock } from '$components';
  import { unproxify } from '$utils/unproxify';
  import { useTranslation } from 'i18next-vue';
  import { ref, useTemplateRef } from 'vue';

  const { t } = useTranslation();

  const title = ref<string>();
  const message = ref<string>();
  const confirmButtonText = ref<string>();
  const cancelButtonText = ref<string>();

  type DoneFunction = (status?: true | Error) => void;

  let resolvePromise = ref<((value: DoneFunction | PromiseLike<DoneFunction>) => void) | null>(null);
  let rejectPromise = ref<((reason?: any) => void) | null>(null);

  /**
   * Triggers the confirm dialog to be shown with the specified parameters.
   *
   * Returns a Promise that resolves if the user confirms or rejects if the user cancels.
   */
  function show(
    dialogTitle: string,
    dialogMessage: string,
    confirmText = 'OK',
    cancelText = 'Cancel'
  ): Promise<DoneFunction> {
    title.value = dialogTitle;
    message.value = dialogMessage;
    confirmButtonText.value = confirmText;
    cancelButtonText.value = cancelText;

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

  function cancel() {
    rejectPromise.value?.();
    confirming.value = false;
    confirmError.value = null;
  }

  defineExpose({
    show,
  });

  const dialogRef = useTemplateRef('dialog');
  const open = () => unproxify(dialogRef.value)?.open();
</script>

<template>
  <ContentDialog :close-on-backdrop-click="false" @close="cancel" ref="dialog" :title>
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
