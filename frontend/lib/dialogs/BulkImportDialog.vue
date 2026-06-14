<script setup lang="ts">
  import { ManagedResourceCreateDialog, showConfirm } from '$dialogs';
  import { useCoreDataStore } from '$stores';
  import { PreventableEvent, readRdpFile, useWebfeedData } from '$utils';
  import { useTranslation } from 'i18next-vue';
  import { storeToRefs } from 'pinia';
  import { ref } from 'vue';

  const { authUser, needsSignInAgain } = storeToRefs(useCoreDataStore());
  const { t } = useTranslation();

  const { refreshWorkspace } = defineProps<{
    refreshWorkspace: ReturnType<typeof useWebfeedData>['refresh'];
  }>();

  async function handleReceivedFile(
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
        'TEMPORARY: No files found',
        'No files were found in the drop event. Please try again.',
        '',
        t('dialog.ok')
      );
      return;
    }

    async function readFile(file: File) {
      if (file.type === 'application/x-rdp') {
        return await readRdpFile(file);
      }

      // display an error dialog for unsupported file types
      alert('Unsupported file type: ' + file.type);
      throw new Error('Unsupported file type: ' + file.type);
    }

    // schedule the first found file to open first
    const firstFileData = await readFile(files[0]);
    uploadedRdpFileData.value = firstFileData;

    // queue the rest of the files to be opened after the first one is saved
    for await (const file of [...files].slice(1)) {
      if (file.type === 'application/x-rdp') {
        const creationInfo = await readRdpFile(file);
        queuedUploadedRdpFileData.value.push(creationInfo);
      }
    }

    // launch the bulk import wizard
    totalQueuedCount.value = queuedUploadedRdpFileData.value.length + (firstFileData ? 1 : 0);
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
</script>

<template>
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
    :bulk-wizard="
      totalQueuedCount > 1
        ? {
            currentIndex: passedCount,
            totalCount: totalQueuedCount,
          }
        : undefined
    "
  >
    <template #default="{ close, open, popoverId }">
      <slot
        :close="close"
        :popover-id="popoverId"
        :drop-zone-handler="handleReceivedFile.bind(null, open)"
      ></slot>
    </template>
  </ManagedResourceCreateDialog>
</template>
