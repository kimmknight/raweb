<script setup lang="ts">
  import { TextBlock } from '$components';
  import { useTranslation } from 'i18next-vue';
  import { computed } from 'vue';

  const { translationKeyPrefix, translationLang = 'en-US' } = defineProps<{
    translationKeyPrefix: string;
    translationLang?: string;
    initialOpen?: boolean;
  }>();

  const { t } = useTranslation(undefined, { lng: translationLang, keyPrefix: translationKeyPrefix });
  const title = computed(() => t('title'));
  const description = computed(() => t('help'));
</script>

<template>
  <details :open="initialOpen">
    <summary>Policy details in RAWeb</summary>

    <p><TextBlock variant="bodyStrong">Policy title:</TextBlock> {{ title }}</p>

    <p>
      <TextBlock variant="body" style="white-space: break-spaces; overflow-wrap: anywhere">
        <TextBlock variant="bodyStrong">Policy description:</TextBlock> {{ description }}
      </TextBlock>
    </p>
  </details>
</template>
