import { computed, ref } from 'vue';

const hidePortsEnabledKey = `${window.__namespace}::hide-ports:enabled`;

const boolTrigger = ref(0);
function boolRefresh() {
  boolTrigger.value++;
}

const hidePortsEnabled = computed({
  get: () => {
    // apply the policy from Web.config if it exists
    if (window.__policies?.hidePortsEnabled) {
      return window.__policies.hidePortsEnabled === 'true';
    }

    // otherwise, use localStorage
    boolTrigger.value;
    const storageValue = localStorage.getItem(hidePortsEnabledKey);
    return storageValue === 'true'; // default to false if not set
  },
  set: (newValue) => {
    localStorage.setItem(hidePortsEnabledKey, String(newValue));
    boolRefresh();
  },
});

window.addEventListener('storage', (event) => {
  if (event.key === hidePortsEnabledKey) {
    boolRefresh();
  }
});

export { hidePortsEnabled };
