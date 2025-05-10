<script setup lang="ts">
  import { useTemplateRef } from 'vue';

  const { disabled, ...restProps } = defineProps<{
    disabled?: boolean;
  }>();

  const model = defineModel({ default: false });

  function update() {
    model.value = !model.value;
  }

  const inputRef = useTemplateRef('inputRef');
  function handleKeydown(evt: KeyboardEvent) {
    if (evt.target !== inputRef.value) {
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
  <label>
    <input
      type="checkbox"
      class="toggle-switch"
      :disabled="disabled"
      :checked="model"
      @click="update"
      @keydown.stop="handleKeydown"
      ref="inputRef"
      :="restProps"
    />
    <span v-if="$slots.default">
      <slot></slot>
    </span>
  </label>
</template>

<style scoped>
  .toggle-switch {
    display: inline-flex;
    align-items: center;
    user-select: none;
    font-family: var(--wui-font-family-text);
    font-size: var(--wui-font-size-body);
    font-weight: normal;
    line-height: 20px;

    position: relative;
    margin: 0;
    border: 1px solid var(--wui-control-strong-stroke-default);
    border-radius: 20px;
    background-color: var(--wui-control-alt-secondary);
    appearance: none;
    inline-size: 40px;
    block-size: 20px;
  }

  .toggle-switch::before {
    content: '';
    position: absolute;
    border-radius: 7px;
    background-color: var(--wui-text-secondary);
    transition: var(--wui-control-fast-duration) ease-in-out transform,
      var(--wui-control-fast-duration) var(--wui-control-fast-out-slow-in-easing) height,
      var(--wui-control-fast-duration) var(--wui-control-fast-out-slow-in-easing) width,
      var(--wui-control-fast-duration) var(--wui-control-fast-out-slow-in-easing) margin,
      var(--wui-control-faster-duration) linear background;
    inset-inline-start: 3px;
    inline-size: 12px;
    block-size: 12px;
  }

  .toggle-switch:hover {
    background-color: var(--wui-control-alt-tertiary);
  }
  .toggle-switch:hover::before {
    inline-size: 14px;
    block-size: 14px;
  }

  .toggle-switch:active {
    background-color: var(--wui-control-alt-quarternary);
  }
  .toggle-switch:active::before {
    inline-size: 17px;
    block-size: 14px;
  }

  .toggle-switch:disabled {
    border-color: var(--wui-control-strong-stroke-disabled);
    background-color: var(--wui-control-alt-disabled);
  }
  .toggle-switch:disabled::before {
    margin: 0 !important;
    background-color: var(--wui-text-disabled);
    box-shadow: none;
    inline-size: 12px;
    block-size: 12px;
  }
  .toggle-switch:disabled + span {
    color: var(--wui-text-disabled);
  }

  .toggle-switch:checked {
    border: none;
    background-color: var(--wui-accent-default);
  }
  .toggle-switch:checked::before {
    background-color: var(--wui-text-on-accent-primary);
    box-shadow: 0 0 0 1px solid var(--wui-control-stroke-default);
    transform: translateX(20px);
  }
  .toggle-switch:checked:hover {
    background-color: var(--wui-accent-secondary);
  }
  .toggle-switch:checked:hover::before {
    margin-inline-start: -1px;
  }
  .toggle-switch:checked:active {
    background-color: var(--wui-accent-tertiary);
  }
  .toggle-switch:checked:active::before {
    margin-inline-start: -4px;
  }
  .toggle-switch:checked:disabled {
    background-color: var(--wui-accent-disabled);
  }
  .toggle-switch:checked:disabled::before {
    box-shadow: none;
    background-color: var(--wui-text-on-accent-disabled);
  }

  label {
    display: inline-flex;
    align-items: center;
    user-select: none;
    font-family: var(--wui-font-family-text);
    font-size: var(--wui-font-size-body);
    font-weight: normal;
    line-height: 20px;
    color: var(--wui-text-primary);
    min-block-size: 32px;
  }

  label > span {
    padding-inline-start: 8px;
  }
</style>
