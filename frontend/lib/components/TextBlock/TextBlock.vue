<script setup lang="ts">
  const {
    variant = 'body',
    tag,
    block = false,
    ...restProps
  } = defineProps<{
    /** Determines which preset text style should be used. */
    variant?: 'caption' | 'body' | 'bodyStrong' | 'bodyLarge' | 'subtitle' | 'title' | 'titleLarge' | 'display';
    /** Overrides the default HTML tag of the block's element with your own tag. */
    tag?: string;
    block?: boolean;
  }>();

  const variantMap = new Map([
    ['caption', { tag: 'span', name: 'caption' }],
    ['body', { tag: 'span', name: 'body' }],
    ['bodyStrong', { tag: 'h5', name: 'body-strong' }],
    ['bodyLarge', { tag: 'h5', name: 'body-large' }],
    ['subtitle', { tag: 'h4', name: 'subtitle' }],
    ['title', { tag: 'h3', name: 'title' }],
    ['titleLarge', { tag: 'h2', name: 'title-large' }],
    ['display', { tag: 'h1', name: 'display' }],
  ]);

  const tagName = tag ?? variantMap.get(variant)?.tag ?? 'span';
  const className = variantMap.get(variant)?.name ?? '';
</script>

<template>
  <component
    :is="tagName"
    :class="['text-block', `type-${className}`, block ? 'block' : undefined]"
    :="restProps"
  >
    <slot></slot>
  </component>
</template>

<style scoped>
  .text-block {
    color: currentColor;
    display: inline-block;
    margin: 0;
    padding: 0;
  }
  .text-block.block {
    display: block;
  }

  .type-subtitle,
  .type-title,
  .type-title-large {
    font-family: var(--wui-font-family-display);
    font-weight: 600;
  }
  .type-body,
  .type-body-strong,
  .type-body-large {
    font-family: var(--wui-font-family-text);
  }
  .type-caption {
    line-height: 16px;
    font-size: var(--wui-font-size-caption);
    font-family: var(--wui-font-family-small);
    font-weight: 400;
  }
  .type-body,
  .type-body-strong,
  .type-body-large {
    line-height: 20px;
    font-weight: 400;
    font-size: var(--wui-font-size-body);
  }
  .type-body-strong {
    font-weight: 600;
  }
  .type-body-large {
    font-size: var(--wui-font-size-body-large);
    line-height: 24px;
  }
  .type-subtitle {
    font-size: var(--wui-font-size-subtitle);
    line-height: 28px;
  }
  .type-title {
    font-size: var(--wui-font-size-title);
    line-height: 36px;
  }
  .type-title-large {
    font-size: var(--wui-font-size-title-large);
    line-height: 52px;
  }
  .type-display {
    font-size: var(--wui-font-size-display);
    line-height: 92px;
  }
</style>
