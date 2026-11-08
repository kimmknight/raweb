<script setup lang="ts">
  import { computed, onBeforeUnmount, onMounted, useTemplateRef } from 'vue';
  import { getPositionArea } from './getPositionArea';

  const {
    popover = 'auto', // default popover trigger type
    placement = 'bottom',
    anchor = 'end',
    ...restProps
  } = defineProps<{
    popover?: 'auto' | 'hint' | 'manual'; // popover trigger type
    placement?: 'top' | 'right' | 'bottom' | 'left';
    anchor?: 'start' | 'center' | 'end';
  }>();

  const positionArea = computed(() => getPositionArea(placement, anchor));

  const dialog = useTemplateRef<HTMLDivElement>('menu');

  function open(options?: ShowPopoverOptions) {
    if (dialog.value) {
      if (!dialog.value.matches(':popover-open')) {
        console.log('Opening menu flyout', dialog.value);
        dialog.value.showPopover(options);
      }
    }
  }

  function close() {
    if (dialog.value) {
      if (dialog.value.matches(':popover-open')) {
        dialog.value.hidePopover();
      }
    }
  }

  function toggle(options?: TogglePopoverOptions) {
    if (dialog.value) {
      dialog.value.togglePopover(options);
    }
  }

  const id = Math.floor(Math.random() * 1000000).toString(16); // generate a random ID for the popover
  const popoverId = `popover-${id}`; // unique ID for the popover

  defineExpose({ open, close, toggle, popoverId });

  function closeOnMouseDownOutside(evt: MouseEvent) {
    const menu = dialog.value;
    const target = evt.target as Element;
    if (popover !== 'auto' || !menu?.matches(':popover-open') || menu.contains(target)) {
      return;
    }

    // the invoker toggles the menu on click, so closing here would make it reopen
    if (target.closest(`[popovertarget="${popoverId}"]`)) {
      return;
    }

    // a right click anywhere in the chain of menus (e.g. in the parent of a submenu) should not close the menus
    let outermost: Element = menu;
    let ancestor = outermost.parentElement?.closest('.menu-flyout');
    while (ancestor) {
      outermost = ancestor;
      ancestor = outermost.parentElement?.closest('.menu-flyout');
    }
    if (evt.button === 2 && outermost.contains(target)) {
      return;
    }

    menu.hidePopover();
  }

  onMounted(() => document.addEventListener('mousedown', closeOnMouseDownOutside, true));
  onBeforeUnmount(() => document.removeEventListener('mousedown', closeOnMouseDownOutside, true));

  /**
   * Gets the menu items of the specified menu flyout. This excludes items from any nested submenus.
   * @param menu The HTML element representing the menu flyout.
   * @returns An array of HTMLLIElement representing the menu items of the specified menu flyout.
   */
  function getMenuItemElements(menu: HTMLElement) {
    return Array.from(menu.querySelectorAll<HTMLLIElement>('li:not([disabled="true"])')).filter(
      (item) => item.closest('.menu-flyout') === menu
    );
  }

  // use arrow keys to navigate the menu items
  function handleKeydown(evt: KeyboardEvent) {
    const menu = dialog.value;
    if (!menu) return;

    const items = getMenuItemElements(menu);
    const currentIndex = Array.from(items).findIndex((item) => item === document.activeElement);

    if (evt.key === 'ArrowDown') {
      evt.preventDefault();
      if (currentIndex < items.length - 1) {
        items[currentIndex + 1].focus();
      } else {
        items[0].focus();
      }
    } else if (evt.key === 'ArrowUp') {
      evt.preventDefault();
      if (currentIndex > 0) {
        items[currentIndex - 1].focus();
      } else {
        items[items.length - 1].focus();
      }
    } else if (evt.key === 'Tab') {
      close();
    }
  }

  // focus the first menu item when the menu is opened
  function focusMenuOnOpen() {
    const menu = dialog.value;
    if (!menu) {
      return;
    }

    // require the menu to be open to focus the first item
    if (!menu.matches(':popover-open')) {
      return;
    }

    const firstItem = getMenuItemElements(menu)[0];
    if (firstItem) {
      setTimeout(() => {
        firstItem.focus();
      }, 0);
    }
  }
</script>

<template>
  <slot :popoverId :open :close :toggle></slot>
  <div
    ref="menu"
    :popover
    :id="popoverId"
    :style="{
      '--positionArea': positionArea,
      '--menu-flyout-transition-offset': placement === 'top' ? '50%' : '-50%',
    }"
    :="restProps"
    class="menu-flyout"
    @click.stop
    @keydown.stop="handleKeydown"
    @toggle="focusMenuOnOpen"
  >
    <div class="menu-flyout-surface">
      <slot name="menu" :popoverId :open :close :toggle></slot>
    </div>
  </div>
</template>

<style scoped>
  .menu-flyout {
    position-area: var(--positionArea);
    /* flip to the other side or direction when the preferred position overflows the viewport */
    position-try-fallbacks:
      flip-block,
      flip-inline,
      flip-block flip-inline;
    margin: 0;
    padding: 0;
    border: 0;
    background: none;
  }

  .menu-flyout:popover-open {
    animation: menu-shadow var(--wui-control-fast-duration) var(--wui-control-fast-out-slow-in-easing)
      var(--wui-control-normal-duration) forwards;
    overflow: hidden;
  }

  .menu-flyout-surface {
    user-select: none;
    font-family: var(--wui-font-family-text);
    font-size: var(--wui-font-size-body);
    font-weight: normal;
    line-height: 20px;
    display: flex;
    flex-direction: column;
    animation: menu-open var(--wui-control-normal-duration) var(--wui-control-fast-out-slow-in-easing);
    min-inline-size: 120px;
    max-inline-size: 100%;
    max-block-size: 100vh;
    margin: 0;
    padding: 0;
    padding-block: 2px;
    box-sizing: border-box;
    color: var(--wui-text-primary);
    border-radius: var(--wui-overlay-corner-radius);
    border: 1px solid var(--wui-surface-stroke-flyout);
    background-color: var(--wui-solid-background-quarternary);
    background-clip: padding-box;
  }
</style>
