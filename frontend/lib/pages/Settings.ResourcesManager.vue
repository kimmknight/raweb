<script setup lang="ts">
  import {
    AnimatedIcon,
    Button,
    InfoBar,
    MenuFlyout,
    MenuFlyoutDivider,
    MenuFlyoutItem,
    MenuFlyoutSubItem,
    ProgressRing,
    TextBlock,
  } from '$components';
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
    refresh,
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
  import { ManagedResourceSource } from '$utils/schemas/ResourceManagementSchemas';
  import { useQuery } from '@tanstack/vue-query';
  import { useTranslation } from 'i18next-vue';
  import { storeToRefs } from 'pinia';
  import { computed } from 'vue';

  const { iisBase } = useCoreDataStore();
  const { needsSignInAgain, capabilities } = storeToRefs(useCoreDataStore());
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
          data
            .sort((a, b) => a.name.localeCompare(b.name))
            .map((resource) => {
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

  const registryResources = computed(
    () => data.value?.filter((resource) => resource.source !== ManagedResourceSource.File) ?? []
  );
  const fileResources = computed(
    () => data.value?.filter((resource) => resource.source === ManagedResourceSource.File) ?? []
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

            <MenuFlyout placement="bottom" anchor="end">
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
                  <MenuFlyoutDivider />
                  <MenuFlyoutItem @click="refetch()" :disabled="isPending || isFetching">
                    {{ t('registryApps.manager.refresh') }}
                    <template #icon><span v-swap="refresh"></span></template>
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

  <template v-else>
    <section v-if="registryResources.length > 0">
      <TextBlock v-if="fileResources.length > 0" variant="bodyStrong" tag="h2" class="section-title">{{
        t('registryApps.manager.registryResources')
      }}</TextBlock>

      <div class="apps-list">
        <ManagedResourceEditDialog
          v-for="app in registryResources"
          :identifier="app.identifier"
          :display-name="app.name"
          #default="{ open }"
          @after-save="handleAppOrDesktopChange"
          @after-delete="handleAppOrDesktopChange"
        >
          <MenuFlyout placement="bottom" anchor="end">
            <template #default="{ toggle: toggleContextMenu }">
              <Button
                v-if="
                  // registry-based resources can only be managed if the server claims the capability
                  app.source !== ManagedResourceSource.File
                    ? capabilities.supportsManageRegistryApps && capabilities.supportsReadRegistryApps
                    : true
                "
                @click="open"
                @contextmenu.prevent.stop="toggleContextMenu({ source: $event.currentTarget })"
                :disabled="!isSecureContext || needsSignInAgain"
                :class="{ notIncludedInWorksapce: !app.includeInWorkspace }"
              >
                <img :key="app.__extra.key" :src="app.__extra.iconUrl" alt="" width="24" height="24" />
                <TextBlock>{{ app.__extra.displayName }}</TextBlock>
              </Button>
            </template>

            <template #menu>
              <MenuFlyoutItem @click="open">
                {{ t('registryApps.resource.properties') }}
                <template #icon>
                  <svg viewBox="0 0 24 24">
                    <path
                      d="M10.5 7.751a5.75 5.75 0 0 1 8.38-5.114.75.75 0 0 1 .186 1.197L16.301 6.6l1.06 1.06 2.779-2.778a.75.75 0 0 1 1.193.179 5.75 5.75 0 0 1-6.422 8.284l-7.365 7.618a3.05 3.05 0 0 1-4.387-4.24l7.475-7.734a5.766 5.766 0 0 1-.134-1.238Zm5.75-4.25a4.25 4.25 0 0 0-4.067 5.489.75.75 0 0 1-.178.74l-7.768 8.035a1.55 1.55 0 1 0 2.23 2.156l7.676-7.941a.75.75 0 0 1 .775-.191 4.25 4.25 0 0 0 5.466-5.03l-2.492 2.492a.75.75 0 0 1-1.061 0L14.71 7.13a.75.75 0 0 1 0-1.06l2.466-2.467a4.268 4.268 0 0 0-.926-.102Z"
                      fill="currentColor"
                    />
                  </svg>
                </template>
              </MenuFlyoutItem>
              <MenuFlyoutDivider />
              <MenuFlyoutItem @click="exportResource(app.identifier)">
                {{ t('registryApps.resource.export') }}
                <template #icon><span v-swap="exportBundle"></span></template>
              </MenuFlyoutItem>
            </template>
          </MenuFlyout>
        </ManagedResourceEditDialog>
      </div>
    </section>

    <section v-if="fileResources.length > 0">
      <TextBlock v-if="registryResources.length > 0" variant="bodyStrong" tag="h2" class="section-title">{{
        t('registryApps.manager.fileResources')
      }}</TextBlock>

      <div class="apps-list">
        <ManagedResourceEditDialog
          v-for="app in fileResources"
          :identifier="app.identifier"
          :display-name="app.name"
          #default="{ open }"
          @after-save="handleAppOrDesktopChange"
          @after-delete="handleAppOrDesktopChange"
        >
          <MenuFlyout placement="bottom" anchor="end">
            <template #default="{ toggle: toggleContextMenu }">
              <Button
                v-if="
                  // registry-based resources can only be managed if the server claims the capability
                  app.source !== ManagedResourceSource.File
                    ? capabilities.supportsManageRegistryApps && capabilities.supportsReadRegistryApps
                    : true
                "
                @click="open"
                @contextmenu.prevent.stop="toggleContextMenu({ source: $event.currentTarget })"
                :disabled="!isSecureContext || needsSignInAgain"
                :class="{ notIncludedInWorksapce: !app.includeInWorkspace }"
              >
                <img :key="app.__extra.key" :src="app.__extra.iconUrl" alt="" width="24" height="24" />
                <TextBlock>{{ app.__extra.displayName }}</TextBlock>
              </Button>
            </template>

            <template #menu>
              <MenuFlyoutItem @click="open">
                {{ t('registryApps.resource.properties') }}
                <template #icon>
                  <svg viewBox="0 0 24 24">
                    <path
                      d="M10.5 7.751a5.75 5.75 0 0 1 8.38-5.114.75.75 0 0 1 .186 1.197L16.301 6.6l1.06 1.06 2.779-2.778a.75.75 0 0 1 1.193.179 5.75 5.75 0 0 1-6.422 8.284l-7.365 7.618a3.05 3.05 0 0 1-4.387-4.24l7.475-7.734a5.766 5.766 0 0 1-.134-1.238Zm5.75-4.25a4.25 4.25 0 0 0-4.067 5.489.75.75 0 0 1-.178.74l-7.768 8.035a1.55 1.55 0 1 0 2.23 2.156l7.676-7.941a.75.75 0 0 1 .775-.191 4.25 4.25 0 0 0 5.466-5.03l-2.492 2.492a.75.75 0 0 1-1.061 0L14.71 7.13a.75.75 0 0 1 0-1.06l2.466-2.467a4.268 4.268 0 0 0-.926-.102Z"
                      fill="currentColor"
                    />
                  </svg>
                </template>
              </MenuFlyoutItem>
              <MenuFlyoutDivider />
              <MenuFlyoutItem @click="exportResource(app.identifier)">
                {{ t('registryApps.resource.export') }}
                <template #icon><span v-swap="exportBundle"></span></template>
              </MenuFlyoutItem>
            </template>
          </MenuFlyout>
        </ManagedResourceEditDialog>
      </div>
    </section>
  </template>
</template>

<style scoped>
  .titlebar-row {
    user-select: none;
    margin-bottom: 16px;
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

  .apps-list {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
    gap: 12px;
    max-height: 600px;
  }

  .apps-list :deep(> .button) {
    justify-content: flex-start;
  }
  .apps-list :deep(> .button.disabled img) {
    opacity: 0.5;
  }
  .apps-list :deep(> .button > span) {
    display: inline-flex;
    align-items: center;
    text-align: start;
    gap: 12px;
    padding: 8px 0;
  }
  .apps-list :deep(> .button.notIncludedInWorksapce img) {
    opacity: 0.36;
  }

  .section-title {
    margin: 24px 0 8px 0;
  }
  section:first-of-type .section-title {
    margin-top: 0;
  }
</style>
