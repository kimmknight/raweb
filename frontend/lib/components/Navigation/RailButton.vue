<script setup lang="ts">
  import TextBlock from '$components/TextBlock/TextBlock.vue';
  import { useTemplateRef } from 'vue';

  const {
    active = false,
    href = '',
    target = '',
    'on:click': onClick,
    disabled = false,
    ...restProps
  } = defineProps<{
    /** Whether the button should be styled as if it is active. */
    active?: boolean;
    href?: string;
    target?: string;
    'on:click'?: (event: MouseEvent) => void;
    disabled?: boolean;
  }>();

  const tag = href ? 'a' : 'button'; // use <a> if href is provided, otherwise use <button>

  const componentRef = useTemplateRef('componentRef');
  function handleKeydown(evt: KeyboardEvent) {
    if (evt.target !== componentRef.value) {
      return;
    }

    if (evt.key === 'Enter' || evt.key === ' ') {
      evt.preventDefault();
      if (evt.currentTarget instanceof HTMLElement) {
        evt.currentTarget.click();
      }
    }
  }
</script>

<template>
  <component
    :is="tag"
    :="restProps"
    class="button"
    :class="{ active, disabled }"
    :href="active || disabled ? null : href"
    :target="active ? null : target"
    :disabled="active || disabled"
    ref="componentRef"
    @click="active ? null : onClick"
    @keydown.stop="handleKeydown"
  >
    <span class="icon">
      <slot name="icon" v-if="!active">
        <svg width="24" height="24" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
          <path
            d="M12 3.5a8.5 8.5 0 1 0 0 17 8.5 8.5 0 0 0 0-17ZM2 12C2 6.477 6.477 2 12 2s10 4.477 10 10-4.477 10-10 10S2 17.523 2 12Z"
          />
        </svg>
      </slot>
      <slot name="icon-active" v-if="active">
        <svg width="24" height="24" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
          <path d="M2 12C2 6.477 6.477 2 12 2s10 4.477 10 10-4.477 10-10 10S2 17.523 2 12Z" />
        </svg>
      </slot>
    </span>
    <TextBlock variant="caption" style="font-size: inherit; line-height: inherit"><slot></slot></TextBlock>
  </component>
</template>

<style scoped>
  .button {
    color: currentColor;
    inline-size: var(--button-size, 64px);
    block-size: calc(var(--button-size, 64px) / 1.1);
    position: relative;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 4px;
    color: var(--wui-text-tertiary);
    user-select: none;
    -webkit-user-drag: none;
    --font-size: 10px;
    font-size: var(--font-size);
    line-height: var(--font-size);
    transition: line-height var(--wui-control-normal-duration), color var(--wui-control-normal-duration),
      background-color var(--wui-control-fast-duration);
    cursor: default;
    text-decoration: none;
    border-radius: var(--wui-control-corner-radius);
  }
  .button:not(.disabled):hover {
    background-color: var(--wui-subtle-secondary);
    color: var(--wui-text-secondary);
  }
  .button:not(.disabled):active {
    background-color: var(--wui-subtle-tertiary);
    color: var(--wui-text-tertiary);
    will-change: line-height, background-color, color;
  }
  .button.disabled {
    color: var(--wui-text-disabled);
  }

  .button.active {
    background-color: var(--wui-control-solid-default);
    gap: 0;
    --font-size: 0px;
  }
  .button.active .icon {
    color: var(--wui-accent-default);
  }
  .button.active::after {
    content: '';
    position: absolute;
    width: 3px;
    height: 38%;
    top: 31%;
    left: 0;
    background-color: var(--wui-accent-default);
    border-radius: var(--wui-control-corner-radius);
  }

  .icon {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 24px;
    height: 24px;
  }
</style>
