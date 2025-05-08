<script setup lang="ts">
  import {
    createHeaderActionModelRefs,
    DesktopCard,
    GenericResourceCard,
    HeaderActions,
    ResourceGrid,
    TextBlock,
  } from '$components';
  import { getAppsAndDevices } from '$utils';
  import { computed } from 'vue';

  const props = defineProps<{
    data: Awaited<ReturnType<typeof getAppsAndDevices>>;
  }>();

  const desktops = computed(() => {
    if (!props.data) return [];

    const desktops = props.data.resources.filter((resource) => resource.type === 'Desktop');

    // return separate dekstops for each host
    const everyDesktop = desktops.flatMap(({ hosts, ...resource }) => {
      return hosts.map((host) => {
        return {
          ...resource,
          hosts: [host], // only keep the current host for this desktop
        };
      });
    });

    return organize(everyDesktop, sortName.value, sortOrder.value, terminalServersFilter.value, query.value);
  });

  const { mode, sortName, sortOrder, terminalServersFilter, query, organize } = createHeaderActionModelRefs({
    defaults: { mode: 'card' },
    persist: 'desktops',
  });
</script>

<template>
  <div class="titlebar-row">
    <TextBlock variant="title" tag="h1">Devices</TextBlock>
    <HeaderActions
      :data="props.data"
      v-model:mode="mode"
      v-model:sortName="sortName"
      v-model:sortOrder="sortOrder"
      v-model:query="query"
      searchPlaceholder="Search devices"
    />
  </div>

  <section>
    <div v-if="mode === 'card'">
      <DesktopCard v-for="resource in desktops" :key="resource.id" :resource="resource" />
    </div>
    <ResourceGrid :mode="mode" v-else>
      <GenericResourceCard v-for="resource in desktops" :key="resource.id" :resource="resource" :mode="mode" />
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
</style>
