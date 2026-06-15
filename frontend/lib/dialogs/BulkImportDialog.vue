<script setup lang="ts">
  import { ContentDialog, ProgressRing, TextBlock } from '$components';
  import { ManagedResourceCreateDialog, showConfirm } from '$dialogs';
  import { useCoreDataStore } from '$stores';
  import {
    PreventableEvent,
    readRdpFile,
    readTsResourceBundleFile,
    readTsResourceFile,
    useWebfeedData,
  } from '$utils';
  import { useTranslation } from 'i18next-vue';
  import { storeToRefs } from 'pinia';
  import { computed, ref } from 'vue';

  const { authUser, needsSignInAgain } = storeToRefs(useCoreDataStore());
  const { t } = useTranslation();

  const { refreshWorkspace } = defineProps<{
    refreshWorkspace: ReturnType<typeof useWebfeedData>['refresh'];
  }>();

  async function handleReceivedFile(
    loadingDialog: { open: () => void; close: () => void },
    openCreationDialog: () => void,
    count: number,
    files?: FileList,
    foundDisallowedMimeTypes?: string[]
  ) {
    if (!authUser.value.isLocalAdministrator || needsSignInAgain.value || !window.isSecureContext) {
      return;
    }

    if (foundDisallowedMimeTypes && foundDisallowedMimeTypes.length > 0) {
      showConfirm(
        t('registryApps.import.invalidMimeTypes.title', { count }),
        t('registryApps.import.invalidMimeTypes.message', { count }) +
          '\n\n' +
          t('registryApps.import.invalidMimeTypes.messageMoreInfo', {
            count,
            invalidTypes:
              (foundDisallowedMimeTypes.length > 1 ? '\n •  ' : '') + foundDisallowedMimeTypes.join('\n •  '),
          }).replaceAll('&amp;#x2F;', '/'),
        '',
        t('dialog.ok')
      );
      return;
    }

    if (!files) {
      showConfirm(
        t('registryApps.import.noValidResources.title'),
        t('registryApps.import.noValidResources.message', { count: 0 }),
        '',
        t('dialog.ok')
      );
      return;
    }

    // if it is taking a while to read the files, show a loading dialog
    // to indicate that something is happening and prevent the user from
    // interacting with the app while the files are being read
    const timeout = setTimeout(() => {
      loadingDialog.open();
    }, 200);

    async function readFile(file: File) {
      if (file.type === 'application/x-rdp') {
        return [await readRdpFile(file)];
      }

      if (file.type === 'application/x-tsresource') {
        return [await readTsResourceFile(file)];
      }

      if (file.type === 'application/x-tsresourcebundle') {
        return await readTsResourceBundleFile(file);
      }

      // display an error dialog for unsupported file types
      alert('Unsupported file type: ' + file.type);
      throw new Error('Unsupported file type: ' + file.type);
    }

    // schedule the first found file to open first
    const firstFileData = await readFile(files[0]);
    uploadedRdpFileData.value = firstFileData[0];

    // queue the rest of the files to be opened after the first one is saved
    if (firstFileData.length > 1) {
      queuedUploadedRdpFileData.value.push(...firstFileData.slice(1));
    }
    for await (const file of [...files].slice(1)) {
      const creationInfos = await readFile(file);
      queuedUploadedRdpFileData.value.push(...creationInfos);
    }

    // launch the bulk import wizard
    totalQueuedCount.value = queuedUploadedRdpFileData.value.length + (firstFileData ? 1 : 0);
    clearTimeout(timeout);
    loadingDialog.close();
    openCreationDialog();
  }

  type UploadedRdpFileData = Awaited<ReturnType<typeof readRdpFile>>;
  const totalQueuedCount = ref(0);
  const passedCount = ref(0);
  const uploadedRdpFileData = ref<UploadedRdpFileData>();
  const queuedUploadedRdpFileData = ref<UploadedRdpFileData[]>([]);

  async function handleAppOrDesktopChange(
    event: PreventableEvent<{
      next: () => void;
      revertToInitialData: (fadeOut?: boolean) => Promise<void>;
      fadeOutDialogContent: () => Promise<void>;
    }>
  ) {
    event.preventDefault();

    // if there is queued data from another imported RDP file, just change the dialog content
    if (queuedUploadedRdpFileData.value.length > 0) {
      // queue the next file data
      await event.detail.fadeOutDialogContent();
      passedCount.value++;
      uploadedRdpFileData.value = queuedUploadedRdpFileData.value.shift();
      await event.detail.revertToInitialData(false); // forces the dialog to reset its internal state to reflect the new initialData
      return;
    }

    // otherwise, refresh the workspace to reflect the changes, and then close the dialog
    await refreshWorkspace();

    // wrap in setTimeout so that the updated resources list can fully render
    // before the dialog is closed
    setTimeout(() => {
      event.detail.next();
    }, 0);
  }

  const bulkWizard = computed(() => {
    if (totalQueuedCount.value > 1) {
      return {
        currentIndex: passedCount.value,
        totalCount: totalQueuedCount.value,
      };
    }
  });

  function exposedHandleFileInput(
    openLoadingDialog: () => void,
    closeLoadingDialog: () => void,
    openCreationDialog: () => void,
    files: File | FileList
  ) {
    const dt = new DataTransfer();
    if (files instanceof File) {
      dt.items.add(files);
      files = dt.files;
    }

    handleReceivedFile(
      { open: openLoadingDialog, close: closeLoadingDialog },
      openCreationDialog,
      files.length,
      files
    );
  }
