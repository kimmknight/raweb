<script setup lang="ts">
  import { DesktopCard, TextBlock } from '$components';
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

    console.log(everyDesktop.map((d) => d.hosts[0]));
    return everyDesktop;
  });
</script>

<template>
  <div class="titlebar-row">
    <TextBlock variant="title" tag="h1">Devices</TextBlock>
  </div>

  <section>
    <div>
      <DesktopCard v-for="(resource, index) in desktops" :key="index" :resource="resource" />
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
</style>
