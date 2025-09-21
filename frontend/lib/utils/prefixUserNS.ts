import { useCoreDataStore } from '$stores';

/**
 * Prefix a key with the current user's namespace.
 *
 * This is useful for localStorage keys to avoid collisions between users.
 */
export function prefixUserNS(key: string) {
  const { userNamespace } = useCoreDataStore();
  return `${userNamespace}::${key}`;
}
