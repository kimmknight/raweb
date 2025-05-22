import { computed, ref } from 'vue';

type Resource = NonNullable<
  Awaited<ReturnType<typeof import('./getAppsAndDevices').getAppsAndDevices>>
>['resources'][number];

const favoriteResourcesKey = `${window.__namespace}::favorite-resources`;
const favoriteResourcesEnabledKey = `${window.__namespace}::favorite-resources:enabled`;

const trigger = ref(0);
function refresh() {
  trigger.value++;
}

const favoriteResources = computed({
  get: () => {
    trigger.value;
    const data = JSON.parse(localStorage.getItem(favoriteResourcesKey) || '[]') as [
      Resource['id'],
      Resource['type'],
      Resource['hosts'][number]['id']
    ][];
    return data;
  },
  set: (newValue) => {
    localStorage.setItem(favoriteResourcesKey, JSON.stringify(newValue));
    refresh();
  },
});

const boolTrigger = ref(0);
function boolRefresh() {
  boolTrigger.value++;
}

const favoriteResourcesEnabled = computed({
  get: () => {
    // apply the policy from Web.config if it exists
    if (window.__policies?.favoritesEnabled) {
      return window.__policies.favoritesEnabled === 'true';
    }

    // otherwise, use localStorage
    boolTrigger.value;
    const storageValue = localStorage.getItem(favoriteResourcesEnabledKey);
    return storageValue === 'true' || storageValue === null; // default to true if not set
  },
  set: (newValue) => {
    localStorage.setItem(favoriteResourcesEnabledKey, String(newValue));
    boolRefresh();
  },
});

window.addEventListener('storage', (event) => {
  if (event.key === favoriteResourcesKey) {
    refresh();
  }
});
window.addEventListener('storage', (event) => {
  if (event.key === favoriteResourcesEnabledKey) {
    boolRefresh();
  }
});

export function useFavoriteResources() {
  return { favoriteResources, refresh };
}

export function useFavoriteResourceTerminalServers(resource: Resource) {
  const favoriteTerminalServers = computed({
    get: () => {
      const data = favoriteResources.value;
      return data
        .filter(([resourceId]) => resourceId === resource.id)
        .map(([, , terminalServerId]) => terminalServerId);
    },
    set: (terminalServerIds) => {
      const otherResouces = favoriteResources.value.filter(([resourceId]) => resourceId !== resource.id);
      const newData = terminalServerIds.map((terminalServerId) => [
        resource.id,
        resource.type,
        terminalServerId,
      ]) satisfies typeof otherResouces;
      favoriteResources.value = [...otherResouces, ...newData];
    },
  });

  function setFavorite(terminalServerId: string, isFavorite: boolean) {
    const currentFavorites = favoriteTerminalServers.value;
    if (isFavorite) {
      if (!currentFavorites.includes(terminalServerId)) {
        favoriteTerminalServers.value = [...currentFavorites, terminalServerId];
      }
    } else {
      favoriteTerminalServers.value = currentFavorites.filter((id) => id !== terminalServerId);
    }
    refresh();
  }

  return { favoriteTerminalServers, refresh, setFavorite };
}

export { favoriteResourcesEnabled as favoritesEnabled };
