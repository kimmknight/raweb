import type { InjectionKey } from 'vue';

export interface TrackHandle {
  register: (el: HTMLElement) => void;
  unregister: (el: HTMLElement) => void;
  /**
   * `indicatorSize` is the length of the accent line along the track's main
   * axis (its height when vertical and its width when horizontal).
   *
   * `instantHandoff` hides the previous selection's indicator in another track immediately
   * instead of animating it out.
   */
  select: (el: HTMLElement | null, indicatorSize?: number, instantHandoff?: boolean) => void;
  /**
   * Clears the selection and fades the indicator out, but only if `el` is still the selected
   * element after a short delay (`DESELECT_GRACE_MS` in `SelectionTrack.vue`).
   *
   * The delay gives other items a chance to become selected first. If a sibling in this track
   * takes over, `el` is no longer selected and nothing is cleared. If an item in another track
   * takes over, its indicator hands off from `el` instead of `el` fading out.
   */
  deselect: (el: HTMLElement) => void;
}

export const SELECTION_TRACK_KEY: InjectionKey<TrackHandle> = Symbol('selection-track');

/**
 * Provided by Selectable to its descendants so they can suppress their own
 * active-state visuals (background, accent bar) in favor of the track indicator.
 *
 * @example Usage in a descendant component
 * ```
 * import { inject } from 'vue';
 *
 * const isInSelectionTrack = inject(IN_SELECTION_TRACK_KEY, false);
 * ```
 */
export const IN_SELECTION_TRACK_KEY: InjectionKey<true> = Symbol('in-selection-track');
