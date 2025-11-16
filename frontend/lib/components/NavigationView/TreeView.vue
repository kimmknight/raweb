<script setup lang="ts">
  import { IconButton, ListItem, MenuFlyout, TextBlock } from '$components';
  import { chevronDown, chevronUp } from '$icons';
  import { prefixUserNS, PreventableEvent, toKebabCase } from '$utils';
  import { computed, onMounted, ref, useAttrs } from 'vue';
  import { useRouter } from 'vue-router';
  import type { TreeItem } from './NavigationTypes';

  const {
    tree: unfilteredTree = [],
    __depth = 0,
    compact = false,
    collapsed = false,
    stateId,
  } = defineProps<{
    tree?: TreeItem[];
    __depth?: number;
    compact?: boolean;
    collapsed?: boolean;
    stateId?: string;
  }>();
  const restProps = useAttrs();
  const router = useRouter();

  const treeViewState = ref<Record<string, boolean> | undefined>(undefined);
  onMounted(() => {
    if (!stateId) {
      treeViewState.value = {};
    } else {
      treeViewState.value = JSON.parse(localStorage.getItem(prefixUserNS(`treeView.${stateId}.state`)) || '{}');
    }
  });

  /**
   * Toggles whether a tree is expanded or collapsed.
   */
  function toggleExpansion(event: MouseEvent | KeyboardEvent, name: string) {
    event.stopPropagation();

    // modify treeViewState to have the opposite of the previous entry for the category
    if (!treeViewState.value) {
      treeViewState.value = {};
    }
    treeViewState.value[toKebabCase(name)] = !treeViewState.value[toKebabCase(name)];

    // update value in localStorage for persistence
    if (stateId) {
      localStorage.setItem(
        prefixUserNS(`treeView.${stateId}.state`),
        JSON.stringify(treeViewState.value || {})
      );
    }
  }

  /**
   * Collapses all trees in the view.
   */
  function collapseAllTrees() {
    treeViewState.value = Object.fromEntries(
      Object.entries(treeViewState || {}).map(([key]) => {
        return [key, false];
      })
    );

    if (stateId) {
      localStorage.setItem(
        prefixUserNS(`treeView.${stateId}.state`),
        JSON.stringify(treeViewState.value || {})
      );
    }
  }

  const base = document.querySelector('base')?.getAttribute('href') || '/';

  function handleLeafClick(event: MouseEvent, onClick: TreeItem['onClick'], href: TreeItem['href']) {
    event.preventDefault();

    const preventableEvent = new PreventableEvent(event);
    onClick?.(preventableEvent);

    if (href && !preventableEvent.defaultPrevented) {
      if (href.startsWith('!/')) {
        window.location.href = href.replace('!/', base);
        return;
      }
      if (href.startsWith('http')) {
        window.open(href, '_blank');
        return;
      }
      router.push(href);
    }
  }

  const footer = computed(() => unfilteredTree.find((tr) => tr.name === 'footer'));
  const tree = computed(() => unfilteredTree.filter((tr) => tr.name !== 'footer'));

  /**
   * Gets whether a tree is expanded or not.
   */
  function getIsExpanded(name: string) {
    if (collapsed) {
      return false;
    }

    if (treeViewState.value && !!treeViewState.value[toKebabCase(name)]) {
      return true;
    }
  }
</script>

