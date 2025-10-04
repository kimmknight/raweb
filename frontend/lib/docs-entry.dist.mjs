import { createPinia } from 'pinia';
import { createApp } from 'vue';
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
    const savedPosition = scrollPositions.get(to.fullPath);
    if (savedPosition) {
      container.scrollTo(savedPosition.left, savedPosition.top);
      return false;
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
        });
      });
    }

    // otherwise, scroll to top
    container.scrollTo(0, 0);
  },
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
  document.title = to.meta.title ? `${to.meta.title} - RAWeb Documentation` : 'RAWeb Documentation';
});

const pinia = createPinia();
await useCoreDataStore(pinia).fetchData(); // fetch core data before mounting the app

const app = i18n(createApp(Documentation));
app.use(pinia);
app.use(router);

app.directive('swap', (el, binding) => {
  if (el.parentNode) {
    el.outerHTML = binding.value;
  }
});

await router.isReady();
app.mount('#app');
