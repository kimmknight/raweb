<script setup lang="ts">
  const { href, disabled, ...restProps } = defineProps<{
    href?: string;
    disabled?: boolean;
  }>();

  const tagName = href ? 'a' : 'button';
</script>

<template>
  <component :is="tagName" :href class="icon-button" :disabled tabindex="0" :="restProps">
    <slot></slot>
  </component>
</template>

<style scoped>
  .icon-button {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    user-select: none;
    position: relative;
    border: none;
    box-sizing: border-box;
    padding-block: 4px 6px;
    min-inline-size: 30px;
    min-block-size: 30px;
    padding: 8px;
    color: var(--wui-text-primary);
    border-radius: var(--wui-control-corner-radius);
    background-color: var(--wui-subtle-transparent);
    transition: var(--wui-control-faster-duration) ease background;
    flex-grow: 0;
    flex-shrink: 0;
  }
  .icon-button:hover {
    background-color: var(--wui-subtle-secondary);
  }
  .icon-button:active {
    background-color: var(--wui-subtle-tertiary);
    color: var(--wui-text-secondary);
  }
  .icon-button:disabled {
    background-color: var(--wui-subtle-disabled);
    color: var(--wui-text-disabled);
  }
</style>

<style>
  .icon-button > svg {
    inline-size: 16px;
    block-size: auto;
    fill: currentColor;
  }
</style>
