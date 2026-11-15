<script setup lang="ts">
  import { MenuFlyoutDivider, MenuFlyoutItem } from '$components/MenuFlyout';
  import type { SortName } from './resourceGridContext';

  const {
    showTerminalServer = false,
    options = ['Name', 'Terminal server', 'Date modified', 'Type', 'Size'], // the options for resources
  } = defineProps<{
    /**
     * whether to offer sorting by terminal server
     * @deprecated use `options` instead
     */
    showTerminalServer?: boolean;
    /** The sort options to offer. */
    options?: SortName[];
  }>();

  const sortName = defineModel<SortName>('sortName', { required: true });
  const sortOrder = defineModel<'asc' | 'desc'>('sortOrder', { required: true });
</script>

<template>
  <MenuFlyoutItem
    v-if="options.includes('Name')"
    @click="() => (sortName = 'Name')"
    :selected="sortName === 'Name'"
  >
    {{ $t('actions.sort.name') }}
  </MenuFlyoutItem>
  <MenuFlyoutItem
    v-if="showTerminalServer && options.includes('Terminal server')"
    @click="() => (sortName = 'Terminal server')"
    :selected="sortName === 'Terminal server'"
  >
    {{ $t('actions.sort.ts') }}
  </MenuFlyoutItem>
  <MenuFlyoutItem
    v-if="options.includes('Date modified')"
    @click="() => (sortName = 'Date modified')"
    :selected="sortName === 'Date modified'"
  >
    {{ $t('actions.sort.date') }}
  </MenuFlyoutItem>
  <MenuFlyoutItem
    v-if="options.includes('Type')"
    @click="() => (sortName = 'Type')"
    :selected="sortName === 'Type'"
  >
    {{ $t('actions.sort.type') }}
  </MenuFlyoutItem>
  <MenuFlyoutItem
    v-if="options.includes('Size')"
    @click="() => (sortName = 'Size')"
    :selected="sortName === 'Size'"
  >
    {{ $t('actions.sort.size') }}
  </MenuFlyoutItem>
  <MenuFlyoutItem
    v-if="options.includes('State')"
    @click="() => (sortName = 'State')"
    :selected="sortName === 'State'"
  >
    {{ $t('actions.sort.state') }}
  </MenuFlyoutItem>
  <MenuFlyoutDivider />
  <MenuFlyoutItem @click="() => (sortOrder = 'asc')" :selected="sortOrder === 'asc'">
    {{ $t('actions.sort.asc') }}
  </MenuFlyoutItem>
  <MenuFlyoutItem @click="() => (sortOrder = 'desc')" :selected="sortOrder === 'desc'">
    {{ $t('actions.sort.desc') }}
  </MenuFlyoutItem>
</template>
