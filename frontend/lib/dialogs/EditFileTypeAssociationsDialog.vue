<script setup lang="ts">
  import { Button, ContentDialog, TextBox } from '$components';
  import { PickIconIndexDialog, showConfirm } from '$dialogs';
  import { PreventableEvent, ResourceManagementSchemas } from '$utils';
  import { useTranslation } from 'i18next-vue';
  import { computed, ref, useTemplateRef } from 'vue';
  import z from 'zod';

  const { t } = useTranslation();

  const { appName, fallbackIconPath, fallbackIconIndex } = defineProps<{
    appName?: string;
    fallbackIconPath?: string;
    fallbackIconIndex?: number;
  }>();

  type FileTypeAssociation = z.infer<typeof ResourceManagementSchemas.RegistryRemoteApp.FileTypeAssociation>;
  const fileTypeAssociations = defineModel<FileTypeAssociation[]>();
  const initialFileTypeAssociations = ref<FileTypeAssociation[]>();
  const isModified = computed(() => {
    return JSON.stringify(fileTypeAssociations.value) !== JSON.stringify(initialFileTypeAssociations.value);
  });

  const openedAt = ref(Date.now());
  function handleAfterOpen() {
    openedAt.value = Date.now();
    shouldResetOnClose.value = true;
    initialFileTypeAssociations.value = JSON.parse(JSON.stringify(fileTypeAssociations.value));
  }

  var shouldResetOnClose = ref(true);
  function handleClose(close: () => void, parentCloseEvent: PreventableEvent) {
    // if the close event from the ContentDialog was prevented, do nothing
    if (parentCloseEvent.defaultPrevented) return;

    // if there were modifications, confirm with the user before discarding changes
    if (isModified.value && shouldResetOnClose.value) {
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

          // reset to initial state
          fileTypeAssociations.value = JSON.parse(JSON.stringify(initialFileTypeAssociations.value));

          closeConfirmDialog();
          initialFileTypeAssociations.value = [];
        })
        .catch(() => {
          // do nothing; user cancelled
        });
    } else {
      close();
      initialFileTypeAssociations.value = [];
    }
  }

  function closeWithPreservedChanges(close: () => void) {
    if (containsInvalidAssociations.value) return;
    shouldResetOnClose.value = false;
    close();
  }

  const ftaList = useTemplateRef('ftaList');
  function createNewAssociation() {
    fileTypeAssociations.value?.push({
      extension: '.*',
      iconPath: fallbackIconPath,
      iconIndex: fallbackIconIndex ?? null,
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
            :src="`/api/management/resources/icon?path=${encodeURIComponent(
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
                  d="M21 6.25A3.25 3.25 0 0 0 17.75 3H6.25A3.25 3.25 0 0 0 3 6.25v4.507a5.495 5.495 0 0 1 1.5-.882V6.25c0-.966.784-1.75 1.75-1.75h11.5c.966 0 1.75.784 1.75 1.75v11.5c0 .209-.037.409-.104.595l-5.822-5.702-.128-.116a2.251 2.251 0 0 0-2.243-.38c.259.425.461.889.598 1.38a.75.75 0 0 1 .724.188L18.33 19.4a1.746 1.746 0 0 1-.581.099h-4.775l.512.513c.278.277.443.626.495.987h3.768A3.25 3.25 0 0 0 21 17.75V6.25Z"
                  fill="currentColor"
                />
                <path
                  d="M17.504 8.752a2.252 2.252 0 1 0-4.504 0 2.252 2.252 0 0 0 4.504 0Zm-3.004 0a.752.752 0 1 1 1.504 0 .752.752 0 0 1-1.504 0ZM9.95 17.89a4.5 4.5 0 1 0-1.145.976l2.915 2.914a.75.75 0 1 0 1.06-1.06l-2.83-2.83ZM6.5 18a3 3 0 1 1 0-6 3 3 0 0 1 0 6Z"
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
