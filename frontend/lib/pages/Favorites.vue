<script setup lang="ts">
  import { Button, DesktopCard, GenericResourceCard, ResourceGrid, TextBlock } from '$components';
  import { favoritesEnabled, getAppsAndDevices, useFavoriteResources } from '$utils';
  import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';

  const props = defineProps<{
    data: Awaited<ReturnType<typeof getAppsAndDevices>>;
  }>();

  const { favoriteResources } = useFavoriteResources();

  const apps = computed(() => {
    if (!props.data) return [];

    const apps = props.data.resources.filter((resource) => resource.type === 'RemoteApp');

    const favorites = favoriteResources.value.flatMap(([id, type, terminalServerId]) => {
      const found = apps.find(
        (app) => app.id === id && app.type === type && app.hosts.some((host) => host.id === terminalServerId)
      );
      if (!found) return [];

      return [
        {
          ...found,
          hosts: found.hosts.filter((host) => host.id === terminalServerId), // only keep the favorite host for this app
        },
      ];
    });

    return favorites.sort((a, b) => a.title.localeCompare(b.title)); // sort by title
  });

  const desktops = computed(() => {
    if (!props.data) return [];

    const desktops = props.data.resources.filter((resource) => resource.type === 'Desktop');

    const favorites = favoriteResources.value.flatMap(([id, type, terminalServerId]) => {
      const found = desktops.find(
        (desktops) =>
          desktops.id === id &&
          desktops.type === type &&
          desktops.hosts.some((host) => host.id === terminalServerId)
      );
      if (!found) return [];

      return [
        {
          ...found,
          hosts: found.hosts.filter((host) => host.id === terminalServerId), // only keep the favorite host for this app
        },
      ];
    });

    return favorites.sort((a, b) => a.title.localeCompare(b.title)); // sort by title
  });

  const favoritesRowSCrollElem = ref<HTMLElement | null>(null);
  function scrollLeft() {
    if (favoritesRowSCrollElem.value) {
      favoritesRowSCrollElem.value.scrollBy({ left: -200, behavior: 'smooth' });
    }
  }
  function scrollRight() {
    if (favoritesRowSCrollElem.value) {
      favoritesRowSCrollElem.value.scrollBy({ left: 200, behavior: 'smooth' });
    }
  }

  const canScrollLeft = ref(false);
  const canScrollRight = ref(false);

  function updateScrollState() {
    if (!favoritesRowSCrollElem.value) {
      canScrollLeft.value = false;
      canScrollRight.value = false;
      return;
    }
    canScrollLeft.value = favoritesRowSCrollElem.value.scrollLeft > 0;
    canScrollRight.value =
      Math.ceil(favoritesRowSCrollElem.value.scrollLeft) <
      favoritesRowSCrollElem.value.scrollWidth - favoritesRowSCrollElem.value.clientWidth;
  }

  watch(
    () => favoritesRowSCrollElem.value,
    (elem) => {
      if (elem) {
        updateScrollState();
        elem.addEventListener('scroll', updateScrollState);
      }
    }
  );

  onMounted(() => {
    updateScrollState();
    if (favoritesRowSCrollElem.value) {
      favoritesRowSCrollElem.value.addEventListener('scroll', updateScrollState);
      document.addEventListener('resize', updateScrollState);
    }
  });

  onBeforeUnmount(() => {
    if (favoritesRowSCrollElem.value) {
      favoritesRowSCrollElem.value.removeEventListener('scroll', updateScrollState);
      document.removeEventListener('resize', updateScrollState);
    }
  });

  function handleDisbleFavorites() {
    window.location.hash = '#apps';
    favoritesEnabled.value = false;
  }
</script>

