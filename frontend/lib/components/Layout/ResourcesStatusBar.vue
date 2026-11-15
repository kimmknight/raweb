<script setup lang="ts">
  import { TextBlock } from '$components';
  import IconButton from '$components/IconButton/IconButton.vue';
  import { content, grid, list, rectangle, tiles } from '$icons';
  import { isStatusBarVisible } from '$utils';
  import { useTranslation } from 'i18next-vue';

  type Mode = 'card' | 'list' | 'grid' | 'tile' | 'details';

  defineProps<{
    /** The total number of items shown. */
    itemCount: number;
    /** The views to offer as buttons. */
    modes: Mode[];
  }>();

  const mode = defineModel<Mode>('mode', { required: true });
  const { t } = useTranslation();

  const modeIcons = { card: rectangle, list: content, grid, tile: tiles, details: list };
</script>

<template>
  <Teleport v-if="isStatusBarVisible(mode)" to="#status-bar-start" defer>
    <TextBlock variant="caption" tag="span">{{ t('resourceTable.itemCount', { count: itemCount }) }}</TextBlock>
  </Teleport>
  <Teleport v-if="isStatusBarVisible(mode)" to="#status-bar-end" defer>
    <IconButton
      v-for="option in modes"
      :key="option"
      :class="{ selected: mode === option }"
      :title="t(`actions.view.${option}`)"
      :aria-label="t(`actions.view.${option}`)"
      :aria-pressed="mode === option"
      @click="mode = option"
    >
      <span v-swap="modeIcons[option]"></span>
    </IconButton>
  </Teleport>
</template>

<style>
  #status-bar-end .icon-button {
    inline-size: 30px;
    block-size: 30px;
    padding: 0;
  }
  #status-bar-end .icon-button.selected {
    background-color: var(--wui-subtle-fill-secondary);
  }
</style>
