<script lang="ts">
  export default defineComponent({ inheritAttrs: false });

  /** The parts of a track that another track can call when selection moves between them. */
  interface SelectionOwner {
    /** Gets the element that is currently selected in the track, or null if there is none. */
    getSelectedElement: () => HTMLElement | null;
    /** Shrinks the track's indicator to nothing and clears its selection. */
    shrinkIndicatorAway: (towardEnd: boolean) => void;
    /** Hides the track's indicators immediately and clears its selection. */
    hideAllIndicators: () => void;
  }

  /**
   * The element that a track most recently deselected, and that track. It is cleared once the
   * track has cleared its selection or another track has handed off from it.
   */
  let pendingDeselection: { owner: SelectionOwner; element: HTMLElement } | null = null;
</script>

<script setup lang="ts">
  import {
    type ComponentPublicInstance,
    defineComponent,
    defineExpose,
    defineProps,
    onMounted,
    onUnmounted,
    provide,
    ref,
    useTemplateRef,
  } from 'vue';
  import { SELECTION_TRACK_KEY, TrackHandle } from './keys';

  const { orientation = 'vertical' } = defineProps<{
    /**
     * The axis upon which the selectable items are positoned and on which the indicator
     * travels. 'vertical' draws an accent bar on the leading (left) edge of the
     * item; 'horizontal' draws an underline along its bottom edge.
     */
    orientation?: 'vertical' | 'horizontal';
  }>();

  // The indicator moves in two phases so it appears to visually stretch toward its
  // destination. The leading edge jumps ahead quickly, and then the trailing edge
  // catches up slightly later.
  // The duration, keyframe offset, and easings match NavigationView::PlayIndicatorAnimations
  // (c_frame1point1/2 and c_frame2point1/2) in microsoft-ui-xaml at 866ba81a1c7c644e41fb838c755273e8f73e3a74.
  const TOTAL_DURATION_MS = 600;
  const LEAD_OFFSET = 0.333; // lead plays for 200ms and trail plays for the remaining 400ms
  const LEAD_EASING = 'cubic-bezier(0.9, 0.1, 1, 0.2)';
  const TRAIL_EASING = 'cubic-bezier(0.1, 0.9, 0.2, 1)';
  // The source animates the shrink/grow between tracks linearly, but WinUI 3 (Windows App SDK 2.1.3)
  // was measured on a 244 Hz display on 2026-11-15 to decelerate: about 600ms with this curve
  // (best fit, 0.021 RMS progress error against 0.048 for linear).
  const HANDOFF_EASING = 'cubic-bezier(0.3, 0.2, 0, 1)';
  const DESELECT_GRACE_MS = 250;

  // for when the indicator is hidden from the track because there is no selected element
  const FADE_TRANSITION = 'opacity var(--wui-control-fast-duration) ease';

  const isHorizontal = orientation === 'horizontal';
  const mainAxis = isHorizontal ? 'X' : 'Y';
  const mainSizeProperty = isHorizontal ? 'width' : 'height';

  interface IndicatorFragmentDetail {
    referenceElement: HTMLElement;
    id: number;
    top: number;
    left: number;
    width: number;
    height: number;
  }

  const trackEl = useTemplateRef<HTMLElement>('trackEl');

  /**
   * The part of the indicator that is loaded into each selectable element.
   * The indicator is fragmented so that it only renders within the bounds
   * of the start and end items, never in the gaps or over any items in between.
   */
  const indicatorFragments = ref<IndicatorFragmentDetail[]>([]);
  let nextIndicatorFragmentId = 0;

  /** map of fragment ids and their inner indicator element */
  const selectableElements = new Map<number, HTMLElement>();
  /** map of fragment ids and their stable ref callback (see getInnerElRef) */
  const refCallbacks = new Map<number, (el: Element | ComponentPublicInstance | null) => void>();

  /** The currently-selected selectable element */
  let selectedElement: HTMLElement | null = null;

  // current logical indicator geometry along the main axis, in coordinates
  // relative to the track container, used for rendering the indicator fragment
  // for each selectable element.
  let currentIndicatorMainStart = 0;
  let currentIndicatorMainSize = 0;
  let isAnimating = false;

  /**
   * Measure an element's box relative to the track container.
   **/
  function measure(el: HTMLElement, track: HTMLElement) {
    const trackRect = track.getBoundingClientRect();
    const elRect = el.getBoundingClientRect();
    return {
      top: elRect.top - trackRect.top,
      left: elRect.left - trackRect.left,
      width: elRect.width,
      height: elRect.height,
    };
  }

  /** Gets the the main-axis start (top for vertical, left for horizontal) of a fragment. */
  function getStartPos(fragmentDetail: IndicatorFragmentDetail) {
    return isHorizontal ? fragmentDetail.left : fragmentDetail.top;
  }

  /** Gets the main-axis size (height for vertical, width for horizontal) of a fragment. */
  function getFragmentSize(fragmentDetail: IndicatorFragmentDetail) {
    return isHorizontal ? fragmentDetail.width : fragmentDetail.height;
  }

  /**
   * Position an indicator fragment along the main axis, converting track coords
   * to fragment-local coords. The cross-axis placement (thickness/edge) is handled by CSS.
   **/
  function place(
    indicatorElement: HTMLElement,
    fragmentDetail: IndicatorFragmentDetail,
    start: number,
    size: number
  ) {
    // stop any in-progress animation so the static position is what is shown
    for (const animation of indicatorElement.getAnimations()) {
      animation.cancel();
    }
    internal_applyPlacement(indicatorElement, fragmentDetail, start, size);
  }

  /**
   * Sets the static position of an indicator fragment without touching any running animation.
   * The position is a translation (not an offset or a size change) so that scaling the
   * indicator during the stretch animation can use the same transform.
   **/
  function internal_applyPlacement(
    indicatorElement: HTMLElement,
    fragmentDetail: IndicatorFragmentDetail,
    start: number,
    size: number
  ) {
    indicatorElement.style.setProperty(mainSizeProperty, `${size}px`);
    indicatorElement.style.transform = `translate${mainAxis}(${start - getStartPos(fragmentDetail)}px)`;
  }

  function refreshFragmentPositions() {
    const track = trackEl.value;
    if (!track) return;
    for (const frag of indicatorFragments.value) {
      Object.assign(frag, measure(frag.referenceElement, track));
    }
  }

  /**
   * Gets the fragment detail for a given selectable element, if it exists.
   */
  function getIndicatorFragmentDetail(element: HTMLElement | null): IndicatorFragmentDetail | undefined {
    if (!element) {
      return undefined;
    }

    return indicatorFragments.value.find(({ referenceElement: el }) => el === element);
  }

  /**
   * Run a callback against a specific item's indicator fragment, if it exists.
   **/
  function forEachIndicatorFragment(
    element: HTMLElement | null,
    fn: (indicatorElement: HTMLElement, frag: IndicatorFragmentDetail) => void
  ) {
    const detail = getIndicatorFragmentDetail(element);
    const indicatorElement = detail && selectableElements.get(detail.id);
    if (detail && indicatorElement) {
      fn(indicatorElement, detail);
    }
  }

  /**
   * Hide every indicator fragment except those belonging to the given items.
   **/
  function whitelistIndicatorFragmentsForSelectableElements(...keep: HTMLElement[]) {
    for (const fragmentDetail of indicatorFragments.value) {
      if (keep.includes(fragmentDetail.referenceElement)) continue;
      const indicatorElement = selectableElements.get(fragmentDetail.id);
      if (indicatorElement) {
        indicatorElement.style.transition = FADE_TRANSITION;
        indicatorElement.style.opacity = '0';
      }
    }
  }

  /**
   * Registers an element as a selectable element that can receive an indicator fragment
   * to indicate active selection. The element must be a child of the track container.
   */
  function register(element: HTMLElement) {
    const track = trackEl.value;
    if (!track) {
      console.warn('Cannot register selectable element: track container not found.');
      return;
    }

    const frag: IndicatorFragmentDetail = {
      referenceElement: element,
      id: nextIndicatorFragmentId++, // incrementing id is used to key the fragment in the v-for
      ...measure(element, track),
    };
    indicatorFragments.value.push(frag);
  }

  /**
   * Unregisters a selectable element, removing its indicator fragment and
   * clearing any selection state.
   */
  function unregister(element: HTMLElement) {
    const registeredIndex = indicatorFragments.value.findIndex(({ referenceElement: el }) => el === element);
    if (registeredIndex !== -1) {
      const [removed] = indicatorFragments.value.splice(registeredIndex, 1);
      selectableElements.delete(removed.id);
      refCallbacks.delete(removed.id);
    }

    // clear selection if the unregistered element was selected
    if (selectedElement === element) {
      select(null);
    }
  }

  /**
   * Returns a cached ref callback for a fragment, keyed by its id.
   *
   * The reasoning behind this is a bit complicated:
   *
   * Vue calls a ref callback with the element when it mounts and with null when
   * it unmounts. But if the callback is a different function on each render (e.g.
   * an inline arrow), Vue also calls the old one with null every render, even
   * though the element never left the DOM. That false "unmount" would drop the
   * tracked element and break an in-progress animation.
   * Reusing one callback per fragment keeps its identity stable, ensuring that the null
   * call only happens on a real unmount.
   */
  function getStableIndicatorFragmentRef(id: number) {
    let callback = refCallbacks.get(id);

    // if the callback does not already exist, create it and save it to the map
    if (!callback) {
      callback = (indicatorElement) => {
        if (indicatorElement instanceof HTMLElement) {
          // Vue runs this callback every time it re-renders and updates this
          // element, not only on mount/unmount. Only sync when we're handed a
          // different node than before; otherwise, a re-render mid-animation (e.g.
          // a route navigation) would clobber the in-progress transition by
          // snapping to the final position.
          const isNewElement = selectableElements.get(id) !== indicatorElement;
          selectableElements.set(id, indicatorElement);
          if (isNewElement) {
            syncIndicatorFragmentElement(id);
          }
        } else {
          selectableElements.delete(id);
        }
      };
      refCallbacks.set(id, callback);
    }

    return callback;
  }

  /**
   * Syncs a freshly-mounted indicator fragment to the current selection state.
   **/
  function syncIndicatorFragmentElement(id: number) {
    const fragmentDetail = indicatorFragments.value.find((f) => f.id === id);
    const indicatorElement = selectableElements.get(id);
    if (!fragmentDetail || !indicatorElement) {
      return;
    }

    indicatorElement.style.transition = 'none';
    place(indicatorElement, fragmentDetail, currentIndicatorMainStart, currentIndicatorMainSize);
    indicatorElement.getBoundingClientRect(); // force reflow
    indicatorElement.style.transition = '';
    indicatorElement.style.opacity = selectedElement === fragmentDetail.referenceElement ? '1' : '0';
  }

  /**
   * Triggers the selection indicator to move to the given element.
   *
   * The element must be a registered selectable element that is a child
   * of the track container.
   *
   * `indicatorSize` is the length of the accent line along the main axis (its
   * height when vertical and its width when horizontal). This defaults to the full
   * main-axis size of the item.
   *
   * When selection moves here from an element that another track has just deselected, the
   * indicator in the other track shrinks away and the indicator here grows in. If
   * `instantHandoff` is true, the indicator in the other track is hidden immediately instead.
   * Tracks that did not deselect anything keep their selection.
   */
  function select(el: HTMLElement | null, indicatorSize?: number, instantHandoff = false) {
    // if the element is null, clear selection and hide all indicator fragments
    if (el === null) {
      whitelistIndicatorFragmentsForSelectableElements();
      selectedElement = null;
      return;
    }

    // require the element to be a registered selectable element
    const fragmentDetail = getIndicatorFragmentDetail(el);
    if (!fragmentDetail) {
      console.warn('Cannot select element: not a registered selectable element.', el);
      return;
    }

    refreshFragmentPositions();

    const track = trackEl.value;
    if (!track) {
      return;
    }

    const lineSize = indicatorSize ?? getFragmentSize(fragmentDetail);
    const nextStart = getStartPos(fragmentDetail) + (getFragmentSize(fragmentDetail) - lineSize) / 2;

    const fromElement = selectedElement;
    const fromStart = currentIndicatorMainStart;
    const fromSize = currentIndicatorMainSize;

    selectedElement = el;
    currentIndicatorMainStart = nextStart;
    currentIndicatorMainSize = lineSize;

    // Running this logic in a microtask ensures that `deselect` has run from any other
    // tracks that exist before we check whether we need to animate the indicator differently.
    queueMicrotask(() => {
      if (selectedElement !== el) {
        return;
      }

      const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
      const shouldSnap =
        // do not animate if the user prefers reduced motion
        prefersReducedMotion ||
        // if there is an animation already in progress, cancel that animation
        // and snap to the current position instead of changing the animation
        // (matches WinUI 3 behavior)
        isAnimating ||
        // if there is no previous selection, snap to the new position with no animation
        !fromElement;

      // selection moved here from another track that has just deselected its element:
      // the old indicator shrinks away while the new one grows in
      const previousSelection = pendingDeselection;
      const isHandoff =
        !prefersReducedMotion &&
        !fromElement &&
        previousSelection !== null &&
        previousSelection.owner !== selectionOwner &&
        previousSelection.owner.getSelectedElement() === previousSelection.element &&
        previousSelection.element.isConnected;

      if (isHandoff) {
        pendingDeselection = null;
        const movingDown =
          el.getBoundingClientRect().top > previousSelection.element.getBoundingClientRect().top;
        if (instantHandoff) {
          previousSelection.owner.hideAllIndicators();
        } else {
          previousSelection.owner.shrinkIndicatorAway(movingDown);
        }
        growIndicatorIn(el, nextStart, lineSize, movingDown);
      } else if (shouldSnap) {
        snapTo(el, nextStart, lineSize);
      }

      // if there is a previous selection, animate the indicator
      // from the previous position to the new position
      else {
        stretchBetween(fromElement, el, fromStart, fromSize, nextStart, lineSize);
      }
    });
  }

  /**
   * Place the indicator on a single item with no animation (first selection).
   **/
  function snapTo(el: HTMLElement, start: number, size: number) {
    whitelistIndicatorFragmentsForSelectableElements(el);
    forEachIndicatorFragment(el, (indicatorElement, fragmentDetail) => {
      indicatorElement.style.transition = 'none';
      place(indicatorElement, fragmentDetail, start, size);
      indicatorElement.getBoundingClientRect(); // force reflow
      indicatorElement.style.transition = '';
      indicatorElement.style.opacity = '1';
    });
  }

  /**
   * Builds the CSS transform that draws the indicator bar at `start` (in track coordinates)
   * with a length of `size` along the main axis, inside the given fragment.
   *
   * `baseSize` is the current length of the fragment's indicator element. The bar is resized
   * by scaling it relative to `baseSize` and positioned by translating it.
   */
  function buildIndicatorTransform(
    fragmentDetail: IndicatorFragmentDetail,
    start: number,
    size: number,
    baseSize: number
  ) {
    return `translate${mainAxis}(${start - getStartPos(fragmentDetail)}px) scale${mainAxis}(${size / baseSize})`;
  }

  /**
   * Moves the indicator to the selected element's current position, canceling any animation
   * running on it. This does nothing if no element is selected.
   */
  function repositionSelectedIndicator() {
    if (!selectedElement) {
      return;
    }
    const fragmentDetail = getIndicatorFragmentDetail(selectedElement);
    if (fragmentDetail) {
      currentIndicatorMainStart =
        getStartPos(fragmentDetail) + (getFragmentSize(fragmentDetail) - currentIndicatorMainSize) / 2;
      forEachIndicatorFragment(selectedElement, (indicatorElement, frag) => {
        place(indicatorElement, frag, currentIndicatorMainStart, currentIndicatorMainSize);
      });
    }
  }

  /** Whether the track was resized while the indicator was animating. */
  let resizedWhileAnimating = false;

  /**
   * Marks the indicator as animating for the duration of an animation. If the track is
   * resized in that time, the fragment positions are refreshed and the selected indicator
   * is placed again once the animation ends.
   */
  function markAnimatingForDuration() {
    isAnimating = true;
    setTimeout(() => {
      isAnimating = false;
      if (resizedWhileAnimating) {
        resizedWhileAnimating = false;
        refreshFragmentPositions();
        repositionSelectedIndicator();
      }
    }, TOTAL_DURATION_MS);
  }

  /**
   * Shrinks the indicator on the selected element to nothing, hides it, and clears this
   * track's selection.
   *
   * The indicator shrinks toward its end edge if `towardEnd` is true, and toward its start
   * edge otherwise. Does nothing if no element is selected.
   */
  function shrinkIndicatorAway(towardEnd: boolean) {
    const element = selectedElement;
    if (!element) {
      return;
    }
    selectedElement = null;

    const start = currentIndicatorMainStart;
    const size = currentIndicatorMainSize;

    // horizontal tracks always shrink toward the start edge
    const anchor = towardEnd && !isHorizontal ? start + size : start;

    forEachIndicatorFragment(element, (indicatorElement, fragmentDetail) => {
      const animation = indicatorElement.animate(
        [
          { transform: buildIndicatorTransform(fragmentDetail, start, size, size) },
          { transform: buildIndicatorTransform(fragmentDetail, anchor, 0, size) },
        ],
        // hold the zero-size end state so the bar does not pop back to full size before it is hidden
        { duration: TOTAL_DURATION_MS, easing: HANDOFF_EASING, fill: 'forwards' }
      );
      // a cancelled animation (new selection in this track) never fires onfinish
      animation.onfinish = () => {
        if (selectedElement === null) {
          indicatorElement.style.transition = 'none';
          indicatorElement.style.opacity = '0';
        }
      };
    });
  }

  /**
   * Grows the indicator on `el` from nothing to a length of `size` at `start`, and hides the
   * indicators on every other element in this track.
   *
   * The indicator grows from its start edge if `movingDown` is true, and from its end edge
   * otherwise.
   */
  function growIndicatorIn(el: HTMLElement, start: number, size: number, movingDown: boolean) {
    // horizontal tracks always grow from the start edge
    const anchor = movingDown || isHorizontal ? start : start + size;

    whitelistIndicatorFragmentsForSelectableElements(el);
    forEachIndicatorFragment(el, (indicatorElement, fragmentDetail) => {
      indicatorElement.style.transition = 'none';
      indicatorElement.style.opacity = '1';
      place(indicatorElement, fragmentDetail, start, size);
      indicatorElement.getBoundingClientRect(); // force reflow
      indicatorElement.style.transition = '';
      indicatorElement.animate(
        [
          { transform: buildIndicatorTransform(fragmentDetail, anchor, 0, size) },
          { transform: buildIndicatorTransform(fragmentDetail, start, size, size) },
        ],
        { duration: TOTAL_DURATION_MS, easing: HANDOFF_EASING }
      );
    });
    markAnimatingForDuration();
  }

  /**
   * Animate the indicator from one item to another. The indicator only ever
   * renders within the start and end elements.
   *
   * A long bar is drawn spanning the distance between both elements, but
   * each element's indicator fragment reveals only its own portion, causing the middle
   * (gaps and any items in between) to stay empty.
   *
   * The bar has a constant size and is stretched with a scale transform instead of changing
   * the actual dimensions. This is why WinUI 3's selection indicator sometimes looks
   * pointier during the animation. The stretch combined with the rounded corners creates this effect.
   */
  function stretchBetween(
    fromElement: HTMLElement,
    toElement: HTMLElement,
    fromStart: number,
    fromSize: number,
    toStart: number,
    toSize: number
  ) {
    const movingForward = toStart > fromStart;

    if (!trackEl.value) {
      return;
    }

    whitelistIndicatorFragmentsForSelectableElements(fromElement, toElement);

    // Phase 0. Snap both indicator fragments to their current positions
    // before starting the animation. This ensures that the animation starts from
    // the correct position even if the previous animation was interrupted.
    for (const element of [fromElement, toElement]) {
      forEachIndicatorFragment(element, (indicatorElement, fragmentDetail) => {
        indicatorElement.style.transition = 'none';
        indicatorElement.style.opacity = '1';
        place(indicatorElement, fragmentDetail, fromStart, fromSize);
        indicatorElement.style.transition = '';
        indicatorElement.getBoundingClientRect(); // force reflow so the reset takes effect
      });
    }

    const toEnd = toStart + toSize;
    const fromEnd = fromStart + fromSize;
    const stretchedStart = movingForward ? fromStart : toStart;
    const stretchedSize = movingForward ? toEnd - fromStart : fromEnd - toStart;

    for (const element of [fromElement, toElement]) {
      forEachIndicatorFragment(element, (indicatorElement, fragmentDetail) => {
        const frame = (start: number, size: number) =>
          buildIndicatorTransform(fragmentDetail, start, size, toSize);

        // the final position shows as soon as the animation ends
        internal_applyPlacement(indicatorElement, fragmentDetail, toStart, toSize);

        indicatorElement.animate(
          [
            // Phase 1. The leading edge of the selection indicator moves to its destination,
            // drawing a bar that spans from the trailing edge all the way to the new leading edge.
            { transform: frame(fromStart, fromSize), easing: LEAD_EASING },

            // Phase 2. The trailing edge catches up, leaving the indicator in its final
            // position on the newly-selected element.
            {
              offset: LEAD_OFFSET,
              transform: frame(stretchedStart, stretchedSize),
              easing: TRAIL_EASING,
            },

            // the end of phase 2
            { transform: frame(toStart, toSize) },
          ],
          { duration: TOTAL_DURATION_MS }
        );

        // the outgoing indicator holds until the trail starts and then fades out
        if (element === fromElement) {
          indicatorElement.animate(
            [{ opacity: 1 }, { offset: LEAD_OFFSET, opacity: 1, easing: TRAIL_EASING }, { opacity: 0 }],
            { duration: TOTAL_DURATION_MS }
          );
        }
      });
    }

    markAnimatingForDuration();
  }

  if ('window' in globalThis) {
    // update the fragment positions whenever the track container resizes
    // so that the selection indicator is always positioned correctly relative
    // to the selectable elements
    const resizeObserver = new ResizeObserver(() => {
      refreshFragmentPositions();

      // placing the indicator cancels running animations (e.g. collapsing a tree resizes
      // the track while the indicator is moving), so wait until they finish
      if (isAnimating) {
        resizedWhileAnimating = true;
        return;
      }
      repositionSelectedIndicator();
    });
    onMounted(() => {
      if (trackEl.value) {
        resizeObserver.observe(trackEl.value);
      }
    });
    onUnmounted(() => {
      resizeObserver.disconnect();
    });
  }

  /**
   * Clears the selection, but only if the given element is still the selected
   * one. A deselected item calls this so that when selection moves to another
   * track the old indicator fades out, while a move between siblings in this
   * track (which has already updated the selection) is left untouched.
   *
   * The selection is cleared after `DESELECT_GRACE_MS`, not immediately.
   */
  function deselect(element: HTMLElement) {
    pendingDeselection = { owner: selectionOwner, element };

    // An item in another track can take over a few frames after this one deselects (e.g. an
    // expanding tree selects its child once its rows finish sliding). Clearing right away would
    // fade this indicator out before the other track can hand off from it.
    setTimeout(() => {
      if (selectedElement === element) {
        select(null);
      }
      if (pendingDeselection?.element === element) {
        pendingDeselection = null;
      }
    }, DESELECT_GRACE_MS);
  }

  const getComputedStyle =
    'window' in globalThis
      ? window.getComputedStyle.bind(window)
      : () => ({ borderRadius: '0px' }) as CSSStyleDeclaration;

  /** Clears this track's selection and hides all of its indicators immediately. */
  function hideAllIndicators() {
    selectedElement = null;
    for (const indicatorElement of selectableElements.values()) {
      indicatorElement.style.transition = 'none';
      indicatorElement.style.opacity = '0';
    }
  }

  const selectionOwner: SelectionOwner = {
    getSelectedElement: () => selectedElement,
    shrinkIndicatorAway,
    hideAllIndicators,
  };

  provide(SELECTION_TRACK_KEY, { register, unregister, select, deselect });

  // exposed so that ancestors can obtain this track's handle via a template ref
  // (e.g. to hand it to Selectable instances mounted in a separate Vue app, which
  // can't reach it through provide+inject since that only works within one app tree)
  defineExpose({ register, unregister, select, deselect } satisfies TrackHandle);
