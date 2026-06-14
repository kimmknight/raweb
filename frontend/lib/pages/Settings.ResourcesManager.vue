<script setup lang="ts">
  import { AnimatedIcon, Button, InfoBar, MenuFlyoutItem, ProgressRing, TextBlock } from '$components';
  import MenuFlyout from '$components/MenuFlyout/MenuFlyout.vue';
  import {
    ManagedResourceCreateDialog,
    ManagedResourceCreateDiscoveryDialog,
    ManagedResourceEditDialog,
    showConfirm,
  } from '$dialogs';
  import { useCoreDataStore } from '$stores';
  import {
    buildManagedIconPath,
    openSignInPagePopup,
    pickRDPFile,
    PreventableEvent,
    ResourceManagementSchemas,
    useWebfeedData,
  } from '$utils';
  import { ManagedResourceSource } from '$utils/schemas/ResourceManagementSchemas';
  import { useQuery } from '@tanstack/vue-query';
  import { useTranslation } from 'i18next-vue';
  import { storeToRefs } from 'pinia';
  import { ref } from 'vue';

  const { iisBase } = useCoreDataStore();
  const { needsSignInAgain, capabilities } = storeToRefs(useCoreDataStore());
  const { t } = useTranslation();

  const { refreshWorkspace } = defineProps<{
    refreshWorkspace: ReturnType<typeof useWebfeedData>['refresh'];
  }>();

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
    } satisfies Awaited<ReturnType<typeof pickRDPFile>>;
  }

  function getEmptyRemoteAppData() {
    return {
      isRemoteApp: true,
      data: {
        identifier: randomUUID(),
        includeInWorkspace: true,
        virtualFolders: ['/'],
      },
    } satisfies Awaited<ReturnType<typeof pickRDPFile>>;
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
        .then((data) => ResourceManagementSchemas.RegistryRemoteApp.App.array().parse(data));
    },
    enabled: true, // fetch automatically
  });

  const uploadedRdpFileData = ref<Awaited<ReturnType<typeof pickRDPFile>>>();
  const uploadedRdpFileKey = ref(0);

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
          <ManagedResourceCreateDialog
            #default="{ open: openCreationDialog }"
            :key="uploadedRdpFileKey"
            is-managed-file-resource
            :initial-data="uploadedRdpFileData?.data"
            :is-remote-app="uploadedRdpFileData?.isRemoteApp"
            @after-save="handleAppOrDesktopChange"
          >
            <!-- apps -->
            <Button
              @click="
                () => {
                  if (capabilities.supportsListInstalledApps) {
                    openDiscoveryDialog();
                  } else {
                    uploadedRdpFileData = getEmptyRemoteAppData();
                    openCreationDialog();
                  }
                }
              "
              @auxclick="
                () => {
                  uploadedRdpFileData = getEmptyRemoteAppData();
                  openCreationDialog();
                }
              "
            >
              <template #icon>
                <svg viewBox="0 0 24 24">
                  <path
                    d="M 16.236328,2.3359375 A 2.25,2.25 0 0 0 14.644531,2.9941406 L 12.060547,5.5800781 A 2.25,2.25 0 0 0 9.835938,3.6640625 h -5.25 a 2.25,2.25 0 0 0 -2.25,2.25 V 19.414063 a 2.25,2.25 0 0 0 2.25,2.25 h 7.972656 A 6.4615383,6.4615383 0 0 1 11.039063,17.5 6.4615383,6.4615383 0 0 1 12.083984,13.976563 v -0.5625 h 0.410157 a 6.4615383,6.4615383 0 0 1 3.072265,-2.080079 L 12.527344,8.2949219 a 0.75,0.75 0 0 1 0,-1.0605469 l 3.177734,-3.1796875 a 0.75,0.75 0 0 1 1.060547,0 l 3.179687,3.1796875 a 0.75,0.75 0 0 1 0,1.0605469 L 17.193359,11.044922 A 6.4615383,6.4615383 0 0 1 17.5,11.039062 6.4615383,6.4615383 0 0 1 19.117187,11.24414 l 1.888672,-1.8886719 a 2.25,2.25 0 0 0 0,-3.1816406 L 17.826172,2.9941406 A 2.25,2.25 0 0 0 16.236328,2.3359375 Z m -11.65039,2.828125 h 5.25 a 0.75,0.75 0 0 1 0.75,0.75 v 6.0000005 h -6.75 V 5.9140625 a 0.75,0.75 0 0 1 0.75,-0.75 z m 7.5,4.8085937 1.939453,1.9414068 h -1.939453 z m -8.25,3.4414068 h 6.75 l -0.002,6.75 H 4.585892 c -0.414001,0 -0.75,-0.336 -0.75,-0.75 z"
                    fill="currentColor"
                  />
                  <path
                    d="M 17.5,12 C 20.53765,12 23,14.46235 23,17.5 23,20.53765 20.53765,23 17.5,23 14.46235,23 12,20.53765 12,17.5 12,14.46235 14.46235,12 17.5,12 Z m 0,2.75 a 0.4125,0.4125 0 0 0 -0.40865,0.3564 l -0.0038,0.0561 v 1.925 h -1.925 a 0.4125,0.4125 0 0 0 -0.0561,0.82115 l 0.0561,0.0038 h 1.925 v 1.925 a 0.4125,0.4125 0 0 0 0.82115,0.0561 l 0.0038,-0.0561 v -1.925 h 1.925 a 0.4125,0.4125 0 0 0 0.0561,-0.82115 l -0.0561,-0.0038 h -1.925 v -1.925 A 0.4125,0.4125 0 0 0 17.5,14.75 Z"
                    fill="currentColor"
                  />
                </svg>
              </template>
              {{ t('registryApps.manager.add') }}
              <template #menu>
                <MenuFlyoutItem v-if="capabilities.supportsListInstalledApps" @click="openDiscoveryDialog">
                  {{ t('registryApps.manager.addFromSystem') }}
                  <template #icon>
                    <svg viewBox="0 0 24 24">
                      <path
                        d="M4 6c0-.69.315-1.293.774-1.78.455-.482 1.079-.883 1.793-1.202C7.996 2.377 9.917 2 12 2c2.083 0 4.004.377 5.433 1.018.714.32 1.338.72 1.793 1.202.459.487.774 1.09.774 1.78v6.257a5.496 5.496 0 0 0-1.5-.882V8.392c-.32.22-.68.417-1.067.59C16.004 9.623 14.083 10 12 10c-2.083 0-4.004-.377-5.433-1.018a6.801 6.801 0 0 1-1.067-.59V18c0 .207.09.46.365.75.279.296.717.596 1.315.864 1.195.535 2.899.886 4.82.886.24 0 .476-.006.708-.016a5.495 5.495 0 0 0 2.15 1.267c-.89.162-1.856.249-2.858.249-2.083 0-4.004-.377-5.433-1.017-.714-.32-1.338-.72-1.793-1.203C4.315 19.293 4 18.69 4 18V6Zm1.5 0c0 .207.09.46.365.75.279.296.717.596 1.315.864 1.195.535 2.899.886 4.82.886 1.921 0 3.625-.35 4.82-.886.598-.268 1.036-.568 1.315-.864.275-.29.365-.543.365-.75 0-.207-.09-.46-.365-.75-.279-.296-.717-.596-1.315-.864C15.625 3.851 13.92 3.5 12 3.5c-1.921 0-3.625.35-4.82.886-.598.268-1.036.568-1.315.864-.275.29-.365.543-.365.75Zm11 15a4.48 4.48 0 0 0 2.607-.832l2.613 2.612a.75.75 0 1 0 1.06-1.06l-2.612-2.613A4.5 4.5 0 1 0 16.5 21Zm0-1.5a3 3 0 1 1 0-6 3 3 0 0 1 0 6Z"
                        fill="currentColor"
                      />
                    </svg>
                  </template>
                </MenuFlyoutItem>
                <MenuFlyoutItem
                  @click="
                    () => {
                      uploadedRdpFileData = getEmptyRemoteAppData();
                      openCreationDialog();
                    }
                  "
                >
                  {{ t('registryApps.manager.fromManualEntry') }}
                  <template #icon>
                    <svg viewBox="0 0 24 24">
                      <path
                        d="M6.25 3.5a.75.75 0 0 0-.75.75v15.5c0 .414.336.75.75.75h3.78a2.077 2.077 0 0 0 .27 1.5H6.25A2.25 2.25 0 0 1 4 19.75V4.25A2.25 2.25 0 0 1 6.25 2h6.086c.464 0 .909.184 1.237.513l5.914 5.914c.329.328.513.773.513 1.237V10h-.13a3.324 3.324 0 0 0-.332 0H14a2 2 0 0 1-2-2V3.5H6.25Zm7.25 1.06V8a.5.5 0 0 0 .5.5h3.44L13.5 4.56Z"
                        fill="currentColor"
                      />
                      <path
                        d="M19.713 11h.002a2.286 2.286 0 0 1 1.615 3.902l-5.902 5.902a2.684 2.684 0 0 1-1.247.707l-1.831.457a1.087 1.087 0 0 1-1.318-1.318l.457-1.83c.118-.473.362-.904.707-1.248l5.902-5.902a2.278 2.278 0 0 1 1.615-.67Z"
                        fill="currentColor"
                      />
                    </svg>
                  </template>
                </MenuFlyoutItem>
                <MenuFlyoutItem
                  indented
                  @click="
                    pickRDPFile()
                      .then((info) => {
                        openCreationDialog();
                        uploadedRdpFileData = info;
                      })
                      .catch((error) => {
                        showConfirm(t('registryApps.manager.rdpUploadFail.title'), error, '', t('dialog.ok'));
                      })
                  "
                >
                  {{ t('registryApps.manager.fromRdpFile') }}
                </MenuFlyoutItem>
              </template>
            </Button>

            <!-- desktops -->
            <Button
              @click="
                () => {
                  uploadedRdpFileData = getEmptyDesktopData();
                  openCreationDialog();
                }
              "
              @auxclick="
                () => {
                  uploadedRdpFileData = getEmptyDesktopData();
                  openCreationDialog();
                }
              "
            >
              <template #icon>
                <svg viewBox="0 0 24 24">
                  <path
                    d="M 17.5,12 C 20.53765,12 23,14.46235 23,17.5 23,20.53765 20.53765,23 17.5,23 14.46235,23 12,20.53765 12,17.5 12,14.46235 14.46235,12 17.5,12 Z m 0,2.75 a 0.4125,0.4125 0 0 0 -0.40865,0.3564 l -0.0038,0.0561 v 1.925 h -1.925 a 0.4125,0.4125 0 0 0 -0.0561,0.82115 l 0.0561,0.0038 h 1.925 v 1.925 a 0.4125,0.4125 0 0 0 0.82115,0.0561 l 0.0038,-0.0561 v -1.925 h 1.925 a 0.4125,0.4125 0 0 0 0.0561,-0.82115 l -0.0561,-0.0038 h -1.925 v -1.925 A 0.4125,0.4125 0 0 0 17.5,14.75 Z"
                    fill="currentColor"
                  />
                  <path
                    d="M 4.25,3 4.0957031,3.00586 A 2.25,2.25 0 0 0 2,5.25 V 15.751953 L 2.00586,15.90625 A 2.25,2.25 0 0 0 4.25,18.001953 H 8.4980469 V 20.5 H 6.75 L 6.6484375,20.5078 A 0.75,0.75 0 0 0 6.75,22 h 6.113281 A 6.4615383,6.4615383 0 0 1 11.777344,20.5 H 9.9980469 V 18.001953 H 11.058594 A 6.4615383,6.4615383 0 0 1 11.039062,17.5 6.4615383,6.4615383 0 0 1 11.115234,16.501953 H 4.25 l -0.1015625,-0.0078 C 3.7824375,16.445141 3.5,16.131953 3.5,15.751953 V 5.25 L 3.50781,5.1484375 A 0.75,0.75 0 0 1 4.25,4.5 h 15.498047 l 0.103515,0.00781 A 0.75,0.75 0 0 1 20.498047,5.25 v 6.527344 a 6.4615383,6.4615383 0 0 1 1.5,1.083984 V 5.25 l -0.0039,-0.1542969 A 2.25,2.25 0 0 0 19.748047,3 Z"
                    fill="currentColor"
                  />
                </svg>
              </template>
              {{ t('registryApps.manager.addDesktop') }}
              <template #menu>
                <MenuFlyoutItem
                  @click="
                    () => {
                      uploadedRdpFileData = getEmptyDesktopData();
                      openCreationDialog();
                    }
                  "
                >
                  {{ t('registryApps.manager.fromManualEntry') }}
                  <template #icon>
                    <svg viewBox="0 0 24 24">
                      <path
                        d="M6.25 3.5a.75.75 0 0 0-.75.75v15.5c0 .414.336.75.75.75h3.78a2.077 2.077 0 0 0 .27 1.5H6.25A2.25 2.25 0 0 1 4 19.75V4.25A2.25 2.25 0 0 1 6.25 2h6.086c.464 0 .909.184 1.237.513l5.914 5.914c.329.328.513.773.513 1.237V10h-.13a3.324 3.324 0 0 0-.332 0H14a2 2 0 0 1-2-2V3.5H6.25Zm7.25 1.06V8a.5.5 0 0 0 .5.5h3.44L13.5 4.56Z"
                        fill="currentColor"
                      />
                      <path
                        d="M19.713 11h.002a2.286 2.286 0 0 1 1.615 3.902l-5.902 5.902a2.684 2.684 0 0 1-1.247.707l-1.831.457a1.087 1.087 0 0 1-1.318-1.318l.457-1.83c.118-.473.362-.904.707-1.248l5.902-5.902a2.278 2.278 0 0 1 1.615-.67Z"
                        fill="currentColor"
                      />
                    </svg>
                  </template>
                </MenuFlyoutItem>
                <MenuFlyoutItem
                  indented
                  @click="
                    pickRDPFile()
                      .then((info) => {
                        openCreationDialog();
                        uploadedRdpFileData = info;
                      })
                      .catch((error) => {
                        showConfirm(t('registryApps.manager.rdpUploadFail.title'), error, '', t('dialog.ok'));
                      })
                  "
                >
                  {{ t('registryApps.manager.fromRdpFile') }}
                </MenuFlyoutItem>
              </template>
            </Button>
          </ManagedResourceCreateDialog>
        </ManagedResourceCreateDiscoveryDialog>

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
            <MenuFlyoutItem
              @click="
                () => {
                  refetch();
                }
              "
              :disabled="isPending || isFetching"
            >
              {{ t('registryApps.manager.refresh') }}
              <template #icon>
                <svg viewBox="0 0 24 24">
                  <path
                    d="M12 4.5C7.85786 4.5 4.5 7.85786 4.5 12C4.5 16.1421 7.85786 19.5 12 19.5C16.1421 19.5 19.5 16.1421 19.5 12C19.5 11.6236 19.4723 11.2538 19.4188 10.8923C19.3515 10.4382 19.6839 10 20.1429 10C20.5138 10 20.839 10.2562 20.8953 10.6228C20.9642 11.0718 21 11.5317 21 12C21 16.9706 16.9706 21 12 21C7.02944 21 3 16.9706 3 12C3 7.02944 7.02944 3 12 3C14.3051 3 16.4077 3.86656 18 5.29168V4.25C18 3.83579 18.3358 3.5 18.75 3.5C19.1642 3.5 19.5 3.83579 19.5 4.25V7.25C19.5 7.66421 19.1642 8 18.75 8H15.75C15.3358 8 15 7.66421 15 7.25C15 6.83579 15.3358 6.5 15.75 6.5H17.0991C15.7609 5.25883 13.9691 4.5 12 4.5Z"
                    fill="currentColor"
                  />
                </svg>
              </template>
            </MenuFlyoutItem>
            <MenuFlyoutItem @click="exportResourceBundle" :disabled="isPending || isFetching" :indented="true">
              {{ t('registryApps.manager.export') }}
            </MenuFlyoutItem>
          </template>
        </MenuFlyout>
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
      <pre v-if="error instanceof Error">{{ error.message }}</pre>
      <pre v-else>{{ error }}</pre>
    </details>
  </InfoBar>

  <div v-else class="apps-list">
    <ManagedResourceEditDialog
      v-for="app in data"
      :identifier="app.identifier"
      :display-name="app.name"
      #default="{ open }"
      @after-save="handleAppOrDesktopChange"
      @after-delete="handleAppOrDesktopChange"
    >
      <Button
        v-if="
          // registry-based resources can only be managed if the server claims the capability
          app.source !== ManagedResourceSource.File
            ? capabilities.supportsManageRegistryApps && capabilities.supportsReadRegistryApps
            : true
        "
        @click="open"
        :disabled="!isSecureContext || needsSignInAgain"
        :class="{ notIncludedInWorksapce: !app.includeInWorkspace }"
      >
        <img
          :key="app.identifier + app.iconIndex + app.iconPath"
          :src="`${iisBase}${buildManagedIconPath(
            app.source === ManagedResourceSource.File
              ? {
                  identifier: app.identifier,
                  isRemoteApp: !!app.remoteAppProperties,
                  isManagedFileResource: true,
                }
              : {
                  iconPath: app.iconPath,
                  iconIndex: app.iconIndex,
                  isRemoteApp: !!app.remoteAppProperties,
                  isManagedFileResource: false,
                },
            dataUpdatedAt,
            undefined,
            true
          )}`"
          alt=""
          width="24"
          height="24"
        />
        <TextBlock>
          {{ app.name }}
          <span v-if="app.source === ManagedResourceSource.File">ᵠ</span>
        </TextBlock>
      </Button>
    </ManagedResourceEditDialog>
  </div>
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
    gap: 8px;
  }

  .apps-list {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
    gap: 12px;
    max-height: 600px;
    overflow: auto;
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
</style>
