<script setup lang="ts">
  import IconButton from '$components/IconButton/IconButton.vue';
  import PropertiesDialog from '$components/ItemCard/PropertiesDialog.vue';
  import TerminalServerPickerDialog from '$components/ItemCard/TerminalServerPickerDialog.vue';
  import { MenuFlyout, MenuFlyoutItem } from '$components/MenuFlyout';
  import { favoritesEnabled, raw, simpleModeEnabled, useFavoriteResourceTerminalServers } from '$utils';
  import { computed, useTemplateRef } from 'vue';

  const terminalServerAliases = window.__terminalServerAliases;

  type Resource = NonNullable<
    Awaited<ReturnType<typeof import('$utils').getAppsAndDevices>>
  >['resources'][number];

  const { resource, class: className } = defineProps<{
    resource: Resource;
    class?: string;
    placement: 'top' | 'bottom';
  }>();

  // TODO: requestClose: remove this logic once all browsers have supported this for some time
  const canUseDialogs = HTMLDialogElement.prototype.requestClose !== undefined;

  // TODO [Anchors]: Remove this when all major browsers support CSS Anchor Positioning
  const supportsAnchorPositions = CSS.supports('position-area', 'center center');

  const tsPickerDialog = useTemplateRef<typeof TerminalServerPickerDialog>('tsPickerDialog');
  const openTsPickerDialog = computed(() => raw(tsPickerDialog.value)?.openDialog);

  const propertiesDialog = useTemplateRef<typeof PropertiesDialog>('propertiesDialog');
  const openPropertiesDialog = computed(() => raw(propertiesDialog.value)?.openDialog);

  const { favoriteTerminalServers, setFavorite } = useFavoriteResourceTerminalServers(resource);

  defineExpose({ connect: openTsPickerDialog });
</script>

<template>
  <MenuFlyout :placement="placement" v-if="supportsAnchorPositions">
    <template v-slot="{ popoverId }">
      <IconButton :popovertarget="popoverId" @click.stop @keydown.stop tabIndex="-1" :class="className">
        <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
          <path
            d="M8 12a2 2 0 1 1-4 0 2 2 0 0 1 4 0ZM14 12a2 2 0 1 1-4 0 2 2 0 0 1 4 0ZM18 14a2 2 0 1 0 0-4 2 2 0 0 0 0 4Z"
            fill="currentColor"
          />
        </svg>
      </IconButton>
    </template>
    <template v-slot:menu>
      <MenuFlyoutItem @click="openTsPickerDialog">
        Connect
        <template v-slot:icon>
          <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path
              d="M7.608 4.615a.75.75 0 0 0-1.108.659v13.452a.75.75 0 0 0 1.108.659l12.362-6.726a.75.75 0 0 0 0-1.318L7.608 4.615ZM5 5.274c0-1.707 1.826-2.792 3.325-1.977l12.362 6.726c1.566.853 1.566 3.101 0 3.953L8.325 20.702C6.826 21.518 5 20.432 5 18.726V5.274Z"
              fill="currentColor"
            />
          </svg>
        </template>
      </MenuFlyoutItem>
      <template v-for="host in resource.hosts" :key="host.id" v-if="favoritesEnabled && !simpleModeEnabled">
        <MenuFlyoutItem v-if="favoriteTerminalServers.includes(host.id)" @click="setFavorite(host.id, false)">
          <span class="dual-line-menu-item">
            <span>Remove from favorites</span>
            <span v-if="resource.hosts.length > 1">{{ terminalServerAliases[host.name] ?? host.name }}</span>
          </span>
          <template v-slot:icon>
            <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path
                d="M3.28 2.22a.75.75 0 1 0-1.06 1.06l4.804 4.805-3.867.561c-1.107.161-1.55 1.522-.748 2.303l3.815 3.719-.9 5.251c-.19 1.103.968 1.944 1.959 1.423l4.715-2.479 4.716 2.48c.99.52 2.148-.32 1.96-1.424l-.04-.223 2.085 2.084a.75.75 0 0 0 1.061-1.06L3.28 2.22Zm13.518 15.639.345 2.014-4.516-2.374a1.35 1.35 0 0 0-1.257 0l-4.516 2.374.862-5.03a1.35 1.35 0 0 0-.388-1.194l-3.654-3.562 4.673-.679 8.45 8.45ZM20.323 10.087l-3.572 3.482 1.06 1.06 3.777-3.68c.8-.781.359-2.142-.748-2.303l-5.273-.766-2.358-4.777c-.495-1.004-1.926-1.004-2.421 0L9.3 6.118l1.12 1.12 1.578-3.2 2.259 4.577a1.35 1.35 0 0 0 1.016.738l5.05.734Z"
                fill="currentColor"
              />
            </svg>
          </template>
        </MenuFlyoutItem>
        <MenuFlyoutItem v-else @click="setFavorite(host.id, true)">
          <span class="dual-line-menu-item">
            <span>Add to favorites</span>
            <span v-if="resource.hosts.length > 1">{{ terminalServerAliases[host.name] ?? host.name }}</span>
          </span>
          <template v-slot:icon>
            <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path
                d="M10.788 3.103c.495-1.004 1.926-1.004 2.421 0l2.358 4.777 5.273.766c1.107.161 1.549 1.522.748 2.303l-3.816 3.72.901 5.25c.19 1.103-.968 1.944-1.959 1.424l-4.716-2.48-4.715 2.48c-.99.52-2.148-.32-1.96-1.424l.901-5.25-3.815-3.72c-.801-.78-.359-2.142.748-2.303L8.43 7.88l2.358-4.777Zm1.21.936L9.74 8.615a1.35 1.35 0 0 1-1.016.738l-5.05.734 3.654 3.562c.318.31.463.757.388 1.195l-.862 5.03 4.516-2.375a1.35 1.35 0 0 1 1.257 0l4.516 2.374-.862-5.029a1.35 1.35 0 0 1 .388-1.195l3.654-3.562-5.05-.734a1.35 1.35 0 0 1-1.016-.738l-2.259-4.576Z"
                fill="currentColor"
              />
            </svg>
          </template>
        </MenuFlyoutItem>
      </template>
      <MenuFlyoutItem @click="openPropertiesDialog" v-if="canUseDialogs">
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
  .dual-line-menu-item {
    display: flex;
    flex-direction: column;
  }
  .dual-line-menu-item > span + span {
    opacity: 0.5;
    font-size: 10px;
    line-height: 10px;
    padding-bottom: 2px;
  }
</style>