<template>
  <div class="titlebar-row">
    <TextBlock variant="title">Favorites</TextBlock>
  </div>
  <section class="favorite-devices" v-if="desktops.length > 0">
    <div class="section-title-row">
      <TextBlock variant="subtitle">Devices</TextBlock>
      <Button href="#devices">
        All devices
        <template v-slot:icon-end>
          <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path
              d="M8.293 4.293a1 1 0 0 0 0 1.414L14.586 12l-6.293 6.293a1 1 0 1 0 1.414 1.414l7-7a1 1 0 0 0 0-1.414l-7-7a1 1 0 0 0-1.414 0Z"
              fill="currentColor"
            />
          </svg>
        </template>
      </Button>
    </div>
    <div class="scroll-arrow left scroll-arrow-bg" v-if="canScrollLeft"></div>
    <Button class="scroll-arrow left" @click="scrollLeft" v-if="canScrollLeft">
      <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
        <path
          d="M15 17.898c0 1.074-1.265 1.648-2.073.941l-6.31-5.522a1.75 1.75 0 0 1 0-2.634l6.31-5.522c.808-.707 2.073-.133 2.073.941v11.796Z"
          fill="currentColor"
        />
      </svg>
    </Button>
    <div ref="favoritesRowSCrollElem">
      <DesktopCard
        v-for="resource in desktops"
        :key="resource.id + resource.hosts[0].id"
        :resource="resource"
      />
    </div>
    <div class="scroll-arrow right scroll-arrow-bg" v-if="canScrollRight"></div>
    <Button class="scroll-arrow right" @click="scrollRight" v-if="canScrollRight">
      <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
        <path
          d="M9 17.898c0 1.074 1.265 1.648 2.073.941l6.31-5.522a1.75 1.75 0 0 0 0-2.634l-6.31-5.522C10.265 4.454 9 5.028 9 6.102v11.796Z"
          fill="currentColor"
        />
      </svg>
    </Button>
  </section>
  <section class="favorite-apps" v-if="apps.length > 0">
    <div class="section-title-row">
      <TextBlock variant="subtitle">Apps</TextBlock>
      <Button href="#apps">
        All apps
        <template v-slot:icon-end>
          <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path
              d="M8.293 4.293a1 1 0 0 0 0 1.414L14.586 12l-6.293 6.293a1 1 0 1 0 1.414 1.414l7-7a1 1 0 0 0 0-1.414l-7-7a1 1 0 0 0-1.414 0Z"
              fill="currentColor"
            />
          </svg>
        </template>
      </Button>
    </div>
    <ResourceGrid mode="card">
      <GenericResourceCard
        v-for="resource in apps"
        :key="resource.id + resource.hosts[0].id"
        :resource="resource"
      />
    </ResourceGrid>
  </section>

  <div class="no-favorites-notice" v-if="desktops.length === 0 && apps.length === 0">
    <TextBlock variant="subtitle">Get started with favorites</TextBlock>
    <div class="prose">
      <TextBlock block>Favoriting makes it easy to get to your most-used devices and apps.</TextBlock>
      <TextBlock block>Add to favorites via the menu button near the device or app name.</TextBlock>
    </div>
    <div class="buttons">
      <div class="button-row">
        <Button href="#devices" variant="accent">Go to devices</Button>
        <Button href="#apps" variant="accent">Go to apps</Button>
      </div>
      <Button variant="hyperlink" @click="handleDisbleFavorites">Disable favorites</Button>
    </div>
  </div>
</template>

<style scoped>
  .no-favorites-notice {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    block-size: 100%;
    gap: 16px;
    padding: 24px 16px;
    background-color: var(--wui-subtle-transparent);
    border-radius: var(--wui-control-corner-radius);
    box-sizing: border-box;
    height: calc(100% - 32px); /* 32px = height of the page title */
    text-align: center;
  }
  .no-favorites-notice .buttons {
    text-align: center;
  }
  .no-favorites-notice .prose > * {
    margin-bottom: 4px;
  }
  .no-favorites-notice .prose > *:last-child {
    margin-bottom: 2px;
  }
  .no-favorites-notice .button-row {
    display: flex;
    flex-direction: row;
    gap: 8px;
    margin-bottom: 8px;
  }

  .titlebar-row,
  section {
    user-select: none;
  }

  section {
    margin: 24px 0 8px 0;
    position: relative;
  }
  section:last-of-type {
    padding-bottom: 36px;
  }

  section > div {
    gap: 16px;
  }
  section.favorite-devices > div {
    display: flex;
    flex-direction: row;
    flex-wrap: wrap;
    flex-wrap: nowrap;
    overflow-x: auto;
    scroll-snap-type: x mandatory;
    scrollbar-width: none;
  }
  section > div::-webkit-scrollbar {
    display: none;
  }

  section > div > * {
    scroll-snap-align: start;
  }

  section.favorite-apps > div.grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  }

  .section-title-row {
    margin: 24px 0 8px 0;
    display: flex;
    align-items: center;
    justify-content: space-between;
  }

  section > .scroll-arrow {
    position: absolute;
    z-index: 10;
    top: 50%;
    transform: translateY(-50%);
    height: 32px;
    width: 20px;
    padding: 0;
  }
  section > .scroll-arrow-bg {
    background-color: var(--wui-solid-background-base);
    border-radius: var(--wui-control-corner-radius);
  }
  section > .scroll-arrow.left {
    left: -10px;
  }
  section > .scroll-arrow.right {
    right: -10px;
  }
  svg {
    inline-size: 12px;
    block-size: auto;
  }
</style>
