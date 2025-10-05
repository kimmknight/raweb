<script setup lang="ts">
  import { IconButton, TextBlock } from '$components';
  import { ref, useAttrs } from 'vue';

  const { code = '' } = defineProps<{
    code?: string;
  }>();
  const restProps = useAttrs();

  let codeBlockElement = ref<HTMLDivElement | null>(null);

  /**
   * Copies the code to the clipboard.
   */
  function copyToClipboard() {
    navigator.clipboard.writeText(code);

    const successNoteElement = codeBlockElement.value?.querySelector(
      '.copy-success-notice'
    ) as HTMLElement | null;
    if (successNoteElement) {
      successNoteElement.classList.add('visible');
      setTimeout(() => {
        if (successNoteElement) {
          successNoteElement.classList.remove('visible');
        }
      }, 2000);
    }
  }
</script>

<template>
  <div class="code-block" ref="codeBlockElement">
    <pre v-bind="restProps"><code>{{ code }}</code></pre>
    <div class="copy-button">
      <IconButton @click="copyToClipboard" :title="$t('copyToClipboard')">
        <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
          <path
            d="M5.503 4.627 5.5 6.75v10.504a3.25 3.25 0 0 0 3.25 3.25h8.616a2.251 2.251 0 0 1-2.122 1.5H8.75A4.75 4.75 0 0 1 4 17.254V6.75c0-.98.627-1.815 1.503-2.123ZM17.75 2A2.25 2.25 0 0 1 20 4.25v13a2.25 2.25 0 0 1-2.25 2.25h-9a2.25 2.25 0 0 1-2.25-2.25v-13A2.25 2.25 0 0 1 8.75 2h9Zm0 1.5h-9a.75.75 0 0 0-.75.75v13c0 .414.336.75.75.75h9a.75.75 0 0 0 .75-.75v-13a.75.75 0 0 0-.75-.75Z"
            fill="currentColor"
          />
        </svg>
      </IconButton>
      <TextBlock class="copy-success-notice" variant="caption" ref="successNoteElement">
        {{ $t('copiedToClipboard') }}
      </TextBlock>
    </div>
  </div>
</template>

<style scoped>
  pre {
    margin: 0;
    padding: 16px;
    overflow: auto;
    flex-grow: 1;
    padding-right: 44px;
    overflow: scroll;
  }
  code {
    font-family: var(---wui-font-family-monospace);
    /* font-size: 13px;
    line-height: 15px; */
  }

  .code-block {
    width: 100%;
    position: relative;
    margin: 16px 0;
    border-radius: var(--wui-overlay-corner-radius);
    background-color: var(--wui-solid-background-secondary);
    box-shadow: inset 0 0 0 1px var(--wui-surface-stroke-default);
  }

  .copy-button {
    position: absolute;
    right: 10px;
    top: 10px;
    background-color: var(--wui-solid-background-quarternary);
    border-radius: var(--wui-control-corner-radius);
    box-shadow: inset 0 0 0 1px var(--wui-surface-stroke-default);
  }

  .code-block :deep(.copy-success-notice) {
    position: absolute;
    top: 4px;
    right: 40px;
    white-space: nowrap;
    opacity: 0;
    transition: opacity var(--wui-control-normal-duration) var(--wui-control-fast-out-slow-in-easing);
    user-select: none;
    cursor: default;

    background-color: var(--wui-solid-background-quarternary);
    box-shadow: inset 0 0 0 1px var(--wui-surface-stroke-flyout);
    box-shadow: inset 0 0 0 1px var(--wui-control-stroke-default), var(--wui-flyout-shadow);
    border-radius: var(--wui-control-corner-radius);
    padding: 4px 8px;
  }
  .code-block :deep(.copy-success-notice.visible) {
    opacity: 1;
  }
</style>
