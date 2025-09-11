<script setup lang="ts">
  import { Button, TextBlock } from '$components';
  import { raw } from '$utils';
  import { computed, useTemplateRef } from 'vue';
  import GenericResourceCardMenuButton from './GenericResourceCardMenuButton.vue';

  type Resource = NonNullable<
    Awaited<ReturnType<typeof import('$utils').getAppsAndDevices>>
  >['resources'][number];

  const { resource, width } = defineProps<{
    resource: Resource;
    width?: string;
  }>();

  const terminalServerAliases = window.__terminalServerAliases;

  const wallpaper = computed(() => {
    const icons = resource.icons.filter((icon) => icon.type === 'png');
    if (icons.length > 0) {
      const url = new URL(icons[0].url.href);
      url.searchParams.set('format', 'png'); // ensure we get the highest quality png icon
      url.searchParams.delete('frame'); // do not surround the wallpaper with a frame
      url.searchParams.set('fallback', 'lib/assets/wallpaper.png'); // fallback to a default wallpaper if the icon is not available
      const light = `url(${url.href})`;
      url.searchParams.set('theme', 'dark');
      url.searchParams.set('fallback', 'lib/assets/wallpaper-dark.png'); // fallback to a default wallpaper if the icon is not available
      const dark = `url(${url.href})`;
      return { light, dark };
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
</script>

<template>
  <article
    :style="[
      `--wallpaper-light: ${wallpaper?.light}; --wallpaper-dark: ${wallpaper?.dark};`,
      width ? `--width: ${width};` : '',
    ]"
  >
    <div class="content">
      <div class="labels">
        <TextBlock tag="h1" variant="subtitle" class="desktop-title">{{ resource.title }}</TextBlock>
        <TextBlock variant="body">
          {{ terminalServerAliases[hostname] ?? hostname }}
        </TextBlock>
      </div>
      <div class="buttons">
        <Button variant="accent" @click="connect">{{ $t('resource.menu.connect') }}</Button>
        <GenericResourceCardMenuButton
          :resource="resource"
          placement="top"
          hideDefaultConnect
          ref="menu"
          @click.stop
          class="actual-menu-button"
        />
      </div>
    </div>
  </article>
</template>

<style scoped>
  article {
    --background-image: var(--wallpaper-light);
    /* --background-image: url('https://preview.redd.it/zfajlqwcbuk81.png?width=3840&format=png&auto=webp&s=143e520ab7b83ee5e5de8ce0ee3ab104a160f499'); */
    --width: calc((240px * 4 + 16px) / 3);
    width: var(--width);
    max-width: calc(100vw - 72px - (2 * 32px));
    min-height: 330px;
    aspect-ratio: 6 / 7;
    border-radius: var(--wui-overlay-corner-radius);
    box-sizing: border-box;
    background-color: var(--wui-card-background-default);
    background-image: var(--background-image);
    background-size: cover;
    background-position: center;
    background-repeat: no-repeat;
    position: relative;
    overflow: hidden;
    flex-grow: 0;
    flex-shrink: 0;
    user-select: none;
  }
  article::before {
    content: '';
    position: absolute;
    inset: 0;
    box-shadow: inset 0 0 0 1px var(--wui-card-stroke-default);
    border-radius: var(--wui-overlay-corner-radius);
  }
  @media (prefers-color-scheme: dark) {
    article {
      --background-image: var(--wallpaper-dark);
      /* --background-image: url('https://preview.redd.it/1b418okbbuk81.png?width=3840&format=png&auto=webp&s=33775695e0621c2bdab64b1ad2db4573f48da56e'); */
    }
  }

  .content {
    --background-color: rgba(255, 255, 255, 0.74);
    position: absolute;
    inset: 60% 0 0 0;
    padding: 16px 16px 16px 16px;
    background-color: var(--background-color);
    backdrop-filter: blur(8px);
    box-sizing: border-box;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    border-radius: 0 0 var(--wui-overlay-corner-radius) var(--wui-overlay-corner-radius);
  }
  .content::before {
    content: '';
    position: absolute;
    inset: -1px 0 0 0;
    box-shadow: inset 0 0 0 1px var(--wui-card-stroke-default);
    border-radius: 0 0 var(--wui-overlay-corner-radius) var(--wui-overlay-corner-radius);
  }
  @media (prefers-color-scheme: dark) {
    .content {
      --background-color: rgba(32, 32, 32, 0.86);
    }
  }

  .labels {
    display: flex;
    flex-direction: column;
    flex-grow: 1;
    flex-shrink: 0;
    gap: 8px;
    width: 100%;
    padding-top: 4px;
  }

  .labels > .desktop-title {
    line-height: 20px;
  }

  .labels > *:last-child {
    opacity: 0.84;
  }

  .buttons {
    display: flex;
    flex-direction: row;
    gap: 8px;
    align-items: center;
    justify-content: space-between;
    flex-grow: 0;
    flex-shrink: 0;
  }
</style>
