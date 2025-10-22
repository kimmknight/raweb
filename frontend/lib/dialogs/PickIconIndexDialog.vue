<script setup lang="ts">
  import { Button, ContentDialog, TextBlock, TextBox } from '$components';
  import { useQuery } from '@tanstack/vue-query';
  import { useTranslation } from 'i18next-vue';
  import z from 'zod';

  const { t } = useTranslation();

  const { currentIndex } = defineProps<{
    currentIndex?: number;
  }>();

  const iconPath = defineModel<string>('iconPath');

  const { isPending, isFetching, isError, data, error, refetch, dataUpdatedAt } = useQuery({
    queryKey: ['icon-indices', iconPath.value],
    queryFn: async () => {
      const staticIconPath = iconPath.value;
      if (!staticIconPath) {
        throw new Error('Icon path is empty');
      }

      return fetch(`/api/management/resources/icon/indices?path=${encodeURIComponent(staticIconPath)}`)
        .then(async (res) => {
          if (!res.ok) {
            await res.json().then((err) => {
              if (err && 'ExceptionMessage' in err) {
                throw new Error(err.ExceptionMessage);
              }
            });
            throw new Error(
              `Error fetching icon indices for path "${staticIconPath}": ${res.status} ${res.statusText}`
            );
          }
          return res.json();
        })
        .then((data) => {
          const iconCount = z.number().parse(data);
          return {
            iconCount,
            iconPath: staticIconPath,
          };
        });
    },
    enabled: false, // do not fetch automatically
  });

  const emit = defineEmits<{
    (e: 'indexSelected', index: number, iconPath: string): void;
    (e: 'onClose'): void;
  }>();
</script>

<template>
  <ContentDialog
    @open="() => refetch()"
    @close="emit('onClose')"
    :close-on-backdrop-click="false"
    :title="t('registryApps.manager.iconPicker.title')"
    size="maxer"
    max-height="680px"
    fill-height
    :updating="isFetching"
    :loading="isPending"
    :error="isError && !data && error !== null ? error : false"
  >
    <template #opener="{ close, open, popoverId }">
      <slot name="default" :close="close" :open="open" :popover-id="popoverId" />
    </template>

    <template #default="{ close, popoverId }">
      <div
        class="header-form"
        @keydown="
          (event) => {
            if (!event.target) {
              return;
            }

            const closestDialog = (event.target as HTMLElement)?.closest?.('dialog');
            if (!closestDialog) {
              return;
            }

            if (event.key === 'Enter' && !event.shiftKey && closestDialog.id === popoverId) {
              event.preventDefault();
              refetch();
            }
          }
        "
      >
        <TextBox v-model:value="iconPath"></TextBox>
      </div>
      <div v-if="error && !isFetching" style="margin-top: 12px">
        <TextBlock>Unable to find icons for the provided path.</TextBlock>
      </div>
      <div class="icons-list" v-else-if="data && !isFetching">
        <Button
          v-for="index in [...Array(data.iconCount).keys()]"
          @click="
            () => {
              emit('indexSelected', index, data?.iconPath ?? iconPath ?? '');
              close();
            }
          "
          :disabled="index === currentIndex"
        >
          <img
            :src="`/api/management/resources/icon?path=${encodeURIComponent(
              data.iconPath ?? ''
            )}&index=${index}&__cacheBust=${dataUpdatedAt}`"
            alt=""
            width="32"
            height="32"
          />
          <TextBlock>{{ index }}</TextBlock>
        </Button>
      </div>
    </template>

    <template #footer="{ close }">
      <Button @click="close">Cancel</Button>
    </template>
  </ContentDialog>
</template>

<style scoped>
  .icons-list {
    display: grid;
    grid-template-columns: repeat(auto-fit, 80px);
    gap: 12px;
    margin-top: 12px;
  }
  .icons-list :deep(> .button img) {
    user-select: none;
  }
  .icons-list :deep(> .button.disabled img) {
    opacity: 0.5;
  }
  .icons-list :deep(> .button.disabled) {
    cursor: not-allowed;
  }
  .icons-list :deep(> .button.disabled::after) {
    content: '';
    position: absolute;
    width: 38%;
    height: 3px;
    left: 31%;
    bottom: 0;
    background-color: var(--wui-accent-default);
    border-radius: var(--wui-control-corner-radius);
  }
  .icons-list :deep(> .button > span) {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 8px 0 2px 0;
    gap: 4px;
  }

  .header-form {
    position: sticky;
    top: var(--title-height);
    z-index: 9;
    background-color: var(--wui-background-default);
    border-bottom: 1px solid var(--wui-surface-stroke-default);
    margin-left: calc(-1 * var(--inner-padding));
    margin-right: calc(-1 * var(--inner-padding));
    padding: 0 var(--inner-padding) 12px var(--inner-padding);
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
