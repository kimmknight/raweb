<script setup lang="ts">
  import { inject, onMounted, onUnmounted, provide, useTemplateRef, watch } from 'vue';
  import { IN_SELECTION_TRACK_KEY, SELECTION_TRACK_KEY } from './keys';

  const { selected = false, indicatorHeight } = defineProps<{
    /** Whether this item is currently the active selection. */
    selected?: boolean;
    /** Height of the accent line indicator in pixels. */
    indicatorHeight?: number;
  }>();

  const trackHandle = inject(SELECTION_TRACK_KEY, null);
  const selectableElementWrapper = useTemplateRef<HTMLElement>('selectableElementWrapper');

  function getSelectableElement(): HTMLElement | null {
    // the wrapper is display:contents, so we measure its first real child instead
    return (selectableElementWrapper.value?.firstElementChild as HTMLElement) ?? null;
  }

  onMounted(() => {
    const element = getSelectableElement();
    if (element) {
      trackHandle?.register(element);
      if (selected) {
        trackHandle?.select(element, indicatorHeight);
      }
    }
  });

  onUnmounted(() => {
    const element = getSelectableElement();
    if (element) {
      trackHandle?.unregister(element);
    }
  });

  watch(
    () => selected,
    (isSelected) => {
      const element = getSelectableElement();
      if (!element) {
        return;
      }

      if (isSelected) {
        trackHandle?.select(element, indicatorHeight);
      }
    }
  );

  // tell descendants (e.g. RailButton) that a track is managing the visual indicator
  provide(IN_SELECTION_TRACK_KEY, true);
</script>

<template>
  <div ref="selectableElementWrapper" style="display: contents">
    <slot></slot>
  </div>
</template>
