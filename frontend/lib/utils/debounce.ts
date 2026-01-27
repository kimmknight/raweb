/**
 * Creates a debounced version of the provided callback function.
 *
 * This function returns a 2-element array:
 * 0. The debounced function that delays invoking the callback until after
 *    the specified wait time has elapsed since the last time it was invoked.
 * 1. A function to cancel any pending invocation of the callback.
 */
export function debounce<T extends Function>(callback: T, wait = 300): [T, () => void] {
  let timeout: ReturnType<typeof setTimeout>;

  const debounced = ((...args: never[]) => {
    clearTimeout(timeout);
    timeout = setTimeout(() => callback(...args), wait);
  }) as unknown as T;

  return [debounced, () => clearTimeout(timeout)];
}
