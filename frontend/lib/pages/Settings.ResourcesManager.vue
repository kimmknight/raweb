<script setup lang="ts">
  import {
    AnimatedIcon,
    Button,
    InfoBar,
    MenuFlyout,
    MenuFlyoutDivider,
    MenuFlyoutItem,
    ProgressRing,
    TextBlock,
  } from '$components';
  import { ManagedResourceCreateDiscoveryDialog, ManagedResourceEditDialog, showConfirm } from '$dialogs';
  import BulkImportDialog from '$dialogs/BulkImportDialog.vue';
  import { useCoreDataStore } from '$stores';
  import {
    buildManagedIconPath,
    openSignInPagePopup,
    pickAnyResourceFile,
    PreventableEvent,
    readRdpFile,
    ResourceManagementSchemas,
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
                <MenuFlyoutItem @click="openCreationDialog(getEmptyRemoteAppData())">
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
                  <template #icon>
                    <svg viewBox="0 0 24 24">
                      <path
                        d="m6.747 3 10.506.002a3.752 3.752 0 0 1 3.745 3.551l.005.2v4.492a.75.75 0 0 1-1.493.102l-.007-.102V6.752c0-1.19-.925-2.165-2.096-2.245l-.154-.005L6.747 4.5a2.249 2.249 0 0 0-2.242 2.057l-.008.159.002 10.536c.001 1.19.926 2.165 2.097 2.245l.154.005h4.496a.75.75 0 0 1 .102 1.493l-.102.007H6.75a3.752 3.752 0 0 1-3.745-3.55l-.006-.2-.001-10.5.004-.203a3.749 3.749 0 0 1 3.546-3.544l.2-.005ZM9.75 9h6.504a.75.75 0 0 1 .102 1.493l-.102.007-4.694-.001 7.224 7.22a.75.75 0 0 1 .073.977l-.073.084a.75.75 0 0 1-.977.073l-.084-.073-7.223-7.22v4.691a.75.75 0 0 1-.648.743l-.102.007a.75.75 0 0 1-.743-.648L9 16.25V9.734c0-.025.002-.05.005-.076l.021-.108.035-.096.005-.012a.721.721 0 0 1 .153-.223l.044-.04.081-.06.06-.035.095-.042.067-.02.062-.013L9.72 9h6.533H9.75Z"
                        fill="currentColor"
                      />
                    </svg>
                  </template>
                </MenuFlyoutItem>
              </template>
            </Button>

            <!-- desktops -->
            <Button
              @click="openCreationDialog(getEmptyDesktopData())"
              @auxclick="openCreationDialog(getEmptyDesktopData())"
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
                <MenuFlyoutItem @click="openCreationDialog(getEmptyDesktopData())">
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
                  <template #icon>
                    <svg viewBox="0 0 24 24">
                      <path
                        d="m6.747 3 10.506.002a3.752 3.752 0 0 1 3.745 3.551l.005.2v4.492a.75.75 0 0 1-1.493.102l-.007-.102V6.752c0-1.19-.925-2.165-2.096-2.245l-.154-.005L6.747 4.5a2.249 2.249 0 0 0-2.242 2.057l-.008.159.002 10.536c.001 1.19.926 2.165 2.097 2.245l.154.005h4.496a.75.75 0 0 1 .102 1.493l-.102.007H6.75a3.752 3.752 0 0 1-3.745-3.55l-.006-.2-.001-10.5.004-.203a3.749 3.749 0 0 1 3.546-3.544l.2-.005ZM9.75 9h6.504a.75.75 0 0 1 .102 1.493l-.102.007-4.694-.001 7.224 7.22a.75.75 0 0 1 .073.977l-.073.084a.75.75 0 0 1-.977.073l-.084-.073-7.223-7.22v4.691a.75.75 0 0 1-.648.743l-.102.007a.75.75 0 0 1-.743-.648L9 16.25V9.734c0-.025.002-.05.005-.076l.021-.108.035-.096.005-.012a.721.721 0 0 1 .153-.223l.044-.04.081-.06.06-.035.095-.042.067-.02.062-.013L9.72 9h6.533H9.75Z"
                        fill="currentColor"
                      />
                    </svg>
                  </template>
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
                  <template #icon>
                    <svg viewBox="0 0 24 24">
                      <path
                        d="M12 4.5C7.85786 4.5 4.5 7.85786 4.5 12C4.5 16.1421 7.85786 19.5 12 19.5C16.1421 19.5 19.5 16.1421 19.5 12C19.5 11.6236 19.4723 11.2538 19.4188 10.8923C19.3515 10.4382 19.6839 10 20.1429 10C20.5138 10 20.839 10.2562 20.8953 10.6228C20.9642 11.0718 21 11.5317 21 12C21 16.9706 16.9706 21 12 21C7.02944 21 3 16.9706 3 12C3 7.02944 7.02944 3 12 3C14.3051 3 16.4077 3.86656 18 5.29168V4.25C18 3.83579 18.3358 3.5 18.75 3.5C19.1642 3.5 19.5 3.83579 19.5 4.25V7.25C19.5 7.66421 19.1642 8 18.75 8H15.75C15.3358 8 15 7.66421 15 7.25C15 6.83579 15.3358 6.5 15.75 6.5H17.0991C15.7609 5.25883 13.9691 4.5 12 4.5Z"
                        fill="currentColor"
                      />
                    </svg>
                  </template>
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
                  <template #icon>
                    <svg viewBox="0 0 24 24">
                      <path
                        d="M8.75 1A7.75 7.75 0 0 0 1 8.75v9.5a.75.75 0 0 0 .75.75.75.75 0 0 0 .75-.75v-9.5A6.22 6.22 0 0 1 8.75 2.5h9.5a.75.75 0 0 0 .75-.75.75.75 0 0 0-.75-.75Zm0 4h10.5c2 0 3.64 1.57 3.75 3.55v4.7a.75.75 0 0 1-1.49.1v-4.6c0-1.19-.93-2.16-2.1-2.24h-.16L8.75 6.5c-1.18 0-2.15.9-2.25 2.06v10.69c0 1.2.93 2.17 2.1 2.25h4.65a.752.752 0 0 1 .1 1.5h-4.6c-2 0-3.64-1.57-3.75-3.55V8.55A3.75 3.75 0 0 1 8.55 5l.2-.01zm3 6h6.5a.752.752 0 0 1 .1 1.5h-4.79l7.22 7.22c.27.27.3.68.08.98l-.08.08a.75.75 0 0 1-.97.07l-.09-.07-7.22-7.22v4.7c0 .37-.28.68-.65.73l-.1.01a.75.75 0 0 1-.74-.65l-.01-.1v-6.52a1 1 0 0 1 0-.07l.03-.11.03-.1a.7.7 0 0 1 .16-.23l.04-.04.08-.06.06-.04.1-.04.07-.02.06-.01.1-.01h6.52z"
                        fill="currentColor"
                      />
                    </svg>
                  </template>
                </MenuFlyoutItem>
                <MenuFlyoutDivider />
                <MenuFlyoutItem @click="exportResourceBundle" :disabled="isPending || isFetching">
                  {{ t('registryApps.manager.export') }}
                  <template #icon>
                    <svg viewBox="0 0 24 24">
                      <path
                        d="M2.752 4.5a.75.75 0 0 1 .744.648l.006.102L3.5 18.254a.75.75 0 0 1-1.493.102L2 18.254 2.002 5.25a.75.75 0 0 1 .75-.75Zm12.895 1.804.073-.084a.75.75 0 0 1 .976-.073l.084.073 4.997 4.997a.75.75 0 0 1 .073.976l-.073.085-4.996 5.003a.75.75 0 0 1-1.134-.976l.072-.084 3.711-3.717H5.753a.75.75 0 0 1-.743-.647l-.007-.102a.75.75 0 0 1 .648-.743l.102-.007 13.69-.001L15.72 7.28a.75.75 0 0 1-.073-.976l.073-.084-.073.084Z"
                        fill="currentColor"
                      />
                    </svg>
                  </template>
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
                  <template #icon>
                    <svg viewBox="0 0 24 24">
                      <path
                        d="M21.25 4.5a.75.75 0 0 1 .743.648L22 5.25v13.5a.75.75 0 0 1-1.493.102l-.007-.102V5.25a.75.75 0 0 1 .75-.75Zm-9.04 1.887.083-.094a1 1 0 0 1 1.32-.083l.094.083 4.997 4.998a1 1 0 0 1 .083 1.32l-.083.093-4.996 5.004a1 1 0 0 1-1.499-1.32l.083-.094L15.581 13H3a1 1 0 0 1-.993-.883L2 12a1 1 0 0 1 .883-.993L3 11h12.584l-3.291-3.293a1 1 0 0 1-.083-1.32l.083-.094-.083.094Z"
                        fill="currentColor"
                      />
                    </svg>
                  </template>
                </MenuFlyoutItem>
              </template>
            </MenuFlyout>
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
                <template #icon>
                  <svg viewBox="0 0 24 24">
                    <path
                      d="M2.752 4.5a.75.75 0 0 1 .744.648l.006.102L3.5 18.254a.75.75 0 0 1-1.493.102L2 18.254 2.002 5.25a.75.75 0 0 1 .75-.75Zm12.895 1.804.073-.084a.75.75 0 0 1 .976-.073l.084.073 4.997 4.997a.75.75 0 0 1 .073.976l-.073.085-4.996 5.003a.75.75 0 0 1-1.134-.976l.072-.084 3.711-3.717H5.753a.75.75 0 0 1-.743-.647l-.007-.102a.75.75 0 0 1 .648-.743l.102-.007 13.69-.001L15.72 7.28a.75.75 0 0 1-.073-.976l.073-.084-.073.084Z"
                      fill="currentColor"
                    />
                  </svg>
                </template>
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
                <template #icon>
                  <svg viewBox="0 0 24 24">
                    <path
                      d="M2.752 4.5a.75.75 0 0 1 .744.648l.006.102L3.5 18.254a.75.75 0 0 1-1.493.102L2 18.254 2.002 5.25a.75.75 0 0 1 .75-.75Zm12.895 1.804.073-.084a.75.75 0 0 1 .976-.073l.084.073 4.997 4.997a.75.75 0 0 1 .073.976l-.073.085-4.996 5.003a.75.75 0 0 1-1.134-.976l.072-.084 3.711-3.717H5.753a.75.75 0 0 1-.743-.647l-.007-.102a.75.75 0 0 1 .648-.743l.102-.007 13.69-.001L15.72 7.28a.75.75 0 0 1-.073-.976l.073-.084-.073.084Z"
                      fill="currentColor"
                    />
                  </svg>
                </template>
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
