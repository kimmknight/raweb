/**
 * Resolves a return URL to an absolute URL with the same origin as the current page.
 *
 * This prevents `javascript:` URLs from running script in this origin and prevents
 * redirects to other sites.
 *
 * If the return URL is missing or not allowed, the fallback URL is returned instead.
 */
export function getSafeReturnUrl(value: string | null | undefined, fallback: string): string {
  const fallbackHref = new URL(fallback, window.location.origin).href;
  if (!value) {
    return fallbackHref;
  }

  try {
    const url = new URL(value, window.location.origin);
    if (url.protocol !== 'http:' && url.protocol !== 'https:') {
      return fallbackHref;
    }
    if (url.origin !== window.location.origin) {
      return fallbackHref;
    }
    return url.href;
  } catch {
    return fallbackHref;
  }
}
