<script setup lang="ts">
  import { Button, ContentDialog, TextBlock } from '$components';
  import { RegistryRemoteAppCreateDiscoveryDialog, RegistryRemoteAppEditDialog } from '$dialogs';
  import { useCoreDataStore } from '$stores';
  import { ResourceManagementSchemas } from '$utils';
  import { useQuery } from '@tanstack/vue-query';
  import { useTranslation } from 'i18next-vue';

  const { iisBase, appBase } = useCoreDataStore();
  const { t } = useTranslation();

  const { isPending, isFetching, isError, data, error, refetch } = useQuery({
    queryKey: ['remote-app-registry'],
    queryFn: async () => {
      return fetch('/api/management/resources/registered')
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
        <RegistryRemoteAppCreateDiscoveryDialog #default="{ open }" @after-save="refetch">
          <Button @click="open">
            <template #icon>
              <svg viewBox="0 0 24 24">
                <path
                  d="M12 3.25C12.4142 3.25 12.75 3.58579 12.75 4V11.25H20C20.4142 11.25 20.75 11.5858 20.75 12C20.75 12.4142 20.4142 12.75 20 12.75H12.75V20C12.75 20.4142 12.4142 20.75 12 20.75C11.5858 20.75 11.25 20.4142 11.25 20V12.75H4C3.58579 12.75 3.25 12.4142 3.25 12C3.25 11.5858 3.58579 11.25 4 11.25H11.25V4C11.25 3.58579 11.5858 3.25 12 3.25Z"
                  fill="currentColor"
                />
              </svg>
            </template>
            {{ t('registryApps.manager.add') }}
          </Button>
        </RegistryRemoteAppCreateDiscoveryDialog>
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
        <RegistryRemoteAppEditDialog
          v-for="app in data"
          :registry-key="app.key"
          :display-name="app.name"
          #default="{ open }"
          @after-save="refetch"
          @after-delete="refetch"
        >
          <Button @click="open">
            <img
              :key="app.key + app.iconIndex + app.iconPath"
              :src="`${iisBase}api/resources/image/registry!${app.key}?format=png&__cacheBust=${app.iconIndex}+${app.iconPath}`"
              alt=""
              width="24"
              height="24"
              @error="($event) => {
              ($event.target as HTMLImageElement).src = `${appBase}api/resources/image/default.ico?format=png`;
            }"
            />
            <TextBlock>{{ app.name }}</TextBlock>
          </Button>
        </RegistryRemoteAppEditDialog>
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
