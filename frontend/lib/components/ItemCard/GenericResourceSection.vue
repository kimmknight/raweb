<script setup lang="ts">
  import { TextBlock } from '$components';
  import ContextMenu from '$components/ContextMenu/ContextMenu.vue';
  import { MenuFlyout, MenuFlyoutDivider, MenuFlyoutItem } from '$components/MenuFlyout';
  import { chevronDown } from '$icons';
  import { collapseUp, expandDown } from '$utils/transitions';
  import { useTranslation } from 'i18next-vue';
  import { computed, inject, nextTick, onBeforeUnmount, ref, useTemplateRef, watch } from 'vue';
  import ResourceGrid from '../Layout/ResourceGrid.vue';
  import { resourceGridContextKey } from '../Layout/resourceGridContext';

  const { title, count } = defineProps<{
    title?: string;
    /** The number to show in parenthesis after the title. It should represent the number of items in the section. */
    count?: number;
  }>();

  const { t } = useTranslation();
  const grid = inject(resourceGridContextKey);

  const isDetails = computed(() => grid?.mode.value === 'details');

  // groups are told apart by their titles
  const collapsed = computed({
    get: () => !!title && !!grid?.sections.isCollapsed(title),
    set: (value: boolean) => title && grid?.sections.setCollapsed(title, value),
  });

  // keep the record of sections in sync with the section title value
  let unregister: (() => void) | undefined;
  watch(
    () => title,
    (newTitle) => {
      unregister?.();
      unregister = newTitle ? grid?.sections.register(newTitle) : undefined;
    },
    { immediate: true }
  );
  onBeforeUnmount(() => unregister?.());

  /** The element that contains the rows. */
  const content = useTemplateRef<HTMLElement>('content');

  /** Whether the rows are animating. The template keeps the rows displayed while this is true. */
  const animating = ref(false);

  /**
   * Whether the chevron shows the expanded direction.
   * This follows `collapsed`, but only once the animation has finished (like the chevron in the tree view).
   */
  const chevronExpanded = ref(!collapsed.value);

  watch(collapsed, async (isCollapsed) => {
    const element = content.value;
    if (!element) {
      chevronExpanded.value = !isCollapsed;
      return;
    }

    // stop any animation that is still running from the last change
    element.getAnimations().forEach((animation) => animation.cancel());

    // set animating to true so that the rows are displayed so that we can measure them
    animating.value = true;
    await nextTick();
    const height = element.scrollHeight;

    if (isCollapsed) {
      await collapseUp(element, { startHeight: height });
    } else {
      await expandDown(element, { endHeight: height, fill: 'backwards' });
    }
    animating.value = false;

    // if the section was toggled again during the animation,
    // let the newer change handle the rest of the animation
    if (collapsed.value !== isCollapsed) {
      return;
    }

    // only update the chevron once the animation is finished, just like in tree views
    chevronExpanded.value = !isCollapsed;

    // cancel the animation to clear any final styles applied by the animation
    await nextTick();
    element.getAnimations().forEach((animation) => animation.cancel());
  });

  // TODO [Anchors]: Remove this when all major browsers support CSS Anchor Positioning
  const supportsAnchorPositions = CSS.supports('position-area', 'center center');
</script>

<template>
  <div v-if="isDetails" role="rowgroup" class="details-group">
    <ContextMenu v-if="title" placement="bottom" anchor="start">
      <template #default>
        <div
          class="details-heading"
          role="row"
          tabindex="0"
          :aria-expanded="!collapsed"
          @click.stop="collapsed = !collapsed"
          @keydown.stop.enter.space.prevent="collapsed = !collapsed"
          @keydown.stop.left.prevent="collapsed = true"
          @keydown.stop.right.prevent="collapsed = false"
        >
          <span class="chevron" :class="{ expanded: chevronExpanded }"><span v-swap="chevronDown"></span></span>
          <TextBlock variant="bodyStrong" tag="h2">
            {{ count === undefined ? title : `${title} (${count})` }}
          </TextBlock>
        </div>
      </template>
      <template #menu>
        <MenuFlyout placement="bottom" v-if="supportsAnchorPositions">
          <template #menu>
            <MenuFlyoutItem
              v-if="!collapsed"
              indented
              :hint="t('resourceTable.keys.leftArrow')"
              @click="collapsed = true"
            >
              {{ t('resourceTable.collapseGroup') }}
            </MenuFlyoutItem>
            <MenuFlyoutItem
              v-else
              indented
              :hint="t('resourceTable.keys.rightArrow')"
              @click="collapsed = false"
            >
              {{ t('resourceTable.expandGroup') }}
            </MenuFlyoutItem>
            <MenuFlyoutDivider />
            <MenuFlyoutItem indented @click="grid?.sections.expandAll()">
              {{ t('resourceTable.expandAllGroups') }}
            </MenuFlyoutItem>
            <MenuFlyoutItem
              indented
              @click="grid?.sections.collapseAll()"
              hint="&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"
            >
              {{ t('resourceTable.collapseAllGroups') }}
            </MenuFlyoutItem>
          </template>
        </MenuFlyout>
      </template>
    </ContextMenu>
    <div class="content" :class="{ hidden: collapsed && !animating }" ref="content" :inert="collapsed">
      <slot></slot>
    </div>
  </div>

  <section v-else class="plain-section">
    <TextBlock v-if="title" variant="bodyStrong" tag="h2" class="plain-heading">{{ title }}</TextBlock>
    <ResourceGrid :mode="grid?.mode.value" class="plain-grid">
      <slot></slot>
    </ResourceGrid>
  </section>
</template>

<style scoped>
  .plain-section {
    margin: 24px 0 8px 0;
  }
  .plain-heading {
    margin: 0 0 8px 0;
    user-select: none;
  }
  .plain-grid {
    gap: 16px;
  }

  .details-heading {
    display: flex;
    flex-direction: row;
    align-items: center;
    gap: 8px;
    block-size: 32px;
    margin-block-start: 4px;
    padding: 0 10px;
    box-sizing: border-box;
    cursor: default;
    user-select: none;
    transition: var(--wui-control-faster-duration) ease background;
  }
  .details-heading:hover {
    background-color: var(--wui-control-fill-secondary);
  }
  .details-heading:focus-visible {
    border-radius: var(--wui-control-corner-radius);
    outline-offset: -2px;
  }

  /* the same chevron as the tree view */
  .chevron {
    display: flex;
    flex-shrink: 0;
    align-items: center;
    justify-content: center;
    inline-size: 16px;
    block-size: 16px;
  }
  .chevron :deep(svg) {
    inline-size: 12px;
    block-size: 12px;
    transform: rotate(-90deg);
    transition: transform 200ms cubic-bezier(0.16, 1, 0.3, 1);
  }
  .chevron.expanded :deep(svg) {
    transform: none;
  }

  .content.hidden {
    display: none;
  }
</style>
