<script setup lang="ts">
  import { MenuFlyoutDivider, MenuFlyoutItem } from '$components/MenuFlyout';

  // whether to offer sorting by terminal server
  const { showTerminalServer = false } = defineProps<{ showTerminalServer?: boolean }>();

  const sortName = defineModel<'Name' | 'Terminal server' | 'Date modified'>('sortName', { required: true });
  const sortOrder = defineModel<'asc' | 'desc'>('sortOrder', { required: true });
</script>

<template>
  <MenuFlyoutItem @click="() => (sortName = 'Name')" :selected="sortName === 'Name'">
    {{ $t('actions.sort.name') }}
  </MenuFlyoutItem>
  <MenuFlyoutItem
    v-if="showTerminalServer"
    @click="() => (sortName = 'Terminal server')"
    :selected="sortName === 'Terminal server'"
  >
    {{ $t('actions.sort.ts') }}
  </MenuFlyoutItem>
  <MenuFlyoutItem @click="() => (sortName = 'Date modified')" :selected="sortName === 'Date modified'">
    {{ $t('actions.sort.date') }}
  </MenuFlyoutItem>
  <MenuFlyoutDivider />
  <MenuFlyoutItem @click="() => (sortOrder = 'asc')" :selected="sortOrder === 'asc'">
    {{ $t('actions.sort.asc') }}
  </MenuFlyoutItem>
  <MenuFlyoutItem @click="() => (sortOrder = 'desc')" :selected="sortOrder === 'desc'">
    {{ $t('actions.sort.desc') }}
  </MenuFlyoutItem>
</template>
