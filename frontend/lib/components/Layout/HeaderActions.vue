<script setup lang="ts">
  import Button from '$components/Button/Button.vue';
  import { MenuFlyout, MenuFlyoutDivider, MenuFlyoutItem } from '$components/MenuFlyout';
  import TextBox from '$components/TextBox/TextBox.vue';
  import {
    checkmark,
    chevronDown,
    content,
    grid,
    rectangle,
    search,
    server,
    sort as sortIcon,
    tiles,
    view,
  } from '$icons';
  import { getAppsAndDevices } from '$utils';

  // TODO [Anchors]: Remove this when all major browsers support CSS Anchor Positioning
  const supportsAnchorPositions = CSS.supports('position-area', 'center center');

  const terminalServerAliases = window.__terminalServerAliases;

  const {
    searchPlaceholder = 'Search',
    data,
    resourceTypes = ['RemoteApp', 'Desktop'],
  } = defineProps<{
    searchPlaceholder?: string;
    data: Awaited<ReturnType<typeof getAppsAndDevices>>;
    resourceTypes?: ('RemoteApp' | 'Desktop')[];
  }>();

  const mode = defineModel<'card' | 'list' | 'grid' | 'tile'>('mode', { required: true });
  const sortName = defineModel<'Name' | 'Terminal server' | 'Date modified'>('sortName', { required: true });
  const sortOrder = defineModel<'asc' | 'desc'>('sortOrder', { required: true });
  const terminalServersFilter = defineModel<string[]>('terminalServersFilter', { required: false });
  const query = defineModel<string>('query', { required: true });

  const resourcesOfType = terminalServersFilter
    ? data.resources.filter((resource) => resourceTypes.includes(resource.type))
    : [];
  const allTerminalServers = Array.from(
    new Set(resourcesOfType.map((resource) => resource.hosts.map((host) => host.id)).flat())
  );
</script>

<template>
  <div class="header-actions">
    <div class="actions" v-if="supportsAnchorPositions">
      <MenuFlyout placement="bottom" anchor="start">
        <template v-slot="{ popoverId }">
          <Button :popovertarget="popoverId" @click.stop>
            <template v-slot:icon><span v-swap="sortIcon"></span></template>
            <span class="label">Sort</span>
            <template v-slot:icon-end><span v-swap="chevronDown"></span></template>
          </Button>
        </template>
        <template v-slot:menu>
          <MenuFlyoutItem @click="() => (sortName = 'Name')" :selected="sortName === 'Name'">
            Name
          </MenuFlyoutItem>
          <MenuFlyoutItem
            @click="() => (sortName = 'Terminal server')"
            :selected="sortName === 'Terminal server'"
          >
            Terminal server
          </MenuFlyoutItem>
          <MenuFlyoutItem @click="() => (sortName = 'Date modified')" :selected="sortName === 'Date modified'">
            Date modified
          </MenuFlyoutItem>
          <MenuFlyoutDivider>hmm</MenuFlyoutDivider>
          <MenuFlyoutItem @click="() => (sortOrder = 'asc')" :selected="sortOrder === 'asc'">
            Ascending
          </MenuFlyoutItem>
          <MenuFlyoutItem @click="() => (sortOrder = 'desc')" :selected="sortOrder === 'desc'">
            Descending
          </MenuFlyoutItem>
        </template>
      </MenuFlyout>
      <MenuFlyout placement="bottom" anchor="start">
        <template v-slot="{ popoverId }">
          <Button :popovertarget="popoverId" @click.stop>
            <template v-slot:icon><span v-swap="view"></span></template>
            <span class="label">View</span>
            <template v-slot:icon-end><span v-swap="chevronDown"></span></template>
          </Button>
        </template>
        <template v-slot:menu>
          <MenuFlyoutItem @click="() => (mode = 'card')" :selected="mode === 'card'">
            <template v-slot:icon><span v-swap="rectangle"></span></template>
            Card
          </MenuFlyoutItem>
          <MenuFlyoutItem @click="() => (mode = 'grid')" :selected="mode === 'grid'">
            <template v-slot:icon><span v-swap="grid"></span></template>
            Grid
          </MenuFlyoutItem>
          <MenuFlyoutItem @click="() => (mode = 'tile')" :selected="mode === 'tile'">
            <template v-slot:icon><span v-swap="tiles"></span></template>
            Tile
          </MenuFlyoutItem>
          <MenuFlyoutItem @click="() => (mode = 'list')" :selected="mode === 'list'">
            <template v-slot:icon><span v-swap="content"></span></template>
            List
          </MenuFlyoutItem>
        </template>
      </MenuFlyout>
      <MenuFlyout placement="bottom" anchor="start" v-if="terminalServersFilter">
        <template v-slot="{ popoverId }">
          <Button :popovertarget="popoverId" @click.stop>
            <template v-slot:icon><span v-swap="server"></span></template>
            <span class="label">Terminal servers</span>
            <template v-slot:icon-end><span v-swap="chevronDown"></span></template>
          </Button>
        </template>
        <template v-slot:menu>
          <MenuFlyoutItem
            v-for="terminalServer in allTerminalServers"
            :indented="!terminalServersFilter.includes(terminalServer)"
            @click="
              () => {
                if (terminalServersFilter?.includes(terminalServer)) {
                  terminalServersFilter = terminalServersFilter.filter((ts) => ts !== terminalServer);
                } else {
                  terminalServersFilter = [...(terminalServersFilter || []), terminalServer];
                }
              }
            "
          >
            <template v-slot:icon v-if="terminalServersFilter.includes(terminalServer)">
              <span v-swap="checkmark"></span>
            </template>
            {{ terminalServerAliases[terminalServer] ?? terminalServer }}
          </MenuFlyoutItem>
          <MenuFlyoutDivider />
          <MenuFlyoutItem
            @click="() => (terminalServersFilter = [])"
            :indented="terminalServersFilter.length > 0"
          >
            <template v-slot:icon v-if="terminalServersFilter.length === 0">
              <span v-swap="checkmark"></span>
            </template>
            All terminal servers
          </MenuFlyoutItem>
        </template>
      </MenuFlyout>
    </div>
    <div :class="['search', !supportsAnchorPositions ? 'left' : '']">
      <TextBox :placeholder="searchPlaceholder" showClearButton v-model:value="query">
        <template v-slot:submit-icon><span v-swap="search"></span></template>
      </TextBox>
    </div>
  </div>
</template>

<style scoped>
  .header-actions {
    margin: 12px 0 8px 0;
  }

  .header-actions,
  .actions {
    display: flex;
    flex-direction: row;
    gap: 8px;
  }

  .search {
    flex-grow: 1;
  }
  .search > * {
    max-width: 360px;
    margin-left: auto;
  }
  .search.left > * {
    margin-left: 0;
  }

  @media (max-width: 799px) {
    .header-actions {
      flex-direction: column-reverse;
    }
    .search > * {
      margin-left: 0;
      max-width: unset;
    }
  }

  @media (max-width: 539px) {
    .label {
      display: none;
    }
  }
</style>
