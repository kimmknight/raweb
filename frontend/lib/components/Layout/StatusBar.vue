<script setup lang="ts">
  import ContextMenu from '$components/ContextMenu/ContextMenu.vue';
  import { MenuFlyout, MenuFlyoutItem } from '$components/MenuFlyout';
  import { checkmark } from '$icons';
  import { useCoreDataStore } from '$stores';
  import { statusBarVisibility, statusBarVisibilityOptions } from '$utils';
  import { computed } from 'vue';

  const coreDataStore = useCoreDataStore();
  const isDefinedByPolicy = computed(() => !!coreDataStore.policies.statusBarVisibility);
</script>

<template>
  <footer class="status-bar">
    <ContextMenu placement="top" anchor="start">
      <div id="status-bar-start"></div>
      <div id="status-bar-end"></div>
      <template #menu>
        <MenuFlyout>
          <template #menu>
            <MenuFlyoutItem
              v-for="option in statusBarVisibilityOptions"
              :key="option"
              :indented="statusBarVisibility !== option"
              :disabled="isDefinedByPolicy"
              @click="statusBarVisibility = option"
            >
              <template v-slot:icon v-if="statusBarVisibility === option">
                <span v-swap="checkmark"></span>
              </template>
              {{ $t(`settings.statusBar.options.${option}`) }}
            </MenuFlyoutItem>
          </template>
        </MenuFlyout>
      </template>
    </ContextMenu>
  </footer>
</template>

<style scoped>
  .status-bar {
    position: relative;
    z-index: 2;
    flex-shrink: 0;
    display: flex;
    align-items: center;
    justify-content: space-between;
    block-size: 30px;
    box-sizing: border-box;
    box-shadow: inset 0 1px 0 var(--wui-divider-stroke-default);
    background-color: var(--wui-solid-background-tertiary);
    color: var(--wui-text-secondary);
    user-select: none;
  }
  .status-bar:not(:has(#status-bar-start > *, #status-bar-end > *)) {
    display: none;
  }
  #status-bar-start,
  #status-bar-end {
    display: flex;
    align-items: center;
    gap: 2px;
    flex-grow: 1;
    flex-shrink: 1;
    block-size: 100%;
    padding-inline: 12px;
  }
  #status-bar-end {
    justify-content: flex-end;
  }
</style>
