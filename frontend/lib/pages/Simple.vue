<script setup lang="ts">
  import {
    createHeaderActionModelRefs,
    GenericResourceCard,
    HeaderActions,
    ResourceGrid,
    TextBlock,
  } from '$components';
  import { flatModeEnabled, getAppsAndDevices } from '$utils';
  import { computed } from 'vue';

  type Resource = NonNullable<
    Awaited<ReturnType<typeof import('$utils').getAppsAndDevices>>
  >['resources'][number];

  const props = defineProps<{
    data: Awaited<ReturnType<typeof getAppsAndDevices>>;
  }>();

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
    <TextBlock variant="title" tag="h1">Apps and desktops</TextBlock>
    <HeaderActions
      :data="props.data"
      v-model:mode="mode"
      v-model:sortName="sortName"
      v-model:sortOrder="sortOrder"
      v-model:terminalServersFilter="terminalServersFilter"
      v-model:query="query"
      searchPlaceholder="Search apps and desktops"
    />
  </div>

  <section v-for="[folderName, resources] in folders" :key="folderName" class="folder-section">
    <div class="section-title-row" v-if="folderName !== '/'">
      <TextBlock variant="bodyStrong" tag="h2">{{ folderName.slice(1).replaceAll('/', ' › ') }}</TextBlock>
    </div>
    <ResourceGrid :mode="mode">
      <GenericResourceCard v-for="resource in resources" :key="resource.id" :resource="resource" :mode="mode" />
    </ResourceGrid>
  </section>
</template>

<style scoped>
  .titlebar-row,
  section {
    user-select: none;
  }

  section {
    margin: 24px 0 8px 0;
  }
  section:last-of-type {
    padding-bottom: 36px;
  }

  section > div {
    display: flex;
    flex-direction: row;
    flex-wrap: wrap;
    gap: 16px;
  }

  .section-title-row {
    margin: 24px 0 8px 0;
  }
</style>
