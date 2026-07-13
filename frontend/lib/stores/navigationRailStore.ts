import type { TrackHandle } from '$components/Navigation/AnimatedNavigationItemIndicator/keys';
import { defineStore } from 'pinia';

/**
 * Exposes the NavigationRail's selection indicator track handle outside of
 * Vue's provide/inject.
 *
 * **You should only use this when registering nav rail items from
 * `App_Data/inject/index.js`.**
 */
export const useNavigationRailStore = defineStore('navigationRail', {
  state: () => ({
    trackHandle: null as TrackHandle | null,
  }),
});
