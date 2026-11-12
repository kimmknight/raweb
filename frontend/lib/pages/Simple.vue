<script setup lang="ts">
  import {
    createHeaderActionModelRefs,
    GenericResourceCard,
    GenericResourceSection,
    HeaderActions,
    ResourceGrid,
    ResourcesPageContextMenu,
    TextBlock,
  } from '$components';
  import { flatModeEnabled } from '$utils';
  import { useTranslation } from 'i18next-vue';
  import { computed } from 'vue';

  type Resource = NonNullable<
    Awaited<ReturnType<typeof import('$utils').getAppsAndDevices>>
  >['resources'][number];

  const { t } = useTranslation();

  const props = defineProps<import('./types.d.ts').PageProps>();

  const folders = computed(() => {
    if (!props.data) return [];

    function _organize(resources: Resource[]) {
      return organize(resources, sortName.value, sortOrder.value, terminalServersFilter.value, query.value);
    }

    if (flatModeEnabled.value) {
      return [['/', _organize(props.data.resources)]] as const;
    }

    // sort the folders by name, but with some extra rules:
    // - the root folder ("/") should come first
    // - folders within other folders should come after their parent folder
    //   - e.g. "/folder1" should come before "/folder1/subfolder"
    //   - e.g. "/folder2" should come after "/folder1/subfolder"
    // - folders should be sorted alphabetically otherwise
    const sortedFolders = Object.entries(props.data.folders).sort(([a], [b]) => {
      if (a === '/') return -1; // root folder comes first
      if (b === '/') return 1; // root folder comes first
      if (a.startsWith(b)) return 1; // parent folder comes before child folder
      if (b.startsWith(a)) return -1; // child folder comes after parent folder
      return a.localeCompare(b); // otherwise sort alphabetically
    });

    return sortedFolders
      .map(([folderName, resources]) => {
        return [folderName, _organize(resources)] as const;
      })
      .filter(([_, resources]) => resources.length > 0);
  });

  const { mode, sortName, sortOrder, terminalServersFilter, query, organize } = createHeaderActionModelRefs({
    defaults: { mode: 'grid' },
    persist: 'simple-apps-and-desktops',
  });
</script>

<template>
  <div class="titlebar-row">
    <TextBlock variant="title" tag="h1">{{ t('simple.title') }}</TextBlock>
    <HeaderActions
      :data="props.data"
      v-model:mode="mode"
      v-model:sortName="sortName"
      v-model:sortOrder="sortOrder"
      v-model:terminalServersFilter="terminalServersFilter"
      v-model:query="query"
      :searchPlaceholder="t('simple.search')"
    />
  </div>

  <ResourceGrid
    :mode="mode"
    v-model:sortName="sortName"
    v-model:sortOrder="sortOrder"
    persist="simple-apps-and-desktops"
    class="resource-grid"
  >
    <GenericResourceSection
      v-for="([folderName, resources], index) in folders"
      :key="index"
      :title="folderName === '/' ? undefined : folderName.slice(1).replaceAll('/', ' › ')"
      :count="resources.length"
    >
      <GenericResourceCard
        v-for="resource in resources"
        :key="resource.id"
        :resource="resource"
        :mode="mode"
        @requestWorkspaceRefresh="refreshWorkspace"
      />
    </GenericResourceSection>
  </ResourceGrid>

  <!-- the context menu for the whole page area is provided by App.vue -->
  <Teleport to="#page-context-menu" defer>
    <ResourcesPageContextMenu
      :data="props.data"
      :refreshWorkspace="props.refreshWorkspace"
      v-model:mode="mode"
      v-model:sortName="sortName"
      v-model:sortOrder="sortOrder"
      v-model:terminalServersFilter="terminalServersFilter"
    />
  </Teleport>
</template>

<style scoped>
  .titlebar-row {
    user-select: none;
  }

  .resource-grid:not(.details-wrapper) {
    padding-bottom: 36px;
  }
</style>
