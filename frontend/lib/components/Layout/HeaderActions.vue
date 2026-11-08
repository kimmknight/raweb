<script setup lang="ts">
  import { AnimatedIcon } from '$components';
  import Button from '$components/Button/Button.vue';
  import { MenuFlyout } from '$components/MenuFlyout';
  import TextBox from '$components/TextBox/TextBox.vue';
  import { search, server, sort as sortIcon, tiles } from '$icons';
  import { getAppsAndDevices } from '$utils';
  import { computed } from 'vue';
  import { getTerminalServerIds } from './getTerminalServerIds';
  import SortMenuItems from './SortMenuItems.vue';
  import TerminalServerMenuItems from './TerminalServerMenuItems.vue';
  import ViewMenuItems from './ViewMenuItems.vue';

  // TODO [Anchors]: Remove this when all major browsers support CSS Anchor Positioning
  const supportsAnchorPositions = CSS.supports('position-area', 'center center');

  const {
    searchPlaceholder = 'Search',
    data: workspaceData,
    resourceTypes = ['RemoteApp', 'Desktop'],
  } = defineProps<{
    searchPlaceholder?: string;
    data?: Awaited<ReturnType<typeof getAppsAndDevices>>;
    resourceTypes?: ('RemoteApp' | 'Desktop')[];
  }>();

  const mode = defineModel<'card' | 'list' | 'grid' | 'tile'>('mode', { required: true });
  const sortName = defineModel<'Name' | 'Terminal server' | 'Date modified'>('sortName', { required: true });
  const sortOrder = defineModel<'asc' | 'desc'>('sortOrder', { required: true });
  const terminalServersFilter = defineModel<string[]>('terminalServersFilter', { required: false });
  const query = defineModel<string>('query', { required: true });

  const allTerminalServers = computed(() =>
    terminalServersFilter.value ? getTerminalServerIds(workspaceData, resourceTypes) : []
  );
</script>

<template>
  <div class="header-actions">
    <div class="actions" v-if="supportsAnchorPositions">
      <MenuFlyout placement="bottom" anchor="start">
        <template v-slot="{ popoverId }">
          <Button :popovertarget="popoverId" @click.stop @contextmenu.prevent>
            <template v-slot:icon><span v-swap="sortIcon"></span></template>
            <span class="label">{{ $t('actions.sort.label') }}</span>
            <template v-slot:icon-end><AnimatedIcon.ChevronDown /></template>
          </Button>
        </template>
        <template v-slot:menu>
          <SortMenuItems
            v-model:sortName="sortName"
            v-model:sortOrder="sortOrder"
            :showTerminalServer="allTerminalServers.length > 0"
          />
        </template>
      </MenuFlyout>
      <MenuFlyout placement="bottom" anchor="start">
        <template v-slot="{ popoverId }">
          <Button :popovertarget="popoverId" @click.stop @contextmenu.prevent>
            <template v-slot:icon><span v-swap="tiles"></span></template>
            <span class="label">{{ $t('actions.view.label') }}</span>
            <template v-slot:icon-end><AnimatedIcon.ChevronDown /></template>
          </Button>
        </template>
        <template v-slot:menu>
          <ViewMenuItems v-model:mode="mode" />
        </template>
      </MenuFlyout>
      <MenuFlyout placement="bottom" anchor="start" v-if="terminalServersFilter">
        <template v-slot="{ popoverId }">
          <Button :popovertarget="popoverId" @click.stop @contextmenu.prevent>
            <template v-slot:icon><span v-swap="server"></span></template>
            <span class="label">{{ $t('actions.ts.label') }}</span>
            <template v-slot:icon-end><AnimatedIcon.ChevronDown /></template>
          </Button>
        </template>
        <template v-slot:menu>
          <TerminalServerMenuItems
            v-model:terminalServersFilter="terminalServersFilter"
            :allTerminalServers="allTerminalServers"
          />
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
