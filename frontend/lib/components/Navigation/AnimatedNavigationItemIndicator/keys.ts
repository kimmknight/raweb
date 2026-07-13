import type { InjectionKey } from 'vue';

export interface TrackHandle {
  register: (el: HTMLElement) => void;
  unregister: (el: HTMLElement) => void;
  /**
   * `indicatorSize` is the length of the accent line along the track's main
   * axis (its height when vertical and its width when horizontal).
   */
  select: (el: HTMLElement | null, indicatorSize?: number) => void;
  /**
   * Clears the selection if (and only if) `el` is still the selected element.
   * Used so an item can fade its indicator out when selection leaves this track
   * without clobbering a sibling that has since become selected.
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
