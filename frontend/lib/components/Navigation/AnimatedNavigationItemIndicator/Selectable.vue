<script setup lang="ts">
  import { inject, onMounted, onUnmounted, provide, useTemplateRef, watch } from 'vue';
  import { IN_SELECTION_TRACK_KEY, SELECTION_TRACK_KEY } from './keys';

  const { selected = false, indicatorSize } = defineProps<{
    /** Whether this item is currently the active selection. */
    selected?: boolean;
    /**
     * Length of the accent line indicator in pixels, measured along the track's
     * main axis.
     */
    indicatorSize?: number;
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
        trackHandle?.select(element, indicatorSize);
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
        trackHandle?.select(element, indicatorSize);
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
