<script setup lang="ts">
  import { Button, ContentDialog, TextBox } from '$components';
  import { showConfirm } from '$dialogs';
  import { useCoreDataStore } from '$stores';
  import { PreventableEvent } from '$utils';
  import { useTranslation } from 'i18next-vue';
  import { computed, nextTick, ref, useTemplateRef } from 'vue';

  const { iisBase } = useCoreDataStore();
  const { t } = useTranslation();

  const { appName, resourceIdentifier, modelValue, isManagedFileResource } = defineProps<{
    appName?: string;
    resourceIdentifier: string;
    isManagedFileResource?: boolean;
    modelValue?: VirtualFolder['path'][];
  }>();

  const emit = defineEmits<{
    (e: 'update:modelValue', value?: VirtualFolder['path'][]): void;
  }>();

  type VirtualFolder = {
    id: string;
    path: string;
  };

  function createVirtualFolder(path: string): VirtualFolder {
    return {
      id: `vf-${crypto.randomUUID()}`,
      path,
    };
  }

  const virtualFolders = ref<VirtualFolder[]>();
  const isModified = computed(() => {
    return JSON.stringify(virtualFolders.value?.map((vf) => vf.path)) !== JSON.stringify(modelValue);
  });

  const openedAt = ref(Date.now());
  function handleAfterOpen() {
    openedAt.value = Date.now();

    // set the working copy of the folders
    virtualFolders.value = (modelValue ?? []).map((path) => createVirtualFolder(path));
  }

  function handleClose(close: () => void, parentCloseEvent: PreventableEvent) {
    // if the close event from the ContentDialog was prevented, do nothing
    if (parentCloseEvent.defaultPrevented) return;

    // if there were modifications, confirm with the user before discarding changes
    if (isModified.value) {
      parentCloseEvent.preventDefault();

      showConfirm(
        t('closeDialogWithUnsavedChangesGuard.title'),
        t('closeDialogWithUnsavedChangesGuard.message'),
        t('dialog.yes'),
        t('dialog.no')
      )
        .then((closeConfirmDialog) => {
          // user wants to discard changes
          close();
          closeConfirmDialog();
        })
        .catch(() => {
          // do nothing; user cancelled
        });
    } else {
      close();
    }
  }

  async function closeWithPreservedChanges(close: () => void) {
    if (containsInvalidVirtualFolders.value) return;
    emit(
      'update:modelValue',
      virtualFolders.value?.map((vf) => vf.path)
    );
    await nextTick();
    close();
  }

  const virtualFoldersListElement = useTemplateRef('virtualFoldersListContainer');
  function createNewVirtualFolder() {
    virtualFolders.value?.push(createVirtualFolder('/'));

    // foucus the last added entry's text box
    setTimeout(() => {
      const container = virtualFoldersListElement.value as HTMLDivElement | null;
      if (container) {
        const children = container.children;
        const lastVirtualFolderContainerElement = children[children.length - 1] as HTMLElement | undefined;
        const textBox = lastVirtualFolderContainerElement?.querySelector('input.text-box') as
          | HTMLInputElement
          | undefined;
        textBox?.focus();
      }
    }, 100);
  }

  const containsInvalidVirtualFolders = computed(() => {
    return (
      !virtualFolders.value ||
      // folders must start with a slash
      virtualFolders.value.some((vf) => !vf.path.startsWith('/')) ||
      // folders must not end with a slash (except for the root folder)
      virtualFolders.value.some((vf) => vf.path.endsWith('/') && vf.path !== '/') ||
      // folder paths must only contain valid characters (alphanumeric, spaces, underscores, hyphens, and slashes)
      virtualFolders.value.some((vf) => !/^[/\w\s-]+$/.test(vf.path))
    );
  });
</script>

