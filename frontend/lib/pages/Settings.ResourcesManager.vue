<script setup lang="ts">
  import {
    AnimatedIcon,
    Button,
    ContextMenu,
    createHeaderActionModelRefs,
    GenericCard,
    GenericResourceSection,
    InfoBar,
    MenuFlyout,
    MenuFlyoutDivider,
    MenuFlyoutItem,
    MenuFlyoutSubItem,
    ProgressRing,
    ResourceGrid,
    TextBlock,
  } from '$components';
  import SortMenuItems from '$components/Layout/SortMenuItems.vue';
  import { ManagedResourceCreateDiscoveryDialog, ManagedResourceEditDialog, showConfirm } from '$dialogs';
  import BulkImportDialog from '$dialogs/BulkImportDialog.vue';
  import {
    appAdd,
    databaseSearch,
    desktopAdd,
    documentArrowIn,
    documentEdit,
    documentsArrowIn,
    exportBundle,
    importBundle,
    list,
    refresh,
    sort as sortIcon,
    tiles,
    wrench,
  } from '$icons';
  import { useCoreDataStore } from '$stores';
  import {
    buildManagedIconPath,
    openSignInPagePopup,
    pickAnyResourceFile,
    PreventableEvent,
    readRdpFile,
    ResourceManagementSchemas,
    useLoadingCursor,
  } from '$utils';
  import { formatFileSize } from '$utils/resourceSize';
  import { ManagedResourceSource } from '$utils/schemas/ResourceManagementSchemas';
  import { useQuery } from '@tanstack/vue-query';
  import { useTranslation } from 'i18next-vue';
  import { storeToRefs } from 'pinia';
  import { computed } from 'vue';

  const { iisBase } = useCoreDataStore();
  const { needsSignInAgain, capabilities } = storeToRefs(useCoreDataStore());
  const { terminalServerAliases, envFQDN, machineName } = useCoreDataStore();
  const { t } = useTranslation();

  const { refreshWorkspace } = defineProps<import('./types.d.ts').PageProps>();

  const isSecureContext = window.isSecureContext;
  const randomUUID = isSecureContext
    ? crypto.randomUUID.bind(crypto)
    : () => {
        throw new Error('crypto.randomUUID is not available in an insecure context');
      };

  function getEmptyDesktopData() {
    return {
      isRemoteApp: false,
      data: {
        identifier: randomUUID(),
        includeInWorkspace: true,
        virtualFolders: ['/'],
      },
    } satisfies Awaited<ReturnType<typeof readRdpFile>>;
  }

  function getEmptyRemoteAppData() {
    return {
      isRemoteApp: true,
      data: {
        identifier: randomUUID(),
        includeInWorkspace: true,
        virtualFolders: ['/'],
      },
    } satisfies Awaited<ReturnType<typeof readRdpFile>>;
  }

  function getTerminalServer(rdpFileString?: string) {
    // without an address, asssume the resource is on this server
    const address = rdpFileString?.match(/full address:s:(.+)/)?.[1]?.trim() || envFQDN || machineName;
    return terminalServerAliases[address] ?? address;
  }

  const { isPending, isFetching, isError, data, error, refetch, dataUpdatedAt } = useQuery({
    queryKey: ['remote-app-registry'],
    queryFn: async () => {
      return fetch(`${iisBase}api/management/resources/registered`, {
        headers: { 'Cache-Control': 'no-cache' },
      })
        .then(async (res) => {
          if (!res.ok) {
            await res.json().then((err) => {
              if (err && ('ExceptionMessage' in err || 'detail' in err)) {
                throw new Error(err.ExceptionMessage || err.detail);
              }
            });
            throw new Error(`Error fetching remote app registry: ${res.status} ${res.statusText}`);
          }
          return res.json();
        })
        .then((data) => ResourceManagementSchemas.RegistryRemoteApp.App.array().parse(data))
        .then((data) =>
          data.map((resource) => {
            return {
              ...resource,
              __extra: {
                key: resource.identifier + resource.iconIndex + resource.iconPath,
                iconUrl: `${iisBase}${buildManagedIconPath(
                  resource.source === ManagedResourceSource.File
                    ? {
                        identifier: resource.identifier,
                        isRemoteApp: !!resource.remoteAppProperties,
                        isManagedFileResource: true,
                      }
                    : {
                        iconPath: resource.iconPath,
                        iconIndex: resource.iconIndex,
                        isRemoteApp: !!resource.remoteAppProperties,
                        isManagedFileResource: false,
                      },
                  new Date().getTime(),
                  undefined,
                  true
                )}`,
                displayName: resource.name + (resource.source === ManagedResourceSource.File ? 'ᵠ ' : ''),
                terminalServer: getTerminalServer(resource.rdpFileString),
                dateModified:
                  resource.lastModified?.toLocaleString(undefined, {
                    dateStyle: 'short',
                    timeStyle: 'short',
                  }) ?? '',
                type: resource.remoteAppProperties
                  ? t('registryApps.manager.typeRemoteApp')
                  : t('registryApps.manager.typeDesktop'),
                size: formatFileSize(resource.fileSize),
              },
            };
          })
        );
    },
    enabled: true, // fetch automatically
  });

  useLoadingCursor(isFetching);

  async function handleAppOrDesktopChange(event: PreventableEvent<{ next: () => void }>) {
    event.preventDefault();
    await refetch();
    await refreshWorkspace();

    // wrap in setTimeout so that the updated resources list can fully render
    // before the dialog is closed
    setTimeout(() => {
      event.detail.next();
    }, 0);
  }

  type ResourceFileHandler = (file: Awaited<ReturnType<typeof pickAnyResourceFile>>) => unknown;

  /**
   * Lets the user pick a resource file and passes it to the import handler, reporting failures in a dialog.
   */
  function addFromFile(handleFileInput: ResourceFileHandler) {
    pickAnyResourceFile()
      .then(handleFileInput)
      .catch((error) => {
        showConfirm(t('registryApps.manager.rdpUploadFail.title'), error, '', t('dialog.ok'));
      });
  }

  /**
   * Lets the user pick several resource files and passes them to the import handler, reporting failures in a dialog.
   */
  function addFromManyFiles(handleFileInput: ResourceFileHandler) {
    pickAnyResourceFile(true)
      .then(handleFileInput)
      .catch((error) => {
        showConfirm(t('registryApps.manager.rdpUploadFail.title'), error, '', t('dialog.ok'));
      });
  }

  /**
   * Lets the user pick a resource bundle and passes it to the import handler, reporting failures in a dialog.
   */
  function importResourceBundle(handleFileInput: ResourceFileHandler) {
    pickAnyResourceFile(true, '.tsresourcebundle')
      .then(handleFileInput)
      .catch((error) => {
        showConfirm(
          t('registryApps.manager.importFail.title'),
          t('registryApps.manager.importFail.message', { details: error?.message || error }),
          '',
          t('dialog.ok')
        );
      });
  }

  async function exportResourceBundle() {
    fetch(`${iisBase}api/management/resources/export-registered`, {
      headers: { 'Cache-Control': 'no-cache' },
    })
      .then(async (res) => {
        if (!res.ok) {
          await res.json().then((err) => {
            if (err && ('ExceptionMessage' in err || 'detail' in err)) {
              throw new Error(err.ExceptionMessage || err.detail);
            }
          });
          throw new Error(`Error exporting resources: ${res.status} ${res.statusText}`);
        }
        return [await res.blob(), res.headers.get('Content-Disposition')] as const;
      })
      .then(([blob, contentDisposition]) => {
        const fileName = contentDisposition
          ? contentDisposition
              .split(';')
              .find((part) => part.trim().startsWith('filename='))
              ?.split('=')[1]
              .trim()
              .replace(/(^"|"$)/g, '') // remove surrounding quotes if present
          : 'resources-export.tsresourcebundle';

        const url = URL.createObjectURL(blob);

        const a = document.createElement('a');
        a.href = url;
        a.download = fileName ?? 'resources-export.tsresourcebundle';
        document.body.appendChild(a);
        a.click();
        a.remove();
        URL.revokeObjectURL(url);
      })
      .catch((error) => {
        showConfirm(t('registryApps.manager.exportFail.title'), error.message, '', t('dialog.ok'));
      });
  }

  async function exportResource(id: string) {
    fetch(`${iisBase}api/management/resources/export-registered/${id}`, {
      headers: { 'Cache-Control': 'no-cache' },
    })
      .then(async (res) => {
        if (!res.ok) {
          await res.json().then((err) => {
            if (err && ('ExceptionMessage' in err || 'detail' in err)) {
              throw new Error(err.ExceptionMessage || err.detail);
            }
          });
          throw new Error(`Error exporting resource: ${res.status} ${res.statusText}`);
        }
        return [await res.blob(), res.headers.get('Content-Disposition')] as const;
      })
      .then(([blob, contentDisposition]) => {
        const fileName = contentDisposition
          ? contentDisposition
              .split(';')
              .find((part) => part.trim().startsWith('filename='))
              ?.split('=')[1]
              .trim()
              .replace(/(^"|"$)/g, '') // remove surrounding quotes if present
          : `resource-${id}.tsresource`;

        const url = URL.createObjectURL(blob);

        const a = document.createElement('a');
        a.href = url;
        a.download = fileName ?? `resource-${id}.tsresource`;
        document.body.appendChild(a);
        a.click();
        a.remove();
        URL.revokeObjectURL(url);
      })
      .catch((error) => {
        showConfirm(t('registryApps.manager.exportFail.title'), error.message, '', t('dialog.ok'));
      });
  }

  const { mode, sortName, sortOrder } = createHeaderActionModelRefs({
    defaults: { mode: 'tile' },
    persist: 'resources-manager',
  });
  const detailsColumns = computed(() => [
    {
      key: 'terminalServer',
      label: t('resourceTable.terminalServer'),
      width: 'minmax(140px, 220px)',
      sortKey: 'Terminal server' as const,
    },
    {
      key: 'dateModified',
      label: t('resourceTable.dateModified'),
      width: '170px',
      sortKey: 'Date modified' as const,
    },
    { key: 'type', label: t('resourceTable.type'), width: '110px' },
    {
      key: 'size',
      label: t('resourceTable.size'),
      width: '100px',
      sortKey: 'Size' as const,
      hiddenByDefault: true,
    },
  ]);

  const sortedResources = computed(() => {
    const direction = sortOrder.value === 'desc' ? -1 : 1;
    // always sort by name within the other criteria
    return [...(data.value ?? [])].sort((a, b) => {
      let primary = 0;
      if (sortName.value === 'Terminal server') {
        primary = a.__extra.terminalServer.localeCompare(b.__extra.terminalServer);
      } else if (sortName.value === 'Date modified') {
        primary = (a.lastModified?.getTime() ?? 0) - (b.lastModified?.getTime() ?? 0);
      } else if (sortName.value === 'Size') {
        primary = (a.fileSize ?? 0) - (b.fileSize ?? 0);
      }
      return direction * (primary || a.name.localeCompare(b.name));
    });
  });
  const registryResources = computed(() =>
    sortedResources.value.filter((resource) => resource.source !== ManagedResourceSource.File)
  );
  const fileResources = computed(() =>
    sortedResources.value.filter((resource) => resource.source === ManagedResourceSource.File)
  );
  const groups = computed(() =>
    [
      {
        key: 'registry',
        title: t('registryApps.manager.registryResources'),
        resources: registryResources.value,
      },
      { key: 'file', title: t('registryApps.manager.fileResources'), resources: fileResources.value },
    ].filter((group) => group.resources.length > 0)
  );
