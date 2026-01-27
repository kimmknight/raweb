import { useCoreDataStore } from '$stores';
import { prefixUserNS } from '$utils';
import { isBrowser } from '$utils/environment.ts';
import { computed, ref } from 'vue';

type Resource = NonNullable<
  Awaited<ReturnType<typeof import('./getAppsAndDevices.ts').getAppsAndDevices>>
>['resources'][number];

const storageKey = `favorite-resources`;
const enabledStorageKey = `favorite-resources:enabled`;

const trigger = ref(0);
function refresh() {
  trigger.value++;
}

export const favoriteResources = computed({
  get: () => {
    trigger.value;
    const data = JSON.parse(localStorage.getItem(prefixUserNS(storageKey)) || '[]') as [
      Resource['id'],
      Resource['type'],
      Resource['hosts'][number]['id'],
    ][];

    return data;
  },
  set: (newValue) => {
    localStorage.setItem(prefixUserNS(storageKey), JSON.stringify(newValue));
    refresh();
  },
});

const boolTrigger = ref(0);
function boolRefresh() {
  boolTrigger.value++;
}

export const favoritesEnabled = computed({
  get: () => {
    const { policies } = useCoreDataStore();

    // apply the policy from Web.config if it exists
    if (policies.favoritesEnabled !== null) {
      return policies.favoritesEnabled;
    }

    // otherwise, use localStorage
    boolTrigger.value;
    const storageValue = localStorage.getItem(prefixUserNS(enabledStorageKey));
    return storageValue === 'true' || storageValue === null; // default to true if not set
  },
  set: (newValue) => {
    localStorage.setItem(prefixUserNS(enabledStorageKey), String(newValue));
    boolRefresh();
  },
});

if (isBrowser) {
  window.addEventListener('storage', (event) => {
    if (event.key === prefixUserNS(storageKey)) {
      refresh();
    }
  });
}
if (isBrowser) {
  window.addEventListener('storage', (event) => {
    if (event.key === prefixUserNS(enabledStorageKey)) {
      boolRefresh();
    }
  });
}

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
