<script setup lang="ts">
  import { useAttrs } from 'vue';

  const {
    containerClass = '',
    showSubmitButton = false,
    showClearButton,
  } = defineProps<{
    containerClass?: string;
    showSubmitButton?: boolean;
    showClearButton?: boolean;
  }>();

  const restProps = useAttrs();

  const model = defineModel<string>('value', { default: '' });

  const emit = defineEmits<{
    (e: 'submit', value: string): void;
  }>();

  function handleSubmit() {
    emit('submit', model.value);
  }

  function handleKeyDown(event: KeyboardEvent) {
    if (event.key === 'Enter') {
      event.preventDefault();
      handleSubmit();
    }
  }

  function clear() {
    model.value = '';
  }
</script>

<template>
  <div class="text-box-container" :class="containerClass">
    <input class="text-box" v-model="model" :="restProps" @keydown="handleKeyDown" />
    <button class="text-box-button clear" v-if="showClearButton" @click="clear">
      <slot name="clear-icon">
        <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
          <path
            d="m4.397 4.554.073-.084a.75.75 0 0 1 .976-.073l.084.073L12 10.939l6.47-6.47a.75.75 0 1 1 1.06 1.061L13.061 12l6.47 6.47a.75.75 0 0 1 .072.976l-.073.084a.75.75 0 0 1-.976.073l-.084-.073L12 13.061l-6.47 6.47a.75.75 0 0 1-1.06-1.061L10.939 12l-6.47-6.47a.75.75 0 0 1-.072-.976l.073-.084-.073.084Z"
            fill="currentColor"
          />
        </svg>
      </slot>
    </button>
    <button class="text-box-button" v-if="showSubmitButton" @click="handleSubmit">
      <slot name="submit-icon">
        <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
          <path
            d="M13.267 4.209a.75.75 0 0 0-1.034 1.086l6.251 5.955H3.75a.75.75 0 0 0 0 1.5h14.734l-6.251 5.954a.75.75 0 0 0 1.034 1.087l7.42-7.067a.996.996 0 0 0 .3-.58.758.758 0 0 0-.001-.29.995.995 0 0 0-.3-.578l-7.419-7.067Z"
            fill="currentColor"
          />
        </svg>
      </slot>
    </button>
    <div class="text-box-underline"></div>
  </div>
</template>

<style scoped>
  input {
    background: none;
    border: none;
    width: 100%;
    background-color: transparent;
    border: none;
    border-radius: var(--wui-control-corner-radius);
    box-sizing: border-box;
    color: var(--wui-text-primary);
    cursor: unset;
    flex: 1 1 auto;
    font-family: var(--wui-font-family-text);
    font-size: var(--wui-font-size-body);
    font-weight: 400;
    inline-size: 100%;
    margin: 0;
    min-block-size: 30px;
    outline: none;
    padding-inline: 10px;
    padding-block: 5px;
    -webkit-user-select: none;
    -moz-user-select: none;
    -ms-user-select: none;
    user-select: none;
    resize: none;
    overflow-y: hidden;
  }

  input:focus {
    outline: none;
  }

  .text-box-container {
    align-items: center;
    background-clip: padding-box;
    background-color: var(--wui-control-fill-default);
    border: 1px solid var(--wui-control-stroke-default);
    border-radius: var(--wui-control-corner-radius);
    cursor: text;
    display: flex;
    inline-size: 100%;
    position: relative;
  }

  .text-box-container:hover {
    background-color: var(--wui-control-fill-secondary);
  }

  .text-box-container.disabled {
    background-color: var(--wui-control-fill-disabled);
    cursor: default;
  }

  .text-box-container.disabled .text-box-underline {
    display: none;
  }

  .text-box-container:focus-within {
    background-color: var(--wui-control-fill-input-active);
  }

  .text-box-container:focus-within .text-box-underline:after {
    border-bottom: 2px solid var(--wui-accent-default);
  }

  .text-box-underline {
    block-size: calc(100% + 2px);
    border-radius: var(--wui-control-corner-radius);
    inline-size: calc(100% + 2px);
    inset-block-start: -1px;
    inset-inline-start: -1px;
    overflow: hidden;
    pointer-events: none;
    position: absolute;
  }

  .text-box-underline::after {
    block-size: 100%;
    border-bottom: 1px solid var(--wui-control-strong-stroke-default);
    box-sizing: border-box;
    content: '';
    inline-size: 100%;
    inset-block-end: 0;
    inset-inline-start: 0;
    position: absolute;
  }

  .text-box-button {
    align-items: center;
    background-color: var(--wui-subtle-transparent);
    border: none;
    border-radius: var(--wui-control-corner-radius);
    box-sizing: border-box;
    color: var(--wui-text-secondary);
    display: flex;
    justify-content: center;
    min-block-size: 22px;
    min-inline-size: 26px;
    margin-inline-start: 6px;
    margin-inline-end: 4px;
    outline: none;
    padding: 3px 5px;
  }
  .text-box-button:hover {
    background-color: var(--wui-subtle-secondary);
  }
  .text-box-button:active {
    background-color: var(--wui-subtle-tertiary);
    color: var(--wui-text-tertiary);
  }
  .text-box-button:disabled {
    background-color: var(--wui-subtle-disabled);
    color: var(--wui-text-disabled);
  }

  .text-box-button.clear {
    margin-inline-end: 0;
    display: none;
  }
  .text-box-container:focus-within .text-box-button.clear {
    display: flex;
  }
</style>

<style>
  .text-box-button > svg {
    inline-size: 12px;
    block-size: auto;
  }
</style>
