import { computed, ref } from 'vue';

const combineTerminalServersModeEnabledKey = `${window.__namespace}::combine-terminal-servers-mode:enabled`;

const boolTrigger = ref(0);
function boolRefresh() {
  boolTrigger.value++;
}

const combineTerminalServersModeEnabled = computed({
  get: () => {
    // apply the policy from Web.config if it exists
    if (window.__policies?.combineTerminalServersModeEnabled) {
      return window.__policies.combineTerminalServersModeEnabled === 'true';
    }

    // otherwise, use localStorage
    boolTrigger.value;
    const storageValue = localStorage.getItem(combineTerminalServersModeEnabledKey);
    return storageValue === 'true' || storageValue === null; // default to true if not set
  },
  set: (newValue) => {
    localStorage.setItem(combineTerminalServersModeEnabledKey, String(newValue));
    boolRefresh();
  },
});

window.addEventListener('storage', (event) => {
  if (event.key === combineTerminalServersModeEnabledKey) {
    boolRefresh();
  }
});

export { combineTerminalServersModeEnabled };
