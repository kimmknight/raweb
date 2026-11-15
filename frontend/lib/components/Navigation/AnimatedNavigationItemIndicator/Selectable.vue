<script setup lang="ts">
  import { inject, onMounted, onUnmounted, provide, useTemplateRef, watch } from 'vue';
  import { IN_SELECTION_TRACK_KEY, SELECTION_TRACK_KEY, type TrackHandle } from './keys';

  const {
    selected = false,
    indicatorSize,
    instantHandoff = false,
    trackHandle: trackHandleProp,
  } = defineProps<{
    /** Whether this item is currently the active selection. */
    selected?: boolean;
    /**
     * Length of the accent line indicator in pixels, measured along the track's
     * main axis.
     */
    indicatorSize?: number;
    /**
     * Hide the previous selection's indicator in another track immediately when this item
     * becomes selected, instead of animating it out. Use this when the previous item is
     * going away, such as the selected child of a collapsing subtree.
     */
    instantHandoff?: boolean;
    /**
     * Explicit track handle to use instead of the one obtained via `inject`.
     *
     * This is needed when this component is mounted in a separate Vue app instance
     * (e.g. `App_Data/inject/index.js`), since provide + inject only resolves
     * within a single app's component tree.
     */
    trackHandle?: TrackHandle;
  }>();

  const injectedTrackHandle = inject(SELECTION_TRACK_KEY, null);
  const trackHandle = trackHandleProp ?? injectedTrackHandle;
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
        trackHandle?.select(element, indicatorSize, instantHandoff);
      } else {
        // The track clears the selection after a short delay, so a sibling that becomes selected
        // in the same tick supersedes this deselect.
        trackHandle?.deselect(element);
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
