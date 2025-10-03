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
    let name = path.replace('../docs/', '').replace('/index.md', '').toLowerCase();
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
    { path: '/:pathMatch(.*)*', component: NotFound, props: { variant: 'docs' } },
    ...docsMarkdownRoutes,
  ],
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
