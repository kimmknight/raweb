import { parse, stringify } from 'devalue';
import { computed, ref } from 'vue';
import { getAppsAndDevices } from './getAppsAndDevices.ts';

const storageKey = 'getAppsAndDevices:data';

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

async function getData(base?: string) {
  loading.value = true;

  return getAppsAndDevices(base)
    .then((result) => {
      data.value = result;
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

export function useWebfeedData(base?: string) {
  // whenever this function is first called,
  // update the data, even if it is cached
  // in localStorage
  if (!hasRunAtLeastOnce.value) {
    getData(base);
    hasRunAtLeastOnce.value = true;
  }
  return { data, loading, error, refresh: () => getData(base) };
}
