import { computed, ref } from 'vue';

const iconBackgroundsEnabledKey = `icon-backgrounds:enabled`;

const boolTrigger = ref(0);
function boolRefresh() {
  boolTrigger.value++;
}

const iconBackgroundsEnabled = computed({
  get: () => {
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
