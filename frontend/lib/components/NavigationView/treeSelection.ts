import type { InjectionKey } from 'vue';

/**
 * Provided by NavigationPane so that descendant TreeViews render the animated
 * AnimatedNavigationItemIndicator selection indicator (a sliding accent bar)
 * instead of each ListItem's static accent bar. TreeViews used outside of a
 * NavigationPane do not inject a value and keep the static indicator.
 */
export const ANIMATE_TREE_SELECTION_KEY: InjectionKey<boolean> = Symbol('animate-tree-selection');

/**
 * The __depth of the nearest ancestor TreeView that created a selection track.
 * A TreeView only creates its own track when its depth is greater than this, so
 * same-depth recursion (top-level categories) shares one track.
 * Functionally, this lets the indicator span every __depth === 0 item
 * while each deeper subtree gets its own track and animates only among its direct
 * siblings.
 */
export const PROVIDED_TRACK_DEPTH_KEY: InjectionKey<number> = Symbol('provided-track-depth');
