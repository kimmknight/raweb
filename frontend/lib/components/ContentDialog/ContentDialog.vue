<script setup lang="ts">
  import { IconButton, ProgressRing } from '$components';
  import TextBlock from '$components/TextBlock/TextBlock.vue';
  import { useCoreDataStore } from '$stores';
  import { PreventableEvent } from '$utils';
  import { useTranslation } from 'i18next-vue';
  import { nextTick, ref, useAttrs, useTemplateRef, watch, watchEffect } from 'vue';

  const {
    closeOnEscape = true,
    closeOnBackdropClick = true,
    size = 'standard',
    loading = false,
    initialOpen = false,
  } = defineProps<{
    closeOnEscape?: boolean;
    closeOnBackdropClick?: boolean;
    title?: string;
    size?: 'min' | 'standard' | 'max' | 'maxer' | 'maxest';
    maxHeight?: string;
    fillHeight?: boolean;
    /** If enabled, a loading indicator will be shown after the title */
    updating?: boolean;
    /** If enabled, the dialog content will be replaced by a loading screen of
     * the maximum height. To control the maximum height, set maxHeight. With
     * this option enabled it is recommended to also enable fillHeight so that
     * the dialog height does not shrink after loading is complete. */
    loading?: boolean;
    /** If an error is provided, the dialog content will be replaced
     * by a generic error message and a details element with the provided
     * error message.
     */
    error?: boolean | Error;
    /** Whether the dialog is initially open. Defaults to false. */
    initialOpen?: boolean;
    /** When specified, a titlebar will be rendered at the top of the dialog. */
    titlebar?: string;
  }>();
  const restProps = useAttrs();

  const emit = defineEmits<{
    (e: 'beforeOpen'): void;
    (e: 'open', event: PreventableEvent): void;
    (e: 'afterOpen'): void;
    (e: 'beforeClose'): void;
    (e: 'close', event: PreventableEvent<{ close: () => void }>): void;
    (e: 'afterClose'): void;
    (e: 'saveKeyboardShortcut', close: () => void): void;
  }>();

  const { t } = useTranslation();
  const { appBase } = useCoreDataStore();

  const dialog = useTemplateRef<HTMLDialogElement>('dialog');

  watchEffect(() => {
    if (initialOpen && dialog.value) {
      open();
    }
  });

  const isOpen = ref(false);
  function open() {
    if (dialog.value) {
      emit('beforeOpen');

      const openEvent = new PreventableEvent();
      emit('open', openEvent);

      if (openEvent.defaultPrevented) return;

      dialog.value.showModal();
      isOpen.value = true;

      emit('afterOpen');
    }
  }

  function close() {
    if (dialog.value && isOpen.value) {
      emit('beforeClose');

      const closeEvent = new PreventableEvent({ close: dialog.value.close.bind(dialog.value) });
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

  // Store whether the dialog was loading for at least 500ms.
  // We use this to determine whether to animate in the content after loading
  // has finished.
  const wasLoading = ref(false);
  watch(
    () => [loading, isOpen.value],
    ([isLoading]) => {
      let timeout: number | undefined;
      if (isLoading && isOpen.value) {
        timeout = window.setTimeout(() => {
          wasLoading.value = true;
        }, 500);
      } else {
        // do not immediately set wasLoading to false
        // so there is time to animate in the content
        timeout = window.setTimeout(() => {
          wasLoading.value = false;
        }, 500);
      }
      return () => {
        if (timeout) {
          clearTimeout(timeout);
        }
      };
    },
    { immediate: true }
  );

  const id = Math.floor(Math.random() * 1000000).toString(16); // generate a random ID for the popover
  const popoverId = `popover-${id}`; // unique ID for the popover

  defineExpose({ open, close, toggle, popoverId, isOpen });

  // track whether focus is directly in the dialog (and not a child dialog)
  const hasFocus = ref(false);
  function handleFocusIn(event: FocusEvent) {
    const target = event.target as HTMLElement | null;
    if (!target) {
      hasFocus.value = false;
      return;
    }

    const closestParentDialog = target.closest('dialog');
    if (!closestParentDialog) {
      hasFocus.value = false;
      return;
    }

    const isDirectlyInThisDialog = closestParentDialog === dialog.value;
    hasFocus.value = isDirectlyInThisDialog ?? false;
  }
  function handleFocusOut(event: FocusEvent) {
    hasFocus.value = false;
  }
  watch(
    () => [isOpen.value, dialog.value],
    ($isOpen) => {
      const dialogElement = dialog.value;
      if ($isOpen && dialogElement) {
        dialogElement.addEventListener('focusin', handleFocusIn);
        dialogElement.addEventListener('focusout', handleFocusOut);
      }
      return () => {
        if (dialogElement) {
          dialogElement.removeEventListener('focusin', handleFocusIn);
          dialogElement.removeEventListener('focusout', handleFocusOut);
        }
      };
    },
    { immediate: true }
  );

  // watch for the closeOnEscape prop to enable/disable the escape key functionality,
  // but only allow close on escape if the focus is directly in this dialog
  // and not in a child dialog
  function handleEscape(event: KeyboardEvent) {
    if (event.key !== 'Escape') {
      return;
    }

    if (dialog.value && hasFocus.value) {
      // allow the keyboard event to finish being processed by other dialogs that
      // do not have focus so that they will not respond to the same escape key event
      setTimeout(() => close(), 10);
    } else {
      event.preventDefault();
    }
    event.stopPropagation();
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
      if (event.target === dialogElement) {
        close();
      }
    }
  }
  watch(
    () => [closeOnBackdropClick, dialog.value] as const,
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

  // track the height of the title element, including when the text changes/wraps
  const titleElement = useTemplateRef<typeof TextBlock>('titleElement');
  const titleHeight = ref(0);
  watch(
    () => [titleElement.value, dialog.value, isOpen.value] as const,
    async () => {
      if (!isOpen.value) {
        return;
      }

      await nextTick();

      const element = titleElement.value?.$el as HTMLElement | undefined;
      if (!element) {
        titleHeight.value = 0;
        return;
      }

      const resizeObserver = new ResizeObserver(() => {
        const style = getComputedStyle(element);
        const marginTop = parseFloat(style.marginTop) || 0;
        const marginBottom = parseFloat(style.marginBottom) || 0;
        titleHeight.value = element.offsetHeight + marginTop + marginBottom;
      });
      resizeObserver.observe(element);

      // set initial height
      const style = getComputedStyle(element);
      const marginTop = parseFloat(style.marginTop) || 0;
      const marginBottom = parseFloat(style.marginBottom) || 0;
      titleHeight.value = element.offsetHeight + marginTop + marginBottom;

      return () => {
        resizeObserver.disconnect();
      };
    },
    { immediate: true }
  );

  // detect the keyboard shortcut for saving (Ctrl+S or Cmd+S) and emit an event
  const isMacOS = window.navigator.platform.startsWith('MacIntel') && window.navigator.maxTouchPoints === 0;
  const isIOS = window.navigator.platform.startsWith('MacIntel') && window.navigator.maxTouchPoints > 1;
  function handleSaveShortcut(event: KeyboardEvent) {
    if (
      (isMacOS || isIOS ? event.metaKey : event.ctrlKey) &&
      event.key.toLowerCase() === 's' &&
      isOpen.value &&
      hasFocus.value
    ) {
      event.preventDefault();
      emit('saveKeyboardShortcut', () => {
        setTimeout(() => close(), 10);
      });
    }
  }
  watch(
    () => [isOpen.value, dialog.value] as const,
    ([$isOpen]) => {
      const dialogElement = dialog.value;
      if ($isOpen && dialogElement) {
        dialogElement.addEventListener('keydown', handleSaveShortcut);
      }
      return () => {
        if (dialogElement) {
          dialogElement.removeEventListener('keydown', handleSaveShortcut);
        }
      };
    },
    { immediate: true }
  );

  function focusNext(element: HTMLElement) {
    const focusable = Array.from(
      document.querySelectorAll(
        'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'
      )
    ).filter((el) => el instanceof HTMLElement) as HTMLElement[];
    const idx = focusable.indexOf(element);
    const next = focusable[idx + 1] || focusable[0];
    next?.focus();
  }
</script>

<template>
  <slot name="opener" :popoverId :open :close></slot>
  <dialog
    ref="dialog"
    popover="manual"
    :id="popoverId"
    class="content-dialog"
    :class="`size-${size}`"
    :style="`--user-provided-dialog-max-height: ${maxHeight ?? ''}; --title-height: ${titleHeight}px; ${
      titlebar ? `--wui-layer-default: transparent;` : ''
    }`"
    :="restProps"
    modal
    @click.stop
    @contextmenu.stop
  >
    <div class="content-dialog-titlebar" v-if="titlebar">
      <img :src="`${appBase}lib/assets/icon.svg`" alt="" class="logo" />
      <TextBlock variant="caption">{{ titlebar }}</TextBlock>
    </div>
    <div class="content-dialog-inner">
      <!-- if clicking the backdrop to close the dialog is disabled, show an X in the corner instead -->
      <IconButton
        class="content-dialog-close-button"
        @click="close"
        v-if="!closeOnBackdropClick"
        tag="div"
        :tabindex="null"
      >
        <svg viewBox="0 0 24 24">
          <path
            d="m4.397 4.554.073-.084a.75.75 0 0 1 .976-.073l.084.073L12 10.939l6.47-6.47a.75.75 0 1 1 1.06 1.061L13.061 12l6.47 6.47a.75.75 0 0 1 .072.976l-.073.084a.75.75 0 0 1-.976.073l-.084-.073L12 13.061l-6.47 6.47a.75.75 0 0 1-1.06-1.061L10.939 12l-6.47-6.47a.75.75 0 0 1-.072-.976l.073-.084-.073.084Z"
            fill="currentColor"
          />
        </svg>
      </IconButton>

      <div
        :class="`content-dialog-body ${wasLoading ? 'wasLoading' : ''}`"
        :style="`${fillHeight ? 'height: 100vh;' : ''}; ${
          titlebar ? `padding-top: calc(var(--inner-padding) - 0px);` : ''
        }`"
      >
        <TextBlock v-if="title" variant="subtitle" class="content-dialog-title" ref="titleElement">
          {{ title }}
          <ProgressRing
            :size="16"
            v-if="updating"
            :style="`
              padding: 0 8px;

              /* fade out as the loading screen fades in */
              ${
                loading
                  ? `
              opacity: 1;
              animation: fade-out var(--wui-view-transition-fade-in) cubic-bezier(0.455, 0.03, 0.515, 0.955)
                1000ms forwards;
              `
                  : ``
              }
            `"
          />
        </TextBlock>

        <div
          class="content-dialog-loading-screen"
          v-if="loading"
          style="
            opacity: 0;
            animation: fade-in var(--wui-view-transition-fade-in) cubic-bezier(0.455, 0.03, 0.515, 0.955) 1000ms
              forwards;
          "
        >
          <ProgressRing :size="48" />
          <TextBlock variant="subtitle" tag="h1" style="font-size: 16px">{{ t('pleaseWait') }}</TextBlock>
        </div>
        <div class="content-dialog-loading-screen" v-else-if="error">
          <TextBlock variant="subtitle" tag="h1" style="font-size: 16px">{{ t('unknownError') }}</TextBlock>
          <details>
            <summary>Error details</summary>
            <pre v-if="error instanceof Error">{{ error.message }}</pre>
            <pre v-else>{{ error }}</pre>
          </details>
        </div>
        <slot v-else :close :popoverId></slot>
      </div>
      <footer
        :class="`content-dialog-footer ${titlebar ? 'noTopPadding' : ''} ${
          (!closeOnBackdropClick && !titlebar) || $slots['footer-left'] ? 'splitMode' : ''
        }`"
        v-if="$slots.footer"
      >
        <template v-if="(!closeOnBackdropClick && !titlebar) || $slots['footer-left']">
          <div class="content-dialog-footer-button-group left">
            <slot name="footer-left" :close></slot>
          </div>
          <div class="content-dialog-footer-button-group right">
            <slot name="footer" :close></slot>
          </div>
        </template>
        <slot name="footer" :close v-else></slot>
      </footer>
    </div>
  </dialog>
</template>

<style scoped>
  .content-dialog {
    --inner-padding: 24px;

    animation: dialog-out var(--wui-control-fast-duration) var(--wui-control-fast-out-slow-in-easing);
    max-inline-size: calc(100% - var(--inner-padding));
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

    --dialog-max-height: calc(
      min(var(--user-provided-dialog-max-height, 100vh), 100vh) - var(--header-height) - var(--inner-padding)
    );
    max-height: var(--dialog-max-height);
    top: var(--header-height);
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
  .content-dialog.size-maxer {
    max-inline-size: 680px;
  }
  .content-dialog.size-maxest {
    max-inline-size: 800px;
  }

  .content-dialog-inner {
    background-color: var(--wui-solid-background-base);
  }

  .content-dialog .content-dialog-title {
    display: block;
    margin-bottom: 12px;
    color: var(--text-primary);
    position: sticky;
    top: 0;
    z-index: 99;
  }
  .content-dialog .content-dialog-title::before {
    content: '';
    position: absolute;
    background-color: var(--wui-solid-background-base);
    inset: 0;
    top: -28px;
    z-index: -1;
  }
  .content-dialog .content-dialog-title::after {
    content: '';
    position: absolute;
    background-color: var(--wui-layer-default);
    inset: 0;
    top: -28px;
    z-index: -1;
  }

  .content-dialog-body,
  .content-dialog-footer {
    position: relative;
    padding: var(--inner-padding);
  }

  .content-dialog-body {
    background-color: var(--wui-layer-default);
    color: var(--wui-text-primary);
    box-sizing: border-box;
    max-height: calc(var(--dialog-max-height) - 80px);
    overflow-y: auto;
    overflow-x: hidden;
    outline: none;
  }

  @keyframes entrance {
    from {
      transform: translateY(20px);
      opacity: 0;
    }
  }

  .content-dialog-body.wasLoading > :deep(*:not(.content-dialog-loading-screen):not(.content-dialog-title)) {
    animation: var(--wui-view-transition-fade-out) both fade-in,
      var(--wui-view-transition-slide-in) cubic-bezier(0.16, 1, 0.3, 1) both entrance;
  }

  .content-dialog-footer:not(.splitMode) {
    display: grid;
    grid-auto-columns: 1fr;
    grid-auto-flow: column;
    grid-gap: 8px;
    border-block-start: 1px solid var(--wui-card-stroke-default);
    white-space: nowrap;
  }
  .content-dialog-footer:not(.splitMode) > :where(.button, button):only-child {
    inline-size: 50%;
    justify-self: end;
  }

  .content-dialog-footer.splitMode {
    display: flex;
    justify-content: space-between;
    border-block-start: 1px solid var(--wui-card-stroke-default);
    white-space: nowrap;
  }
  .content-dialog-footer.splitMode > .content-dialog-footer-button-group {
    display: inline-grid;
    grid-auto-flow: column;
    grid-auto-columns: 1fr;
    gap: 8px;
  }
  .content-dialog-footer.splitMode > .content-dialog-footer-button-group > :where(.button, button) {
    min-width: 100px;
  }

  .content-dialog-footer.noTopPadding {
    padding-top: 0;
  }

  .content-dialog-loading-screen {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 24px;
    height: calc(100% - var(--inner-padding) * 2);
  }

  .content-dialog :deep(.content-dialog-close-button) {
    position: absolute;
    top: 0;
    right: 0;
    z-index: 100;
    height: 32px;
    width: 48px;
    color: var(--wui-text-primary);
    border-radius: 0;
    transition: none;
  }
  .content-dialog :deep(.content-dialog-close-button:hover) {
    background-color: #e81123;
    color: white;
  }
  .content-dialog :deep(.content-dialog-close-button:active) {
    background-color: #f1707a;
    color: black;
  }

  .content-dialog .content-dialog-titlebar {
    background-color: var(--wui-solid-background-base);
    height: 48px;
    margin-bottom: -16px;
    display: flex;
    flex-direction: row;
    gap: 0;
    flex-wrap: nowrap;
    align-items: center;
    justify-content: flex-start;
    padding: 0 var(--inner-padding);
    position: relative;
  }
  .content-dialog .content-dialog-titlebar::before {
    content: '';
    position: absolute;
    background-color: var(--wui-layer-default);
    inset: 0;
    height: 32px;
    z-index: 0;
  }
  .content-dialog .content-dialog-titlebar > * {
    z-index: 1;
  }

  .content-dialog .content-dialog-titlebar img.logo {
    block-size: 16px;
    padding: 0 var(--inner-padding) 0 0;
    object-fit: cover;
    -webkit-user-drag: none;
  }
</style>
