<script setup lang="ts">
  import { TextBlock } from '$components';

  const { ondblclick, name, value } = defineProps<{
    ondblclick?: () => void;
    name?: string;
    value: string;
  }>();

  const selectedTerminalServer = defineModel<string>();
</script>

<template>
  <div class="picker-item" @dblclick="ondblclick" :class="{ hasIcon: !!$slots.icon }">
    <label>
      <slot name="icon"></slot>
      <TextBlock variant="body"><slot></slot></TextBlock>
      <input type="radio" :name :value v-model="selectedTerminalServer" />
    </label>
  </div>
</template>

<style scoped>
  .picker-item {
    color: currentColor;
    inline-size: 100%;
    min-block-size: 40px;
    position: relative;
    user-select: none;
    -webkit-user-drag: none;
    position: relative;
    border-radius: var(--wui-control-corner-radius);
    transition: background var(--wui-control-faster-duration) ease;
  }
  .picker-item.hasIcon {
    block-size: 52px;
  }
  .picker-item:hover {
    background-color: var(--wui-subtle-secondary);
    color: var(--wui-text-secondary);
  }
  .picker-item:active {
    background-color: var(--wui-subtle-tertiary);
    color: var(--wui-text-tertiary);
    will-change: line-height, background-color, color;
  }
  .picker-item:has(input:checked) {
    background-color: var(--wui-subtle-secondary);
  }
  .picker-item:has(input:checked):hover {
    background-color: var(--wui-subtle-tertiary);
  }

  .picker-item input {
    appearance: none;
    position: absolute;
    height: 100%;
    width: 100%;
    margin: 0;
    left: 0;
    display: flex;
    align-items: center;
    border-radius: var(--wui-control-corner-radius);
  }
  .picker-item input::after {
    content: '';
    position: absolute;
    width: 3px;
    block-size: 0;
    left: 0;
    background-color: var(--wui-accent-default);
    transition: transform var(--wui-control-fast-duration) var(--wui-control-fast-out-slow-in-easing);
    border-radius: var(--wui-control-corner-radius);
  }
  .picker-item input:checked::after {
    block-size: 16px;
    transition:
      transform var(--wui-control-fast-duration) var(--wui-control-fast-out-slow-in-easing),
      block-size var(--wui-control-fast-duration) var(--wui-control-fast-out-slow-in-easing);
  }
  .picker-item input:checked:active::after {
    transform: scaleY(0.625);
  }

  .picker-item label {
    position: absolute;
    inset: 0;
    display: flex;
    align-items: center;
    padding: 16px;
  }

  .picker-item :deep(img) {
    block-size: 2rem;
    inline-size: 2rem;
    margin-inline-end: 0.75rem;
    margin-inline-start: -0.25rem;
  }
</style>
