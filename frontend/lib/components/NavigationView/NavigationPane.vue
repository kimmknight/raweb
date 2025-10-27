<script setup lang="ts">
  import { IconButton, ListItem, TreeView } from '$components';
  import { arrowLeft, navigation } from '$icons';
  import { ref } from 'vue';
  import { useRouter } from 'vue-router';
  import { TreeItem } from './NavigationTypes';

  const {
    headerText = '',
    menuItems = [],
    showBackArrow = false,
    hideMenuButton = false,
    compact = false,
    variant = 'left',
    width = 290,
    stateId,
  } = defineProps<{
    headerText?: string;
    menuItems?: TreeItem[];
    showBackArrow?: boolean;
    hideMenuButton?: boolean;
    compact?: boolean;
    variant?: 'left' | 'leftCompact';
    stateId?: string;
    width?: number;
  }>();
  const collapsed = defineModel<boolean>('collapsed', {
    default: false,
  });
  const router = useRouter();

  const previousPage = ref<string | null>(null);
  router.afterEach((to, from) => {
    if (to.fullPath !== from.fullPath && to.fullPath !== previousPage.value) {
      previousPage.value = from.fullPath;
    }
  });

  function goBack() {
    if (previousPage.value) {
      router.back();
    }
  }

  function toggleCollapse() {
    collapsed.value = !collapsed.value;
  }
</script>

<template>
  <aside :class="[collapsed ? 'collapsed' : '', variant === 'leftCompact' ? 'leftCompact' : '']">
    <template v-if="(!headerText && !hideMenuButton) || showBackArrow">
      <div class="buttonrow">
        <IconButton :disabled="!previousPage" @click="goBack" v-if="showBackArrow">
          <span style="display: contents" v-html="arrowLeft"></span>
        </IconButton>
        <IconButton @click="toggleCollapse" v-if="!headerText && !hideMenuButton">
          <span style="display: contents" v-html="navigation"></span>
        </IconButton>
      </div>
    </template>
    <ListItem v-if="headerText && !hideMenuButton" @click="toggleCollapse">
      <template #icon>
        <span style="display: contents" v-html="navigation"></span>
      </template>
      <span>{{ headerText }}</span>
    </ListItem>

    <slot name="custom" />

    <TreeView :tree="menuItems" :compact :collapsed :stateId></TreeView>

    <div
      v-if="variant === 'leftCompact'"
      :class="['navigation-pane-click-away', !collapsed ? 'darken' : '']"
      @click="() => (collapsed = true)"
      @keypress="
        (evt) => {
          if (evt.key === 'Esc') {
            collapsed = true;
          }
        }
      "
    />
  </aside>

  <div v-if="variant === 'leftCompact'" class="spacer" />
</template>

<style scoped>
  aside {
    width: 290px;
    box-sizing: border-box;
    display: flex;
    flex-direction: column;
    flex-shrink: 0;
    flex-grow: 0;
    transition: width 200ms cubic-bezier(0.16, 1, 0.3, 1);
  }

  aside.collapsed {
    width: 50px;
  }

  aside.leftCompact {
    position: absolute;
    background-color: black;
    /* height: calc(100% - env(titlebar-area-height, 33px)); */
    height: 100%;
    z-index: 999;
    background-color: var(--wui-solid-background-base);
    box-shadow: inset -1px 0 0 0 var(--wui-surface-stroke-flyout), var(--wui-flyout-shadow);
    border-radius: 0 var(--wui-overlay-corner-radius) var(--wui-overlay-corner-radius) 0;
  }

  aside.leftCompact.collapsed {
    border: none;
    border-radius: 0;
    box-shadow: none;
  }

  .spacer {
    width: 50px;
    flex-grow: 0;
    flex-shrink: 0;
  }

  span {
    font-weight: 500;
  }

  .buttonrow {
    margin: 3px 5px 3px 9px;
  }

  .navigation-pane-click-away {
    align-items: center;
    block-size: 100%;
    display: none;
    flex-direction: column;
    inline-size: 100%;
    inset-block-start: 0;
    inset-inline-start: 0;
    justify-content: center;
    position: fixed;
    z-index: -1;
  }
  .navigation-pane-click-away.darken {
    display: flex;
  }
</style>
