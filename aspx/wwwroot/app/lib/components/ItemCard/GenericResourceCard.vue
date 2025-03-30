<script setup lang="ts">
  import TextBlock from '$components/TextBlock/TextBlock.vue';
  import { iconBackgroundsEnabled, raw } from '$utils';
  import { computed, useTemplateRef } from 'vue';
  import GenericResourceCardMenuButton from './GenericResourceCardMenuButton.vue';

  type Resource = NonNullable<
    Awaited<ReturnType<typeof import('$utils').getAppsAndDevices>>
  >['resources'][number];

  const { resource, mode = 'card' } = defineProps<{
    resource: Resource;
    mode?: 'card' | 'list' | 'grid' | 'tile';
  }>();

  const icon = computed(() => {
    const icons = resource.icons.filter((icon) => icon.type === 'png');
    const largestIcon = icons.reduce((prev, current) => {
      const currentWidth = current.dimensions?.split('x')[0] ?? 0;
      const prevWidth = prev.dimensions?.split('x')[0] ?? 0;
      return prevWidth > currentWidth ? prev : current;
    }, icons[0]);
    if (largestIcon?.url.href.endsWith('format=png32')) {
      return largestIcon.url.href.slice(0, -2);
    }
  });

  const hostname = computed(() => {
    if (resource.hosts.length > 1) {
      return 'Multiple devices';
    }
    return resource.hosts[0]?.name || 'Unknown device';
  });

  const _menu = useTemplateRef<typeof GenericResourceCardMenuButton>('menu');
  const connect = computed(() => raw(_menu.value)?.connect);

  function handleRightClick(evt) {
    evt.preventDefault();
    const actualMenuButton = evt.currentTarget.querySelector('.actual-menu-button');
    if (actualMenuButton) {
      const pointerType = evt.pointerType || 'mouse';
      actualMenuButton.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, pointerType }));
    }
  }
</script>

<template>
  <article @click="connect" tabIndex="0" :class="`mode-${mode}`" @contextmenu="handleRightClick">
    <div class="icon-wrapper" :class="`mode-${mode}`">
      <div class="banner-background-wrapper">
        <div class="banner-background" :style="`background-image: url('${icon}')`"></div>
      </div>
      <img :src="icon" alt="" :class="{ withBackground: mode === 'card' && iconBackgroundsEnabled }" />
    </div>
    <div class="bottom-area" :class="`mode-${mode}`">
      <div class="labels">
        <TextBlock tag="h1" variant="bodyStrong" class="app-name">{{ resource.title }}</TextBlock>
        <TextBlock variant="caption">
          {{ hostname }}
        </TextBlock>
      </div>
    </div>
    <div :class="`menu-button mode-${mode}`">
      <GenericResourceCardMenuButton
        :resource="resource"
        placement="bottom"
        ref="menu"
        @click.stop
        class="actual-menu-button"
      />
    </div>
  </article>
</template>

