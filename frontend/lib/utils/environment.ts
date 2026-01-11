const isBrowser = typeof window !== 'undefined';
const isServer = typeof window === 'undefined';

export { isBrowser, isServer };
