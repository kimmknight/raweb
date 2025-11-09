<script setup lang="ts">
  import { Button, ContentDialog, TextBox } from '$components';
  import { PickIconIndexDialog, showConfirm } from '$dialogs';
  import { useCoreDataStore } from '$stores';
  import { PreventableEvent, ResourceManagementSchemas } from '$utils';
  import { useTranslation } from 'i18next-vue';
  import { computed, nextTick, ref, useTemplateRef } from 'vue';
  import z from 'zod';

  const { iisBase } = useCoreDataStore();
  const { t } = useTranslation();

  const { appName, fallbackIconPath, fallbackIconIndex, modelValue } = defineProps<{
    appName?: string;
    fallbackIconPath?: string;
    fallbackIconIndex?: number;
    modelValue?: FileTypeAssociation[];
  }>();

  const emit = defineEmits<{
    (e: 'update:modelValue', value?: FileTypeAssociation[]): void;
  }>();

  type FileTypeAssociation = z.infer<typeof ResourceManagementSchemas.RegistryRemoteApp.FileTypeAssociation>;
  const fileTypeAssociations = ref<FileTypeAssociation[]>();
  const isModified = computed(() => {
    return JSON.stringify(fileTypeAssociations.value) !== JSON.stringify(modelValue);
  });

  const openedAt = ref(Date.now());
  function handleAfterOpen() {
    openedAt.value = Date.now();

    // set the working copy of the file type associations
    fileTypeAssociations.value = JSON.parse(JSON.stringify(modelValue));
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
        'Yes',
        'No'
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
    if (containsInvalidAssociations.value) return;
    emit('update:modelValue', fileTypeAssociations.value);
    await nextTick();
    close();
  }

  const ftaList = useTemplateRef('ftaList');
  function createNewAssociation() {
    fileTypeAssociations.value?.push({
      extension: '.*',
      iconPath: fallbackIconPath,
      iconIndex: fallbackIconIndex ?? 0,
    } satisfies FileTypeAssociation);

    // foucus the last added entry's extension textbox
    setTimeout(() => {
      const container = ftaList.value as HTMLDivElement | null;
      if (container) {
        const children = container.children;
        const lastFta = children[children.length - 1] as HTMLElement | undefined;
        const textBox = lastFta?.querySelector('input.text-box') as HTMLInputElement | undefined;
        textBox?.focus();
      }
    }, 100);
  }

  const containsInvalidAssociations = computed(() => {
    return (
      !fileTypeAssociations.value ||
      // extensions must start with a dot
      fileTypeAssociations.value.some((fta) => !fta.extension.startsWith('.')) ||
      // empty extensions are invalid
      fileTypeAssociations.value.some((fta) => fta.extension.slice(1).trim() === '') ||
      // wildcard extensions are invalid
      fileTypeAssociations.value.some((fta) => fta.extension.includes('*')) ||
      // duplicate extensions are invalid
      fileTypeAssociations.value.some(
        (fta, index, arr) =>
          arr.findIndex((otherFta) => otherFta.extension.toLowerCase() === fta.extension.toLowerCase()) !==
          index
      )
    );
  });
</script>

<template>
  <ContentDialog
    @close="handleClose($event.detail.close, $event)"
    @after-open="handleAfterOpen()"
    @save-keyboard-shortcut="closeWithPreservedChanges"
    :close-on-backdrop-click="false"
    :title="t('registryApps.manager.fileTypeAssociationsEditor.title', { app_name: appName })"
    size="standard"
    max-height="640px"
    fill-height
  >
    <template #opener="{ close, open, popoverId }">
      <slot name="default" :close="close" :open="open" :popover-id="popoverId" />
    </template>

    <template #default>
      <div class="header-form">
        <Button @click="createNewAssociation">
          {{ t('registryApps.manager.fileTypeAssociationsEditor.add') }}
        </Button>
        <Button @click="() => (fileTypeAssociations = [])">{{
          t('registryApps.manager.fileTypeAssociationsEditor.removeAll')
        }}</Button>
      </div>

      <div class="fta-list" ref="ftaList">
        <div class="fta" v-for="(fta, index) in fileTypeAssociations" :key="index">
          <img
            :src="`${iisBase}api/management/resources/icon?path=${encodeURIComponent(
              fta.iconPath || fallbackIconPath || ''
            )}&index=${fta.iconIndex || fallbackIconIndex || 0}&__cacheBust=${openedAt}`"
            alt=""
            width="24"
            height="24"
          />
          <TextBox v-model:value="fta.extension" />
          <Button
            @click="
              () => {
                fileTypeAssociations?.splice(index, 1);
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
            {{ t('registryApps.manager.fileTypeAssociationsEditor.remove') }}
          </Button>
          <PickIconIndexDialog
            :current-index="fta.iconIndex || 0"
            :icon-path="fta.iconPath || fallbackIconPath || ''"
            #default="{ open }"
            @index-selected="(indexSelected: number, iconPath: string) => {
              fta.iconIndex = indexSelected;
              fta.iconPath = iconPath;
            }"
          >
            <Button @click="open">
              <template #icon>
                <svg viewBox="0 0 24 24">
                  <path
                    d="M21 6.25A3.25 3.25 0 0 0 17.75 3H6.25A3.25 3.25 0 0 0 3 6.25v4.507a5.495 5.495 0 0 1 1.5-.882V6.25c0-.966.784-1.75 1.75-1.75h11.5c.966 0 1.75.784 1.75 1.75v11.5c0 .209-.037.409-.104.595l-5.822-5.702-.128-.116a2.251 2.251 0 0 0-2.243-.38c.259.425.461.889.598 1.38a.75.75 0 0 1 .724.188L18.33 19.4a1.746 1.746 0 0 1-.581.099h-4.775l.512.513c.278.277.443.626.495.987h3.768A3.25 3.25 0 0 0 21 17.75V6.25Z"
                    fill="currentColor"
                  />
                  <path
                    d="M17.504 8.752a2.252 2.252 0 1 0-4.504 0 2.252 2.252 0 0 0 4.504 0Zm-3.004 0a.752.752 0 1 1 1.504 0 .752.752 0 0 1-1.504 0ZM9.95 17.89a4.5 4.5 0 1 0-1.145.976l2.915 2.914a.75.75 0 1 0 1.06-1.06l-2.83-2.83ZM6.5 18a3 3 0 1 1 0-6 3 3 0 0 1 0 6Z"
                    fill="currentColor"
                  />
                </svg>
              </template>
              {{ t('registryApps.manager.fileTypeAssociationsEditor.selectIcon') }}
            </Button>
          </PickIconIndexDialog>
        </div>
      </div>
    </template>

    <template #footer="{ close }">
      <Button @click="closeWithPreservedChanges(close)" :disabled="containsInvalidAssociations">{{
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
  .fta > Button {
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
