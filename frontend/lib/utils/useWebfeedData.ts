import { prefixUserNS } from '$utils';
import { parse, stringify } from 'devalue';
import { computed, ref, WritableComputedRef } from 'vue';
import { getAppsAndDevices } from './getAppsAndDevices.ts';

const storageKey = `getAppsAndDevices:data`;

const trigger = ref(0);

const data = computed<Awaited<ReturnType<typeof getAppsAndDevices>> | null>({
  get: () => {
    trigger.value;
    const storageValue = localStorage.getItem(prefixUserNS(storageKey));
    if (storageValue) {
      try {
        const deserialized = parse(storageValue, {
          URL: (href: string) => new URL(href),
        });
        return deserialized;
      } catch {}
    }
    return null;
  },
  set: (value) => {
    if (value) {
      const serialized = stringify(value, {
        URL: (value: unknown) => value instanceof URL && value.href,
      });
      localStorage.setItem(prefixUserNS(storageKey), serialized);
    } else {
      localStorage.removeItem(prefixUserNS(storageKey));
    }
    trigger.value++;
  },
});

window.addEventListener('storage', (event) => {
  if (event.key === prefixUserNS(storageKey)) {
    trigger.value++;
  }
});

const loading = ref(false);
const error = ref<unknown>();

async function getData(base?: string, { mergeTerminalServers = true, hidePortsWhenPossible = false } = {}) {
  loading.value = true;

  return getAppsAndDevices(base, { mergeTerminalServers, redirect: true, hidePortsWhenPossible })
    .then((result) => {
      data.value = result;
      error.value = null;
    })
    .catch((err) => {
      console.error('Error fetching apps and devices:', err);
      error.value = err;
    })
    .finally(() => {
      loading.value = false;
    });
}

const hasRunAtLeastOnce = ref(false);

interface UseWebfeedDataOptions {
  mergeTerminalServers?: WritableComputedRef<boolean>;
  hidePortsWhenPossible?: WritableComputedRef<boolean>;
}

export function useWebfeedData(
  base?: string,
  { mergeTerminalServers, hidePortsWhenPossible }: UseWebfeedDataOptions = {}
) {
  // whenever this function is first called,
  // update the data, even if it is cached
  // in localStorage
  if (!hasRunAtLeastOnce.value) {
    getData(base, {
      mergeTerminalServers: mergeTerminalServers?.value,
      hidePortsWhenPossible: hidePortsWhenPossible?.value,
    });
    hasRunAtLeastOnce.value = true;
  }

  return {
    data,
    loading,
    error,
    refresh: async ({ mergeTerminalServers, hidePortsWhenPossible }: UseWebfeedDataOptions = {}) => {
      await getData(base, {
        mergeTerminalServers: mergeTerminalServers?.value,
        hidePortsWhenPossible: hidePortsWhenPossible?.value,
      });
      return { data, loading, error };
    },
  };
}