<style scoped>
  article {
    appearance: none;
    border: none;
    /* min-width: 240px; */
    padding: 0;
    text-align: left;
    position: relative;

    flex-grow: 0;
    flex-shrink: 0;

    border-radius: var(--wui-control-corner-radius);
    box-sizing: border-box;
    cursor: default;
    box-shadow: inset 0 0 0 1px var(--wui-control-stroke-default),
      inset 0 -1px 0 0 var(--wui-control-stroke-secondary-overlay);
    background-color: var(--wui-control-fill-default);
    color: var(--wui-text-primary);
    background-clip: padding-box;
    user-select: none;
    -webkit-user-drag: none;
    transition: var(--wui-control-faster-duration) ease background-color,
      var(--wui-control-faster-duration) ease color;

    position: relative;
    overflow: hidden;
  }
  article:hover:not(:has(:where(.menu-button:hover, .menu-button:active))) {
    background-color: var(--wui-control-fill-secondary);
  }
  article:active:not(:has(:where(.menu-button:hover, .menu-button:active))) {
    --wui-control-stroke-secondary-overlay: transparent;
    background-color: var(--wui-control-fill-tertiary);
    color: var(--wui-text-secondary);
  }
  article.disabled {
    background-color: var(--wui-control-fill-disabled);
    color: var(--wui-text-disabled);
  }

  article.mode-list,
  article.mode-tile {
    display: flex;
    flex-direction: row;
    align-items: center;
    gap: 16px;
    height: 66px;
    padding: 16px;
    box-sizing: border-box;
  }

  :where(article.mode-list, article.mode-tile):hover {
    background-color: var(--wui-control-alt-secondary);
  }
  :where(article.mode-list, article.mode-tile):active {
    --wui-control-stroke-secondary-overlay: transparent;
    background-color: var(--wui-control-alt-tertiary);
    color: var(--wui-text-secondary);
  }
  :where(article.mode-list, article.mode-tile).disabled {
    background-color: var(--wui-control-alt-disabled);
    color: var(--wui-text-disabled);
  }

  .banner-background-wrapper {
    position: absolute;
    inset: 0;
    z-index: -1;
  }
  :where(article.mode-card, article.mode-grid) .banner-background-wrapper {
    transform: scale(2.5);
    filter: blur(20px) saturate(1.4);
  }
  :where(article.mode-list, article.mode-tile) .banner-background-wrapper {
    background: var(--wui-card-background-default);
  }
  @media (prefers-color-scheme: dark) {
    :where(article.mode-list, article.mode-tile) .banner-background-wrapper {
      background: none;
    }
  }

  .banner-background {
    background-size: cover;
    position: absolute;
    inset: 0;
    opacity: 0.4;
    transition: var(--wui-control-faster-duration) ease opacity;
  }
  :where(article.mode-list, article.mode-tile) .banner-background {
    display: none;
  }
  button:hover .banner-background {
    opacity: 0.3;
  }
  button:active .banner-background {
    opacity: 0.24;
  }
  @media (prefers-color-scheme: dark) {
    .banner-background {
      opacity: 0.1;
    }
    button:hover .banner-background {
      opacity: 0.17;
    }
    button:active .banner-background {
      opacity: 0.14;
    }
  }

  .bottom-area.mode-card,
  .bottom-area.mode-grid {
    padding: 10px 16px 12px;
    box-sizing: border-box;
    display: flex;
    flex-direction: row;
    align-items: center;
  }
  .bottom-area.mode-grid {
    text-align: center;
  }
  .bottom-area.mode-list,
  .bottom-area.mode-tile {
    flex-grow: 1;
  }

  :where(.bottom-area.mode-card, .bottom-area.mode-grid) .labels {
    display: flex;
    flex-direction: column;
    justify-content: center;
    gap: 2px;
    width: 100%;
  }
  .bottom-area.mode-grid .labels {
    align-items: center;
  }

  .app-name {
    line-height: 16px !important;
    overflow: hidden;
    text-overflow: ellipsis;
    -webkit-line-clamp: 3;
    line-clamp: 3;
    display: -webkit-box !important;
    -webkit-box-orient: vertical;
    font-weight: 400 !important;
  }

  .labels > *:last-child {
    opacity: 0.5;
    text-overflow: ellipsis;
    white-space: nowrap;
    overflow: hidden;
    font-weight: 300 !important;
  }

  .icon-wrapper.mode-card,
  .icon-wrapper.mode-grid {
    width: 100%;
    height: var(--height);
    display: flex;
    justify-content: center;
    align-items: center;
  }
  .icon-wrapper.mode-card {
    --height: 110px;
    position: relative;
    overflow: hidden;
    border-bottom: 1px solid var(--wui-control-stroke-default);
  }
  .icon-wrapper.mode-grid {
    --height: 60px;
    align-items: flex-end;
    padding-bottom: 6px;
  }

  img {
    object-fit: cover;
    filter: drop-shadow(0 1px 0 var(--wui-surface-stroke-default));
    user-select: none;
    -webkit-user-drag: none;
    filter: drop-shadow(2px 2px 10px rgba(0, 0, 0, 0.08));
  }
  @media (prefers-color-scheme: dark) {
    img {
      filter: drop-shadow(2px 2px 10px rgba(0, 0, 0, 0.16));
    }
  }
  img.withBackground {
    background-color: var(--wui-solid-background-tertiary);
    box-shadow: inset 0 -1px 0 0 var(--wui-surface-stroke-default);
    padding: 10px;
    border-radius: var(--wui-control-corner-radius);
    filter: none;
  }

  :where(.icon-wrapper.mode-card, .icon-wrapper.mode-grid) img {
    width: 42px;
    height: 42px;
  }
  :where(.icon-wrapper.mode-card, .icon-wrapper.mode-grid) img.withBackground {
    width: 32px;
    height: 32px;
  }

  :where(.icon-wrapper.mode-list, .icon-wrapper.mode-tile) img {
    inline-size: 24px;
    block-size: auto;
    flex-shrink: 0;
  }
  :where(.icon-wrapper.mode-list, .icon-wrapper.mode-tile) img.withBackground {
    inline-size: 24px;
  }

  .menu-button.mode-card,
  .menu-button.mode-grid {
    position: absolute !important;
    top: 0;
    right: 0;
    opacity: 0;
  }
  :is(article:hover, article:focus, article:focus-within) .menu-button {
    opacity: 1;
  }
</style>
