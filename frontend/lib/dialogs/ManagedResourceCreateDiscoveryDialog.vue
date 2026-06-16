<script setup lang="ts">
  import { Button, ContentDialog, InfoBar, TextBlock, TreeView } from '$components';
  import { TreeItem } from '$components/NavigationView/NavigationTypes';
  import { BulkImportDialog, ManagedResourceCreateDialog, showConfirm } from '$dialogs';
  import { useCoreDataStore } from '$stores';
  import { hashString, pickAnyResourceFile, PreventableEvent, ResourceManagementSchemas } from '$utils';
  import { CommandLineMode } from '$utils/schemas/ResourceManagementSchemas';
  import { useQuery } from '@tanstack/vue-query';
  import { useTranslation } from 'i18next-vue';
  import { computed, nextTick, ref, useTemplateRef } from 'vue';
  import z from 'zod';

  const { iisBase } = useCoreDataStore();
  const { t } = useTranslation();

  const { isPending, isFetching, isError, data, error, refetch, dataUpdatedAt } = useQuery({
    queryKey: ['remote-app-registry--discovery-available'],
    queryFn: async () => {
      return fetch(`${iisBase}api/management/resources/available`, {
        headers: { 'Cache-Control': 'no-cache' },
      })
        .then(async (res) => {
          if (!res.ok) {
            await res.json().then((err) => {
              if (err && ('ExceptionMessage' in err || 'detail' in err)) {
                throw new Error(err.ExceptionMessage || err.detail);
              }
            });
            throw new Error(`Error fetching installed apps list: ${res.status} ${res.statusText}`);
          }
          return res.json();
        })
        .then((data) => ResourceManagementSchemas.InstalledApp.App.array().parse(data));
    },
    enabled: false, // do not fetch automatically
  });

  // group the data by folder name
  const tree = computed(() => {
    if (!data.value) return [];

    const grouped = data.value.reduce(
      (acc, app) => {
        const folderParts = app.displayFolder?.split('\\').filter((part) => part.trim() !== '') || [];
        const alphabeticFirstChar =
          folderParts.length > 0
            ? folderParts[0].charAt(0).toUpperCase()
            : app.displayName.charAt(0).toUpperCase();

        const blankIcon = new URL(`${iisBase}api/resources/image/default.ico?format=png`, window.location.href);
        const folderIcon = new URL(
          `${iisBase}api/management/resources/icon?path=${encodeURIComponent(
            'C:\\WINDOWS\\system32\\imageres.dll'
          )}&index=4&__cacheBust=${dataUpdatedAt.value}`,
          window.location.href
        );
        const appIcon = app.iconPath
          ? new URL(
              `${iisBase}api/management/resources/icon?path=${encodeURIComponent(app.iconPath)}&index=${
                app.iconIndex
              }&__cacheBust=${dataUpdatedAt.value}`,
              window.location.href
            )
          : blankIcon;

        async function openDialog() {
          // we hash the path AND command line arguments to create a unique registry key
          // based on what will actually be executed when the RemoteApp is launched
          createDialog_registryKey.value = await hashString(app.path + (app.commandLineArguments || ''));
          createDialog_name.value = app.displayName;
          createDialog_path.value = app.path;
          createDialog_iconPath.value = app.iconPath || '';
          createDialog_iconIndex.value = (app.iconIndex || 0).toString();
          createDialog_commandLine.value = app.commandLineArguments || '';
          createDialog_commandLineOption.value =
            ResourceManagementSchemas.RegistryRemoteApp.CommandLineMode.Optional;
          createDialog_includeInWorkspace.value = true;
          createDialog_fileTypeAssociations.value = app.fileTypeAssociations || [];
          createDialog_virtualFolders.value = ['/'];

          nextTick(() => {
            createDialog.value?.open?.();
          });
        }

        if (!acc[alphabeticFirstChar]) {
          acc[alphabeticFirstChar] = {
            name: alphabeticFirstChar,
            type: 'category',
            children: [],
          };
        }

        if (folderParts.length > 0) {
          const folderName = folderParts[0];
          const existingFolder = acc[alphabeticFirstChar]?.children?.find(
            (child) => child.name === folderName && child.type === 'expander'
          );

          if (existingFolder) {
            existingFolder.children = existingFolder.children || [];
            existingFolder.children.push({
              name: app.displayName,
              icon: appIcon,
              onClick: openDialog,
            });
            existingFolder.children = existingFolder.children.sort((a, b) => a.name.localeCompare(b.name));
            return acc;
          }

          acc[alphabeticFirstChar].children = acc[alphabeticFirstChar].children || [];
          acc[alphabeticFirstChar].children.push({
            name: folderName,
            type: 'expander',
            icon: folderIcon,
            children: [
              {
                name: app.displayName,
                icon: appIcon,
                onClick: openDialog,
              },
            ],
          });
          return acc;
        }

        acc[alphabeticFirstChar].children = acc[alphabeticFirstChar].children || [];
        acc[alphabeticFirstChar].children.push({
          name: app.displayName,
          icon: appIcon,
          onClick: openDialog,
        });
        return acc;
      },
      {} as Record<string, TreeItem>
    );

    const tree = Object.values(grouped).sort((a, b) => a.name.localeCompare(b.name));
    return tree;
  });

  const emit = defineEmits<{
    (e: 'afterSave', event: PreventableEvent<{ next: () => void }>): void;
    (e: 'onClose'): void;
  }>();

  const createDialog = useTemplateRef<InstanceType<typeof ManagedResourceCreateDialog> | null>('createDialog');

  const createDialog_registryKey = ref<string>();
  const createDialog_name = ref<string>();
  const createDialog_path = ref<string>();
  const createDialog_iconPath = ref<string>();
  const createDialog_iconIndex = ref<string>();
  const createDialog_commandLine = ref<string>();
  const createDialog_commandLineOption = ref<CommandLineMode>();
  const createDialog_includeInWorkspace = ref<boolean>();
  const createDialog_fileTypeAssociations =
    ref<z.infer<typeof ResourceManagementSchemas.RegistryRemoteApp.FileTypeAssociation>[]>();
  const createDialog_securityDescription = ref<
    z.infer<typeof ResourceManagementSchemas.RegistryRemoteApp.App>['securityDescription']
  >({
    readAccessAllowedSids: [],
    readAccessDeniedSids: [],
  });
  const createDialog_virtualFolders = ref<string[]>();

  const randomUUID = crypto.randomUUID.bind(crypto);
