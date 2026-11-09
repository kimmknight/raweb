<script setup lang="ts">
  import { MenuFlyoutItem } from '$components/MenuFlyout';
  import { content, grid, rectangle, tiles } from '$icons';

  const mode = defineModel<'card' | 'list' | 'grid' | 'tile'>('mode', { required: true });
</script>

<template>
  <MenuFlyoutItem @click="() => (mode = 'card')" :selected="mode === 'card'">
    <template v-slot:icon><span v-swap="rectangle"></span></template>
    {{ $t('actions.view.card') }}
  </MenuFlyoutItem>
  <MenuFlyoutItem @click="() => (mode = 'grid')" :selected="mode === 'grid'">
    <template v-slot:icon><span v-swap="grid"></span></template>
    {{ $t('actions.view.grid') }}
  </MenuFlyoutItem>
  <MenuFlyoutItem @click="() => (mode = 'tile')" :selected="mode === 'tile'">
    <template v-slot:icon><span v-swap="tiles"></span></template>
    {{ $t('actions.view.tile') }}
  </MenuFlyoutItem>
  <MenuFlyoutItem @click="() => (mode = 'list')" :selected="mode === 'list'">
    <template v-slot:icon><span v-swap="content"></span></template>
    {{ $t('actions.view.list') }}
  </MenuFlyoutItem>
</template>
