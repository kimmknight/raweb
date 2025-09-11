<script setup lang="ts">
  import { Button, ContentDialog, PickerItem, TextBlock } from '$components';
  import { raw } from '$utils';
  import { useTranslation } from 'i18next-vue';
  import { computed, ref, useTemplateRef } from 'vue';
  import {
    appStoreBadgeDark,
    appStoreBadgeLight,
    macAppStoreBadgeDark,
    macAppStoreBadgeLight,
  } from './badges.ts';

  const { t } = useTranslation();

  interface OnCloseParameters {
    selectedMethod: (typeof allMethods)['value'][number]['id'];
  }

  const props = defineProps<{
    resourceTitle: string;
    allowRememberMethod?: boolean;
  }>();

  const emit = defineEmits<{
    (e: 'close', params: OnCloseParameters): void;
  }>();

  const allMethods = computed(() => {
    return [
      { id: 'rdpFile', label: t('resource.methodPicker.rdpFile') },
      { id: 'rdpProtocolUri', label: t('resource.methodPicker.rdpProtocolUri') },
    ] as { id: 'rdpFile' | 'rdpProtocolUri'; label: string }[];
  });

  const methodPickerDialog = useTemplateRef<typeof ContentDialog>('methodPickerDialog');
  const openDialog = computed(() => raw(methodPickerDialog.value)?.open);
  const closeDialog = computed(() => raw(methodPickerDialog.value)?.close);
  const dialogIsOpen = computed(() => raw(methodPickerDialog.value)?.isOpen);
  const popoverId = computed(() => raw(methodPickerDialog.value)?.popoverId);
  const selectedMethod = ref<OnCloseParameters['selectedMethod']>('rdpFile');
  const methods = ref(allMethods.value);
  const memoryKey = `${window.__namespace}::preferredConnectionMethod`;
  const lastKey = `${window.__namespace}::lastConnectionMethod`;

  const downloadRdpProtocolHandlerAppDialog = useTemplateRef<typeof ContentDialog>(
    'downloadRdpProtocolHandlerAppDialog'
  );
  const openDownloadRdpProtocolHandlerAppDialog = computed(
    () => raw(downloadRdpProtocolHandlerAppDialog.value)?.open
  );
  const closeDownloadRdpProtocolHandlerAppDialog = computed(
    () => raw(downloadRdpProtocolHandlerAppDialog.value)?.close
  );

  const downloadWindowsAppDialog = useTemplateRef<typeof ContentDialog>('downloadWindowsAppDialog');
  const openWindowsAppDialog = computed(() => raw(downloadWindowsAppDialog.value)?.open);
  const closeWindowsAppDialog = computed(() => raw(downloadWindowsAppDialog.value)?.close);

  const showAppDownloadKey = `${window.__namespace}::shouldShowDownloadRdpProtocolHandlerAppDialog`;

  const isWindows = window.navigator.platform.startsWith('Win');
  const isMacOS = window.navigator.platform.startsWith('MacIntel') && window.navigator.maxTouchPoints === 0;
  const isIOS = window.navigator.platform.startsWith('MacIntel') && window.navigator.maxTouchPoints > 1;
  const isDarkMode = window.matchMedia('(prefers-color-scheme: dark)').matches;

  /**
   * Emits the selected connection method and closes the dialog.
   * If the user has chosen to remember their method, it will be stored in localStorage.
   * If the user has chosen to only use a method once, RAWeb will be configured
   * to show the method picker dialog again next time.
   * @param rememberMethod  Whether RAWeb should remember the selected method (stored in localStorage). If null, the localStorage entry will not be modified.
   */
  function submit(rememberMethod: boolean | null = false) {
    // on first attempt to connect with the rdp:// protocol method, show a dialog with instructions to download the RDP protocol handler app
    const shouldShowAppDownloadDialog =
      (isWindows || isMacOS || isIOS) &&
      selectedMethod.value === 'rdpProtocolUri' &&
      localStorage.getItem(showAppDownloadKey) !== 'false';
    if (shouldShowAppDownloadDialog) {
      if (isWindows) {
        openDownloadRdpProtocolHandlerAppDialog.value?.();
      } else if (isMacOS || isIOS) {
        openWindowsAppDialog.value?.();
      }
      localStorage.setItem(showAppDownloadKey, 'false');
      return;
    } else {
      closeDownloadRdpProtocolHandlerAppDialog.value?.();
      closeWindowsAppDialog.value?.();
    }

    closeDialog.value?.();

    // if the user has chosen to remember their method ("always"), store it in localStorage
    if (rememberMethod) {
      localStorage.setItem(memoryKey, selectedMethod.value);
    } else if (rememberMethod === false) {
      // remove when the user changes to "just once"
      localStorage.removeItem(memoryKey);
    }

    // track the last used method in localStorage
    localStorage.setItem(lastKey, selectedMethod.value);

    emit('close', {
      selectedMethod: selectedMethod.value,
    });
  }

  function handleSubmitKeydown(evt: KeyboardEvent) {
    if (evt.key === 'Enter' || evt.key === ' ') {
      evt.preventDefault();
      submit(null);
    }
  }

  /**
   * Opens the method picker dialog. If a preferred connection method has already been selected,
   * it will automatically select that method and submit without showing the dialog.
   *
   * @param force  Show the dialog even if a preferred connection method has already be selected
   */
  function open(force = false, permittedMethods?: OnCloseParameters['selectedMethod'][]) {
    if (permittedMethods) {
      methods.value = allMethods.value.filter((method) => permittedMethods.includes(method.id));
    } else {
      methods.value = allMethods.value;
    }

    // the android client does not support URL-encoded RDP URIs, which we must use from the browser
    const isAndroid = window.navigator.userAgent.toLowerCase().includes('android');
    if (isAndroid) {
      methods.value = methods.value.filter((method) => method.id !== 'rdpProtocolUri');
    }

    // if there is only one method available, select it and submit without showing the dialog
    if (!force && methods.value.length === 1) {
      selectedMethod.value = methods.value[0].id;
      submit(null);
      return;
    }

    const preferredMethod = localStorage.getItem(memoryKey) as OnCloseParameters['selectedMethod'];

    // if the user has a preferred method, select it and submit it without showing the dialog
    if (
      !force &&
      props.allowRememberMethod &&
      preferredMethod &&
      methods.value.some((method) => method.id === preferredMethod)
    ) {
      selectedMethod.value = preferredMethod;
      submit(true);
      return;
    }

    // if the user has a last used method, select it
    const lastUsedMethod = localStorage.getItem(lastKey) as OnCloseParameters['selectedMethod'];
    if (lastUsedMethod && methods.value.some((method) => method.id === lastUsedMethod)) {
      selectedMethod.value = lastUsedMethod;
    }

    // otherwise, show the dialog with the default method selected
    openDialog.value?.();
  }

  // Expose the openDialog method to the parent component
  defineExpose({ openDialog: open, isOpen: dialogIsOpen });
