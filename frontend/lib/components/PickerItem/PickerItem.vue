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
  <div class="picker-item" @dblclick="ondblclick">
    <label>
      <TextBlock variant="body"><slot></slot></TextBlock>
      <input type="radio" :name :value v-model="selectedTerminalServer" />
    </label>
  </div>
</template>

<style scoped>
  .picker-item {
    color: currentColor;
    inline-size: 100%;
    block-size: 40px;
    position: relative;
    user-select: none;
    -webkit-user-drag: none;
    position: relative;
    border-radius: var(--wui-control-corner-radius);
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
  }
  .picker-item input:checked::after {
    content: '';
    position: absolute;
    width: 3px;
    height: 38%;
    top: 31%;
    left: 0;
    background-color: var(--wui-accent-default);
    border-radius: var(--wui-control-corner-radius);
  }

  .picker-item label {
    position: absolute;
    inset: 0;
    display: flex;
    align-items: center;
    padding: 16px;
  }
</style>
