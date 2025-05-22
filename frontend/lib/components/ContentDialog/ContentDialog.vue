<script setup lang="ts">
  import TextBlock from '$components/TextBlock/TextBlock.vue';
  import { PreventableEvent } from '$utils';
  import { ref, useTemplateRef, watch } from 'vue';

  const {
    closeOnEscape = true,
    closeOnBackdropClick = true,
    title,
    size = 'standard',
    ...restProps
  } = defineProps<{
    closeOnEscape?: boolean;
    closeOnBackdropClick?: boolean;
    title?: string;
    size?: 'min' | 'standard' | 'max';
  }>();

  const emit = defineEmits<{
    (e: 'beforeClose'): void;
    (e: 'close', event: PreventableEvent): void;
    (e: 'afterClose'): void;
  }>();

  const dialog = useTemplateRef<HTMLDialogElement>('dialog');

  const isOpen = ref(false);
  function open() {
    if (dialog.value) {
      dialog.value.showModal();
      isOpen.value = true;
    }
  }

  function close() {
    if (dialog.value) {
      emit('beforeClose');

      const closeEvent = new PreventableEvent();
      emit('close', closeEvent);

      if (closeEvent.defaultPrevented) return;

      // TODO: requestClose: always use requestClose once all browsers have supported it for a while
      try {
        dialog.value.requestClose();
      } catch (error) {
        dialog.value.close();
      }
      isOpen.value = false;

      emit('afterClose');
    }
  }

  function toggle() {
    if (isOpen) {
      open();
    } else {
      close();
    }
  }

  const id = Math.floor(Math.random() * 1000000).toString(16); // generate a random ID for the popover
  const popoverId = `popover-${id}`; // unique ID for the popover

  defineExpose({ open, close, toggle, popoverId, isOpen });

  // Watch for the closeOnEscape prop to enable/disable the escape key functionality
  function handleEscape(event: KeyboardEvent) {
    if (event.key === 'Escape' && dialog.value) {
      close();
    }
  }
  watch(
    () => closeOnEscape,
    ($closeOnEscape) => {
      if ($closeOnEscape) {
        document.addEventListener('keydown', handleEscape);
      } else {
        document.removeEventListener('keydown', handleEscape);
      }
    },
    { immediate: true }
  );

  function handleBackdropClick(event: MouseEvent) {
    const dialogElement = dialog.value;
    if (closeOnBackdropClick && dialogElement && event.target) {
      if (event.target === dialog.value) {
        close();
      }
    }
  }
  watch(
    () => [closeOnBackdropClick, isOpen.value] as const,
    ([$closeOnBackdropClick]) => {
      const dialogElement = dialog.value;
      if ($closeOnBackdropClick && dialogElement) {
        dialogElement.addEventListener('click', handleBackdropClick);
      }
      return () => {
        if (dialogElement) {
          dialogElement.removeEventListener('click', handleBackdropClick);
        }
      };
    },
    { immediate: true }
  );
</script>

<template>
  <dialog
    ref="dialog"
    popover="manual"
    :id="popoverId"
    class="content-dialog"
    :class="`size-${size}`"
    :="restProps"
    modal
    @click.stop
  >
    <div class="content-dialog-inner">
      <div class="content-dialog-body">
        <TextBlock v-if="title" variant="subtitle" class="content-dialog-title">{{ title }}</TextBlock>
        <slot></slot>
      </div>
      <footer class="content-dialog-footer">
        <slot name="footer"></slot>
      </footer>
    </div>
  </dialog>
</template>

<style scoped>
  .content-dialog {
    animation: dialog-out var(--wui-control-fast-duration) var(--wui-control-fast-out-slow-in-easing);
    max-inline-size: calc(100% - 24px);
    inline-size: 100%;
    border-radius: var(--wui-overlay-corner-radius);

    background-clip: padding-box;
    box-shadow: var(--wui-dialog-shadow);
    border: 1px solid var(--wui-surface-stroke-default);
    overflow: hidden;
    padding: 0;
    user-select: none;
    font-family: var(--wui-font-family-text);
    font-size: var(--wui-body-font-size);
    font-weight: normal;
    line-height: 20px;
    /* keep the popover open so we can animate out */
    transition: display var(--wui-control-fast-duration) allow-discrete,
      overlay var(--wui-control-faster-duration) allow-discrete;
  }
  .content-dialog:open {
    animation-name: dialog-in;
  }
  .content-dialog::backdrop {
    top: var(--header-height);
    animation: fade-out var(--wui-control-faster-duration) linear;
    background-color: var(--wui-smoke-default);
  }
  .content-dialog:open::backdrop {
    animation-name: fade-in;
  }

  .content-dialog.size-min {
    max-inline-size: 320px;
  }
  .content-dialog.size-standard {
    max-inline-size: 448px;
  }
  .content-dialog.size-max {
    max-inline-size: 540px;
  }

  .content-dialog-inner {
    background-color: var(--wui-solid-background-base);
  }

  .content-dialog .content-dialog-title {
    display: block;
    margin-bottom: 12px;
    color: var(--text-primary);
  }

  .content-dialog-body,
  .content-dialog-footer {
    position: relative;
    padding: 24px;
  }

  .content-dialog-body {
    background-color: var(--wui-layer-default);
    color: var(--wui-text-primary);
  }

  .content-dialog-footer {
    display: grid;
    grid-auto-rows: 1fr;
    grid-auto-flow: column;
    grid-gap: 8px;
    border-block-start: 1px solid var(--wui-card-stroke-default);
    white-space: nowrap;
  }

  .content-dialog-footer > :where(.button, button):only-child {
    inline-size: 50%;
    justify-self: end;
  }
</style>
