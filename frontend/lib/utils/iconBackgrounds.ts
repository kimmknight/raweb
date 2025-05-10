import { computed, ref } from 'vue';

const iconBackgroundsEnabledKey = `${window.__namespace}::icon-backgrounds:enabled`;

const boolTrigger = ref(0);
function boolRefresh() {
  boolTrigger.value++;
}

const iconBackgroundsEnabled = computed({
  get: () => {
    // apply the policy from Web.config if it exists
    if (window.__policies?.iconBackgroundsEnabled) {
      return window.__policies.iconBackgroundsEnabled === 'true';
    }

    // otherwise, use localStorage
    boolTrigger.value;
    const storageValue = localStorage.getItem(iconBackgroundsEnabledKey);
    return storageValue === 'true' || storageValue === null; // default to true if not set
  },
  set: (newValue) => {
    localStorage.setItem(iconBackgroundsEnabledKey, String(newValue));
    boolRefresh();
  },
});

window.addEventListener('storage', (event) => {
  if (event.key === iconBackgroundsEnabledKey) {
    boolRefresh();
  }
});

export { iconBackgroundsEnabled };