</script>

<template>
  <div class="track" :class="{ horizontal: isHorizontal }" v-bind="$attrs" ref="trackEl">
    <slot></slot>
    <div
      v-for="frag in indicatorFragments"
      :key="frag.id"
      class="clip-fragment"
      :style="{
        top: `${frag.top}px`,
        left: `${frag.left}px`,
        width: `${frag.width}px`,
        height: `${frag.height}px`,
        'border-radius': getComputedStyle(frag.referenceElement).borderRadius || '4px',
      }"
      aria-hidden="true"
    >
      <div :ref="getStableIndicatorFragmentRef(frag.id)" class="indicator"></div>
    </div>
  </div>
</template>

<style scoped>
  .track {
    position: relative;
  }

  .clip-fragment {
    position: absolute;
    overflow: hidden;
    pointer-events: none;
    z-index: 2;
  }

  .indicator {
    position: absolute;
    /* cross-axis offset lets consumers indent the bar (e.g. nested tree items) */
    left: var(--indicator-cross-offset, 0);
    top: 0;
    width: 3px;
    height: 0;
    /* the stretch animation scales the indicator from its start edge (see stretchBetween) */
    transform-origin: 0 0;
    background-color: var(--wui-accent-default);
    border-radius: var(--wui-control-corner-radius);
    opacity: 0;
    transition: opacity var(--wui-control-fast-duration) ease;
  }

  /* horizontal: the accent line becomes an underline along the bottom edge */
  .track.horizontal .indicator {
    top: auto;
    bottom: 2px;
    left: 0;
    width: 0;
    height: 3px;
  }
</style>
