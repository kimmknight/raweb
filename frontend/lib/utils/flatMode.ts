import { useCoreDataStore } from '$stores';
import { prefixUserNS } from '$utils';
import { computed, ref } from 'vue';

const storageKey = `flat-mode:enabled`;

const boolTrigger = ref(0);
function boolRefresh() {
  boolTrigger.value++;
}

export const flatModeEnabled = computed({
  get: () => {
    const { policies } = useCoreDataStore();

    // apply the policy from Web.config if it exists
    if (policies.flatModeEnabled !== null) {
      return policies.flatModeEnabled;
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

window.addEventListener('storage', (event) => {
  if (event.key === prefixUserNS(storageKey)) {
    boolRefresh();
  }
});