</script>

<template>
  <ContentDialog
    :close-on-backdrop-click="false"
    :close-on-escape="false"
    :show-close-caption-button="false"
    size="max"
    max-height="760px"
    fill-height
    :titlebar="t('registryApps.import.title')"
  >
    <div class="loading-screen" style="padding-top: 0.75rem">
      <ProgressRing :size="48" />
      <TextBlock variant="subtitle" tag="h1" style="font-size: 16px">{{ t('pleaseWait') }}</TextBlock>
    </div>

    <template #opener="{ close: closeLoadingDialog, open: openLoadingDialog }">
      <ManagedResourceCreateDialog
        ref="createDialog"
        is-managed-file-resource
        :initial-data="uploadedRdpFileData?.data"
        :is-remote-app="uploadedRdpFileData?.isRemoteApp"
        @after-save="handleAppOrDesktopChange"
        @after-skip="handleAppOrDesktopChange"
        @on-close="
          () => {
            uploadedRdpFileData = undefined;
            queuedUploadedRdpFileData = [];
            totalQueuedCount = 0;
            passedCount = 0;
          }
        "
        :bulk-wizard="bulkWizard"
      >
        <template #default="{ close: closeCreateDialog, open: openCreateDialog, popoverId }">
          <slot
            :close="closeCreateDialog"
            :popover-id="popoverId"
            :drop-zone-handler="{
              mimeTypes: ['application/x-rdp', 'application/x-tsresource', 'application/x-tsresourcebundle'],
              handler: handleReceivedFile.bind(
                null,
                { open: openLoadingDialog, close: closeLoadingDialog },
                openCreateDialog
              ),
            }"
            :handle-file-input="
              exposedHandleFileInput.bind(null, openLoadingDialog, closeLoadingDialog, openCreateDialog)
            "
            :open="
              (data: Awaited<ReturnType<typeof readRdpFile>>) => {
                uploadedRdpFileData = data;
                totalQueuedCount = 1;
                openCreateDialog();
              }
            "
          ></slot>
        </template>
      </ManagedResourceCreateDialog>
    </template>
  </ContentDialog>
</template>

<style scoped>
  .loading-screen {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 24px;
    height: calc(100% - var(--inner-padding) * 2);
  }
</style>
