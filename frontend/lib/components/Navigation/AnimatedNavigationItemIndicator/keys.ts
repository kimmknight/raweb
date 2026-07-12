import type { InjectionKey } from 'vue';

interface TrackHandle {
  register: (el: HTMLElement) => void;
  unregister: (el: HTMLElement) => void;
  select: (el: HTMLElement | null, indicatorHeight?: number) => void;
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
