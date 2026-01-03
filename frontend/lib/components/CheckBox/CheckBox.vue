<script setup lang="ts">
  const {
    name,
    disabled = false,
    indeterminate = false,
    size = 20,
    labelStyle = '',
  } = defineProps<{
    name?: string;
    indeterminate?: boolean;
    disabled?: boolean;
    size?: number;
    labelStyle?: string;
  }>();

  const checked = defineModel<boolean>('checked', { required: true });

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
  <label
    class="checkbox-container"
    :class="{ disabled, indeterminate }"
    :style="`--size: ${size}px; ${labelStyle}`"
  >
    <div class="checkbox-inner">
      <input
        type="checkbox"
        class="checkbox"
        :name
        :checked
        :indeterminate
        :value="checked"
        :disabled
        @change="handleChange"
      />
      <svg aria-hidden="true" class="checkbox-glyph" :viewBox="indeterminate ? '171 470 683 85' : '0 0 24 24'">
        <path
          v-if="indeterminate"
          class="path-indeterminate"
          d="M213.5,554.5C207.5,554.5 201.917,553.417 196.75,551.25C191.583,549.083 187.083,546.083 183.25,542.25C179.417,538.417 176.333,533.917 174,528.75C171.667,523.583 170.5,518 170.5,512C170.5,506 171.667,500.417 174,495.25C176.333,490.083 179.417,485.583 183.25,481.75C187.083,477.917 191.583,474.917 196.75,472.75C201.917,470.583 207.5,469.5 213.5,469.5L810.5,469.5C816.5,469.5 822.083,470.583 827.25,472.75C832.417,474.917 836.917,477.917 840.75,481.75C844.583,485.583 847.667,490.083 850,495.25C852.333,500.417 853.5,506 853.5,512C853.5,518 852.333,523.583 850,528.75C847.667,533.917 844.583,538.417 840.75,542.25C836.917,546.083 832.417,549.083 827.25,551.25C822.083,553.417 816.5,554.5 810.5,554.5Z"
        />
        <path v-else class="path-checkmark" d="M 4.5303 12.9697 L 8.5 16.9393 L 18.9697 6.4697" fill="none" />
      </svg>
    </div>
    <span v-if="$slots.default">
      <slot></slot>
    </span>
  </label>
</template>

<style>
  .checkbox {
    appearance: none;
    background-clip: padding-box;
    background-color: var(--wui-control-alt-secondary);
    block-size: var(--size);
    border: 1px solid var(--wui-control-strong-stroke-default);
    border-radius: var(--wui-control-corner-radius);
    font-family: var(--wui-font-family-text);
    font-size: var(--wui-font-size-body);
    font-weight: 400;
    inline-size: var(--size);
    line-height: var(--size);
    margin: 0;
    outline: none;
    user-select: none;
  }
  .checkbox:focus-visible {
    box-shadow: var(--wui-focus-stroke);
  }
  .checkbox:hover {
    background-color: var(--wui-control-alt-tertiary);
  }
  .checkbox:active {
    background-color: var(--wui-control-alt-quarternary);
    border-color: var(--wui-control-strong-stroke-disabled);
  }
  .checkbox:active + .checkbox-glyph {
    color: var(--wui-text-on-accent-secondary);
  }
  .checkbox:disabled {
    background-color: var(--wui-control-alt-disabled);
    border-color: var(--wui-control-strong-stroke-disabled);
    pointer-events: none;
  }
  .checkbox:checked,
  .checkbox:indeterminate {
    background-color: var(--wui-accent-default);
    border: none;
  }
  .checkbox:checked:hover,
  .checkbox:indeterminate:hover {
    background-color: var(--wui-accent-secondary);
  }
  .checkbox:checked:active,
  .checkbox:indeterminate:active {
    background-color: var(--wui-accent-tertiary);
  }
  .checkbox:checked:disabled,
  .checkbox:indeterminate:disabled {
    background-color: var(--wui-accent-disabled);
    border-color: var(--wui-control-strong-stroke-disabled);
  }
  .checkbox:checked:disabled + .checkbox-glyph,
  .checkbox:indeterminate:disabled + .checkbox-glyph {
    color: var(--wui-text-on-accent-disabled);
  }
  .checkbox:checked + .checkbox-glyph .path-checkmark,
  .checkbox:indeterminate + .checkbox-glyph .path-checkmark {
    stroke-dashoffset: 0;
    transition: var(--wui-control-normal-duration) cubic-bezier(0.55, 0, 0, 1) stroke-dashoffset;
  }
  .checkbox-container {
    align-items: center;
    color: var(--wui-text-primary);
    display: inline-flex;
    font-family: var(--wui-font-family-text);
    font-size: var(--wui-font-size-body);
    font-weight: 400;
    line-height: var(--size);
    min-block-size: var(--size);
    -webkit-user-select: none;
    -moz-user-select: none;
    -ms-user-select: none;
    user-select: none;
  }
  .checkbox-container > span {
    -webkit-padding-start: 8px;
    padding-inline-start: 8px;
  }
  .checkbox-container.disabled > span {
    color: var(--wui-text-disabled);
  }
  .checkbox-inner {
    align-items: center;
    display: flex;
    justify-content: center;
    position: relative;
  }
  .checkbox-glyph {
    block-size: 12px;
    color: inherit;
    color: var(--wui-text-on-accent-primary);
    inline-size: 12px;
    position: absolute;
  }
  .checkbox-glyph path {
    transform-origin: center;
  }
  .checkbox-glyph .path-checkmark {
    stroke: currentColor;
    stroke-width: 2;
    stroke-linecap: round;
    stroke-linejoin: round;
    stroke-dasharray: 20.5;
    stroke-dashoffset: 20.5;
    transform: scale(1.2);
  }
  .checkbox-glyph .path-indeterminate {
    fill: currentColor;
    transform: scale(0.6666666667) translateX(80px) translateY(240px);
  }
</style>
