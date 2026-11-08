<script setup lang="ts">
  import { chevronDown } from '$icons';
  import { onBeforeUnmount, onMounted, useTemplateRef } from 'vue';
  import MenuFlyout from './MenuFlyout.vue';
  import MenuFlyoutItem from './MenuFlyoutItem.vue';

  const { indented = false, disabled = false } = defineProps<{
    indented?: boolean;
    disabled?: boolean;
  }>();

  const flyout = useTemplateRef<InstanceType<typeof MenuFlyout>>('flyout');
  const item = useTemplateRef<InstanceType<typeof MenuFlyoutItem>>('item');

  // how long the pointer must rest before the submenu opens or is replaced by another
  const hoverDelayMs = 400; // this is what Windows uses
  let hoverTimer: ReturnType<typeof setTimeout> | undefined;

  /**
   * Opens the submenu associated with this item.
   * When the submenu opens, it's component will claim keyboard focus.
   */
  function openSubmenu() {
    clearTimeout(hoverTimer);
    if (!disabled) {
      flyout.value?.open({ source: item.value?.$el });
    }
  }

  function cancelPendingOpen() {
    clearTimeout(hoverTimer);
  }

  function openSubmenuAfterDelay() {
    clearTimeout(hoverTimer);
    hoverTimer = setTimeout(openSubmenu, hoverDelayMs);
  }

  /**
   * Closes the open submenu after a delay when the pointer is over an element of the
   * parent menu other than this item or the submenu.
   *
   * If a pointer does not rest on the parent menu for the specified delay, the submenu will not close.
   */
  function handleParentMouseOver(evt: MouseEvent) {
    const submenu = flyout.value ? document.getElementById(flyout.value.popoverId) : null;
    if (!submenu?.matches(':popover-open')) {
      return;
    }

    clearTimeout(hoverTimer);
    const target = evt.target as Node;
    if (!item.value?.$el.contains(target) && !submenu.contains(target)) {
      hoverTimer = setTimeout(() => flyout.value?.close(), hoverDelayMs);
    }
  }

  let parentMenu: Element | null = null;
  onMounted(() => {
    parentMenu = item.value?.$el.closest('.menu-flyout') ?? null;
    parentMenu?.addEventListener('mouseover', handleParentMouseOver as EventListener);
  });
  onBeforeUnmount(() => {
    clearTimeout(hoverTimer);
    parentMenu?.removeEventListener('mouseover', handleParentMouseOver as EventListener);
  });

  /**
   * Closes the submenu and returns focus to the trigger item.
   */
  function closeSubmenu() {
    flyout.value?.close();
    (item.value?.$el as HTMLElement | undefined)?.focus();
  }
</script>

<template>
  <!-- trigger item that appears in the parent menu flyout -->
  <MenuFlyoutItem
    ref="item"
    keep-open
    :indented="indented"
    :disabled="disabled"
    @click="openSubmenu"
    @mouseenter="openSubmenuAfterDelay"
    @mouseleave="cancelPendingOpen"
    @keydown.right.prevent="openSubmenu"
  >
    <template #icon><slot name="icon"></slot></template>
    <slot></slot>
    <template #icon-end><span class="menu-flyout-sub-item-chevron" v-html="chevronDown"></span></template>
  </MenuFlyoutItem>

  <!-- child menu flyout -->
  <MenuFlyout ref="flyout" placement="right" anchor="start">
    <template #menu>
      <div class="menu-flyout-sub-items" @keydown.left.prevent="closeSubmenu">
        <slot name="menu"></slot>
      </div>
    </template>
  </MenuFlyout>
</template>

<style scoped>
  .menu-flyout-sub-item-chevron {
    display: inline-flex;
    transform: rotate(-90deg);
  }

  .menu-flyout-sub-items {
    display: contents;
  }
</style>
