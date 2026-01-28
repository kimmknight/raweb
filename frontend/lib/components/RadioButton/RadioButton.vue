<script setup lang="ts">
  const {
    name,
    value,
    disabled = false,
    ...restProps
  } = defineProps<{
    name?: string;
    value: string;
    disabled?: boolean;
  }>();

  const state = defineModel<string>('state', { required: true });

  const emit = defineEmits<{
    (e: 'update:state', value: string): void;
  }>();

  function handleChange(event: Event) {
    const target = event.target as HTMLInputElement;
    const delay = parseFloat(
      getComputedStyle(document.documentElement).getPropertyValue('--wui-control-normal-duration')
    );
    setTimeout(() => {
      emit('update:state', target.value);
    }, delay);
  }
</script>

<template>
  <label class="radio-button-container">
    <input
      type="radio"
      class="radio-button"
      :checked="state === value"
      :name
      :value
      :disabled
      @change="handleChange"
    />
    <span v-if="$slots.default">
      <slot></slot>
    </span>
  </label>
</template>

<style scoped>
  .radio-button-container {
    display: flex;
    align-items: center;

    font-family: var(--wui-font-family-body);
    font-size: var(--wui-font-size-body);
    font-weight: 400;

    color: var(--wui-text-primary);
    user-select: none;
    min-block-size: 32px;
  }

  .radio-button-container > span {
    padding-inline-start: 8px;
  }

  .radio-button {
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    flex-grow: 0;

    position: relative;
    margin: 0;
    --border-color: var(--wui-control-strong-stroke-default);
    box-shadow: inset 0 0 0 1px var(--border-color);
    border-radius: 20px;
    background-clip: padding-box;
    background-color: var(--wui-control-alt-secondary);
    appearance: none;
    inline-size: 20px;
    block-size: 20px;
  }
  .radio-button::before {
    content: '';
    inline-size: 4px;
    block-size: 4px;
    visibility: hidden;
    position: absolute;
    border-radius: 12px;
    background-color: var(--wui-text-on-accent-primary);
  }

  .radio-button:hover {
    background-color: var(--wui-control-alt-tertiary);
  }

  .radio-button:active {
    --border-color: var(--wui-control-strong-stroke-disabled);
    background-color: var(--wui-control-alt-quarternary);
  }
  .radio-button:active::before {
    transition: var(--wui-control-normal-duration) var(--wui-control-fast-out-slow-in-easing);
    visibility: visible;
    transform: scale(2.5);
  }

  .radio-button:disabled {
    --border-color: var(--wui-control-strong-stroke-disabled);
    background-color: var(--wui-control-alt-disabled);
  }
  .radio-button:disabled::before {
    visibility: hidden;
  }
  .radio-button:disabled + span {
    color: var(--wui-text-disabled);
  }

  .radio-button:checked {
    --border-color: transparent;
    background-color: var(--wui-accent-default);
    transition: var(--wui-control-normal-duration) var(--wui-control-fast-out-slow-in-easing);
  }
  .radio-button:checked::before {
    visibility: visible;
    transition: var(--wui-control-normal-duration) var(--wui-control-fast-out-slow-in-easing);
    box-shadow: 0 0 0 1px var(--wui-control-stroke-default);
    transform: scale(3);
  }

  .radio-button:checked:hover {
    background-color: var(--wui-accent-secondary);
  }
  .radio-button:checked:hover::before {
    transform: scale(3.5);
  }

  .radio-button:checked:active {
    background-color: var(--wui-accent-tertiary);
  }
  .radio-button:checked:active::before {
    transform: scale(2.5);
  }

  .radio-button:checked:disabled {
    background-color: var(--wui-accent-disabled);
  }
  .radio-button:checked:disabled::before {
    box-shadow: none;
    transform: scale(3);
  }
</style>