</script>

<template>
  <ContentDialog
    @open="() => refetch()"
    @close="() => emit('onClose')"
    :close-on-backdrop-click="false"
    :title="t('registryApps.manager.discover.title')"
    size="standard"
    max-height="700px"
    fill-height
    :updating="isFetching"
    :loading="isPending"
    :error="isError && error !== null ? error : false"
  >
    <template #opener="{ close, open, popoverId }">
      <slot name="default" :close="close" :open="open" :popover-id="popoverId" />
    </template>

    <template #default="{ close }">
      <div class="content">
        <div class="actions">
          <hr />
          <Button
            @click="
              () => {
                createDialog_registryKey = randomUUID();
                createDialog_name = '';
                createDialog_path = '';
                createDialog_iconPath = '';
                createDialog_iconIndex = '0';
                createDialog_commandLine = '';
                createDialog_commandLineOption =
                  ResourceManagementSchemas.RegistryRemoteApp.CommandLineMode.Optional;
                createDialog_includeInWorkspace = true;
                createDialog_fileTypeAssociations = [];
                createDialog_virtualFolders = ['/'];

                nextTick(() => {
                  createDialog?.open?.();
                });
              }
            "
          >
            <template #icon>
              <svg viewBox="0 0 24 24">
                <path
                  d="M12 3.25C12.4142 3.25 12.75 3.58579 12.75 4V11.25H20C20.4142 11.25 20.75 11.5858 20.75 12C20.75 12.4142 20.4142 12.75 20 12.75H12.75V20C12.75 20.4142 12.4142 20.75 12 20.75C11.5858 20.75 11.25 20.4142 11.25 20V12.75H4C3.58579 12.75 3.25 12.4142 3.25 12C3.25 11.5858 3.58579 11.25 4 11.25H11.25V4C11.25 3.58579 11.5858 3.25 12 3.25Z"
                  fill="currentColor"
                />
              </svg>
            </template>
            {{ t('registryApps.manager.discover.manualAdd') }}
          </Button>
          <BulkImportDialog
            #default="{ open: openCreationDialog, handleFileInput }"
            @after-save="
              () => {
                const next = () => {
                  close();
                };

                const event = new PreventableEvent({ next });
                emit('afterSave', event);
                if (!event.defaultPrevented) {
                  next();
                }
              }
            "
          >
            <Button
              @click="
                pickAnyResourceFile()
                  .then(handleFileInput)
                  .catch((error) => {
                    showConfirm(t('registryApps.manager.rdpUploadFail.title'), error, '', t('dialog.ok'));
                  })
              "
            >
              <template #icon>
                <svg viewBox="0 0 24 24">
                  <path
                    d="m6.747 3 10.506.002a3.752 3.752 0 0 1 3.745 3.551l.005.2v4.492a.75.75 0 0 1-1.493.102l-.007-.102V6.752c0-1.19-.925-2.165-2.096-2.245l-.154-.005L6.747 4.5a2.249 2.249 0 0 0-2.242 2.057l-.008.159.002 10.536c.001 1.19.926 2.165 2.097 2.245l.154.005h4.496a.75.75 0 0 1 .102 1.493l-.102.007H6.75a3.752 3.752 0 0 1-3.745-3.55l-.006-.2-.001-10.5.004-.203a3.749 3.749 0 0 1 3.546-3.544l.2-.005ZM9.75 9h6.504a.75.75 0 0 1 .102 1.493l-.102.007-4.694-.001 7.224 7.22a.75.75 0 0 1 .073.977l-.073.084a.75.75 0 0 1-.977.073l-.084-.073-7.223-7.22v4.691a.75.75 0 0 1-.648.743l-.102.007a.75.75 0 0 1-.743-.648L9 16.25V9.734c0-.025.002-.05.005-.076l.021-.108.035-.096.005-.012a.721.721 0 0 1 .153-.223l.044-.04.081-.06.06-.035.095-.042.067-.02.062-.013L9.72 9h6.533H9.75Z"
                    fill="currentColor"
                  />
                </svg>
              </template>
              {{ t('registryApps.manager.discover.fromFile') }}
            </Button>
          </BulkImportDialog>
          <Button @click="refetch" :disabled="isPending || isFetching">
            <template #icon>
              <svg viewBox="0 0 24 24">
                <path
                  d="M12 4.5C7.85786 4.5 4.5 7.85786 4.5 12C4.5 16.1421 7.85786 19.5 12 19.5C16.1421 19.5 19.5 16.1421 19.5 12C19.5 11.6236 19.4723 11.2538 19.4188 10.8923C19.3515 10.4382 19.6839 10 20.1429 10C20.5138 10 20.839 10.2562 20.8953 10.6228C20.9642 11.0718 21 11.5317 21 12C21 16.9706 16.9706 21 12 21C7.02944 21 3 16.9706 3 12C3 7.02944 7.02944 3 12 3C14.3051 3 16.4077 3.86656 18 5.29168V4.25C18 3.83579 18.3358 3.5 18.75 3.5C19.1642 3.5 19.5 3.83579 19.5 4.25V7.25C19.5 7.66421 19.1642 8 18.75 8H15.75C15.3358 8 15 7.66421 15 7.25C15 6.83579 15.3358 6.5 15.75 6.5H17.0991C15.7609 5.25883 13.9691 4.5 12 4.5Z"
                  fill="currentColor"
                />
              </svg>
            </template>
            {{ t('registryApps.manager.discover.refresh') }}
          </Button>
          <hr />
        </div>

        <InfoBar severity="information" style="margin-bottom: 8px">
          Select an app to convert it into a RemoteApp that appears in RAWeb and workspace clients.
        </InfoBar>

        <div class="tree-area">
          <TreeView v-if="data && data.length > 0" :tree state-id="app-discovery">
            <template #default>
              <TextBlock>{{ t('registryApps.manager.noApps') }}</TextBlock>
            </template>
          </TreeView>
        </div>
      </div>

      <ManagedResourceCreateDialog
        ref="createDialog"
        :initial-data="{
          identifier: createDialog_registryKey || '',
          name: createDialog_name,
          path: createDialog_path,
          iconPath: createDialog_iconPath,
          iconIndex: parseInt(createDialog_iconIndex || '0'),
          commandLine: createDialog_commandLine,
          commandLineOption: createDialog_commandLineOption,
          includeInWorkspace: createDialog_includeInWorkspace,
          fileTypeAssociations: createDialog_fileTypeAssociations,
          securityDescription: createDialog_securityDescription,
          virtualFolders: createDialog_virtualFolders,
        }"
        :is-remote-app="true"
        :is-managed-file-resource="false"
        @after-save="
          () => {
            const next = () => {
              close();
            };

            const event = new PreventableEvent({ next });
            emit('afterSave', event);
            if (!event.defaultPrevented) {
              next();
            }
          }
        "
      />
    </template>

    <template #footer="{ close }">
      <Button @click="close">Close</Button>
    </template>
  </ContentDialog>
</template>

<style scoped>
  .content {
    margin: calc(-1 * var(--padding));
    padding: var(--padding);
    box-sizing: border-box;
    display: flex;
    flex-direction: column;
  }

  .actions {
    margin: 0 0 8px 0;
    display: flex;
    flex-direction: row;
    flex-wrap: wrap;
    gap: 8px;
    padding: 8px 0;
  }
  .actions {
    position: sticky;
    top: 0;
    z-index: 98;
  }
  .actions hr {
    border: none;
    border-top: 1px solid var(--wui-divider-stroke-default);
    position: absolute;
    top: -1px;
    left: 0;
    right: 0;
    margin: 0;
  }
  .actions hr:last-of-type {
    top: unset;
    bottom: -1px;
  }
  .actions::before {
    content: '';
    position: absolute;
    background-color: var(--wui-solid-background-base);
    inset: 0;
    top: -28px;
    z-index: -1;
  }
  .actions::after {
    content: '';
    position: absolute;
    background-color: var(--wui-layer-default);
    inset: 0;
    top: -28px;
    z-index: -1;
  }

  .tree-area :deep(> .tree-view) {
    box-sizing: border-box;
    overflow-y: visible;
  }
</style>
