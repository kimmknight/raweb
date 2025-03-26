<script setup lang="ts">
  import { GenericResourceCard, TextBlock } from '$components';
  import { flatModeEnabled, getAppsAndDevices } from '$utils';
  import { computed } from 'vue';

  const props = defineProps<{
    data: Awaited<ReturnType<typeof getAppsAndDevices>>;
  }>();

  const folders = computed(() => {
    if (!props.data) return [];

    if (flatModeEnabled.value) {
      return [['/', props.data.resources]] as const;
    }

    // sort the folders by name, but with some extra rules:
    // - the root folder ("/") should come first
    // - folders within other folders should come after their parent folder
    //   - e.g. "/folder1" should come before "/folder1/subfolder"
    //   - e.g. "/folder2" should come after "/folder1/subfolder"
    // - folders should be sorted alphabetically otherwise
    const sorted = Object.entries(props.data.folders).sort(([a], [b]) => {
      if (a === '/') return -1; // root folder comes first
      if (b === '/') return 1; // root folder comes first
      if (a.startsWith(b)) return 1; // parent folder comes before child folder
      if (b.startsWith(a)) return -1; // child folder comes after parent folder
      return a.localeCompare(b); // otherwise sort alphabetically
    });

    // provide a list of resources for folders that have at least one app
    return sorted.filter(([_, resources]) => resources.length > 0);
  });
</script>

<template>
  <div class="titlebar-row">
    <TextBlock variant="title" tag="h1">Apps and desktops</TextBlock>
  </div>

  <section v-for="([folderName, resources], index) in folders" :key="index" class="folder-section">
    <div class="section-title-row" v-if="folderName !== '/'">
      <TextBlock variant="bodyStrong" tag="h2">{{ folderName.slice(1).replaceAll('/', ' › ') }}</TextBlock>
    </div>
    <div class="grid">
      <GenericResourceCard
        v-for="(resource, resourceIndex) in resources"
        :key="resourceIndex"
        :resource="resource"
      />
    </div>
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

  .grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
  }
</style>
