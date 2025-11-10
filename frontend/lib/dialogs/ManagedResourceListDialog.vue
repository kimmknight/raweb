<script setup lang="ts">
  import { Button, ContentDialog, MenuFlyoutItem, TextBlock } from '$components';
  import {
    ManagedResourceCreateDialog,
    ManagedResourceCreateDiscoveryDialog,
    ManagedResourceEditDialog,
    showConfirm,
  } from '$dialogs';
  import { useCoreDataStore } from '$stores';
  import { pickRDPFile, ResourceManagementSchemas } from '$utils';
  import { ManagedResourceSource } from '$utils/schemas/ResourceManagementSchemas';
  import { useQuery } from '@tanstack/vue-query';
  import { useTranslation } from 'i18next-vue';
  import { ref } from 'vue';

  const { iisBase, capabilities } = useCoreDataStore();
  const { t } = useTranslation();

  const { isPending, isFetching, isError, data, error, refetch } = useQuery({
    queryKey: ['remote-app-registry'],
    queryFn: async () => {
      return fetch(`${iisBase}api/management/resources/registered`)
        .then(async (res) => {
          if (!res.ok) {
            await res.json().then((err) => {
              if (err && 'ExceptionMessage' in err) {
                throw new Error(err.ExceptionMessage);
              }
            });
            throw new Error(`Error fetching remote app registry: ${res.status} ${res.statusText}`);
          }
          return res.json();
        })
        .then((data) => ResourceManagementSchemas.RegistryRemoteApp.App.array().parse(data));
    },
    enabled: false, // do not fetch automatically
  });

  const uploadedRdpFileData = ref<Awaited<ReturnType<typeof pickRDPFile>>>();
  const uploadedRdpFileKey = ref(0);

  function handleAppOrDesktopChange() {
    refetch();
    emit('appOrDesktopChange');
  }

  const emit = defineEmits<{
    (e: 'appOrDesktopChange'): void;
  }>();
</script>

<template>
  <ContentDialog
    @open="() => refetch()"
    :close-on-backdrop-click="false"
    :title="t('registryApps.manager.title')"
    size="maxest"
    max-height="800px"
    fill-height
    :updating="isFetching"
    :loading="isPending"
    :error="isError && error !== null ? error : false"
  >
    <template #opener="{ close, open, popoverId }">
      <slot name="default" :close="close" :open="open" :popover-id="popoverId" />
    </template>

    <template #default>
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
            <Button @click="openDiscoveryDialog">
              <template #icon>
                <svg viewBox="0 0 24 24">
                  <path
                    d="M12 3.25C12.4142 3.25 12.75 3.58579 12.75 4V11.25H20C20.4142 11.25 20.75 11.5858 20.75 12C20.75 12.4142 20.4142 12.75 20 12.75H12.75V20C12.75 20.4142 12.4142 20.75 12 20.75C11.5858 20.75 11.25 20.4142 11.25 20V12.75H4C3.58579 12.75 3.25 12.4142 3.25 12C3.25 11.5858 3.58579 11.25 4 11.25H11.25V4C11.25 3.58579 11.5858 3.25 12 3.25Z"
                    fill="currentColor"
                  />
                </svg>
              </template>
              {{ t('registryApps.manager.add') }}
              <template #menu v-if="capabilities.supportsCentralizedPublishing">
                <MenuFlyoutItem @click="openDiscoveryDialog">
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
        <Button @click="refetch" :disabled="isPending || isFetching">
          <template #icon>
            <svg viewBox="0 0 24 24">
              <path
                d="M12 4.5C7.85786 4.5 4.5 7.85786 4.5 12C4.5 16.1421 7.85786 19.5 12 19.5C16.1421 19.5 19.5 16.1421 19.5 12C19.5 11.6236 19.4723 11.2538 19.4188 10.8923C19.3515 10.4382 19.6839 10 20.1429 10C20.5138 10 20.839 10.2562 20.8953 10.6228C20.9642 11.0718 21 11.5317 21 12C21 16.9706 16.9706 21 12 21C7.02944 21 3 16.9706 3 12C3 7.02944 7.02944 3 12 3C14.3051 3 16.4077 3.86656 18 5.29168V4.25C18 3.83579 18.3358 3.5 18.75 3.5C19.1642 3.5 19.5 3.83579 19.5 4.25V7.25C19.5 7.66421 19.1642 8 18.75 8H15.75C15.3358 8 15 7.66421 15 7.25C15 6.83579 15.3358 6.5 15.75 6.5H17.0991C15.7609 5.25883 13.9691 4.5 12 4.5Z"
                fill="currentColor"
              />
            </svg>
          </template>
          {{ t('registryApps.manager.refresh') }}
        </Button>
      </div>
      <div class="apps-list">
        <ManagedResourceEditDialog
          v-for="app in data"
          :identifier="app.identifier"
          :display-name="app.name"
          #default="{ open }"
          @after-save="handleAppOrDesktopChange"
          @after-delete="handleAppOrDesktopChange"
        >
          <Button @click="open">
            <img
              :key="app.identifier + app.iconIndex + app.iconPath"
              :src="`${iisBase}api/management/resources/icon?path=${encodeURIComponent(
                app.iconPath || ''
              )}&index=${app.iconIndex}${
                app.source === ManagedResourceSource.File ? '&fallback=../lib/assets/remoteicon.png' : ''
              }&__cacheBust=${app.iconIndex}+${app.iconPath}`"
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

    <template #footer="{ close }">
      <Button @click="close">Close</Button>
    </template>
  </ContentDialog>
</template>

<style scoped>
  .actions {
    margin: 12px 0 8px 0;
    display: flex;
    flex-direction: row;
    gap: 8px;
    padding: 8px 0;
    border-bottom: 1px solid var(--wui-divider-stroke-default);
    border-top: 1px solid var(--wui-divider-stroke-default);
  }

  .apps-list {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(184px, 1fr));
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
</style>
