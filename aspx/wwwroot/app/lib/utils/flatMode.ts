import { computed, ref } from 'vue';

const flatModeEnabledKey = `${window.__namespace}::flat-mode:enabled`;

const boolTrigger = ref(0);
function boolRefresh() {
  boolTrigger.value++;
}

const flatModeEnabled = computed({
  get: () => {
    // apply the policy from Web.config if it exists
    if (window.__policies?.flatModeEnabled) {
      return window.__policies.flatModeEnabled === 'true';
    }

    // otherwise, use localStorage
    boolTrigger.value;
    const storageValue = localStorage.getItem(flatModeEnabledKey);
    return storageValue === 'true'; // default to false if not set
  },
  set: (newValue) => {
    localStorage.setItem(flatModeEnabledKey, String(newValue));
    boolRefresh();
  },
});

window.addEventListener('storage', (event) => {
  if (event.key === flatModeEnabledKey) {
    boolRefresh();
  }
});

export { flatModeEnabled };
