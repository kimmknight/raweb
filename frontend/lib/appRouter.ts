import { useCoreDataStore } from '$stores';
import { favoritesEnabled, simpleModeEnabled } from '$utils';
import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router';
import NotFound from './404.vue';
import Apps from './pages/Apps.vue';
import Devices from './pages/Devices.vue';
import Favorites from './pages/Favorites.vue';
import Policies from './pages/Policies.vue';
import Settings from './pages/Settings.vue';
import Simple from './pages/Simple.vue';

const routes = [
  { path: '/apps', component: Apps },
  { path: '/devices', component: Devices },
  { path: '/favorites', component: Favorites },
  { path: '/policies', component: Policies },
  { path: '/settings', component: Settings },
  { path: '/simple', component: Simple },
  {
    path: '/',
    redirect(to) {
      return goHome();
    },
  },
  {
    path: '/:pathMatch(.*)*',
    component: NotFound,
    beforeEnter: (to, from, next) => {
      // remove index.html if present
      if (to.path.endsWith('/index.html')) {
        const newPath = to.path.slice(0, -10) || '/';
        const base = new URL(document.baseURI.replace('/index.html', '/')).pathname;
        if (newPath === base) {
          console.log(base.slice(0, -1) + goHome());
          window.location.href = base.slice(0, -1) + goHome();
        }
        return next(newPath);
      }

      // remove .html extension if present
      if (to.path.endsWith('.html')) {
        const newPath = to.path.slice(0, -5);
        return next(newPath);
      }
      next();
    },
  },
] satisfies RouteRecordRaw[];

function goHome() {
  return simpleModeEnabled.value ? '/simple' : favoritesEnabled.value ? '/favorites' : '/apps';
}

export const router = createRouter({
  history: createWebHistory(),
  routes,
});

router.beforeEach((to, from, next) => {
  if (!simpleModeEnabled.value && to.path === '/simple') {
    return next('/favorites');
  }

  if (simpleModeEnabled.value && ['/apps', '/devices', '/favorites', '/policies'].includes(to.path)) {
    return next('/simple');
  }

  if (!favoritesEnabled.value && to.path === '/favorites') {
    return next('/apps');
  }

  const coreAppData = useCoreDataStore();
  if (!coreAppData.authUser.isLocalAdministrator && to.path === '/policies') {
    return next('/favorites');
  }

  next();
});
