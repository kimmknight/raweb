<script setup lang="ts">
  import { registerIconAnimationKey, type IconAnimationHandle } from '$components/AnimatedIcon/iconAnimation';
  import ProgressRing from '$components/ProgressRing/ProgressRing.vue';
  import { computed, provide } from 'vue';

  export interface StandardButtonProps {
    variant?: 'standard' | 'accent' | 'hyperlink';
    href?: string;
    disabled?: boolean;
    loading?: boolean;
  }

  const { variant = 'standard', href, disabled, loading } = defineProps<StandardButtonProps>();

  const tagName = computed(() => (href ? 'a' : 'button'));

  // the icon-end slot content (e.g. AnimatedChevronDown) can register itself
  // here to receive press/release animation triggers from pointer events
  let iconEndAnimation: IconAnimationHandle | undefined;
  provide(registerIconAnimationKey, (handle) => (iconEndAnimation = handle));
  function press() {
    if (disabled || loading) return;
    iconEndAnimation?.press();
  }
  function onPointerEnter(event: PointerEvent) {
    if (event.buttons & 1) {
      press();
    }
  }
  function release() {
    iconEndAnimation?.release();
  }
</script>

<template>
  <component
    :is="tagName"
    :href
    :class="['button', `style-${variant}`, disabled ? 'disabled' : '', loading ? 'loading' : '']"
    :disabled="disabled || loading"
    tabindex="0"
    @pointerdown="press"
    @pointerup="release"
    @pointerenter="onPointerEnter"
    @pointerleave="release"
    @pointercancel="release"
  >
    <slot name="icon"></slot>
    <ProgressRing v-if="$slots.default && loading" style="position: absolute" :size="16" />
    <span v-if="$slots.default" :style="`opacity: ${loading ? 0 : 1}; text-box: trim-both cap alphabetic`"
      ><slot></slot
    ></span>
    <span
      class="icon-end-wrapper"
      v-if="$slots['icon-end']"
      :class="{ noMargin: !$slots['icon'] && !$slots.default }"
      ><slot name="icon-end"></slot
    ></span>
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
    color: var(--wui-text-primary);
    line-height: 20px;
    position: relative;
    box-sizing: border-box;
    padding-block: 5px 6px;
    padding-inline: 11px;
    text-decoration: none;
    border: none;
    cursor: default;
    border-radius: var(--wui-control-corner-radius);
    transition: background var(--wui-control-faster-duration) ease;
    min-height: 30px;
  }

  .button.style-standard {
    border: 0;
    box-shadow:
      inset 0 0 0 1px var(--wui-control-stroke-default),
      inset 0 -1px 0 0 var(--wui-control-stroke-secondary-overlay);
    background-color: var(--wui-control-fill-default);
    color: var(--text-primary);
    background-clip: padding-box;
  }
  .button.style-standard:hover:not(.disabled) {
    background-color: var(--wui-control-fill-secondary);
  }
  .button.style-standard:hover:active:not(.disabled) {
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
  .button.style-hyperlink:hover:not(.disabled) {
    background-color: var(--wui-subtle-secondary);
  }
  .button.style-hyperlink:hover:active:not(.disabled) {
    background-color: var(--wui-subtle-tertiary);
    color: var(--wui-accent-text-tertiary);
  }
  .button.style-hyperlink.disabled {
    color: var(--wui-accent-text-disabled);
  }

  .button.style-accent {
    background-color: var(--wui-accent-default);
    box-shadow:
      inset 0 0 0 1px var(--wui-control-stroke-on-accent-default),
      inset 0 -1px 0 0 var(--wui-control-stroke-on-accent-secondary);
    color: var(--wui-text-on-accent-primary);
  }
  .button.style-accent:hover:not(.disabled) {
    background-color: var(--wui-accent-secondary);
  }
  .button.style-accent:hover:active:not(.disabled) {
    background-color: var(--wui-accent-tertiary);
    box-shadow: none;
    color: var(--wui-text-on-accent-secondary);
  }
  .button.style-accent.disabled {
    background-color: var(--wui-accent-disabled);
    color: var(--wui-text-on-accent-disabled);
  }
</style>

<style>
  .button > svg,
  .button > .icon-end-wrapper svg {
    fill: currentColor;
  }
  .button.loading > svg:not(.progress-ring) {
    opacity: 0;
  }

  .button > svg:first-child {
    margin-inline-end: 8px;
    inline-size: 16px;
    block-size: 16px;
  }
  .button > .icon-end-wrapper svg {
    margin-inline-start: 8px;
    inline-size: 12px;
    block-size: 12px;
  }
  .button > .icon-end-wrapper.noMargin svg {
    margin-inline-start: 0;
  }

  .button > .icon-end-wrapper {
    transform: translateY(0px);
  }
</style>
