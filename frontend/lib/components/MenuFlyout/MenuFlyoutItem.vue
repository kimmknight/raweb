<script setup lang="ts">
  import { computed, onMounted, ref } from 'vue';
  import TextBlock from '../TextBlock/TextBlock.vue';

  const {
    variant = 'standard',
    hint,
    selected = false,
    indented = false,
    disabled = false,
    keepOpen = false,
    href,
    class: className = '',
  } = defineProps<{
    variant?: 'standard';
    hint?: string;
    selected?: boolean;
    indented?: boolean;
    disabled?: boolean;
    /**
     * When true, the menu flyout (popover) will remain open after the item is clicked.
     */
    keepOpen?: boolean;
    class?: string;
    href?: string;
  }>();

  const parentFlyout = ref<HTMLElement | null>(null);
  const element = ref<HTMLElement | null>(null);
  onMounted(() => {
    if (element.value) {
      parentFlyout.value = element.value.closest('.menu-flyout');
    }
  });

  const emit = defineEmits<{ (evt: 'click', event: Event): void }>();
  function close(event: Event) {
    if (disabled) {
      event.preventDefault();
      return;
    }

    emit('click', event);
    if (parentFlyout.value && !keepOpen) {
      // close the outermost flyout, which also closes any submenus between it and this item
      let outermost: HTMLElement = parentFlyout.value;
      let ancestor = outermost.parentElement?.closest<HTMLElement>('.menu-flyout');
      while (ancestor) {
        outermost = ancestor;
        ancestor = outermost.parentElement?.closest<HTMLElement>('.menu-flyout');
      }
      outermost.hidePopover();
    }
  }

  function handleKeyDown(event: KeyboardEvent) {
    const { key } = event;
    if (key === 'Enter' || key === ' ') {
      event.preventDefault();
      (event.target as HTMLElement).click();
    }
  }

  const tagName = computed(() => (href ? 'a' : 'li'));
</script>

<template>
  <component
    v-if="variant === 'standard'"
    :is="tagName"
    :tabindex="-1"
    role="menuitem"
    :aria-selected="selected"
    :class="['menu-flyout-item', `type-${variant}`, className, { selected, disabled, indented }]"
    ref="element"
    :disabled="tagName === 'li' ? undefined : disabled"
    :aria-disabled="disabled || undefined"
    :href="disabled ? 'javascript:void(0)' : href"
    @click="close"
    @keydown="handleKeyDown"
  >
    <slot name="icon"></slot>
    <span class="menu-flyout-item-content">
      <slot></slot>
    </span>
    <TextBlock v-if="hint" class="menu-flyout-item-hint" variant="caption">
      {{ hint }}
    </TextBlock>
    <span v-if="$slots['icon-end']" class="menu-flyout-item-icon-end">
      <slot name="icon-end"></slot>
    </span>
  </component>
</template>

<style scoped>
  .menu-flyout-item {
    display: flex;
    /* align-items: center; */
    inline-size: calc(100% - 8px);
    position: relative;
    box-sizing: border-box;
    flex: 0 0 auto;
    margin: 2px 4px;
    padding-inline: 12px;
    /* reserves room for the end icon that other items in the menu have */
    padding-inline-end: calc(12px + var(--menu-flyout-item-end-gap, 0px));
    border-radius: var(--wui-control-corner-radius);
    outline: none;
    background-color: var(--wui-subtle-transparent);
    color: var(--wui-text-primary);
    cursor: default;
    user-select: none;
    min-block-size: 28px;
    white-space: nowrap;
    text-overflow: ellipsis;
    text-decoration: none;
    transition: background var(--wui-control-faster-duration) ease;
  }

  .menu-flyout-item-content::before {
    content: '';
    position: absolute;
    border-radius: 3px;
    background-color: var(--wui-accent-default);
    transition: transform var(--wui-control-fast-duration) var(--wui-control-fast-out-slow-in-easing);
    opacity: 0;
    inset-inline-start: 0;
    inline-size: 3px;
    block-size: 0;
    margin-top: 2px;
  }

  .menu-flyout-item:focus-visible {
    outline: var(--focus-outline);
    outline-offset: var(--focus-outline-offset);
  }

  .menu-flyout-item:hover,
  .menu-flyout-item.selected {
    background-color: var(--wui-subtle-secondary);
  }

  .menu-flyout-item:active {
    background-color: var(--wui-subtle-tertiary);
  }

  .menu-flyout-item:active .menu-flyout-item-content::before {
    transform: scaleY(0.625);
  }

  .menu-flyout-item.selected .menu-flyout-item-content::before {
    opacity: 1;
    block-size: 16px;
  }

  .menu-flyout-item.disabled {
    background-color: var(--wui-subtle-transparent);
    color: var(--wui-text-disabled);
    pointer-events: none;
  }

  .menu-flyout-item.disabled.selected {
    background-color: var(--wui-subtle-secondary);
  }

  .menu-flyout-item.disabled.selected .menu-flyout-item-content::before {
    background-color: var(--wui-accent-disabled);
  }

  .menu-flyout-item.disabled .menu-flyout-item-hint {
    color: var(--wui-text-disabled);
  }

  .menu-flyout-item.indented {
    padding-inline-start: 40px;
  }

  li .menu-flyout-item-hint {
    flex: 1 1 auto;
    text-align: end;
    align-self: center;
    padding-left: 24px;
    overflow: hidden;
    text-overflow: ellipsis;
    color: var(--wui-text-secondary);
  }

  .menu-flyout-item-content {
    padding: 4px 0;
  }

  /* icon at end */
  .menu-flyout-item:has(> .menu-flyout-item-icon-end) {
    padding-inline-end: 12px;
  }

  .menu-flyout-item-icon-end {
    display: inline-flex;
    align-self: center;
    margin-inline-start: auto;
    padding-inline-start: 24px;
  }

  .menu-flyout-item-icon-end :deep(svg) {
    inline-size: 16px;
    block-size: 16px;
    fill: currentColor;
  }
</style>

<style>
  /*
   * If any item of a menu has an end icon, every item reserves the same space for it.
   * The gap is the end icon width (16px) plus its start padding (24px).
   * Each menu flyout surface sets the gap itself so that a submenu does not inherit it from its parent menu.
   */
  .menu-flyout-surface {
    --menu-flyout-item-end-gap: 0px;
  }
  .menu-flyout-surface:has(
    > .menu-flyout-item > .menu-flyout-item-icon-end,
    > .menu-flyout-sub-items > .menu-flyout-item > .menu-flyout-item-icon-end
  ) {
    --menu-flyout-item-end-gap: 40px;
  }

  /* icon in front */
  .menu-flyout-item > svg {
    inline-size: 16px;
    padding: 2px 0;
    fill: currentColor;
    margin-inline-end: 12px;
  }
</style>
