<script setup lang="ts">
  import { AnimatedNavigationItemIndicator, ListItem, MenuFlyout, TextBlock } from '$components';
  import { chevronDown } from '$icons';
  import { prefixUserNS, PreventableEvent, toKebabCase } from '$utils';
  import { isBrowser } from '$utils/environment.ts';
  import { collapseUp, expandDown } from '$utils/transitions';
  import {
    computed,
    type FunctionalComponent,
    inject,
    onMounted,
    provide,
    ref,
    useAttrs,
    useTemplateRef,
    watch,
  } from 'vue';
  import { useRouter } from 'vue-router';
  import type { TreeItem } from './NavigationTypes';
  import { ANIMATE_TREE_SELECTION_KEY, PROVIDED_TRACK_DEPTH_KEY } from './treeSelection.ts';

  const Track = AnimatedNavigationItemIndicator.Track;
  const Selectable = AnimatedNavigationItemIndicator.Selectable;

  // Renders its slotted content with no wrapper element, used as the "off" state
  // for the conditional Track/Selectable wrappers below so that TreeViews outside
  // a NavigationPane keep their original markup and each ListItem's static bar.
  const Passthrough: FunctionalComponent = (_props, { slots }) => slots.default?.() ?? null;

  const {
    tree: unfilteredTree = [],
    __depth = 0,
    compact = false,
    collapsed = false,
    unlabeled = false,
    stateId,
    blockAnimatedIndicatorDetection = false,
  } = defineProps<{
    tree?: TreeItem[];
    __depth?: number;
    compact?: boolean;
    collapsed?: boolean;
    unlabeled?: boolean;
    stateId?: string;
    blockAnimatedIndicatorDetection?: boolean;
  }>();
  const restProps = useAttrs();
  const router = useRouter();

  // when inside a NavigationPane, wrap this level's items in a selection track so
  // the accent bar slides between direct siblings; each TreeView provides its own
  // track, so selection moving across levels lands in a different track (and only
  // animates within a level). The indicator is indented to match this depth.
  const animateSelection = !blockAnimatedIndicatorDetection && inject(ANIMATE_TREE_SELECTION_KEY, false);

  // Only create a track when there isn't already one at this depth.
  // Top-level categories recurse at the same __depth and reuse the ancestor track,
  // so every __depth === 0 item (leaves and subtree buttons) shares one indicator.
  // Each deeper subtree makes its own track and animates only among its direct siblings.
  const injectedTrackDepth = inject(PROVIDED_TRACK_DEPTH_KEY, -1);
  const createsOwnTrack = animateSelection && __depth > injectedTrackDepth;
  provide(PROVIDED_TRACK_DEPTH_KEY, createsOwnTrack ? __depth : injectedTrackDepth);

  const indicatorCrossOffset = computed(() => `--indicator-cross-offset: ${__depth * 32}px`);
  const INDICATOR_SIZE = 16;

  const treeViewState = ref<Record<string, boolean> | undefined>(undefined);
  const delayedTreeViewState = ref<Record<string, boolean> | undefined>(undefined);
  onMounted(() => {
    if (!stateId) {
      treeViewState.value = {};
    } else if (isBrowser) {
      treeViewState.value = JSON.parse(localStorage.getItem(prefixUserNS(`treeView.${stateId}.state`)) || '{}');
    }
    delayedTreeViewState.value = treeViewState.value;
  });

  /**
   * Toggles whether a tree is expanded or collapsed.
   */
  function toggleExpansion(event: MouseEvent | KeyboardEvent, name: string) {
    event.stopPropagation();

    // ensure treeViewState is initialized and synced with delayed state
    if (!treeViewState.value) {
      treeViewState.value = {};
    }
    delayedTreeViewState.value = { ...treeViewState.value };

    // modify treeViewState to have the opposite of the previous entry for the category
    treeViewState.value[toKebabCase(name)] = !treeViewState.value[toKebabCase(name)];
    setTimeout(() => {
      delayedTreeViewState.value = { ...treeViewState.value };
    }, 130);

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

  const base = (isBrowser && document.querySelector('base')?.getAttribute('href')) || '/';

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
  function getIsExpanded(name: string, delayed = false) {
    if (collapsed || unlabeled) {
      return false;
    }

    const state = delayed ? delayedTreeViewState.value : treeViewState.value;

    if (state && !!state[toKebabCase(name)]) {
      return true;
    }
  }

  // track the height of the content in the tree so we can animate it if needed
  const treeViewContentElem = useTemplateRef<HTMLDivElement>('treeViewContent');
  const contentHeight = ref<number | null>(null);
  watch(
    () => treeViewContentElem.value?.scrollHeight,
    (newHeight) => {
      if (newHeight) {
        contentHeight.value = newHeight;
      }
    }
  );

  // when we change the collapsed state, animate changing the height of the tree view
  const treeViewElem = useTemplateRef<HTMLDivElement>('treeView');
  const collapsedStateIsResolved = ref(true);
  watch(
    () => collapsed,
    async () => {
      if (!treeViewElem.value || contentHeight.value === null) {
        return;
      }

      // hide overflow when animating to prevent scrollbars or overlapping contnet
      treeViewElem.value.style.overflow = 'hidden';

      // animate the height change
      collapsedStateIsResolved.value = false;
      if (!collapsed) {
        await expandDown(treeViewContentElem.value, { endHeight: contentHeight.value });
      } else {
        await collapseUp(treeViewContentElem.value, { startHeight: contentHeight.value });
      }
      collapsedStateIsResolved.value = true;

      // restore overflow to previous state
      treeViewElem.value.style.overflow = '';

      // ensure any running animations are cancelled
      // before we potentially start a new set of animations
      return () => {
        treeViewContentElem.value?.getAnimations().forEach((animation) => animation.cancel());
        collapsedStateIsResolved.value = true;
      };
    }
  );

  function isItemSelected(item: TreeItem) {
    if (item.selected) {
      return true;
    }
    if (item.href && router.currentRoute.value.path === item.href) {
      return true;
    }
    return false;
  }

  /**
   * Recursively checks if a tree item has any selected children.
   */
  function hasSelectedChild(item: TreeItem) {
    if (item.children) {
      for (const child of item.children) {
        if (isItemSelected(child)) {
          return true;
        }
        if (child.children && hasSelectedChild(child)) {
          return true;
        }
      }
    }
    return false;
  }
</script>

<template>
  <!--  tree -->
  <div
    class="tree-view"
    :class="[collapsed && collapsedStateIsResolved ? 'collapsed' : '', unlabeled ? 'unlabeled' : '']"
    ref="treeView"
    :inert="collapsed && collapsedStateIsResolved"
  >
    <div class="tree-view-content" ref="treeViewContent">
      <component
        :is="createsOwnTrack ? Track : Passthrough"
        v-bind="createsOwnTrack ? { class: 'selection-track', style: indicatorCrossOffset } : {}"
      >
        <template v-for="({ name, href, type, children, icon, onClick, selected, disabled }, index) in tree">
          <hr v-if="name === 'hr'" />

          <!-- top-level categories -->
          <template v-else-if="__depth === 0 && type === 'category'">
            <TextBlock :class="['category-header', unlabeled ? 'hidden' : '']" variant="bodyStrong">{{
              name
            }}</TextBlock>
            <TreeView
              :__depth="__depth"
              :tree="children"
              :compact
              :collapsed
              :unlabeled
              :stateId
              :="restProps"
            />
          </template>

          <!-- branch (subtree) -->
          <template v-else-if="(__depth > 0 && type === 'category') || type === 'expander'">
            <div class="subtree-buttons">
              <!-- unlabeled state -->
              <div class="unlabeled-subtree-button" :inert="!unlabeled">
                <!-- show icons with menus for top-level expanders (hide otherwise) -->
                <template v-if="type === 'expander' && __depth === 0">
                  <MenuFlyout placement="right" anchor="start">
                    <template #default="{ popoverId }">
                      <component
                        :is="animateSelection && unlabeled ? Selectable : Passthrough"
                        v-bind="
                          animateSelection
                            ? {
                                selected: !getIsExpanded(name, true) && hasSelectedChild(tree[index]),
                                indicatorSize: INDICATOR_SIZE,
                              }
                            : {}
                        "
                      >
                        <ListItem
                          :popovertarget="popoverId"
                          :title="name"
                          :compact
                          :disabled
                          :selected="!getIsExpanded(name, true) && hasSelectedChild(tree[index])"
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
                        </ListItem>
                      </component>
                    </template>
                    <template #menu>
                      <TextBlock class="category-header" variant="bodyStrong">{{ name }}</TextBlock>
                      <TreeView :__depth="0.0000000000001" :tree="children" :compact :stateId :="restProps" />
                    </template>
                  </MenuFlyout>
                </template>
              </div>

              <!-- labeled state -->
              <div class="labeled-subtree-button" :inert="unlabeled">
                <component
                  :is="animateSelection && !unlabeled ? Selectable : Passthrough"
                  v-bind="
                    animateSelection
                      ? {
                          selected: !getIsExpanded(name, true) && hasSelectedChild(tree[index]),
                          indicatorSize: INDICATOR_SIZE,
                        }
                      : {}
                  "
                >
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
                    :selected="!getIsExpanded(name, true) && hasSelectedChild(tree[index])"
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
                      <span
                        :class="['chevron', getIsExpanded(name, true) ? 'expanded' : '']"
                        v-html="chevronDown"
                      ></span>
                    </template>
                  </ListItem>
                </component>
              </div>
            </div>

            <!-- subtree from expander -->
            <div class="subtree-items" v-if="treeViewState !== undefined">
              <TreeView
                :__depth="__depth + 1"
                :tree="children"
                :compact
                :collapsed="collapsed || !getIsExpanded(name)"
                :unlabeled
                :stateId
                :="restProps"
              />
            </div>
          </template>

          <!-- leaf -->
          <component
            v-else
            :is="animateSelection ? Selectable : Passthrough"
            v-bind="
              animateSelection
                ? {
                    selected: !collapsed && collapsedStateIsResolved && isItemSelected(tree[index]),
                    indicatorSize: INDICATOR_SIZE,
                  }
                : {}
            "
          >
            <ListItem
              @click="handleLeafClick($event, onClick, href)"
              @keypress="
                if ($event.key === 'Enter' || $event.key === ' ') {
                  handleLeafClick($event, onClick, href);
                }
              "
              :disabled
              type="navigation"
              :selected="!collapsed && collapsedStateIsResolved && isItemSelected(tree[index])"
              :href="
                href ? (base.endsWith('/') ? base.slice(0, -1) : base) + href.replace('!/', '/') : undefined
              "
              :style="`--depth: ${__depth}`"
              :compact
              :class="`tree-leaf ${collapsed ? 'collapsed' : ''}`"
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
          </component>
        </template>
      </component>
    </div>
  </div>

  <div v-if="footer" class="footer">
    <TreeView
      :__depth="__depth + 1"
      :tree="footer.children"
      :compact
      :collapsed
      :unlabeled
      :stateId
      :="restProps"
    />
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
    opacity: 1;
    transition: all 130ms cubic-bezier(0.16, 1, 0.3, 1);
  }

  /* the selection track wraps this level's items but must not change their layout */
  .tree-view-content > .selection-track {
    display: flex;
    flex-direction: column;
  }

  /* hide scrollbar on collapsed trees */
  .tree-view.collapsed::-webkit-scrollbar {
    width: 0px;
    background: transparent; /* make scrollbar transparent */
  }

  /* collapse tree views that are collapsed */
  .tree-view.collapsed {
    height: 0;
    overflow: hidden;
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
    white-space: nowrap;
    opacity: 1;
    transition: all 130ms cubic-bezier(0.16, 1, 0.3, 1);
  }
  .tree-view :deep(.category-header.hidden) {
    inline-size: 100%;
    padding-inline: 16px;
    padding-block: 0;
    opacity: 0;
    height: 0;
  }

  /* disable margin collapsing */
  .tree-view-content {
    display: flex;
    flex-direction: column;
    max-height: fit-content;
  }

  /* prevent deeply nested trees from having internal expansion or collapses (with scroll bars) */
  .tree-view :deep(.tree-view:not(.collapsed)) {
    flex-grow: 0;
    flex-shrink: 0;
    overflow: unset;
  }

  hr {
    margin: 6px 0;
    border: none;
    border-bottom: 1px solid var(--wui-subtle-secondary);
  }

  /* subtree label positions and visibility */
  .subtree-buttons {
    position: relative;
    display: contents;
    width: 100%;
  }
  .subtree-buttons .labeled-subtree-button {
    position: relative;
  }
  .subtree-buttons .unlabeled-subtree-button {
    position: relative;
  }
  .subtree-buttons .labeled-subtree-button,
  .subtree-buttons .unlabeled-subtree-button {
    width: 100%;
    transition: all 130ms cubic-bezier(0.16, 1, 0.3, 1);
    opacity: 1;
  }
  .tree-view.unlabeled .subtree-buttons .labeled-subtree-button,
  .tree-view:not(.unlabeled) .subtree-buttons .unlabeled-subtree-button {
    opacity: 0;
    height: 0;
    pointer-events: none;
  }

  .chevron {
    display: contents;
  }
  .chevron > :deep(svg) {
    inline-size: 12px !important;
    transition: transform 200ms cubic-bezier(0.16, 1, 0.3, 1);
  }
  .chevron.expanded > :deep(svg) {
    transform: rotate(180deg);
  }
</style>
