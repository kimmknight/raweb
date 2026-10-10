<script setup lang="ts">
  import { Button, ContentDialog, InfoBar, PickerItem, ProgressRing, TextBlock } from '$components';
  import { useCoreDataStore } from '$stores';
  import { guacdKeyboardLayouts, loadServerKeyboardLayouts, serverLayoutStorageKey } from '$utils';
  import { useTranslation } from 'i18next-vue';
  import { defineEmits, defineProps, ref, useTemplateRef } from 'vue';

  const {
    hostId,
    title,
    currentLayout,
    warnReconnect = false,
    closeOnBackdropClick = false,
    closeOnEscape = false,
  } = defineProps<{
    /** Used to remember the choice in localStorage. */
    hostId: string;
    title: string;
    currentLayout?: string;
    /** Shows a warning that applying a choice will reconnect the active session. */
    warnReconnect?: boolean;
    closeOnBackdropClick?: boolean;
    closeOnEscape?: boolean;
  }>();

  const emit = defineEmits<{
    /** Emitted when the user picks a layout. */
    (e: 'confirm', layout: string): void;
    /** Emitted whenever the dialog closes. */
    (e: 'close'): void;
  }>();

  const { t } = useTranslation();
  const { iisBase } = useCoreDataStore();

  const dialog = useTemplateRef<InstanceType<typeof ContentDialog>>('dialog');
  const selection = ref('en-us-qwerty');
  const isLoading = ref(true);

  function lastSelectedStorageKey() {
    return `${serverLayoutStorageKey(hostId)}::lastSelected`;
  }

  function handleOpen() {
    selection.value =
      currentLayout ||
      localStorage.getItem(serverLayoutStorageKey(hostId)) ||
      localStorage.getItem(`${serverLayoutStorageKey(hostId)}::lastSelected`) ||
      'en-us-qwerty';
    isLoading.value = true;
    loadServerKeyboardLayouts(iisBase)
      .catch(() => {})
      .finally(() => {
        isLoading.value = false;
      });
  }

  /**
   * Confirms the current selection.
   *
   * If `remember` is true, the choice is also saved as the connection's remembered layout.
   */
  function confirm(remember: boolean) {
    localStorage.setItem(lastSelectedStorageKey(), selection.value);
    if (remember) {
      localStorage.setItem(serverLayoutStorageKey(hostId), selection.value);
    } else {
      localStorage.removeItem(serverLayoutStorageKey(hostId));
    }
    emit('confirm', selection.value);
    dialog.value?.close();
  }
</script>

<template>
  <ContentDialog
    ref="dialog"
    :close-on-backdrop-click="closeOnBackdropClick"
    :close-on-escape="closeOnEscape"
    :title="title"
    class="server-layout-picker-dialog"
    acrylic
    max-height="48rem"
    :force-split-mode="false"
    @open="handleOpen"
    @close="emit('close')"
  >
    <template #opener="{ open }">
      <slot name="opener" :open="open" />
    </template>

    <slot />

    <InfoBar
      v-if="warnReconnect"
      severity="caution"
      :title="t('client.serverLayout.reconnectWarningTitle')"
      style="margin: 0.75rem 0"
    >
      {{ t('client.serverLayout.reconnectWarningMessage') }}
    </InfoBar>

    <div class="divider"></div>

    <div class="picker-items-loading" v-if="isLoading">
      <ProgressRing :size="24" />
    </div>
    <div class="picker-items" v-else>
      <PickerItem
        v-for="{ value, label } in guacdKeyboardLayouts"
        :key="value"
        name="server-layout-picker"
        :value
        v-model="selection"
        @dblclick="() => confirm(false)"
      >
        {{ label }}
      </PickerItem>
    </div>

    <ContentDialog :title="t('client.serverLayout.missingLayoutHelpTitle')">
      <template #opener="{ open: openHelp }">
        <Button variant="hyperlink" style="margin: 0 0.375rem" @click="openHelp">
          {{ t('client.serverLayout.missingLayoutHelpLink') }}
        </Button>
      </template>
      <TextBlock block>{{ t('client.serverLayout.missingLayoutHelpMessage') }}</TextBlock>
      <template #footer="{ close: closeHelp }">
        <Button @click="closeHelp">{{ t('dialog.close') }}</Button>
      </template>
    </ContentDialog>

    <template #footer>
      <Button @click="confirm(false)">{{ t('dialog.once') }}</Button>
      <Button @click="confirm(true)">{{ t('dialog.always') }}</Button>
    </template>
  </ContentDialog>
</template>

<style>
  .server-layout-picker-dialog .divider {
    border-top: 1px solid var(--wui-divider-stroke-default);
    margin-bottom: 0.75rem;
  }

  .server-layout-picker-dialog .picker-items {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
    padding-bottom: 0.75rem;
  }

  .server-layout-picker-dialog .picker-items-loading {
    display: flex;
    justify-content: center;
    padding: 1.5rem 0;
  }
</style>
