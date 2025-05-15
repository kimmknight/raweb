<script setup lang="ts">
  import Button from '$components/Button/Button.vue';
  import IconButton from '$components/IconButton/IconButton.vue';
  import PropertiesDialog from '$components/ItemCard/PropertiesDialog.vue';
  import TerminalServerPickerDialog from '$components/ItemCard/TerminalServerPickerDialog.vue';
  import { MenuFlyout, MenuFlyoutItem } from '$components/MenuFlyout/index.mjs';
  import TextBlock from '$components/TextBlock/TextBlock.vue';
  import { favoritesEnabled, raw, simpleModeEnabled, useFavoriteResourceTerminalServers } from '$utils';
  import { computed, useTemplateRef } from 'vue';

  type Resource = NonNullable<
    Awaited<ReturnType<typeof import('$utils').getAppsAndDevices>>
  >['resources'][number];

  const { resource } = defineProps<{
    resource: Resource;
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

  const tsPickerDialog = useTemplateRef<typeof TerminalServerPickerDialog>('tsPickerDialog');
  const openTsPickerDialog = computed(() => raw(tsPickerDialog.value)?.openDialog);

  const propertiesDialog = useTemplateRef<typeof PropertiesDialog>('propertiesDialog');
  const openPropertiesDialog = computed(() => raw(propertiesDialog.value)?.openDialog);

  const { favoriteTerminalServers, setFavorite } = useFavoriteResourceTerminalServers(resource);
</script>

<template>
  <article :style="`--wallpaper-light: ${wallpaper?.light}; --wallpaper-dark: ${wallpaper?.dark}`">
    <div class="content">
      <div class="labels">
        <TextBlock tag="h1" variant="subtitle" class="desktop-title">{{ resource.title }}</TextBlock>
        <TextBlock variant="body">
          {{ terminalServerAliases[hostname] ?? hostname }}
        </TextBlock>
      </div>
      <div class="buttons">
        <Button variant="accent" @click="openTsPickerDialog">Connect</Button>
        <MenuFlyout placement="top">
          <template v-slot="{ popoverId }">
            <IconButton :popovertarget="popoverId" @click.stop>
              <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                <path
                  d="M8 12a2 2 0 1 1-4 0 2 2 0 0 1 4 0ZM14 12a2 2 0 1 1-4 0 2 2 0 0 1 4 0ZM18 14a2 2 0 1 0 0-4 2 2 0 0 0 0 4Z"
                  fill="currentColor"
                />
              </svg>
            </IconButton>
          </template>
          <template v-slot:menu>
            <template
              v-for="host in resource.hosts"
              :key="host.id"
              v-if="favoritesEnabled && !simpleModeEnabled"
            >
              <MenuFlyoutItem
                v-if="favoriteTerminalServers.includes(host.id)"
                @click="setFavorite(host.id, false)"
              >
                Remove from favorites
                <template v-slot:icon>
                  <svg
                    width="24"
                    height="24"
                    fill="none"
                    viewBox="0 0 24 24"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      d="M3.28 2.22a.75.75 0 1 0-1.06 1.06l4.804 4.805-3.867.561c-1.107.161-1.55 1.522-.748 2.303l3.815 3.719-.9 5.251c-.19 1.103.968 1.944 1.959 1.423l4.715-2.479 4.716 2.48c.99.52 2.148-.32 1.96-1.424l-.04-.223 2.085 2.084a.75.75 0 0 0 1.061-1.06L3.28 2.22Zm13.518 15.639.345 2.014-4.516-2.374a1.35 1.35 0 0 0-1.257 0l-4.516 2.374.862-5.03a1.35 1.35 0 0 0-.388-1.194l-3.654-3.562 4.673-.679 8.45 8.45ZM20.323 10.087l-3.572 3.482 1.06 1.06 3.777-3.68c.8-.781.359-2.142-.748-2.303l-5.273-.766-2.358-4.777c-.495-1.004-1.926-1.004-2.421 0L9.3 6.118l1.12 1.12 1.578-3.2 2.259 4.577a1.35 1.35 0 0 0 1.016.738l5.05.734Z"
                      fill="currentColor"
                    />
                  </svg>
                </template>
              </MenuFlyoutItem>
              <MenuFlyoutItem v-else @click="setFavorite(host.id, true)">
                Add to favorites
                <template v-slot:icon>
                  <svg
                    width="24"
                    height="24"
                    fill="none"
                    viewBox="0 0 24 24"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      d="M10.788 3.103c.495-1.004 1.926-1.004 2.421 0l2.358 4.777 5.273.766c1.107.161 1.549 1.522.748 2.303l-3.816 3.72.901 5.25c.19 1.103-.968 1.944-1.959 1.424l-4.716-2.48-4.715 2.48c-.99.52-2.148-.32-1.96-1.424l.901-5.25-3.815-3.72c-.801-.78-.359-2.142.748-2.303L8.43 7.88l2.358-4.777Zm1.21.936L9.74 8.615a1.35 1.35 0 0 1-1.016.738l-5.05.734 3.654 3.562c.318.31.463.757.388 1.195l-.862 5.03 4.516-2.375a1.35 1.35 0 0 1 1.257 0l4.516 2.374-.862-5.029a1.35 1.35 0 0 1 .388-1.195l3.654-3.562-5.05-.734a1.35 1.35 0 0 1-1.016-.738l-2.259-4.576Z"
                      fill="currentColor"
                    />
                  </svg>
                </template>
              </MenuFlyoutItem>
            </template>
            <MenuFlyoutItem @click="openPropertiesDialog">
              Properties
              <template v-slot:icon>
                <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path
                    d="M10.5 7.751a5.75 5.75 0 0 1 8.38-5.114.75.75 0 0 1 .186 1.197L16.301 6.6l1.06 1.06 2.779-2.778a.75.75 0 0 1 1.193.179 5.75 5.75 0 0 1-6.422 8.284l-7.365 7.618a3.05 3.05 0 0 1-4.387-4.24l7.475-7.734a5.766 5.766 0 0 1-.134-1.238Zm5.75-4.25a4.25 4.25 0 0 0-4.067 5.489.75.75 0 0 1-.178.74l-7.768 8.035a1.55 1.55 0 1 0 2.23 2.156l7.676-7.941a.75.75 0 0 1 .775-.191 4.25 4.25 0 0 0 5.466-5.03l-2.492 2.492a.75.75 0 0 1-1.061 0L14.71 7.13a.75.75 0 0 1 0-1.06l2.466-2.467a4.268 4.268 0 0 0-.926-.102Z"
                    fill="currentColor"
                  />
                </svg>
              </template>
            </MenuFlyoutItem>
          </template>
        </MenuFlyout>
      </div>
    </div>
  </article>

  <TerminalServerPickerDialog
    :resource="resource"
    ref="tsPickerDialog"
    @close="
      ({ downloadRdpFile }) => {
        downloadRdpFile();
      }
    "
  />

  <PropertiesDialog :resource="resource" ref="propertiesDialog" />
</template>

<style scoped>
  article {
    --background-image: var(--wallpaper-light);
    /* --background-image: url('https://preview.redd.it/zfajlqwcbuk81.png?width=3840&format=png&auto=webp&s=143e520ab7b83ee5e5de8ce0ee3ab104a160f499'); */
    --width: calc((240px * 4 + 16px) / 3);
    width: var(--width);
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
