import { parse, stringify } from 'devalue';
import { computed, ref, WritableComputedRef } from 'vue';
import { getAppsAndDevices } from './getAppsAndDevices.ts';

const storageKey = `${window.__namespace}::getAppsAndDevices:data`;

const trigger = ref(0);

const data = computed<Awaited<ReturnType<typeof getAppsAndDevices>> | null>({
  get: () => {
    trigger.value;
    const storageValue = localStorage.getItem(storageKey);
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
      localStorage.setItem(storageKey, serialized);
    } else {
      localStorage.removeItem(storageKey);
    }
    trigger.value++;
  },
});

window.addEventListener('storage', (event) => {
  if (event.key === storageKey) {
    trigger.value++;
  }
});

const loading = ref(false);
const error = ref<unknown>();

async function getData(base?: string, { mergeTerminalServers = true } = {}) {
  loading.value = true;

  return getAppsAndDevices(base, { mergeTerminalServers })
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
}

export function useWebfeedData(base?: string, { mergeTerminalServers }: UseWebfeedDataOptions = {}) {
  // whenever this function is first called,
  // update the data, even if it is cached
  // in localStorage
  if (!hasRunAtLeastOnce.value) {
    getData(base, { mergeTerminalServers: mergeTerminalServers?.value });
    hasRunAtLeastOnce.value = true;
  }

  return {
    data,
    loading,
    error,
    refresh: async ({ mergeTerminalServers }: UseWebfeedDataOptions = {}) => {
      await getData(base, { mergeTerminalServers: mergeTerminalServers?.value });
      return { data, loading, error };
    },
  };
}
