import { entranceIn, fadeOut } from '$utils/transitions';
import { createPinia } from 'pinia';
import { createApp, reactive } from 'vue';
import { createRouter, createWebHistory } from 'vue-router';
import NotFound from './404.vue';
import Documentation from './Documentation.vue';
import i18n from './i18n.ts';
import { useCoreDataStore } from './stores';

const docsPages = import.meta.glob('../docs/**/*.md', { eager: true });

const docsMarkdownRoutes = await Promise.all(
  Object.entries(docsPages).map(async ([path, { default: Component, ...frontmatter }]) => {
    let name = path.replace('../docs/', '').replace('/index.md', '/').toLowerCase();
    if (name === 'index.md') {
      name = 'index';
    }

    return {
      path:
        '/docs/' +
        (name === 'index'
          ? ''
          : name.replace('(user-guide)/', '').replace('(administration)/', '').replace('(welcome)/', '')),
      name,
      meta: {
        ...frontmatter,
      },
      component: Component || NotFound,
    };
  })
);

const router = createRouter({
  history: createWebHistory(),
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
    if (!docsNavigationContext.resetScrollRequested) {
      const savedPosition = scrollPositions.get(to.fullPath);
      if (savedPosition) {
        container.scrollTo(savedPosition.left, savedPosition.top);
        return false;
      }
    }

    // scroll to the hash if it exists
    if (to.hash) {
      // wait for the element to exist before scrolling
      return new Promise((resolve) => {
        requestAnimationFrame(() => {
          const el = document.querySelector(to.hash);
          if (el) {
            el.scrollIntoView();
          }
          resolve(false); // let the browser handle it

          docsNavigationContext.resetScrollRequested = false;
        });
      });
    }

    // otherwise, scroll to top
    container.scrollTo(0, 0);
    docsNavigationContext.resetScrollRequested = false;
  },
});

/** @satisfies {DocsNavigationContext} */
const docsNavigationContext = reactive({
  animating: false,
  resetScrollRequested: false,
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

  // force the hash to be seen by the browser css selector :target
  // after navigating to a new page
  if (to.hash && to.fullPath !== from.fullPath) {
    document.location.hash = to.hash;
  }
});

// remember scroll positions for the main scroll container
const scrollPositions = new Map();
router.beforeEach((to, from, next) => {
  const container = document.querySelector('#app main');
  if (container && from.fullPath) {
    scrollPositions.set(from.fullPath, {
      left: container.scrollLeft,
      top: container.scrollTop,
    });
  }
  next();
});

router.afterEach((to) => {
  document.title = to.meta.title ? `${to.meta.title} - RAWeb Wiki` : 'RAWeb Wiki';
});

const pinia = createPinia();
await useCoreDataStore(pinia).fetchData(); // fetch core data before mounting the app

const app = i18n(createApp(Documentation));
app.use(pinia);
app.use(router);
app.component('CodeBlock', (await import('$components')).CodeBlock);
app.provide('docsNavigationContext', docsNavigationContext);

app.directive('swap', (el, binding) => {
  if (el.parentNode) {
    el.outerHTML = binding.value;
  }
});

app.config.globalProperties.docsNavigationContext = docsNavigationContext;

await router.isReady();
app.mount('#app');
