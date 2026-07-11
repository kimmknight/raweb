<script setup lang="ts">
  import { registerIconAnimationKey, type IconAnimationHandle } from '$components/AnimatedIcon/iconAnimation';
  import { computed, provide, useAttrs } from 'vue';

  const { href, tag, disabled, tabindex } = defineProps<{
    href?: string;
    disabled?: boolean;
    tag?: string;
    tabindex?: number | null;
  }>();
  const restProps = useAttrs();

  const tagName = computed(() => tag ?? (href ? 'a' : 'button'));

  // slotted content (e.g. AnimatedChevronDown) can register itself here to
  // receive press/release animation triggers from pointer events
  let iconAnimation: IconAnimationHandle | undefined;
  provide(registerIconAnimationKey, (handle) => (iconAnimation = handle));

  function press() {
    if (disabled) return;
    iconAnimation?.press();
  }
  function onPointerEnter(event: PointerEvent) {
    if (event.buttons & 1) {
      press();
    }
  }
  function release() {
    iconAnimation?.release();
  }
</script>

<template>
  <component
    :is="tagName"
    :href
    class="icon-button"
    :disabled
    :tabindex="tabindex === null ? undefined : (tabindex ?? 0)"
    :="restProps"
    @pointerdown="press"
    @pointerup="release"
    @pointerenter="onPointerEnter"
    @pointerleave="release"
    @pointercancel="release"
  >
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
    transition:
      background var(--wui-control-faster-duration) ease,
      color var(--wui-control-faster-duration) ease;
    flex-grow: 0;
    flex-shrink: 0;
    -webkit-user-drag: none;
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
  .icon-button svg {
    inline-size: 16px;
    block-size: auto;
    fill: currentColor;
  }
</style>
