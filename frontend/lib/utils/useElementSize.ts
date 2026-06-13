import { onWatcherCleanup, ref, ShallowRef, watchEffect } from 'vue';

/**
 * Retreives thw width and height of an element and updates
 * whenever the element is resized.
 * @param target
 * @param initialSize
 * @returns
 */
export function useElementSize(
  target: Readonly<ShallowRef<HTMLElement | null>>,
  initialSize: { width: number; height: number } = { width: 0, height: 0 }
) {
  const size = ref(initialSize);

  watchEffect(() => {
    if (!target.value) {
      size.value = initialSize;
      return;
    }

    const observer = new ResizeObserver((entries) => {
      if (entries[0]) {
        const { width, height } = entries[0].contentRect;
        size.value = { width, height };
      }
    });

    observer.observe(target.value);
    onWatcherCleanup(() => {
      observer.disconnect();
    });
  });

  return size;
}
