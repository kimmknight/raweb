import { useCoreDataStore } from '$stores';
import { prefixUserNS } from '$utils';
import { isBrowser } from '$utils/environment.ts';
import { computed, ref } from 'vue';

export function createWritableBooleanSetting(
  storageKey: string,
  policyKey: keyof ReturnType<typeof useCoreDataStore>['policies'],
  defaultValue: boolean
) {
  if (!isBrowser) {
    return computed({
      get: () => defaultValue,
      set: (_newValue) => {},
    });
  }

  const boolTrigger = ref(0);
  function boolRefresh() {
    boolTrigger.value++;
  }

  const enabled = computed({
    get: () => {
      const { policies } = useCoreDataStore();

      // apply the policy from Web.config if it exists
      if (
        policies[policyKey] !== null &&
        policies[policyKey] !== undefined &&
        typeof policies[policyKey] === 'boolean'
      ) {
        return policies[policyKey];
      }

      // otherwise, use localStorage
      boolTrigger.value;
      const storageValue = localStorage.getItem(prefixUserNS(storageKey));
      if (storageValue === null) {
        return defaultValue;
      }
      return storageValue === 'true';
    },
    set: (newValue) => {
      localStorage.setItem(prefixUserNS(storageKey), String(newValue));
      boolRefresh();
    },
  });

  window.addEventListener('storage', (event) => {
    if (event.key === prefixUserNS(storageKey)) {
      boolRefresh();
    }
  });

  return enabled;
}
