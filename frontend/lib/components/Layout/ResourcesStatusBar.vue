<script setup lang="ts">
  import { TextBlock } from '$components';
  import IconButton from '$components/IconButton/IconButton.vue';
  import { content, grid, list, rectangle, tiles } from '$icons';
  import { isStatusBarVisible } from '$utils';
  import { useTranslation } from 'i18next-vue';
  import { getViewShortcutHint, type ViewMode } from './viewShortcuts';

  defineProps<{
    /** The total number of items shown. */
    itemCount: number;
    /** The views to offer as buttons. */
    modes: ViewMode[];
  }>();

  const mode = defineModel<ViewMode>('mode', { required: true });
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
      :title="`${t(`actions.view.${option}`)} (${getViewShortcutHint(option)})`"
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
    inline-size: var(--status-bar-size);
    block-size: var(--status-bar-size);
    min-inline-size: 0;
    min-block-size: 0;
    padding: 0;
  }
  #status-bar-end .icon-button svg {
    inline-size: 12px;
  }
  #status-bar-end .icon-button.selected {
    background-color: var(--wui-subtle-secondary);
  }
</style>
