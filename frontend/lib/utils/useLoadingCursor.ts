import { isBrowser } from '$utils/environment.ts';
import { onScopeDispose, ref, watch, watchEffect, type WatchSource } from 'vue';

// the number of active loading sources, so that one finishing does not clear the cursor of another
const activeSources = ref(0);

if (isBrowser) {
  watchEffect(() => {
    document.documentElement.classList.toggle('loading-cursor', activeSources.value > 0);
  });
}

/**
 * Shows the progress cursor everywhere while the given source is true.
 *
 * The cursor is removed when the source becomes false or when the calling scope (e.g. a page) is disposed.
 */
export function useLoadingCursor(isLoading: WatchSource<boolean>) {
  let counted = false;

  watch(
    isLoading,
    (loading) => {
      if (loading && !counted) {
        counted = true;
        activeSources.value++;
      } else if (!loading && counted) {
        counted = false;
        activeSources.value--;
      }
    },
    { immediate: true }
  );

  onScopeDispose(() => {
    if (counted) {
      counted = false;
      activeSources.value--;
    }
  });
}