<template>
  <!--  tree -->
  <div class="tree-view" :class="[collapsed ? 'collapsed' : '']">
    <template v-for="{ name, href, type, children, icon, onClick, selected, disabled } in tree">
      <hr v-if="name === 'hr'" />

      <!-- top-level categories -->
      <template v-else-if="__depth === 0 && type === 'category'">
        <TextBlock class="category-header" variant="bodyStrong" v-if="!collapsed">{{ name }}</TextBlock>
        <TreeView :__depth="__depth" :tree="children" :compact :collapsed :stateId :restProps />
      </template>

      <!-- collapsed branch/subtree/expander -->
      <template v-else-if="type === 'expander' && __depth === 0 && collapsed">
        <MenuFlyout placement="right" anchor="start">
          <template #default="{ popoverId }">
            <IconButton
              :popovertarget="popoverId"
              :class="['tree-view-collapsed-flyout-button', compact ? 'compact' : '']"
              :title="name"
            >
              <img
                v-if="icon && typeof icon !== 'string'"
                :src="icon.href"
                alt=""
                width="24"
                height="24"
                style="margin-right: 8px"
              />
              <span v-else style="display: contents" v-html="icon"></span>
            </IconButton>
          </template>
          <template #menu>
            <TextBlock class="category-header" variant="bodyStrong">{{ name }}</TextBlock>
            <TreeView :__depth :tree="children" :compact :stateId :restProps />
          </template>
        </MenuFlyout>
      </template>

      <!-- branch (subtree) -->
      <template v-else-if="(__depth > 0 && type === 'category') || type === 'expander'">
        <ListItem
          @click="
            ($event: MouseEvent) => {
              const preventableEvent = new PreventableEvent($event);
              onClick?.(preventableEvent);

              if (!preventableEvent.defaultPrevented) {
                toggleExpansion($event, name);
              }
            }
          "
          @keypress="
            ($event: KeyboardEvent) => {
              if ($event.key === 'Enter') {
              const preventableEvent = new PreventableEvent($event);
              onClick?.(preventableEvent);

              if (!preventableEvent.defaultPrevented) {
                toggleExpansion($event, name);
              }
            }
            }
          "
          :disabled
          type="navigation"
          :style="`--depth: ${__depth}`"
          :compact
          :class="`${collapsed ? 'collapsed' : ''}`"
        >
          <template #icon>
            <img
              v-if="icon && typeof icon !== 'string'"
              :src="icon.href"
              alt=""
              width="24"
              height="24"
              style="margin-right: 8px"
            />
            <span v-else style="display: contents" v-html="icon"></span>
          </template>
          {{ name }}
          <template #icon-end>
            <span v-if="getIsExpanded(name)" style="display: contents" v-html="chevronUp"></span>
            <span v-else style="display: contents" v-html="chevronDown"></span>
          </template>
        </ListItem>
        <!-- subtree from expander -->
        <div v-if="getIsExpanded(name)" class="subtree-items">
          <TreeView :__depth="__depth + 1" :tree="children" :compact :collapsed :stateId :restProps />
        </div>
      </template>

      <!-- leaf -->
      <ListItem
        v-else
        @click="handleLeafClick($event, onClick, href)"
        @keypress="
          if ($event.key === 'Enter' || $event.key === ' ') {
            handleLeafClick($event, onClick, href);
          }
        "
        :disabled
        type="navigation"
        :selected="selected ?? (href ? router.currentRoute.value.path === href : false)"
        :href="href ? (base.endsWith('/') ? base.slice(0, -1) : base) + href.replace('!/', '/') : undefined"
        :style="`--depth: ${__depth}`"
        :compact
        :class="`${collapsed ? 'collapsed' : ''}`"
        :title="collapsed ? name : undefined"
      >
        <template v-if="icon" #icon>
          <img
            v-if="icon && typeof icon !== 'string'"
            :src="icon.href"
            alt=""
            width="24"
            height="24"
            style="margin-right: 8px"
          />
          <span v-else style="display: contents" v-html="icon"></span>
        </template>
        {{ name }}
      </ListItem>
    </template>
  </div>

  <div v-if="footer" class="footer">
    <TreeView :__depth="__depth + 1" :tree="footer.children" :compact :collapsed :stateId :restProps />
  </div>
</template>

<style scoped>
  /* add padding to subtrees for the nesting effect */
  .subtree-items :deep(.list-item) {
    padding-inline-start: calc((var(--depth, 0) * 32px) + 12px);
  }
  .subtree-items :deep(.list-item::before) {
    inset-inline-start: calc(var(--depth, 0) * 32px);
  }

  /* ensure the text does not wrap when switching to collapsed mode */
  .tree-view :deep(.list-item .text-block) {
    display: block;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  /* hide scrollbar on collapsed trees */
  .tree-view.collapsed::-webkit-scrollbar {
    width: 0px;
    background: transparent; /* make scrollbar transparent */
  }

  .tree-view {
    max-block-size: 100%;
    min-block-size: 0;
    display: flex;
    flex-direction: column;
    flex-grow: 1;
    overflow-y: auto;
    overflow-x: hidden;
  }

  .tree-view :deep(.category-header) {
    inline-size: 100%;
    padding-inline: 16px;
    padding-block: 10px;
  }

  /* prevent deeply nested trees from having internal expansion or collapses (with scroll bars) */
  .tree-view :deep(.tree-view) {
    flex-grow: 0;
    flex-shrink: 0;
  }

  hr {
    margin: 6px 0;
    border: none;
    border-bottom: 1px solid var(--wui-subtle-secondary);
  }

  /* collapsed mode flyouts */
  .tree-view :deep(.tree-view-collapsed-flyout .category-header) {
    box-sizing: border-box;
  }
  .tree-view :deep(.tree-view-collapsed-flyout-button) {
    margin: 3px 5px;
    block-size: 34px;
    width: 40px;
  }
  .tree-view :deep(.tree-view-collapsed-flyout-button.compact) {
    margin-top: 1px;
    margin-bottom: 1px;
    block-size: 30px;
  }
</style>
