/**
 * Checks whether a string is a valid URL.
 */
export function isUrl(value: string, { requireTopLevelDomain = false } = {}): boolean {
  try {
    const url = new URL(value);
    if (requireTopLevelDomain && !url.hostname.includes('.')) {
      return false;
    }
    return true;
  } catch {
    return false;
  }
}
