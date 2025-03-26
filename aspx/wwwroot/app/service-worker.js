const CACHE_VERSION = 1;
const CURRENT_CACHE = `app-cache-v${CACHE_VERSION}`;
const omitted = ['/app/'];

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
    const shouldLog = request.url.includes('Logi');
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
    console.log(error);
    return new Response('Failed to fetch', { status: 500 });
  }
}

/**
 * @param {FetchEvent} event
 */
async function fetchWithCache(event) {
  const cache = await caches.open(CURRENT_CACHE);
  const response = await cache.match(event.request);
  if (!!response) {
    // revalidate the response in the background (to be available on next load)
    backgroundFetchQueue.push(event.request.clone());
    // return the cached response
    done(event.request.url);
    return response;
  } else {
    // it was not cached yet
    return fetchAndCacheIfOk(event.request);
  }
}

let fetchQueueInterval = null;

/**
 * @param {FetchEvent} event
 */
function handleFetch(event) {
  if (omitted.some((path) => new URL(event.request.url).pathname.endsWith(path))) {
    console.log('Omitted', event.request.url);
    return;
  }

  start(event.request.url);

  // only intercept the request if there is no no-cache header
  if (event.request.headers.get('cache-control') !== 'no-cache') {
    event.respondWith(fetchWithCache(event));
  }

  // wait fetchStatus map has at least one entry and all are false
  // before running the background fetches
  clearInterval(fetchQueueInterval);
  fetchQueueInterval = setInterval(() => {
    if (fetchStatus.size > 0 && [...fetchStatus.values()].every((v) => !v)) {
      clearInterval(fetchQueueInterval);
      backgroundFetchQueue.forEach(fetchAndCacheIfOk);
      backgroundFetchQueue = [];
      fetchStatus.clear();
    }
  }, 1000);
}

self.addEventListener('fetch', handleFetch);

self.addEventListener('install', (event) => {
  self.skipWaiting();
});
