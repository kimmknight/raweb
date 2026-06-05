import { useCoreDataStore } from '$stores';

/**
 * Prefix a key with the current user's namespace.
 *
 * This is useful for localStorage keys to avoid collisions between users.
 */
export function prefixUserNS(key: string) {
  const { userNamespace } = useCoreDataStore();

  const prefix = `${userNamespace ?? ''}::`;
  const withPrefix = new String(prefix + key);

  Object.defineProperty(withPrefix, 'prefix', { value: prefix });
  Object.defineProperty(withPrefix, 'key', { value: key });
  Object.defineProperty(withPrefix, 'userNamespace', { value: userNamespace || undefined });

  return withPrefix as string & {
    prefix: string;
    key: string;
    userNamespace: string | undefined;
  };
}
