<script lang="ts">
  import { defineComponent } from 'vue';

  export default defineComponent({ inheritAttrs: false });
</script>

<script setup lang="ts">
  import { type ComponentPublicInstance, onMounted, onUnmounted, provide, ref, useTemplateRef } from 'vue';
  import { SELECTION_TRACK_KEY } from './keys';

  // The indicator moves in two phases so it appears to visually stretch toward its
  // destination. The leading edge jumps ahead quickly, and then the trailing edge
  // catches up slightly later.
  // TODO: Determine the actual easings and durations. These are approximations.
  const LEAD_MS = 200;
  const LEAD_EASING = 'cubic-bezier(0.85, 0, 0.15, 1)';
  const TRAIL_MS = 200;
  const TRAIL_EASING = 'cubic-bezier(0.24, 1, 0.45, 1)';
  const TRAIL_DELAY_MS = 200;

  interface IndicatorFragmentDetail {
    el: HTMLElement;
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

  // current logical indicator geometry using coordinates relative to the track
  // container, used for rendering the indicator fragment for each selectable element.
  let currentIndicatorPositionTop = 0;
  let currentIndicatorHeight = 0;
  let trailTimer: ReturnType<typeof setTimeout> | null = null;
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

  /**
   * Position an indicator fragment, converting track coords to fragment-local.
   **/
  function place(
    indicatorElement: HTMLElement,
    fragmentDetail: IndicatorFragmentDetail,
    top: number,
    height: number
  ) {
    indicatorElement.style.top = `${top - fragmentDetail.top}px`;
    indicatorElement.style.height = `${height}px`;
  }

  function refreshFragmentPositions() {
    const track = trackEl.value;
    if (!track) return;
    for (const frag of indicatorFragments.value) {
      Object.assign(frag, measure(frag.el, track));
    }
  }

  /**
   * Gets the fragment detail for a given selectable element, if it exists.
   */
  function getIndicatorFragmentDetail(element: HTMLElement | null): IndicatorFragmentDetail | undefined {
    if (!element) {
      return undefined;
    }

    return indicatorFragments.value.find(({ el }) => el === element);
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
      if (keep.includes(fragmentDetail.el)) continue;
      const indicatorElement = selectableElements.get(fragmentDetail.id);
      if (indicatorElement) {
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
      el: element,
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
    const registeredIndex = indicatorFragments.value.findIndex(({ el }) => el === element);
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
    place(indicatorElement, fragmentDetail, currentIndicatorPositionTop, currentIndicatorHeight);
    indicatorElement.getBoundingClientRect(); // force reflow
    indicatorElement.style.transition = '';
    indicatorElement.style.opacity = selectedElement === fragmentDetail.el ? '1' : '0';
  }

  /**
   * Triggers the selection indicator to move to the given element.
   *
   * The element must be a registered selectable element that is a child
   * of the track container.
   */
  function select(el: HTMLElement | null, indicatorHeight?: number) {
    // cancel any in-progress trailing edge animation so we can start a new one
    if (trailTimer !== null) {
      clearTimeout(trailTimer);
      trailTimer = null;
      setTimeout(() => {
        isAnimating = false;
      }, TRAIL_MS);
    }

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

    const lineHeight = indicatorHeight ?? fragmentDetail.height;
    const nextTop = fragmentDetail.top + (fragmentDetail.height - lineHeight) / 2;

    const fromElement = selectedElement;
    const fromTop = currentIndicatorPositionTop;
    const fromHeight = currentIndicatorHeight;

    selectedElement = el;
    currentIndicatorPositionTop = nextTop;
    currentIndicatorHeight = lineHeight;

    // if there is an animation already in progress, cancel that animation
    // and snap to the current position instead of changing the animation
    // (matches WinUI 3 behavior)
    if (isAnimating) {
      snapTo(el, nextTop, lineHeight);
    }

    // if there is no previous selection, snap to the new position with no animation
    else if (!fromElement) {
      snapTo(el, nextTop, lineHeight);
    }

    // if there is a previous selection, animate the indicator
    // from the previous position to the new position
    else {
      stretchBetween(fromElement, el, fromTop, fromHeight, nextTop, lineHeight);
    }
  }

  /**
   * Place the indicator on a single item with no animation (first selection).
   **/
  function snapTo(el: HTMLElement, top: number, height: number) {
    whitelistIndicatorFragmentsForSelectableElements(el);
    forEachIndicatorFragment(el, (indicatorElement, fragmentDetail) => {
      indicatorElement.style.transition = 'none';
      place(indicatorElement, fragmentDetail, top, height);
      indicatorElement.getBoundingClientRect(); // force reflow
      indicatorElement.style.transition = '';
      indicatorElement.style.opacity = '1';
    });
  }

  /**
   * Animate the indicator from one item to another. The indicator only ever
   * renders within the start and end elements.
   *
   * A tall bar is drawn spanning this distance between both elements, but
   * each element's indicator fragment reveals only its own portion, causing the middle
   * (gaps and any items in between) to stay empty.
   */
  function stretchBetween(
    fromElement: HTMLElement,
    toElement: HTMLElement,
    fromTop: number,
    fromHeight: number,
    toTop: number,
    toHeight: number
  ) {
    const movingDown = toTop > fromTop;
    const fromBottom = fromTop + fromHeight;
    const toBottom = toTop + toHeight;

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
        place(indicatorElement, fragmentDetail, fromTop, fromHeight);
        indicatorElement.getBoundingClientRect(); // force reflow so the reset tak1es effect
      });
    }

    // Phase 1. The leading edge of the selection indicator expands toward
    // the destination, drawing a bar that spans from the trailing edge all
    // the way to the new leading edge.

    isAnimating = true;

    const leadTop = movingDown ? fromTop : toTop;
    const leadHeight = movingDown ? toBottom - fromTop : fromBottom - toTop;
    const leadTransition = movingDown
      ? `height ${LEAD_MS}ms ${LEAD_EASING}`
      : `top ${LEAD_MS}ms ${LEAD_EASING}, height ${LEAD_MS}ms ${LEAD_EASING}`;

    for (const element of [fromElement, toElement]) {
      forEachIndicatorFragment(element, (indicatorElement, fragmentDetail) => {
        indicatorElement.style.transition = leadTransition;
        place(indicatorElement, fragmentDetail, leadTop, leadHeight);
      });
    }

    // Phase 2. The trailing edge catches up after a short delay, leaving
    // the indicator in its final position on the newly-selected element.

    const trailTransition = movingDown
      ? `top ${TRAIL_MS}ms ${TRAIL_EASING}, height ${TRAIL_MS}ms ${TRAIL_EASING}`
      : `height ${TRAIL_MS}ms ${TRAIL_EASING}`;

    trailTimer = setTimeout(() => {
      trailTimer = null;
      for (const element of [fromElement, toElement]) {
        forEachIndicatorFragment(element, (indicatorElement, fragmentDetail) => {
          indicatorElement.style.transition = trailTransition;
          place(indicatorElement, fragmentDetail, toTop, toHeight);
        });
      }

      setTimeout(() => {
        isAnimating = false;
      }, TRAIL_MS);
    }, TRAIL_DELAY_MS);
  }

  // update the fragment positions whenever the track container resizes
  // so that the selection indicator is always positioned correctly relative
  // to the selectable elements
  const resizeObserver = new ResizeObserver(() => {
    refreshFragmentPositions();
    if (selectedElement) {
      const fragmentDetail = getIndicatorFragmentDetail(selectedElement);
      if (fragmentDetail) {
        currentIndicatorPositionTop = fragmentDetail.top + (fragmentDetail.height - currentIndicatorHeight) / 2;
        forEachIndicatorFragment(selectedElement, (indicatorElement, frag) => {
          place(indicatorElement, frag, currentIndicatorPositionTop, currentIndicatorHeight);
        });
      }
    }
  });
  onMounted(() => {
    if (trackEl.value) {
      resizeObserver.observe(trackEl.value);
    }
  });
  onUnmounted(() => {
    resizeObserver.disconnect();
  });

  provide(SELECTION_TRACK_KEY, { register, unregister, select });
</script>

<template>
  <div class="track" v-bind="$attrs" ref="trackEl">
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
    left: 0;
    top: 0;
    width: 3px;
    height: 0;
    background-color: var(--wui-accent-default);
    border-radius: var(--wui-control-corner-radius);
    opacity: 0;
    transition: opacity var(--wui-control-fast-duration) ease;
  }
</style>
