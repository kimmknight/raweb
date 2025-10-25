<script setup lang="ts">
  import { Button, ContentDialog, InfoBar, TextBlock, TreeView } from '$components';
  import { TreeItem } from '$components/NavigationView/NavigationTypes';
  import { RegistryRemoteAppCreateDialog } from '$dialogs';
  import { useCoreDataStore } from '$stores';
  import { ResourceManagementSchemas } from '$utils';
  import { CommandLineMode } from '$utils/schemas/ResourceManagementSchemas';
  import { useQuery } from '@tanstack/vue-query';
  import { useTranslation } from 'i18next-vue';
  import { computed, nextTick, ref, useTemplateRef } from 'vue';
  import z from 'zod';

  const { appBase } = useCoreDataStore();
  const { t } = useTranslation();

  const { isPending, isFetching, isError, data, error, refetch, dataUpdatedAt } = useQuery({
    queryKey: ['remote-app-registry--discovery-available'],
    queryFn: async () => {
      return fetch('/api/management/resources/available')
        .then(async (res) => {
          if (!res.ok) {
            await res.json().then((err) => {
              if (err && 'ExceptionMessage' in err) {
                throw new Error(err.ExceptionMessage);
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

    const grouped = data.value.reduce((acc, app) => {
      const folderParts = app.displayFolder?.split('\\').filter((part) => part.trim() !== '') || [];
      const alphabeticFirstChar =
        folderParts.length > 0
          ? folderParts[0].charAt(0).toUpperCase()
          : app.displayName.charAt(0).toUpperCase();

      const blankIcon = new URL(`${appBase}api/resources/image/default.ico?format=png`, window.location.href);
      const folderIcon = new URL(
        `/api/management/resources/icon?path=${encodeURIComponent(
          'C:\\WINDOWS\\system32\\imageres.dll'
        )}&index=4&__cacheBust=${dataUpdatedAt.value}`,
        window.location.href
      );
      const appIcon = app.iconPath
        ? new URL(
            `/api/management/resources/icon?path=${encodeURIComponent(app.iconPath)}&index=${
              app.iconIndex
            }&__cacheBust=${dataUpdatedAt.value}`,
            window.location.href
          )
        : blankIcon;

      function openDialog() {
        createDialog_registryKey.value = app.displayName.replace(/\s+/g, '') + '__' + crypto.randomUUID();
        createDialog_name.value = app.displayName;
        createDialog_path.value = app.path;
        createDialog_vPath.value = app.path;
        createDialog_iconPath.value = app.iconPath || '';
        createDialog_iconIndex.value = (app.iconIndex || 0).toString();
        createDialog_commandLine.value = app.commandLineArguments || '';
        createDialog_commandLineOption.value =
          ResourceManagementSchemas.RegistryRemoteApp.CommandLineMode.Optional;
        createDialog_includeInWorkspace.value = true;
        createDialog_fileTypeAssociations.value = app.fileTypeAssociations || [];

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
    }, {} as Record<string, TreeItem>);

    const tree = Object.values(grouped).sort((a, b) => a.name.localeCompare(b.name));
    return tree;
  });

  const emit = defineEmits<{
    (e: 'afterSave'): void;
    (e: 'onClose'): void;
  }>();

  const createDialog = useTemplateRef<InstanceType<typeof RegistryRemoteAppCreateDialog> | null>(
    'createDialog'
  );

  const createDialog_registryKey = ref<string>();
  const createDialog_name = ref<string>();
  const createDialog_path = ref<string>();
  const createDialog_vPath = ref<string>();
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
                createDialog_vPath = '';
                createDialog_iconPath = '';
                createDialog_iconIndex = '0';
                createDialog_commandLine = '';
                createDialog_commandLineOption =
                  ResourceManagementSchemas.RegistryRemoteApp.CommandLineMode.Optional;
                createDialog_includeInWorkspace = true;
                createDialog_fileTypeAssociations = [];

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

      <RegistryRemoteAppCreateDialog
        ref="createDialog"
        :registry-key="createDialog_registryKey"
        :name="createDialog_name"
        :path="createDialog_path"
        :v-path="createDialog_vPath"
        :icon-path="createDialog_iconPath"
        :icon-index="createDialog_iconIndex"
        :command-line="createDialog_commandLine"
        :command-line-option="createDialog_commandLineOption"
        :include-in-workspace="createDialog_includeInWorkspace"
        :file-type-associations="createDialog_fileTypeAssociations"
        :security-description="createDialog_securityDescription"
        @after-save="
          () => {
            close();
            emit('afterSave');
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
    gap: 8px;
    padding: 8px 0;
  }
  .actions {
    position: sticky;
    top: var(--title-height);
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
