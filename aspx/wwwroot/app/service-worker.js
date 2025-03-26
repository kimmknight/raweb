const CACHE_VERSION = 1;
const CURRENT_CACHE = `app-cache-v${CACHE_VERSION}`;
const omitted = ['/app/', '/app/manifest.json', '/app/service-worker.js', '/app/login.aspx'];

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
  const url = new URL(event.request.url);
  if (
    (!url.pathname.includes('/app/') &&
      !url.pathname.includes('/multiuser-resources/') &&
      !url.pathname.includes('/resources/') &&
      !url.pathname.endsWith('/webfeed.aspx')) ||
    omitted.some((path) => url.pathname.endsWith(path))
  ) {
    console.debug('Omitted', event.request.url, 'from service worker request cache');
    return;
  }

  // only intercept the request if there is no no-cache header
  if (event.request.headers.get('cache-control') === 'no-cache') {
    return;
  }

  start(event.request.url);

  event.respondWith(fetchWithCache(event));

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
