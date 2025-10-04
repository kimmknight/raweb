<script setup lang="ts">
  import TextBlock from '$components/TextBlock/TextBlock.vue';
  import { useAttrs } from 'vue';

  const {
    selected = false,
    disabled = false,
    compact = false,
    href,
    role = 'listitem',
    class: className = '',
  } = defineProps<{
    selected?: boolean;
    disabled?: boolean;
    href?: string;
    role?: string;
    class?: string;
    compact?: boolean;
  }>();
  const restProps = useAttrs();

  const tagName = href ? 'a' : 'li';

  function handleKeyDown(event: KeyboardEvent) {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      (event.target as HTMLElement).click();
    }
  }
</script>

<template>
  <component
    :is="tagName"
    onkeydown="handleKeyDown"
    :tabindex="disabled ? -1 : 0"
    :area-selected="selected"
    :class="[
      'list-item',
      className,
      selected ? 'selected' : '',
      disabled ? 'disabled' : '',
      compact ? 'compact' : '',
    ]"
    :href
    :role
    :="restProps"
  >
    <slot name="icon"></slot>
    <TextBlock>
      <slot />
    </TextBlock>
    <slot name="icon-end"></slot>
  </component>
</template>

<style scoped>
  .list-item {
    display: flex;
    align-items: center;
    inline-size: calc(100% - 10px);
    position: relative;
    box-sizing: border-box;
    flex: 0 0 auto;
    margin: 3px 5px;
    padding-inline: 12px;
    border-radius: var(--wui-control-corner-radius);
    background-color: transparent;
    color: var(--wui-text-primary);
    text-decoration: none;
    cursor: default;
    user-select: none;
    block-size: 34px;
    text-decoration: none;
    transition: var(--wui-control-faster-duration) ease;
  }
  .list-item .text-block {
    flex-grow: 1;
    white-space: nowrap;
    display: block;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  .list-item.compact {
    margin-top: 1px;
    margin-bottom: 1px;
    block-size: 30px;
  }

  /* vertical selection line */
  .list-item::before {
    content: '';
    position: absolute;
    background-color: var(--wui-accent-default);
    border-radius: var(--wui-control-corner-radius);
    transition: transform var(--wui-control-fast-duration) var(--wui-control-fast-out-slow-in-easing);
    inset-inline-start: 0;
    inline-size: 3px;
    block-size: 16px;
    transform: scaleY(0);
    opacity: 0;
  }
  .list-item.selected::before {
    transform: scaleY(1);
    opacity: 1;
  }

  .list-item:hover,
  .list-item.selected {
    background-color: var(--wui-subtle-secondary);
  }

  .list-item:active {
    background-color: var(--wui-subtle-tertiary);
    color: var(--wui-text-tertiary);
  }
  .list-item:active::before {
    transform: scaleY(0.625);
  }

  .list-item.disabled {
    background-color: transparent;
    color: var(--wui-text-disabled);
    pointer-events: none;
  }
  .list-item.disabled.selected {
    background-color: var(--wui-subtle-secondary);
  }
  .list-item.disabled.selected::before {
    background-color: var(--wui-accent-disabled);
  }

  .list-item :deep(svg) {
    flex-grow: 0;
    flex-shrink: 0;
    inline-size: 16px;
    block-size: auto;
    fill: currentColor;
    margin-inline-end: 16px;
  }
  .list-item :deep(.text-block + * > svg) {
    inline-size: 16px;
    block-size: auto;
    fill: currentColor;
    margin-inline-start: 16px;
    margin-inline-end: 0;
  }
</style>
