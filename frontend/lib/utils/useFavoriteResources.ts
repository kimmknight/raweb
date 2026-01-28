import { prefixUserNS } from '$utils';
import { isBrowser } from '$utils/environment.ts';
import { computed, ref } from 'vue';
import { createWritableBooleanSetting } from './createBooleanWritableSetting';

type Resource = NonNullable<
  Awaited<ReturnType<typeof import('./getAppsAndDevices.ts').getAppsAndDevices>>
>['resources'][number];

const storageKey = `favorite-resources`;

const trigger = ref(0);
function refresh() {
  trigger.value++;
}

export const favoriteResources = computed({
  get: () => {
    if (!isBrowser) {
      return [];
    }

    trigger.value;
    const data = JSON.parse(localStorage.getItem(prefixUserNS(storageKey)) || '[]') as [
      Resource['id'],
      Resource['type'],
      Resource['hosts'][number]['id'],
    ][];

    return data;
  },
  set: (newValue) => {
    if (!isBrowser) {
      return;
    }

    localStorage.setItem(prefixUserNS(storageKey), JSON.stringify(newValue));
    refresh();
  },
});

export const favoritesEnabled = createWritableBooleanSetting(
  'favorite-resources:enabled',
  'favoritesEnabled',
  true
);

if (isBrowser) {
  window.addEventListener('storage', (event) => {
    if (event.key === prefixUserNS(storageKey)) {
      refresh();
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
