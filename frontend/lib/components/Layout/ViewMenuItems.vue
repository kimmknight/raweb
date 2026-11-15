<script setup lang="ts">
  import { MenuFlyoutItem } from '$components/MenuFlyout';
  import { content, grid, list, rectangle, tiles } from '$icons';
  import { onBeforeUnmount, onMounted } from 'vue';
  import { getViewModeForShortcut, getViewShortcutHint, type ViewMode } from './viewShortcuts';

  const mode = defineModel<ViewMode>('mode', { required: true });

  function handleShortcut(event: KeyboardEvent) {
    const shortcutMode = getViewModeForShortcut(event);
    if (shortcutMode) {
      event.preventDefault();
      mode.value = shortcutMode;
    }
  }
  onMounted(() => window.addEventListener('keydown', handleShortcut));
  onBeforeUnmount(() => window.removeEventListener('keydown', handleShortcut));
</script>

<template>
  <MenuFlyoutItem
    @click="() => (mode = 'card')"
    :selected="mode === 'card'"
    :hint="getViewShortcutHint('card')"
  >
    <template v-slot:icon><span v-swap="rectangle"></span></template>
    {{ $t('actions.view.card') }}
  </MenuFlyoutItem>
  <MenuFlyoutItem
    @click="() => (mode = 'grid')"
    :selected="mode === 'grid'"
    :hint="getViewShortcutHint('grid')"
  >
    <template v-slot:icon><span v-swap="grid"></span></template>
    {{ $t('actions.view.grid') }}
  </MenuFlyoutItem>
  <MenuFlyoutItem
    @click="() => (mode = 'tile')"
    :selected="mode === 'tile'"
    :hint="getViewShortcutHint('tile')"
  >
    <template v-slot:icon><span v-swap="tiles"></span></template>
    {{ $t('actions.view.tile') }}
  </MenuFlyoutItem>
  <MenuFlyoutItem
    @click="() => (mode = 'list')"
    :selected="mode === 'list'"
    :hint="getViewShortcutHint('list')"
  >
    <template v-slot:icon><span v-swap="content"></span></template>
    {{ $t('actions.view.list') }}
  </MenuFlyoutItem>
  <MenuFlyoutItem
    @click="() => (mode = 'details')"
    :selected="mode === 'details'"
    :hint="getViewShortcutHint('details')"
  >
    <template v-slot:icon><span v-swap="list"></span></template>
    {{ $t('actions.view.details') }}
  </MenuFlyoutItem>
</template>
