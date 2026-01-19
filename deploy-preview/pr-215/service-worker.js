// Built timestamp: 2026-01-19T08:04:48.602Z
const SERVICE_WORKER_VERSION = 2;
const CURRENT_CACHE = `app-cache-v${SERVICE_WORKER_VERSION}`;

// only cache these path prefixes for offline use
// (require a fresh response when online)
const offlineOnlyPathnamePrefixes = ['api/app-init-details', 'manifest.webmanifest'];

// skip caching for these paths (e.g., unbundled dev mode paths)
// const omiitedPathnamePrefixes = ['node_modules/', '@vite/', '@id/'];
const omiitedPathnamePrefixes = [];

// these are the HTML entry points of the app
// and should immediately be cached for offline use
// and upon subsequent page reloads
const htmlEntryPoints = [
  '/',
  '/favorites',
  '/simple',
  '/settings',
  '/apps',
  '/devices',
  '/policies',
  '/login',
  '/logoff',
  '/password',
];

/** @type {Request[]} */
let backgroundFetchQueue = [];
/** @type {Map<string, boolean>} */
const fetchStatus = new Map();
function start(href) {
  if (fetchStatus) fetchStatus.set(href, true);
}
function done(href) {
  if (fetchStatus) fetchStatus.set(href, false);
}

/**
 * @param {Request} request
 */
async function fetchAndCacheIfOk(request) {
  try {
    const response = await fetch(request).finally(() => {
      done(request.url);
    });

    if (response.ok) {
      const responseClone = response.clone();
      const cache = await caches.open(CURRENT_CACHE);
      await cache.put(request, responseClone);
    }

    return response;
  } catch (error) {
    done(request.url);
    console.error(error);
    return new Response('Failed to fetch', { status: 500 });
  }
}

/**
 * @param {FetchEvent} event
 * @param {'swr' | 'offline'} [mode]
 */
async function fetchWithCache(event, mode = 'swr') {
  const cache = await caches.open(CURRENT_CACHE);
  const cachedResponse = await cache.match(event.request);

  // if the response is not cached, fetch and cache it
  if (!cachedResponse) {
    return fetchAndCacheIfOk(event.request);
  }

  // in case of stale-while-revalidate, we want to
  // revalidate the response in the background
  // (to be available on next load)
  if (mode === 'swr') {
    backgroundFetchQueue.push(event.request.clone());
  }

  // in the case of the offline-only cache, we always want
  // a fresh response unless we are offline or the request fails
  if (mode === 'offline') {
    const freshResponse = await fetchAndCacheIfOk(event.request);
    if (freshResponse.ok) {
      done(event.request.url);
      return freshResponse;
    }
  }

  // return the cached response
  done(event.request.url);
  return cachedResponse;
}

let fetchQueueInterval = null;

/**
 * @param {FetchEvent} event
 */
function handleFetch(event) {
  const url = new URL(event.request.url);
  const scope = new URL(self.registration.scope);

  // skip out-of-scope requests
  if (
    !url.pathname.startsWith(scope.pathname) ||
    url.origin !== scope.origin ||
    event.request.method !== 'GET'
  ) {
    return;
  }

  // skip omitted paths
  if (omiitedPathnamePrefixes.some((path) => url.pathname.startsWith(scope.pathname + path))) {
    return;
  }

  // update the entry points in the background
  // whenever an in-scope document is requested
  if (event.request.destination === 'document') {
    updateEntryPointsCache({ background: true });
  }

  let shouldUseOfflineCache = offlineOnlyPathnamePrefixes.some((path) =>
    url.pathname.startsWith(scope.pathname + path)
  );

  // if there is a no-cache header, only cache for offline mode
  if (event.request.headers.get('cache-control') === 'no-cache') {
    shouldUseOfflineCache = true;
  }

  // redirect '/locales/en-US.json' to '/locales/en.json'
  // since en is equivalent to en-US
  if (url.pathname === '/locales/en-US.json') {
    event.respondWith(Response.redirect('/locales/en.json'));
    return;
  }

  // if trying to login with loginfeed.aspx, which means there was
  // a redirect to loginfeed.aspx because credentials expired,
  // we should redirect to /logoff so that a full logoff
  // can be triggered, which will clear the credentials and the cache
  if (url.pathname === '/auth/loginfeed.aspx') {
    event.respondWith(fetch('/logoff'));
    return;
  }

  start(event.request.url);
  event.respondWith(fetchWithCache(event, shouldUseOfflineCache ? 'offline' : 'swr'));

  // wait fetchStatus map has at least one entry and all are false
  // before running the background fetches
  clearInterval(fetchQueueInterval);
  fetchQueueInterval = setInterval(() => {
    clients.matchAll().then((clientList) => {
      clientList.forEach((client) => {
        client.postMessage({
          type: 'fetch-queue',
          backgroundFetchQueueLength: backgroundFetchQueue.length,
        });
      });
    });

    if (fetchStatus.size > 0 && [...fetchStatus.values()].every((v) => !v)) {
      clearInterval(fetchQueueInterval);
      const backgroundFetchQueueSettled = Promise.allSettled(backgroundFetchQueue.map(fetchAndCacheIfOk)).then(
        () => true
      );
      backgroundFetchQueue = [];
      fetchStatus.clear();
    }
  }, 1000);

  clients.matchAll().then((clientList) => {
    clientList.forEach((client) => {
      client.postMessage({
        type: 'fetch-queue',
        backgroundFetchQueueLength: backgroundFetchQueue.length,
      });
    });
  });
}

/**
 * Fetches and caches the HTML entry points.
 *
 * If `background` is `true`, the requests are added to the background fetch queue.
 * They will be fetched and cached when the fetch queue is processed.
 *
 * @param {{ background?: boolean }} options
 */
async function updateEntryPointsCache({ background = false } = {}) {
  for await (const path of htmlEntryPoints) {
    const url = new URL(path, self.location.origin);
    const request = new Request(url.href);
    if (background) {
      backgroundFetchQueue.push(request);
      return;
    }
    await fetchAndCacheIfOk(request);
  }
}

self.addEventListener('fetch', handleFetch);

self.addEventListener('install', (event) => {
  self.skipWaiting();
});

self.addEventListener('activate', async (event) => {
  // delete old caches
  const cacheNames = await caches.keys();
  await Promise.all(
    cacheNames.map((cacheName) => {
      return caches.delete(cacheName);
    })
  );

  // cache HTML entry points
  updateEntryPointsCache();

  // take control of all clients immediately
  self.clients.claim();
});

/** @type {Record<string, unknown>} */
const variables = {};

self.addEventListener('message', (event) => {
  if (event.data.type === 'variable') {
    variables[event.data.key] = event.data.value;
  }
});
