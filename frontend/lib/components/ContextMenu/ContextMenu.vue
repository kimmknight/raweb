<script setup lang="ts">
  import { useTemplateRef } from 'vue';
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

  /**
   * Opens the menu with its corner at the given viewport coordinates, replaying the open animation if it is already open.
   */
  function openAt(x: number, y: number) {
    const menu = getMenu();
    if (!menu || !anchorPoint.value) {
      return;
    }

    menu.style.setProperty('--positionArea', getPositionArea(placement, anchor) ?? '');

    // reposition the zero-size anchor, then use it as the anchor for the popover
    anchorPoint.value.style.left = `${x}px`;
    anchorPoint.value.style.top = `${y}px`;
    if (menu.matches(':popover-open')) {
      menu.hidePopover();
      // flush styles so the open animation restarts instead of continuing
      void menu.offsetHeight;
    }
    menu.showPopover({ source: anchorPoint.value });

    // the open animation slides from the anchor, so it must follow a flip to the opposite vertical side
    const rect = menu.getBoundingClientRect();
    const opensAbove = rect.top + rect.height / 2 < y;
    menu.style.setProperty('--menu-flyout-transition-offset', opensAbove ? '50%' : '-50%');
  }

  /**
   * Opens the menu at the cursor on a right click, unless the click was ignored, already handled, or on the menu itself.
   */
  function handleContextMenu(evt: MouseEvent) {
    // if a deeper context menu already handled this event,
    // we should not do anything with this menu
    if (evt.defaultPrevented || isIgnored(evt)) {
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

    // a right click on the open menu itself should not move it
    if (getMenu()?.contains(evt.target as Node)) {
      return;
    }

    openAt(evt.clientX, evt.clientY);
  }

  /**
   * Opens the menu beside the focused element when the menu key or Shift+F10 is pressed.
   */
  function handleKeydown(evt: KeyboardEvent) {
    if (
      !getMenu() ||
      evt.defaultPrevented ||
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
    <slot></slot>
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
