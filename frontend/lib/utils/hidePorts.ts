import { useCoreDataStore } from '$stores';
import { prefixUserNS } from '$utils';
import { isBrowser } from '$utils/environment.ts';
import { computed, ref } from 'vue';

const storageKey = `hide-ports:enabled`;

const boolTrigger = ref(0);
function boolRefresh() {
  boolTrigger.value++;
}

export const hidePortsEnabled = computed({
  get: () => {
    const { policies } = useCoreDataStore();

    // apply the policy from Web.config if it exists
    if (policies.hidePortsEnabled !== null) {
      return policies.hidePortsEnabled;
    }

    // otherwise, use localStorage
    boolTrigger.value;
    const storageValue = localStorage.getItem(prefixUserNS(storageKey));
    return storageValue === 'true'; // default to false if not set
  },
  set: (newValue) => {
    localStorage.setItem(prefixUserNS(storageKey), String(newValue));
    boolRefresh();
  },
});

if (isBrowser) {
  window.addEventListener('storage', (event) => {
    if (event.key === prefixUserNS(storageKey)) {
      boolRefresh();
    }
  });
}
