<script setup lang="ts">
  import { chevronDown, chevronUp } from '$icons';
  import { useCoreDataStore } from '$stores';
  import { isBrowser } from '$utils/environment';
  import { useTranslation } from 'i18next-vue';
  import { computed, onMounted, onUpdated, provide, reactive, toRef, useTemplateRef, watch } from 'vue';
  import { type DetailsColumn, resourceGridContextKey } from './resourceGridContext';

  const props = defineProps<{
    mode: 'card' | 'list' | 'grid' | 'tile' | 'details' | undefined;
    /**
     * The columns that the details view shows after the name column.
     * Cards give their values for these columns by column key, and a card without a value leaves the column empty.
     * The default columns are for apps and devices.
     */
    columns?: DetailsColumn[];
    /** If provided, the collapsed section names are stored in localStorage and made unique with this key. */
    persist?: string;
  }>();
  const sortName = defineModel<'Name' | 'Terminal server' | 'Date modified'>('sortName');
  const sortOrder = defineModel<'asc' | 'desc'>('sortOrder');
  const mode = toRef(props, 'mode');
  const { t } = useTranslation();

  const columns = computed(() => {
    const list: DetailsColumn[] = props.columns ?? [
      {
        key: 'terminalServer',
        label: t('resourceTable.terminalServer'),
        width: 'minmax(140px, 220px)',
        sortKey: 'Terminal server',
      },
      { key: 'dateModified', label: t('resourceTable.dateModified'), width: '170px', sortKey: 'Date modified' },
      { key: 'type', label: t('resourceTable.type'), width: '110px' },
    ];
    return list
      .map((column, index) => ({ ...column, order: column.order ?? index }))
      .sort((a, b) => a.order - b.order);
  });
  const columnTracks = computed(
    () => '36px minmax(200px, 420px) ' + columns.value.map((c) => c.width ?? 'minmax(110px, 220px)').join(' ')
  );
  const headers = computed(() => [
    { key: '$name', label: t('resourceTable.name'), sortKey: 'Name' as const },
    ...columns.value,
  ]);

  // the sections that headings inside this grid have registered and
  // whether each is collapsed (only used by the details view)
  const sections = reactive(new Map<string, boolean>());

  // remember which sections are collapsed (a section that is not collapsed does not need to be remembered)
  const { userNamespace } = useCoreDataStore();
  const collapsedSectionsStorageKey = computed(() =>
    props.persist ? `${userNamespace}::resource-grid:${props.persist}:collapsedSections` : undefined
  );
  if (isBrowser && collapsedSectionsStorageKey.value) {
    try {
      const stored = JSON.parse(localStorage.getItem(collapsedSectionsStorageKey.value) ?? '[]');
      if (Array.isArray(stored)) {
        stored.filter((key) => typeof key === 'string').forEach((key) => sections.set(key, true));
      }
    } catch {
      // ignore stored values that cannot be read
    }
  }
  watch(
    () =>
      JSON.stringify(
        Array.from(sections)
          .filter(([_, collapsed]) => collapsed)
          .map(([key]) => key)
      ),
    (collapsedSectionsJson) => {
      if (isBrowser && collapsedSectionsStorageKey.value) {
        localStorage.setItem(collapsedSectionsStorageKey.value, collapsedSectionsJson);
      }
    }
  );

  provide(resourceGridContextKey, {
    mode,
    columns,
    sections: {
      isCollapsed: (sectionKey) => sections.get(sectionKey) ?? false,
      setCollapsed: (sectionKey, collapsed) => sections.set(sectionKey, collapsed),
      expandAll: () => sections.forEach((_, sectionKey) => sections.set(sectionKey, false)),
      collapseAll: () => sections.forEach((_, sectionKey) => sections.set(sectionKey, true)),
      register(sectionKey) {
        if (!sections.has(sectionKey)) sections.set(sectionKey, false);
        // a collapsed section stays in the record when it goes away (such as when a search hides it),
        // so it is still collapsed when it comes back
        return () => {
          if (sections.get(sectionKey) === false) sections.delete(sectionKey);
        };
      },
    },
  });

  /** The element representing the contents of the table. We use this element to handle keyboard navigation. */
  const tbody = useTemplateRef<HTMLElement>('tbody');

  function getFocusableRows() {
    return Array.from(tbody.value?.querySelectorAll<HTMLElement>('[role="row"]') ?? []).filter(
      (row) => !row.closest('[inert]')
    );
  }

  function setTabIndexForEachRow(focusedRow?: HTMLElement) {
    const rows = getFocusableRows();
    focusedRow ??= rows.find((row) => row.tabIndex === 0) ?? rows[0];
    rows.forEach((row) => (row.tabIndex = row === focusedRow ? 0 : -1));
  }
  onMounted(() => setTabIndexForEachRow());
  onUpdated(() => setTabIndexForEachRow());

  function moveFocusUp(evt: KeyboardEvent) {
    const rows = getFocusableRows();
    const index = rows.indexOf(document.activeElement as HTMLElement);
    if (index > 0) {
      evt.preventDefault();
      rows[index - 1].focus();
    }
  }

  function moveFocusDown(evt: KeyboardEvent) {
    const rows = getFocusableRows();
    const index = rows.indexOf(document.activeElement as HTMLElement);
    if (index !== -1 && index < rows.length - 1) {
      evt.preventDefault();
      rows[index + 1].focus();
    }
  }

  function sortBy(name: 'Name' | 'Terminal server' | 'Date modified') {
    if (sortName.value === name) {
      sortOrder.value = sortOrder.value === 'asc' ? 'desc' : 'asc';
      return;
    }
    sortName.value = name;
    sortOrder.value = 'asc';
  }