</script>

<template>
  <ContentDialog
    :title="`${$t('resource.methodPicker.title')} ${props.resourceTitle}`"
    ref="methodPickerDialog"
    @contextmenu.stop
    @keydown.stop
    @click.stop
  >
    <PickerItem
      v-for="method in methods"
      :key="popoverId + method.id"
      :name="`${popoverId}-method-${method.id}`"
      :value="method.id"
      v-model="selectedMethod"
      @dblclick="() => submit(null)"
    >
      {{ method.label }}
    </PickerItem>

    <template v-slot:footer>
      <Button
        @click="() => submit(true)"
        @keydown.stop="handleSubmitKeydown"
        v-if="allowRememberMethod !== false"
        >{{ $t('dialog.always') }}</Button
      >
      <Button @click="() => submit(false)" @keydown.stop="handleSubmitKeydown">{{ $t('dialog.once') }}</Button>
    </template>
  </ContentDialog>

  <ContentDialog
    :title="`${$t('resource.getProtocolApp.title')}`"
    ref="downloadRdpProtocolHandlerAppDialog"
    @contextmenu.stop
    @keydown.stop
    @click.stop
  >
    <TextBlock>
      {{ $t('resource.getProtocolApp.message') }}
    </TextBlock>
    <br />
    <br />
    <TextBlock>
      {{ $t('resource.getProtocolApp.message2') }}
    </TextBlock>
    <br />
    <br />
    <!-- https://github.com/microsoft/app-store-badge -->
    <ms-store-badge
      productid="9n1192wschv9"
      cid="raweb-web-app"
      productname="RDP Protocol Handler"
      window-mode="full"
      theme="auto"
      size="large"
      language="en-us"
      animation="on"
    >
    </ms-store-badge>

    <template v-slot:footer>
      <Button @click="() => submit(null)" @keydown.stop="handleSubmitKeydown">{{
        $t('resource.getProtocolApp.action')
      }}</Button>
    </template>
  </ContentDialog>

  <ContentDialog
    :title="`${$t('resource.getWindowsApp.title')}`"
    ref="downloadWindowsAppDialog"
    @contextmenu.stop
    @keydown.stop
    @click.stop
  >
    <TextBlock>
      {{ $t('resource.getWindowsApp.message', { store: isMacOS || isIOS ? 'App Store' : 'store' }) }}
    </TextBlock>
    <br />
    <br />
    <TextBlock>
      {{ $t('resource.getWindowsApp.message2') }}
    </TextBlock>
    <br />
    <br />

    <a
      v-if="isMacOS"
      tabindex="0"
      style="display: inline-flex"
      href="https://apps.apple.com/us/app/windows-app/id1295203466"
      v-html="isDarkMode ? macAppStoreBadgeDark : macAppStoreBadgeLight"
    ></a>

    <a
      v-if="isIOS"
      tabindex="0"
      style="display: inline-flex"
      href="https://apps.apple.com/us/app/windows-app-mobile/id714464092"
      v-html="isDarkMode ? appStoreBadgeDark : appStoreBadgeLight"
    ></a>

    <template v-slot:footer>
      <Button @click="() => submit(null)" @keydown.stop="handleSubmitKeydown">{{
        $t('resource.getWindowsApp.action')
      }}</Button>
    </template>
  </ContentDialog>
</template>

<style>
  ms-store-badge::part(img) {
    height: 56px;
  }
</style>
