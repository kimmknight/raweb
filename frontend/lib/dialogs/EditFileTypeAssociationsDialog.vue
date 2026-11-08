<script setup lang="ts">
  import { Button, ContentDialog, InfoBar, MenuFlyoutItem, TextBlock, TextBox } from '$components';
  import { PickIconIndexDialog, showConfirm } from '$dialogs';
  import { useCoreDataStore } from '$stores';
  import { buildManagedIconPath, pickImageFile, PreventableEvent, ResourceManagementSchemas } from '$utils';
  import { useTranslation } from 'i18next-vue';
  import { computed, nextTick, onBeforeUnmount, ref, useTemplateRef } from 'vue';
  import z from 'zod';

  const { iisBase } = useCoreDataStore();
  const { t } = useTranslation();

  const {
    appName,
    resourceIdentifier,
    fallbackIconPath,
    fallbackIconIndex,
    modelValue,
    isManagedFileResource,
    pendingIcons,
    locked,
  } = defineProps<{
    appName?: string;
    resourceIdentifier: string;
    fallbackIconPath?: string;
    fallbackIconIndex?: number;
    isManagedFileResource?: boolean;
    modelValue?: FileTypeAssociation[];
    /**
     * Icons for managed file resources that have not been saved yet, keyed by lowercase file extension.
     * An empty blob means that the file type's icon will be removed (so the resource's icon is used).
     */
    pendingIcons?: Record<string, Blob>;
    /**
     * Whether the file type associations cannot be added, removed, or renamed
     * because the resource's signed RDP file stores them. Icons can still be changed.
     */
    locked?: boolean;
  }>();

  const emit = defineEmits<{
    (e: 'update:modelValue', value?: FileTypeAssociation[]): void;
    (e: 'update:pendingIcons', value: Record<string, Blob>): void;
  }>();

  type FileTypeAssociation = z.infer<typeof ResourceManagementSchemas.RegistryRemoteApp.FileTypeAssociation>;

  type WorkingFileTypeAssociation = FileTypeAssociation & { originalExtension?: string };
  const fileTypeAssociations = ref<WorkingFileTypeAssociation[]>();

  /** A `FileTypeAssociation` array that can be safely emitted to parent components */
  const associationsToEmit = computed(() => {
    return fileTypeAssociations.value?.map(({ originalExtension: _originalExtension, ...fta }) => fta);
  });

  // the working copy of the pending icons so that an icon
  // stays with its association if the extension is edited
  const workingIcons = ref(new Map<WorkingFileTypeAssociation, Blob>());
  const iconsModified = ref(false);

  const isModified = computed(() => {
    return iconsModified.value || JSON.stringify(associationsToEmit.value) !== JSON.stringify(modelValue);
  });

  const openedAt = ref(Date.now());
  function handleAfterOpen() {
    openedAt.value = Date.now();

    // set the working copy of the file type associations
    fileTypeAssociations.value = JSON.parse(JSON.stringify(modelValue));

    // store each association's extension before any edits
    // so that we can keep icons when the extension is edited
    for (const fta of fileTypeAssociations.value ?? []) {
      fta.originalExtension = fta.extension.toLowerCase();
    }

    // populate the working copy with the pending icons
    const icons = new Map<WorkingFileTypeAssociation, Blob>();
    for (const fta of fileTypeAssociations.value ?? []) {
      const pendingIcon = pendingIcons?.[fta.originalExtension!];
      if (pendingIcon) {
        icons.set(fta, pendingIcon);
      }
    }
    workingIcons.value = icons;
    iconsModified.value = false;
  }

  // object URLs for previewing the pending icons
  const iconObjectUrls = new Map<Blob, string>();
  function getIconObjectUrl(blob: Blob) {
    let url = iconObjectUrls.get(blob);
    if (!url) {
      url = URL.createObjectURL(blob);
      iconObjectUrls.set(blob, url);
    }
    return url;
  }
  onBeforeUnmount(() => {
    iconObjectUrls.forEach((url) => URL.revokeObjectURL(url));
    iconObjectUrls.clear();
  });

  const browserSupportsImageUpload = 'toBase64' in Uint8Array.prototype;

  /**
   * Whether the server already has an icon for the association stored under its original extension.
   * The server only sets the icon path when the managed resource contains an icon for that extension.
   */
  function hasSavedIcon(fta: WorkingFileTypeAssociation) {
    return !!fta.originalExtension && fta.iconPath === `./icons/${fta.originalExtension}.png`;
  }

  /**
   * Whether the association will use its own icon instead of the resource's icon after saving.
   */
  function hasCustomIcon(fta: WorkingFileTypeAssociation) {
    const pendingIcon = workingIcons.value.get(fta);
    return pendingIcon ? pendingIcon.size > 0 : hasSavedIcon(fta);
  }

  function getIconSrc(fta: WorkingFileTypeAssociation) {
    if (!isManagedFileResource) {
      return `${iisBase}${buildManagedIconPath(
        {
          iconPath: fta.iconPath || fallbackIconPath || '',
          iconIndex: fta.iconIndex || fallbackIconIndex || 0,
          isManagedFileResource: false,
          isRemoteApp: true,
        },
        openedAt.value
      )}`;
    }

    // an icon was selected but not saved yet
    const pendingIcon = workingIcons.value.get(fta);
    if (pendingIcon && pendingIcon.size > 0) {
      return getIconObjectUrl(pendingIcon);
    }

    // use the file type's icon if it has one; otherwise, use the resource's icon
    return `${iisBase}${buildManagedIconPath(
      {
        identifier: resourceIdentifier,
        isRemoteApp: true,
        isManagedFileResource: true,
        fileTypeExtension: !pendingIcon && hasSavedIcon(fta) ? fta.originalExtension : undefined,
      },
      openedAt.value
    )}`;
  }

  const processingIconsFor = ref(new Set<WorkingFileTypeAssociation>());
  const iconError = ref<string | null>(null);
  function selectIcon(fta: WorkingFileTypeAssociation) {
    iconError.value = null;
    processingIconsFor.value.add(fta);
    pickImageFile()
      .then((blob) => {
        workingIcons.value.set(fta, blob);
        iconsModified.value = true;
      })
      .catch((error) => {
        // cancelling the file picker is not an error
        if (error !== 'User cancelled image selection') {
          iconError.value = String(error);
        }
      })
      .finally(() => {
        processingIconsFor.value.delete(fta);
      });
  }

  function resetIcon(fta: WorkingFileTypeAssociation) {
    // an empty blob removes the saved icon so that the resource's icon is used
    workingIcons.value.set(fta, new Blob([]));
    iconsModified.value = true;
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

    // pass the pending icons for the associations that still exist, keyed by their final extension
    // (the server cleans up icons for extensions that are no longer listed in the file type associations list)
    let pendingIconsToEmit: Record<string, Blob> | undefined;
    if (isManagedFileResource) {
      pendingIconsToEmit = {};
      for (const fta of fileTypeAssociations.value ?? []) {
        const extension = fta.extension.toLowerCase();
        let icon = workingIcons.value.get(fta);

        // the server deletes the old extension's icon, so a renamed association must bring its saved icon along
        const needToFetchOriginalExtensionIcon =
          !icon && hasSavedIcon(fta) && fta.originalExtension !== extension;
        if (needToFetchOriginalExtensionIcon) {
          const currentIconResponse = await fetch(
            `${iisBase}${buildManagedIconPath(
              {
                identifier: resourceIdentifier,
                isRemoteApp: true,
                isManagedFileResource: true,
                fileTypeExtension: fta.originalExtension,
              },
              openedAt.value
            )}`
          );
          if (!currentIconResponse.ok) {
            throw new Error(
              `Failed to load the saved icon for ${fta.originalExtension} (${currentIconResponse.status}).`
            );
          }
          icon = await currentIconResponse.blob();
        }

        if (icon) {
          pendingIconsToEmit[extension] = icon;
        }
      }
    }

    emit('update:modelValue', associationsToEmit.value);
    if (pendingIconsToEmit) {
      emit('update:pendingIcons', pendingIconsToEmit);
    }

    // the changes were passed to the parent, so closing must not ask whether to discard them
    iconsModified.value = false;

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
        <Button :disabled="locked" @click="createNewAssociation">
          {{ t('registryApps.manager.fileTypeAssociationsEditor.add') }}
        </Button>
        <Button :disabled="locked" @click="() => (fileTypeAssociations = [])">{{
          t('registryApps.manager.fileTypeAssociationsEditor.removeAll')
        }}</Button>
      </div>

      <InfoBar
        v-if="locked"
        severity="caution"
        :title="t('resource.props.lockedInfoBar.title')"
        style="margin-top: calc(var(--inner-padding) / 2)"
      >
        <TextBlock>{{ t('registryApps.manager.fileTypeAssociationsEditor.lockedMessage') }}</TextBlock>
      </InfoBar>

      <InfoBar
        v-if="iconError"
        severity="critical"
        :title="iconError"
        style="margin-top: calc(var(--inner-padding) / 2)"
      />

      <div class="fta-list" ref="ftaList">
        <div class="fta" v-for="(fta, index) in fileTypeAssociations" :key="index">
          <img :src="getIconSrc(fta)" alt="" width="24" height="24" />
          <TextBox v-model:value="fta.extension" :disabled="locked" />
          <Button
            :disabled="locked"
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
            @index-selected="
              (indexSelected: number, iconPath: string) => {
                fta.iconIndex = indexSelected;
                fta.iconPath = iconPath;
              }
            "
            v-if="!isManagedFileResource"
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
          <template v-if="isManagedFileResource">
            <div class="select-icon-sizer">
              <!--
                Both versions of the button are rendered in the same grid cell.
                This keeps the width from changing when the button switches between
                a button with an icon and a split button.
              -->
              <div
                v-for="version in ['select-icon', 'manage-icon']"
                :key="version"
                :class="{ hidden: version !== (hasCustomIcon(fta) ? 'manage-icon' : 'select-icon') }"
                :inert="version !== (hasCustomIcon(fta) ? 'manage-icon' : 'select-icon')"
                :aria-hidden="version !== (hasCustomIcon(fta) ? 'manage-icon' : 'select-icon')"
              >
                <Button
                  :loading="processingIconsFor.has(fta)"
                  :disabled="!browserSupportsImageUpload"
                  @click="selectIcon(fta)"
                >
                  <template v-if="version === 'select-icon'" #icon>
                    <svg
                      width="24"
                      height="24"
                      viewBox="0 0 24 24"
                      fill="none"
                      xmlns="http://www.w3.org/2000/svg"
                    >
                      <path
                        d="M12 6.5C12 3.46243 9.53757 1 6.5 1C3.46243 1 1 3.46243 1 6.5C1 9.53757 3.46243 12 6.5 12C9.53757 12 12 9.53757 12 6.5ZM7.00065 7.00001L7.00111 9.50352C7.00111 9.77966 6.77725 10.0035 6.50111 10.0035C6.22497 10.0035 6.00111 9.77966 6.00111 9.50352L6.00065 7.00001H3.4956C3.21973 7.00001 2.99609 6.77615 2.99609 6.50001C2.99609 6.22387 3.21973 6.00001 3.4956 6.00001H6.00046L6 3.49927C6 3.22313 6.22386 2.99927 6.5 2.99927C6.77614 2.99927 7 3.22313 7 3.49927L7.00046 6.00001H9.49659C9.77246 6.00001 9.99609 6.22387 9.99609 6.50001C9.99609 6.77615 9.77246 7.00001 9.49659 7.00001H7.00065ZM17.75 4.5H12.6865C12.5142 3.96653 12.2747 3.46321 11.9782 3H17.75C19.5449 3 21 4.45507 21 6.25V17.75C21 19.5449 19.5449 21 17.75 21H6.25C4.45507 21 3 19.5449 3 17.75V11.9782C3.46321 12.2747 3.96653 12.5142 4.5 12.6865V17.75C4.5 17.9584 4.53643 18.1583 4.60326 18.3437L10.4258 12.643C11.2589 11.8273 12.5675 11.7885 13.4458 12.5266L13.5742 12.6431L19.3964 18.3447C19.4634 18.159 19.5 17.9588 19.5 17.75V6.25C19.5 5.2835 18.7165 4.5 17.75 4.5ZM18.3305 19.4014L12.5247 13.7148C12.2596 13.4553 11.8501 13.4316 11.5588 13.644L11.4752 13.7148L5.66845 19.4011C5.8504 19.4651 6.04613 19.5 6.25 19.5H17.75C17.9535 19.5 18.1489 19.4653 18.3305 19.4014ZM15.2521 6.5C16.4959 6.5 17.5042 7.50831 17.5042 8.75212C17.5042 9.99592 16.4959 11.0042 15.2521 11.0042C14.0083 11.0042 13 9.99592 13 8.75212C13 7.50831 14.0083 6.5 15.2521 6.5ZM15.2521 8C14.8367 8 14.5 8.33673 14.5 8.75212C14.5 9.1675 14.8367 9.50423 15.2521 9.50423C15.6675 9.50423 16.0042 9.1675 16.0042 8.75212C16.0042 8.33673 15.6675 8 15.2521 8Z"
                        fill="currentColor"
                      />
                    </svg>
                  </template>
                  {{ t('registryApps.manager.fileTypeAssociationsEditor.selectIcon') }}
                  <template v-if="version === 'manage-icon'" #menu>
                    <MenuFlyoutItem @click="resetIcon(fta)">
                      {{ t('registryApps.manager.fileTypeAssociationsEditor.resetIcon') }}
                    </MenuFlyoutItem>
                  </template>
                </Button>
              </div>
            </div>
          </template>
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
  .select-icon-sizer {
    display: grid;
    flex-shrink: 0;
  }
  .select-icon-sizer > * {
    grid-area: 1 / 1;
  }
  .select-icon-sizer > .hidden {
    visibility: hidden;
    pointer-events: none;
  }
  .select-icon-sizer > div > :deep(.button),
  .select-icon-sizer > div > :deep(.split-button) {
    width: 100%;
  }
  .select-icon-sizer :deep(.split-button > .button:first-of-type) {
    flex: 1;
  }

  .header-form {
    position: sticky;
    top: 0;
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