</script>

<template>
  <div v-if="props.mode === 'details'" class="details-wrapper">
    <div role="table" class="details-table" :style="{ '--details-columns': columnTracks }">
      <div role="rowgroup" class="thead">
        <div role="row">
          <span role="columnheader"></span>
          <template v-for="column in headers" :key="column.key">
            <span
              v-if="column.sortKey"
              role="columnheader"
              class="sortable"
              tabindex="0"
              :aria-sort="
                sortName === column.sortKey ? (sortOrder === 'desc' ? 'descending' : 'ascending') : 'none'
              "
              @click="sortBy(column.sortKey)"
              @keydown.enter.space.prevent="sortBy(column.sortKey)"
            >
              {{ column.label }}
              <span class="sort-icon" v-if="sortName === column.sortKey" :key="sortOrder">
                <span v-swap="sortOrder === 'desc' ? chevronDown : chevronUp"></span>
              </span>
            </span>
            <span v-else role="columnheader">{{ column.label }}</span>
          </template>
        </div>
      </div>
      <div
        role="rowgroup"
        class="tbody"
        ref="tbody"
        @focusin="
          getFocusableRows().includes($event.target as HTMLElement) &&
          setTabIndexForEachRow($event.target as HTMLElement)
        "
        @keydown.up="moveFocusUp($event)"
        @keydown.down="moveFocusDown($event)"
      >
        <slot></slot>
      </div>
    </div>
  </div>
  <div v-else class="grid" :class="`mode-${props.mode}`">
    <slot></slot>
  </div>
</template>

<style scoped>
  .grid {
    display: grid;
  }
  .grid.mode-card {
    grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  }
  .grid.mode-grid {
    grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
  }
  .grid.mode-list {
    grid-template-columns: 1fr;
  }
  .grid.mode-tile {
    grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  }
  @media (max-width: 599px) {
    .grid.mode-tile {
      grid-template-columns: 1fr;
    }
  }

  .grid:has(> .plain-section) {
    display: block;
  }

  .details-wrapper {
    box-shadow: 0 0 0 1px var(--wui-divider-stroke-default);
    border-radius: var(--wui-control-corner-radius);
    margin-block-start: 16px;
    flex-grow: 1;
    flex-shrink: 1;
    min-block-size: 0;
    overflow: auto;
  }
  .thead {
    position: sticky;
    top: 0;
    z-index: 1;
    background-color: var(--wui-solid-background-tertiary);
  }
  .details-table {
    min-width: 700px;
    font-family: var(--wui-font-family-text);
    font-size: var(--wui-font-size-body);
    line-height: 20px;
  }
  .details-table [role='row'] {
    display: grid;
    grid-template-columns: var(--details-columns);
    align-items: center;
  }
  .thead [role='row'] {
    min-height: 36px;
    border-bottom: 1px solid var(--wui-divider-stroke-default);
    background-color: var(--wui-solid-background-tertiary);
    user-select: none;
  }
  [role='columnheader'] {
    padding: 4px 10px;
    box-sizing: border-box;
  }
  [role='columnheader'].sortable {
    display: flex;
    align-items: center;
    gap: 6px;
    block-size: 100%;
    cursor: default;
    transition: var(--wui-control-faster-duration) ease background;
  }
  [role='columnheader'].sortable:hover {
    background-color: var(--wui-control-fill-secondary);
  }
  [role='columnheader'].sortable:active {
    background-color: var(--wui-control-fill-tertiary);
  }
  [role='columnheader']:focus-visible {
    border-radius: var(--wui-control-corner-radius);
    outline-offset: -2px;
  }
  .sort-icon {
    display: flex;
    inline-size: 12px;
    block-size: 12px;
  }
  .sort-icon :deep(svg) {
    inline-size: 12px;
    block-size: 12px;
  }
  .tbody {
    margin-block: 8px;
  }
</style>
