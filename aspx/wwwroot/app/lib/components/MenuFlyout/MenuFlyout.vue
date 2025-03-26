<script setup lang="ts">
  import { computed, useTemplateRef } from 'vue';

  const {
    popover = 'auto', // default popover trigger type
    placement = 'bottom',
    anchor = 'end',
    ...restProps
  } = defineProps<{
    popover?: 'auto' | 'hint' | 'manual'; // popover trigger type
    placement?: 'top' | 'right' | 'bottom' | 'left';
    anchor?: 'start' | 'center' | 'end';
  }>();

  const positionArea = computed(() => {
    if (placement === 'top') {
      if (anchor === 'start') return 'top span-right';
      if (anchor === 'center') return 'top center';
      if (anchor === 'end') return 'top span-left';
    }
    if (placement === 'right') {
      if (anchor === 'start') return 'right span-bottom';
      if (anchor === 'center') return 'right center';
      if (anchor === 'end') return 'right span-top';
    }
    if (placement === 'bottom') {
      if (anchor === 'start') return 'bottom span-right';
      if (anchor === 'center') return 'bottom center';
      if (anchor === 'end') return 'bottom span-left';
    }
    if (placement === 'left') {
      if (anchor === 'start') return 'left span-bottom';
      if (anchor === 'center') return 'left center';
      if (anchor === 'end') return 'left span-top';
    }
  });

  const dialog = useTemplateRef<HTMLDivElement>('menu');

  function open() {
    if (dialog.value) {
      if (!dialog.value.matches(':popover-open')) {
        dialog.value.showPopover();
      }
    }
  }

  function close() {
    if (dialog.value) {
      if (dialog.value.matches(':popover-open')) {
        dialog.value.hidePopover();
      }
    }
  }

  function toggle() {
    if (dialog.value) {
      dialog.value.togglePopover();
    }
  }

  const id = Math.floor(Math.random() * 1000000).toString(16); // generate a random ID for the popover
  const popoverId = `popover-${id}`; // unique ID for the popover

  defineExpose({ open, close, toggle, popoverId });
</script>

<template>
  <slot :popoverId></slot>
  <div
    ref="menu"
    :popover
    :id="popoverId"
    :style="{
      '--positionArea': positionArea,
      '--menu-flyout-transition-offset': placement === 'top' ? '50%' : '-50%',
    }"
    :="restProps"
    class="menu-flyout"
    @click.stop
  >
    <div class="menu-flyout-surface">
      <slot name="menu"></slot>
    </div>
  </div>
</template>

<style scoped>
  .menu-flyout {
    position-area: var(--positionArea);
    margin: 0;
    padding: 0;
    border: 0;
    background: none;
  }

  .menu-flyout:popover-open {
    animation: menu-shadow var(--wui-control-fast-duration) var(--wui-control-fast-out-slow-in-easing)
      var(--wui-control-normal-duration) forwards;
    overflow: hidden;
  }

  .menu-flyout-surface {
    user-select: none;
    font-family: var(--wui-font-family-text);
    font-size: var(--wui-font-size-body);
    font-weight: normal;
    line-height: 20px;
    display: flex;
    flex-direction: column;
    animation: menu-open var(--wui-control-normal-duration) var(--wui-control-fast-out-slow-in-easing);
    min-inline-size: 120px;
    max-inline-size: 100%;
    max-block-size: 100vh;
    margin: 0;
    padding: 0;
    padding-block: 2px;
    box-sizing: border-box;
    color: var(--wui-text-primary);
    border-radius: var(--wui-overlay-corner-radius);
    border: 1px solid var(--wui-surface-stroke-flyout);
    background-color: var(--wui-solid-background-quarternary);
    background-clip: padding-box;
  }
</style>
