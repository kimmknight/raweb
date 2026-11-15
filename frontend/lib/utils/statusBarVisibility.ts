import { useCoreDataStore } from '$stores';
import { isBrowser } from '$utils/environment.ts';
import { prefixUserNS } from '$utils/prefixUserNS';
import { computed, ref } from 'vue';

export const statusBarVisibilityOptions = ['never', 'always', 'details-only'] as const;
export type StatusBarVisibility = (typeof statusBarVisibilityOptions)[number];

const storageKey = 'status-bar:visibility';
const defaultValue: StatusBarVisibility = 'details-only';

function isStatusBarVisibility(value: unknown): value is StatusBarVisibility {
  return statusBarVisibilityOptions.includes(value as StatusBarVisibility);
}

const storageTrigger = ref(0);

if (isBrowser) {
  window.addEventListener('storage', (event) => {
    if (event.key === prefixUserNS(storageKey)) {
      storageTrigger.value++;
    }
  });
}

/**
 * When the status bar appears. The `App.StatusBarVisibility` policy overrides the user's choice.
 */
export const statusBarVisibility = computed<StatusBarVisibility>({
  get: () => {
    const { policies } = useCoreDataStore();
    if (isStatusBarVisibility(policies.statusBarVisibility)) {
      return policies.statusBarVisibility;
    }

    if (!isBrowser) {
      return defaultValue;
    }
    storageTrigger.value;
    const storedValue = localStorage.getItem(prefixUserNS(storageKey));
    return isStatusBarVisibility(storedValue) ? storedValue : defaultValue;
  },
  set: (newValue) => {
    if (!isBrowser) {
      return;
    }
    localStorage.setItem(prefixUserNS(storageKey), newValue);
    storageTrigger.value++;
  },
});

/**
 * Whether the status bar should appear for a page that is in the given view mode.
 */
export function isStatusBarVisible(mode?: string) {
  return (
    statusBarVisibility.value === 'always' ||
    (statusBarVisibility.value === 'details-only' && mode === 'details')
  );
}
