<script setup lang="ts">
  import { onMounted, ref, useTemplateRef } from 'vue';
  import { getPositionArea } from '../MenuFlyout/getPositionArea';

  const {
    placement = 'bottom',
    anchor = 'start',
    ignore,
    suppressNativeMenu = false,
  } = defineProps<{
    /**
     * The preferred placement of the menu relative to the anchor element.
     * The menu will flip to the opposite side if it does not fit in the preferred placement.
     */
    placement?: 'top' | 'right' | 'bottom' | 'left';
    /**
     * The preferred alignment of the menu relative to the anchor element.
     * The menu will flip to the opposite alignment if it does not fit in the preferred alignment.
     */
    anchor?: 'start' | 'center' | 'end';
    /** CSS selector for elements that this menu should not handle */
    ignore?: string;
    /** Hides the browser context menu (except for ignored elements) when there is no menu to show. */
    suppressNativeMenu?: boolean;
  }>();

  /**
   * Checks whether the event target is inside an element that matches the `ignore` selector.
   */
  function isIgnored(evt: Event) {
    if (!ignore) {
      return false;
    }
    const match = (evt.target as Element).closest(ignore);
    return !!match && (evt.currentTarget as Node).contains(match);
  }

  const menuSlot = useTemplateRef<HTMLElement>('menuSlot');
  const anchorPoint = useTemplateRef<HTMLElement>('anchorPoint');

  /**
   * Gets the menu element directly from the menu slot
   *
   * We must ensure that we only look inside the menu slot
   * since the page might have multiple popovers.
   */
  function getMenu() {
    return menuSlot.value?.querySelector<HTMLElement>('.menu-flyout');
  }

  // we grab each menu's generated ID so that it can be shared to the slot content
  const menuId = ref<string>();
  onMounted(() => {
    menuId.value = getMenu()?.id;
  });

  /**
   * Gets the element that contains the content of the menu.
   */
  function getMenuSurface(menu: HTMLElement) {
    return menu.querySelector<HTMLElement>(':scope > .menu-flyout-surface');
  }

  /**
   * Removes the position that was set for opening the menu at a point after the menu closes,
   * so that a button that opens the same menu positions it next to the button.
   */
  function clearPointPosition(evt: Event) {
    const menu = evt.currentTarget as HTMLElement;
    if (menu.matches(':popover-open')) {
      return;
    }

    menu.style.removeProperty('position-area');
    menu.style.removeProperty('margin');
    getMenuSurface(menu)?.style.removeProperty('--menu-flyout-transition-offset');
    menu.removeEventListener('toggle', clearPointPosition);
  }

  /**
   * Opens the menu at the given viewport coordinates.
   *
   * If the menu is already open, it closes and opens again so that the open animation plays again.
   */
  function openAt(x: number, y: number) {
    const menu = getMenu();
    if (!menu || !anchorPoint.value) {
      return;
    }

    // the menu normally positions itself next to the button that opens it,
    // so we must replace that position until the menu closes
    menu.style.setProperty('position-area', getPositionArea(placement, anchor) ?? '');

    // keep the corner of the menu 1px away from the point
    menu.style.setProperty('margin', '1px');

    menu.addEventListener('toggle', clearPointPosition);

    // reposition the zero-size anchor, then use it as the anchor for the popover
    anchorPoint.value.style.left = `${x}px`;
    anchorPoint.value.style.top = `${y}px`;
    if (menu.matches(':popover-open')) {
      menu.hidePopover();
      // end the running animations so that the open animation plays again
      menu.getAnimations({ subtree: true }).forEach((animation) => animation.cancel());
    }
    menu.showPopover({ source: anchorPoint.value });

    // the open animation slides from the anchor, so it must follow a flip to the opposite vertical side
    const rect = menu.getBoundingClientRect();
    const opensAbove = rect.top + rect.height / 2 < y;
    getMenuSurface(menu)?.style.setProperty('--menu-flyout-transition-offset', opensAbove ? '50%' : '-50%');
  }

  /**
   * Opens the menu at the cursor on a right click, unless the click was ignored, already handled, or inside a menu.
   */
  function handleContextMenu(evt: MouseEvent) {
    // if a deeper context menu already handled this event,
    // we should not do anything with this menu
    if (evt.defaultPrevented) {
      return;
    }

    // a right click inside a dialog should not open this menu behind it
    if (isFromDialog(evt)) {
      return;
    }

    // a right click inside any menu (this one or another on the page) should not open or move this menu
    if ((evt.target as Element).closest('.menu-flyout')) {
      evt.preventDefault();
      return;
    }

    if (isIgnored(evt)) {
      return;
    }

    // without a menu, the browser menu appears unless it should be suppressed
    if (!getMenu()) {
      if (suppressNativeMenu) {
        evt.preventDefault();
      }
      return;
    }
    evt.preventDefault();
    openAt(evt.clientX, evt.clientY);
  }

  /**
   * Checks whether the event came from inside a dialog that opened from this content (such as a content dialog).
   *
   * Dialogs are in the DOM of the content they were opened from, so their events reach this menu.
   * A dialog that contains this context menu does not count.
   */
  function isFromDialog(evt: Event) {
    const dialog = (evt.target as Element).closest('dialog');
    return !!dialog && !dialog.contains(evt.currentTarget as Node);
  }

  /**
   * Opens the menu beside the focused element when the menu key or Shift+F10 is pressed.
   */
  function handleKeydown(evt: KeyboardEvent) {
    if (
      !getMenu() ||
      evt.defaultPrevented ||
      isFromDialog(evt) ||
      (evt.target as Element).closest('.menu-flyout') ||
      isIgnored(evt) ||
      (evt.key !== 'ContextMenu' && !(evt.key === 'F10' && evt.shiftKey))
    ) {
      return;
    }

    evt.preventDefault();
    const rect = (evt.target as HTMLElement).getBoundingClientRect();
    const remInPixels = parseFloat(getComputedStyle(document.documentElement).fontSize);
    openAt(rect.left + Math.min(3 * remInPixels, rect.width / 2), rect.top + rect.height / 2);
  }
</script>

<template>
  <div class="context-menu" @contextmenu="handleContextMenu" @keydown="handleKeydown">
    <slot :menuId="menuId"></slot>
    <span ref="anchorPoint" class="context-menu-anchor"></span>
    <div ref="menuSlot" class="context-menu">
      <slot name="menu"></slot>
    </div>
  </div>
</template>

<style scoped>
  .context-menu {
    display: contents;
  }

  .context-menu-anchor {
    position: fixed;
    inline-size: 0;
    block-size: 0;
    pointer-events: none;
  }
</style>
