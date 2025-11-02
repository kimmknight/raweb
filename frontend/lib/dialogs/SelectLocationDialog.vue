<script setup lang="ts">
  import { Button, ContentDialog, PickerItem } from '$components';
  import { useTranslation } from 'i18next-vue';

  const { t } = useTranslation();

  const { locations } = defineProps<{
    locations: string[];
  }>();

  const selectedLocation = defineModel<string>();
</script>

<template>
  <ContentDialog :title="t('registryApps.manager.selectLocation.title')">
    <template #opener="{ close, open, popoverId }">
      <slot name="default" :close="close" :open="open" :popover-id="popoverId" />
    </template>

    <template #default="{ close, popoverId }">
      <PickerItem
        v-for="location in locations"
        :key="popoverId + location"
        :name="`${popoverId}-location-${location}`"
        :value="location"
        v-model="selectedLocation"
        @dblclick="close"
      >
        {{ location }}
      </PickerItem>
    </template>

    <template #footer="{ close }">
      <Button @click="close">{{ t('dialog.once') }}</Button>
    </template>
  </ContentDialog>
</template>
