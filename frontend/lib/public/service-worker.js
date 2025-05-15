const CACHE_VERSION = 1;
const CURRENT_CACHE = `app-cache-v${CACHE_VERSION}`;
const included = [
  "resources/",
  "multiuser-resources/",
  "lib/",
  "get-image.aspx",
  "Default.aspx",
  "icon.svg",
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
    return new Response("Failed to fetch", { status: 500 });
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
  const scope = new URL(self.registration.scope).pathname;
  if (!included.some((path) => url.pathname.startsWith(scope + path))) {
    console.debug(
      "Omitted",
      event.request.url,
      "from service worker request cache"
    );
    return;
  }

  // only intercept the request if there is no no-cache header
  if (event.request.headers.get("cache-control") === "no-cache") {
    return;
  }

  // if trying to login with loginfeed.aspx, which means there was
  // a redirect to loginfeed.aspx because credentials expired,
  // we should redirect to /logoff.aspx so that a full logoff
  // can be triggered, which will clear the credentials and the cache
  if (url.pathname === "/auth/loginfeed.aspx") {
    event.respondWith(fetch("/logoff.aspx"));
    return;
  }

  start(event.request.url);

  event.respondWith(fetchWithCache(event));

  // wait fetchStatus map has at least one entry and all are false
  // before running the background fetches
  clearInterval(fetchQueueInterval);
  fetchQueueInterval = setInterval(() => {
    clients.matchAll().then((clientList) => {
      clientList.forEach((client) => {
        client.postMessage({
          type: "fetch-queue",
          backgroundFetchQueueLength: backgroundFetchQueue.length,
        });
      });
    });

    if (fetchStatus.size > 0 && [...fetchStatus.values()].every((v) => !v)) {
      clearInterval(fetchQueueInterval);
      const backgroundFetchQueueSettled = Promise.allSettled(
        backgroundFetchQueue.map(fetchAndCacheIfOk)
      ).then(() => true);
      backgroundFetchQueue = [];
      fetchStatus.clear();
    }
  }, 1000);

  clients.matchAll().then((clientList) => {
    clientList.forEach((client) => {
      client.postMessage({
        type: "fetch-queue",
        backgroundFetchQueueLength: backgroundFetchQueue.length,
      });
    });
  });
}

self.addEventListener("fetch", handleFetch);

self.addEventListener("install", (event) => {
  self.skipWaiting();
});

/** @type {Record<string, unknown>} */
const variables = {};

self.addEventListener("message", (event) => {
  if (event.data.type === "variable") {
    variables[event.data.key] = event.data.value;
  }
});
