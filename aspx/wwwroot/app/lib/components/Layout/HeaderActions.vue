<script setup lang="ts">
  import Button from '$components/Button/Button.vue';
  import { MenuFlyout, MenuFlyoutItem } from '$components/MenuFlyout';
  import TextBox from '$components/TextBox/TextBox.vue';
  import { chevronDown, content, grid, rectangle, search, sort as sortIcon, tiles, view } from '$icons';
  import { getAppsAndDevices } from '$utils';

  const { searchPlaceholder = 'Search', data } = defineProps<{
    searchPlaceholder?: string;
    data: Awaited<ReturnType<typeof getAppsAndDevices>>;
  }>();

  const mode = defineModel<'card' | 'list' | 'grid' | 'tile'>('mode', { required: true });
  const sortName = defineModel<'Name' | 'Terminal server' | 'Date modified'>('sortName', { required: true });
  const sortOrder = defineModel<'asc' | 'desc'>('sortOrder', { required: true });
  const query = defineModel<string>('query', { required: true });

  function handleSubmitQuery(value: string) {
    query.value = value;
  }
</script>

<template>
  <div class="header-actions">
    <MenuFlyout placement="bottom" anchor="start">
      <template v-slot="{ popoverId }">
        <Button :popovertarget="popoverId" @click.stop>
          <template v-slot:icon><span v-swap="sortIcon"></span></template>
          Sort
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
          View
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
    <div class="search">
      <TextBox :placeholder="searchPlaceholder" showSubmitButton showClearButton @submit="handleSubmitQuery">
        <template v-slot:submit-icon><span v-swap="search"></span></template>
      </TextBox>
    </div>
  </div>
</template>

<style scoped>
  .header-actions {
    display: flex;
    flex-direction: row;
    gap: 8px;
    margin: 12px 0 8px 0;
  }

  .search {
    flex-grow: 1;
  }
  .search > * {
    max-width: 360px;
    margin-left: auto;
  }
</style>
