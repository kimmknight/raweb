export function debounce(callback: Function, wait = 300) {
  let timeout: ReturnType<typeof setTimeout>;

  const debounced = (...args: any[]) => {
    clearTimeout(timeout);
    timeout = setTimeout(() => callback(...args), wait);
  };

  return [debounced, () => clearTimeout(timeout)];
}
