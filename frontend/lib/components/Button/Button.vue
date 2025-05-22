<script setup lang="ts">
  const {
    variant = 'standard',
    href,
    disabled,
    ...restProps
  } = defineProps<{
    variant?: 'standard' | 'accent' | 'hyperlink';
    href?: string;
    disabled?: boolean;
  }>();

  const tagName = href ? 'a' : 'button';
</script>

<template>
  <component
    :is="tagName"
    :href
    :class="['button', `style-${variant}`, disabled ? 'disabled' : '']"
    :disabled
    tabindex="0"
    :="restProps"
  >
    <slot name="icon"></slot>
    <span v-if="$slots.default"><slot></slot></span>
    <slot name="icon-end"></slot>
  </component>
</template>

<style scoped>
  .button {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    user-select: none;
    font-family: var(--wui-font-family-text);
    font-size: var(--wui-font-size-body);
    font-weight: normal;
    line-height: 20px;
    position: relative;
    box-sizing: border-box;
    padding-block: 4px 6px;
    padding-inline: 11px;
    text-decoration: none;
    border: none;
    cursor: default;
    border-radius: var(--wui-control-corner-radius);
    transition: var(--wui-control-faster-duration) ease background;
    min-height: 30px;
  }

  .button.style-standard {
    border: 0;
    box-shadow: inset 0 0 0 1px var(--wui-control-stroke-default),
      inset 0 -1px 0 0 var(--wui-control-stroke-secondary-overlay);
    background-color: var(--wui-control-fill-default);
    color: var(--text-primary);
    background-clip: padding-box;
  }
  .button.style-standard:hover {
    background-color: var(--wui-control-fill-secondary);
  }
  .button.style-standard:active {
    background-color: var(--wui-control-fill-tertiary);
    color: var(--wui-text-secondary);
  }
  .button.style-standard.disabled {
    background-color: var(--wui-control-fill-disabled);
    color: var(--wui-text-disabled);
  }

  .button.style-hyperlink {
    background-color: var(--wui-subtle-transparent);
    color: var(--wui-accent-text-primary);
    cursor: pointer;
  }
  .button.style-hyperlink:hover {
    background-color: var(--wui-subtle-secondary);
  }
  .button.style-hyperlink:active {
    background-color: var(--wui-subtle-tertiary);
    color: var(--wui-accent-text-tertiary);
  }

  .button.style-accent {
    background-color: var(--wui-accent-default);
    border: 1px solid var(--wui-control-stroke-on-accent-default);
    border-bottom-color: var(--wui-control-stroke-on-accent-secondary);
    color: var(--wui-text-on-accent-primary);
  }
  .button.style-accent:hover {
    background-color: var(--wui-accent-secondary);
  }
  .button.style-accent:active {
    background-color: var(--wui-accent-tertiary);
    border-color: transparent;
    color: var(--wui-text-on-accent-secondary);
  }
</style>

<style>
  .button > svg {
    fill: currentColor;
  }

  .button > svg:first-child {
    margin-inline-end: 8px;
    inline-size: 16px;
    block-size: 16px;
  }
  .button > svg:last-child {
    margin-inline-start: 8px;
    inline-size: 12px;
    block-size: 12px;
  }
  .button > svg:first-child + svg:last-child {
    margin-inline-start: 0;
  }
</style>
