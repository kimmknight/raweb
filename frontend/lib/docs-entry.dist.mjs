import { entranceIn, fadeOut } from '$utils/transitions';
import { createPinia } from 'pinia';
import { createApp, reactive } from 'vue';
import { createRouter, createWebHistory } from 'vue-router';
import NotFound from './404.vue';
import Documentation from './Documentation.vue';
import i18n, { i18nextPromise } from './i18n.ts';
import { useCoreDataStore } from './stores/index.mjs';

const app = i18n(createApp(Documentation));
const t = await i18nextPromise;

/** @type {Record<string, Record<string, unknown>>} */
const docsPages = import.meta.glob('../docs/**/*.md', { eager: true });

const docsMarkdownRoutes = await Promise.all(
  Object.entries(docsPages).map(async ([path, { default: Component, ...frontmatter }]) => {
    let name = path.replace('../docs/', '').replace('/index.md', '/').toLowerCase();
    if (name === 'index.md') {
      name = 'index';
    }

    // if the frontmatter contains $t, look for translation keys and replace them
    if (frontmatter) {
      for (const [key, value] of Object.entries(frontmatter)) {
        if (!value || typeof value !== 'string' || !value.includes('$t{{')) {
          continue;
        }

        // the $t{{ some.key }} syntax may appear multiple times in a string,
        // so extract and replace all matches
        const regex = /\$t\{\{\s*([^\}]+)\s*\}\}/g;
        frontmatter[key] = value.replaceAll(regex, (_, translationKey) => {
          const translation = t(translationKey.trim(), { lng: 'en-US' });
          return translation;
        });
      }
    }

    return {
      path:
        '/docs/' +
        (name === 'index'
          ? ''
          : name
              .replace('(user-guide)/', '')
              .replace('(administration)/', '')
              .replace('(welcome)/', '')
              .replace('(development)/', '')),
      name,
      meta: {
        ...frontmatter,
      },
      component: Component || NotFound,
    };
  })
);

const history = createWebHistory();

const router = createRouter({
  history,
  routes: [
    // 404
    { path: '/:pathMatch(.*)*', component: NotFound, props: { variant: 'docs' } },
    // add trailing slashes to URLs
    {
      path: '/:pathMatch(.*[^/])',
      redirect: (to) => `/${to.params.pathMatch}/`,
    },
    // docs routes
    ...docsMarkdownRoutes,
  ],
  strict: true,
  scrollBehavior(to, from) {
    const container = document.querySelector('#app main');
    if (!container) return;

    // restore custom position for history navigation
    // as long as a scroll reset is not requested
    if (docsNavigationContext.restoreScrollRequested) {
      const savedPosition = scrollPositions.get(to.fullPath);
      if (savedPosition) {
        container.scrollTo(savedPosition.left, savedPosition.top);
        docsNavigationContext.restoreScrollRequested = false;
        return false;
      }
    }

    // scroll to the hash if it exists
    if (to.hash) {
      // wait for the element to exist before scrolling
      return new Promise((resolve) => {
        requestAnimationFrame(() => {
          const el = document.querySelector(to.hash);
          if (el && el instanceof HTMLElement) {
            // scroll with an offset so the element is not directly at the top of the container
            container.scrollTo(0, el.offsetTop - 32);
          }
          resolve(false); // let the browser handle it
        });
      });
    }

    // otherwise, scroll to top
    container.scrollTo(0, 0);
  },
});

// remember scroll positions for the main scroll container
const scrollPositions = new Map();
router.beforeEach((to, from) => {
  const container = document.querySelector('#app main');
  if (container && from.fullPath) {
    scrollPositions.set(from.fullPath, {
      left: container.scrollLeft,
      top: container.scrollTop,
    });
  }
});

// restore scroll positions on history navigation only
// (not clicking links or programmatic navigation)
history.listen(() => {
  docsNavigationContext.restoreScrollRequested = true;
});

/** @type {DocsNavigationContext} */
const docsNavigationContext = reactive({
  animating: false,
  restoreScrollRequested: false,
});

// page transition: fade out old content and scroll to the top of the content area
router.beforeEach(async (to, from) => {
  // whether we need to animate for this navigation
  // - note that we do not animate if only the hash changes
  const shouldAnimate = to.path !== from.path;
  if (!shouldAnimate) {
    return;
  }

  const contentElem = document.querySelector('#app main > #page');
  docsNavigationContext.animating = true;
  await fadeOut(contentElem);
});

// page transition: animate in new content
router.afterEach(async (to, from) => {
  const contentElem = document.querySelector('#app main > #page');
  await entranceIn(contentElem);
  docsNavigationContext.animating = false;
});

// add a .router-target class to an element with the target hash
router.afterEach((to) => {
  if (!to.hash) {
    return;
  }

  requestAnimationFrame(() => {
    const targetElems = document.querySelectorAll(to.hash);
    targetElems.forEach((targetElem) => {
      targetElem.classList.add('router-target');
    });
  });
});

// remove any existing .router-target classes
router.beforeEach(() => {
  const previousTargetElems = document.querySelectorAll('.router-target');
  previousTargetElems.forEach((elem) => {
    elem.classList.remove('router-target');
  });
});

router.afterEach((to) => {
  document.title = to.meta.title ? `${to.meta.title} - RAWeb Wiki` : 'RAWeb Wiki';
});

const pinia = createPinia();
await useCoreDataStore(pinia).fetchData(); // fetch core data before mounting the app

app.use(pinia);
app.use(router);
app.component('CodeBlock', (await import('$components')).CodeBlock);
app.component('PolicyDetails', (await import('$components')).PolicyDetails);
app.component('InfoBar', (await import('$components')).InfoBar);
app.provide('docsNavigationContext', docsNavigationContext);

app.directive('swap', (el, binding) => {
  if (el.parentNode) {
    el.outerHTML = binding.value;
  }
});

app.config.globalProperties.docsNavigationContext = docsNavigationContext;

await router.isReady();
app.mount('#app');
