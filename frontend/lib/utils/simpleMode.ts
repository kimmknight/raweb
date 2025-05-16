import { computed, ref } from 'vue';

const simpleModeEnabledKey = `${window.__namespace}::simple-mode:enabled`;

const boolTrigger = ref(0);
function boolRefresh() {
  boolTrigger.value++;
}

const simpleModeEnabled = computed({
  get: () => {
    // apply the policy from Web.config if it exists
    if (window.__policies?.simpleModeEnabled) {
      return window.__policies.simpleModeEnabled === 'true';
    }

    // otherwise, use localStorage
    boolTrigger.value;
    const storageValue = localStorage.getItem(simpleModeEnabledKey);
    return storageValue === 'true'; // default to false if not set
  },
  set: (newValue) => {
    localStorage.setItem(simpleModeEnabledKey, String(newValue));
    boolRefresh();
  },
});

window.addEventListener('storage', (event) => {
  if (event.key === simpleModeEnabledKey) {
    boolRefresh();
  }
});

export { simpleModeEnabled };