<template>
  <ContentDialog
    @close="handleClose($event.detail.close, $event)"
    @after-open="handleAfterOpen()"
    @save-keyboard-shortcut="closeWithPreservedChanges"
    :close-on-backdrop-click="false"
    :title="t('registryApps.manager.virtualFoldersEditor.title', { app_name: appName })"
    size="standard"
    max-height="640px"
    fill-height
  >
    <template #opener="{ close, open, popoverId }">
      <slot name="default" :close="close" :open="open" :popover-id="popoverId" />
    </template>

    <template #default>
      <div class="header-form">
        <Button @click="createNewVirtualFolder">
          {{ t('registryApps.manager.virtualFoldersEditor.add') }}
        </Button>
        <Button @click="() => (virtualFolders = [])">{{
          t('registryApps.manager.virtualFoldersEditor.removeAll')
        }}</Button>
      </div>

      <div class="fta-list" ref="virtualFoldersListContainer">
        <div class="fta" v-for="(virtualFolderPath, index) in virtualFolders" :key="virtualFolderPath.id">
          <TextBox v-model:value="virtualFolderPath.path" />
          <Button
            @click="
              () => {
                virtualFolders?.splice(index, 1);
              }
            "
          >
            <template #icon>
              <svg viewBox="0 0 24 24">
                <path
                  d="M12 1.75a3.25 3.25 0 0 1 3.245 3.066L15.25 5h5.25a.75.75 0 0 1 .102 1.493L20.5 6.5h-.796l-1.28 13.02a2.75 2.75 0 0 1-2.561 2.474l-.176.006H8.313a2.75 2.75 0 0 1-2.714-2.307l-.023-.174L4.295 6.5H3.5a.75.75 0 0 1-.743-.648L2.75 5.75a.75.75 0 0 1 .648-.743L3.5 5h5.25A3.25 3.25 0 0 1 12 1.75Zm6.197 4.75H5.802l1.267 12.872a1.25 1.25 0 0 0 1.117 1.122l.127.006h7.374c.6 0 1.109-.425 1.225-1.002l.02-.126L18.196 6.5ZM13.75 9.25a.75.75 0 0 1 .743.648L14.5 10v7a.75.75 0 0 1-1.493.102L13 17v-7a.75.75 0 0 1 .75-.75Zm-3.5 0a.75.75 0 0 1 .743.648L11 10v7a.75.75 0 0 1-1.493.102L9.5 17v-7a.75.75 0 0 1 .75-.75Zm1.75-6a1.75 1.75 0 0 0-1.744 1.606L10.25 5h3.5A1.75 1.75 0 0 0 12 3.25Z"
                  fill="currentColor"
                />
              </svg>
            </template>
            {{ t('registryApps.manager.virtualFoldersEditor.remove') }}
          </Button>
        </div>
      </div>
    </template>

    <template #footer="{ close }">
      <Button @click="closeWithPreservedChanges(close)" :disabled="containsInvalidVirtualFolders">{{
        t('dialog.ok')
      }}</Button>
      <Button @click="close">{{ t('dialog.cancel') }}</Button>
    </template>
  </ContentDialog>
</template>

<style scoped>
  .fta-list {
    display: flex;
    flex-direction: column;
    gap: 8px;
    padding-top: calc(var(--inner-padding) / 2);
  }
  .fta {
    display: flex;
    align-items: center;
    gap: 8px;
  }
  .fta > button {
    flex-shrink: 0;
  }
  .header-form {
    position: sticky;
    top: var(--title-height);
    z-index: 9;
    background-color: var(--wui-background-default);
    border-bottom: 1px solid var(--wui-surface-stroke-default);
    margin-left: calc(-1 * var(--inner-padding));
    margin-right: calc(-1 * var(--inner-padding));
    padding: 0 var(--inner-padding) calc(var(--inner-padding) / 2) var(--inner-padding);
    display: flex;
    flex-direction: row;
    gap: 8px;
    flex-wrap: wrap;
  }
  .header-form::before {
    content: '';
    position: absolute;
    background-color: var(--wui-solid-background-base);
    inset: 0;
    top: -28px;
    z-index: -1;
  }
  .header-form::after {
    content: '';
    position: absolute;
    background-color: var(--wui-layer-default);
    inset: 0;
    top: -28px;
    z-index: -1;
  }
</style>