</script>

<template>
  <div class="titlebar-row">
    <TextBlock variant="title">
      {{ t('registryApps.manager.title') }}
    </TextBlock>
    <div class="header-actions" v-if="isSecureContext && !needsSignInAgain">
      <div class="actions">
        <ManagedResourceCreateDiscoveryDialog
          #default="{ open: openDiscoveryDialog }"
          @after-save="handleAppOrDesktopChange"
        >
          <BulkImportDialog
            #default="{ open: openCreationDialog, handleFileInput }"
            @after-save="handleAppOrDesktopChange"
          >
            <!-- apps -->
            <Button
              @click="
                () => {
                  if (capabilities.supportsListInstalledApps) {
                    openDiscoveryDialog();
                  } else {
                    openCreationDialog(getEmptyRemoteAppData());
                  }
                }
              "
              @auxclick="openCreationDialog(getEmptyRemoteAppData())"
            >
              <template #icon><span v-swap="appAdd"></span></template>
              {{ t('registryApps.manager.add') }}
              <template #menu>
                <MenuFlyoutItem v-if="capabilities.supportsListInstalledApps" @click="openDiscoveryDialog">
                  {{ t('registryApps.manager.addFromSystem') }}
                  <template #icon><span v-swap="databaseSearch"></span></template>
                </MenuFlyoutItem>
                <MenuFlyoutItem @click="openCreationDialog(getEmptyRemoteAppData())">
                  {{ t('registryApps.manager.fromManualEntry') }}
                  <template #icon><span v-swap="documentEdit"></span></template>
                </MenuFlyoutItem>
                <MenuFlyoutDivider />
                <MenuFlyoutItem
                  @click="
                    pickAnyResourceFile()
                      .then(handleFileInput)
                      .catch((error) => {
                        showConfirm(t('registryApps.manager.rdpUploadFail.title'), error, '', t('dialog.ok'));
                      })
                  "
                >
                  {{ t('registryApps.manager.fromFile') }}
                  <template #icon><span v-swap="documentArrowIn"></span></template>
                </MenuFlyoutItem>
              </template>
            </Button>

            <!-- desktops -->
            <Button
              @click="openCreationDialog(getEmptyDesktopData())"
              @auxclick="openCreationDialog(getEmptyDesktopData())"
            >
              <template #icon><span v-swap="desktopAdd"></span></template>
              {{ t('registryApps.manager.addDesktop') }}
              <template #menu>
                <MenuFlyoutItem @click="openCreationDialog(getEmptyDesktopData())">
                  {{ t('registryApps.manager.fromManualEntry') }}
                  <template #icon><span v-swap="documentEdit"></span></template>
                </MenuFlyoutItem>
                <MenuFlyoutDivider />
                <MenuFlyoutItem
                  @click="
                    pickAnyResourceFile()
                      .then(handleFileInput)
                      .catch((error) => {
                        showConfirm(t('registryApps.manager.rdpUploadFail.title'), error, '', t('dialog.ok'));
                      })
                  "
                >
                  {{ t('registryApps.manager.fromFile') }}
                  <template #icon><span v-swap="documentArrowIn"></span></template>
                </MenuFlyoutItem>
              </template>
            </Button>

            <MenuFlyout placement="bottom" anchor="start">
              <template v-slot="{ popoverId }">
                <Button
                  :popovertarget="popoverId"
                  @click.stop
                  @auxclick.stop="refetch"
                  :disabled="isPending || isFetching"
                  :loading="isFetching && !isPending"
                >
                  <span class="label">{{ $t('registryApps.manager.moreActions') }}</span>
                  <template v-slot:icon-end><AnimatedIcon.ChevronDown /></template>
                </Button>
              </template>
              <template #menu>
                <MenuFlyoutSubItem>
                  <template #icon><span v-swap="tiles"></span></template>
                  {{ t('actions.view.label2') }}
                  <template #menu>
                    <MenuFlyoutItem @click="mode = 'tile'" :selected="mode === 'tile'">
                      <template #icon><span v-swap="tiles"></span></template>
                      {{ t('actions.view.tile') }}
                    </MenuFlyoutItem>
                    <MenuFlyoutItem @click="mode = 'details'" :selected="mode === 'details'">
                      <template #icon><span v-swap="list"></span></template>
                      {{ t('actions.view.details') }}
                    </MenuFlyoutItem>
                  </template>
                </MenuFlyoutSubItem>
                <MenuFlyoutSubItem>
                  <template #icon><span v-swap="sortIcon"></span></template>
                  {{ t('actions.sort.label2') }}
                  <template #menu>
                    <SortMenuItems
                      v-model:sortName="sortName"
                      v-model:sortOrder="sortOrder"
                      showTerminalServer
                    />
                  </template>
                </MenuFlyoutSubItem>
                <MenuFlyoutItem @click="refetch()" :disabled="isPending || isFetching">
                  {{ t('registryApps.manager.refresh') }}
                  <template #icon><span v-swap="refresh"></span></template>
                </MenuFlyoutItem>
                <MenuFlyoutDivider />
                <MenuFlyoutItem
                  @click="
                    () =>
                      pickAnyResourceFile(true)
                        .then(handleFileInput)
                        .catch((error) => {
                          showConfirm(t('registryApps.manager.rdpUploadFail.title'), error, '', t('dialog.ok'));
                        })
                  "
                  :disabled="isPending || isFetching"
                >
                  {{ t('registryApps.manager.fromManyFiles') }}
                  <template #icon><span v-swap="documentsArrowIn"></span></template>
                </MenuFlyoutItem>
                <MenuFlyoutDivider />
                <MenuFlyoutItem @click="exportResourceBundle" :disabled="isPending || isFetching">
                  {{ t('registryApps.manager.export') }}
                  <template #icon><span v-swap="exportBundle"></span></template>
                </MenuFlyoutItem>
                <MenuFlyoutItem
                  @click="
                    () => {
                      pickAnyResourceFile(true, '.tsresourcebundle')
                        .then(handleFileInput)
                        .catch((error) => {
                          showConfirm(
                            t('registryApps.manager.importFail.title'),
                            t('registryApps.manager.importFail.message', { details: error?.message || error }),
                            '',
                            t('dialog.ok')
                          );
                        });
                    }
                  "
                  :disabled="isPending || isFetching"
                >
                  {{ t('registryApps.manager.import') }}
                  <template #icon><span v-swap="importBundle"></span></template>
                </MenuFlyoutItem>
              </template>
            </MenuFlyout>

            <Teleport to="#page-context-menu" defer>
              <MenuFlyout>
                <template #menu>
                  <MenuFlyoutSubItem>
                    <template #icon><span v-swap="tiles"></span></template>
                    {{ t('actions.view.label2') }}
                    <template #menu>
                      <MenuFlyoutItem @click="mode = 'tile'" :selected="mode === 'tile'">
                        <template #icon><span v-swap="tiles"></span></template>
                        {{ t('actions.view.tile') }}
                      </MenuFlyoutItem>
                      <MenuFlyoutItem @click="mode = 'details'" :selected="mode === 'details'">
                        <template #icon><span v-swap="list"></span></template>
                        {{ t('actions.view.details') }}
                      </MenuFlyoutItem>
                    </template>
                  </MenuFlyoutSubItem>
                  <MenuFlyoutSubItem>
                    <template #icon><span v-swap="sortIcon"></span></template>
                    {{ t('actions.sort.label2') }}
                    <template #menu>
                      <SortMenuItems
                        v-model:sortName="sortName"
                        v-model:sortOrder="sortOrder"
                        showTerminalServer
                      />
                    </template>
                  </MenuFlyoutSubItem>
                  <MenuFlyoutItem @click="refetch()" :disabled="isPending || isFetching">
                    {{ t('registryApps.manager.refresh') }}
                    <template #icon><span v-swap="refresh"></span></template>
                  </MenuFlyoutItem>
                  <MenuFlyoutDivider />
                  <MenuFlyoutSubItem>
                    <template #icon><span v-swap="appAdd"></span></template>
                    {{ t('registryApps.manager.add') }}
                    <template #menu>
                      <MenuFlyoutItem
                        v-if="capabilities.supportsListInstalledApps"
                        @click="openDiscoveryDialog"
                      >
                        {{ t('registryApps.manager.addFromSystem') }}
                        <template #icon><span v-swap="databaseSearch"></span></template>
                      </MenuFlyoutItem>
                      <MenuFlyoutItem @click="openCreationDialog(getEmptyRemoteAppData())">
                        {{ t('registryApps.manager.fromManualEntry') }}
                        <template #icon><span v-swap="documentEdit"></span></template>
                      </MenuFlyoutItem>
                      <MenuFlyoutDivider />
                      <MenuFlyoutItem @click="addFromFile(handleFileInput)">
                        {{ t('registryApps.manager.fromFile') }}
                        <template #icon><span v-swap="documentArrowIn"></span></template>
                      </MenuFlyoutItem>
                    </template>
                  </MenuFlyoutSubItem>
                  <MenuFlyoutSubItem>
                    <template #icon><span v-swap="desktopAdd"></span></template>
                    {{ t('registryApps.manager.addDesktop') }}
                    <template #menu>
                      <MenuFlyoutItem @click="openCreationDialog(getEmptyDesktopData())">
                        {{ t('registryApps.manager.fromManualEntry') }}
                        <template #icon><span v-swap="documentEdit"></span></template>
                      </MenuFlyoutItem>
                      <MenuFlyoutDivider />
                      <MenuFlyoutItem @click="addFromFile(handleFileInput)">
                        {{ t('registryApps.manager.fromFile') }}
                        <template #icon><span v-swap="documentArrowIn"></span></template>
                      </MenuFlyoutItem>
                    </template>
                  </MenuFlyoutSubItem>
                  <MenuFlyoutDivider />
                  <MenuFlyoutItem @click="exportResourceBundle" :disabled="isPending || isFetching">
                    {{ t('registryApps.manager.export') }}
                    <template #icon><span v-swap="exportBundle"></span></template>
                  </MenuFlyoutItem>
                  <MenuFlyoutItem
                    @click="importResourceBundle(handleFileInput)"
                    :disabled="isPending || isFetching"
                  >
                    {{ t('registryApps.manager.import') }}
                    <template #icon><span v-swap="importBundle"></span></template>
                  </MenuFlyoutItem>
                </template>
              </MenuFlyout>
            </Teleport>
          </BulkImportDialog>
        </ManagedResourceCreateDiscoveryDialog>
      </div>
    </div>
  </div>

  <div v-if="needsSignInAgain" class="full-page-notice">
    <TextBlock variant="subtitle">{{ t('needsSignInAgain.title') }}</TextBlock>
    <TextBlock block>{{ t('needsSignInAgain.message-policies') }}</TextBlock>
    <div class="button-row">
      <Button
        variant="accent"
        @click.prevent="
          openSignInPagePopup('sign-in-again', () => {
            refreshWorkspace();
            refetch();
          })
        "
        >{{ t('needsSignInAgain.action') }}</Button
      >
    </div>
  </div>

  <div v-else-if="isPending">
    <div style="display: flex; gap: 8px; align-items: center">
      <ProgressRing :size="24" />
      <TextBlock style="font-weight: 500">{{ t('pleaseWait') }}</TextBlock>
    </div>
  </div>

  <InfoBar v-else-if="isError" severity="critical" :title="t('unknownError')">
    <details>
      <summary>Error details</summary>
      <pre>{{ error?.message || error }}</pre>
    </details>
  </InfoBar>

  <ResourceGrid
    v-else
    :mode="mode"
    :columns="detailsColumns"
    persist="resources-manager"
    class="resources-grid"
    v-model:sortName="sortName"
    v-model:sortOrder="sortOrder"
  >
    <GenericResourceSection
      v-for="group in groups"
      :key="group.key"
      :title="groups.length > 1 ? group.title : undefined"
      :count="group.resources.length"
    >
      <ManagedResourceEditDialog
        v-for="app in group.resources"
        :key="app.identifier"
        :identifier="app.identifier"
        :display-name="app.name"
        #default="{ open }"
        @after-save="handleAppOrDesktopChange"
        @after-delete="handleAppOrDesktopChange"
      >
        <ContextMenu
          v-if="
            // registry-based resources can only be managed if the server claims the capability
            app.source !== ManagedResourceSource.File
              ? capabilities.supportsManageRegistryApps && capabilities.supportsReadRegistryApps
              : true
          "
          placement="bottom"
          anchor="start"
        >
          <GenericCard
            v-if="mode === 'details'"
            mode="details"
            :icon="app.__extra.iconUrl"
            :title="app.__extra.displayName"
            caption=""
            :details="{
              terminalServer: app.__extra.terminalServer,
              dateModified: app.__extra.dateModified,
              type: app.__extra.type,
              size: app.__extra.size,
            }"
            @click="open"
            @keydown.enter.space.prevent="open"
          />
          <Button
            v-else
            class="resource-button"
            @click="open"
            :disabled="!isSecureContext || needsSignInAgain"
            :class="{ notIncludedInWorksapce: !app.includeInWorkspace }"
          >
            <img :key="app.__extra.key" :src="app.__extra.iconUrl" alt="" width="24" height="24" />
            <TextBlock>{{ app.__extra.displayName }}</TextBlock>
          </Button>

          <template #menu>
            <MenuFlyout>
              <template #menu>
                <MenuFlyoutItem @click="open">
                  {{ t('registryApps.resource.properties') }}
                  <template #icon><span v-swap="wrench"></span></template>
                </MenuFlyoutItem>
                <MenuFlyoutDivider />
                <MenuFlyoutItem @click="exportResource(app.identifier)">
                  {{ t('registryApps.resource.export') }}
                  <template #icon><span v-swap="exportBundle"></span></template>
                </MenuFlyoutItem>
              </template>
            </MenuFlyout>
          </template>
        </ContextMenu>
      </ManagedResourceEditDialog>
    </GenericResourceSection>
  </ResourceGrid>
</template>

<style scoped>
  .titlebar-row {
    user-select: none;
  }

  .header-actions {
    margin: 12px 0 8px 0;
  }

  .header-actions,
  .actions {
    display: flex;
    flex-direction: row;
    flex-wrap: wrap;
    gap: 8px;
  }

  .resources-grid {
    --resource-grid-tile-min-width: 240px;
    --resource-grid-gap: 12px;
  }

  .resource-button {
    justify-content: flex-start;
  }
  .resource-button.disabled img {
    opacity: 0.5;
  }
  .resource-button :deep(> span) {
    display: inline-flex;
    align-items: center;
    text-align: start;
    gap: 12px;
    padding: 8px 0;
  }
  .resource-button.notIncludedInWorksapce img {
    opacity: 0.36;
  }
</style>
