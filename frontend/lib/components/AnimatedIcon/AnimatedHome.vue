<!-- animation created based on a recording from Microsoft Store -->
<script setup lang="ts">
  import { computed, inject, onMounted, onUnmounted, useTemplateRef } from 'vue';
  import { registerIconAnimationKey } from './iconAnimation';

  const { filled } = defineProps<{
    filled?: boolean;
  }>();

  const rootRef = useTemplateRef<HTMLElement>('root');
  const registerIconAnimation = inject(registerIconAnimationKey, undefined);

  let pressAnimation: Animation | undefined;

  function fmt(n: number) {
    return Math.round(n * 1000) / 1000;
  }

  // the floor of the house never moves; the roof shift up by `roofRise`
  // to make the walls taller, while the door grows from the (fixed) floor upward
  // and widens around its (fixed) horizontal center
  function buildOuterPath(doorHalfWidthTop: number, doorTop: number, roofRise: number) {
    const wallBottom = 21.497;
    const apexY = 2.532 - roofRise;
    const eaveY = 9.944 - roofRise;
    const doorOuterRight = 12 + doorHalfWidthTop + 0.25;
    const rightWallVLen = wallBottom - 1.75 - eaveY;
    const bottomHLen = 19.25 - (doorOuterRight + 1.75);
    const doorVLen = wallBottom - 1.75 - (doorTop + 0.25);
    const doorTopHLen = doorHalfWidthTop * 2;
    return `M10.55 ${fmt(apexY)}a2.25 2.25 0 0 1 2.9 0l6.75 5.692c.507.428.8 1.057.8 1.72v${fmt(rightWallVLen)}a1.75 1.75 0 0 1-1.75 1.75h-${fmt(bottomHLen)}a1.75 1.75 0 0 1-1.75-1.75v-${fmt(doorVLen)}a.25.25 0 0 0-.25-.25h-${fmt(doorTopHLen)}a.25.25 0 0 0-.25.25v${fmt(doorVLen)}a1.75 1.75 0 0 1-1.75 1.75h-${fmt(bottomHLen)}A1.75 1.75 0 0 1 3 ${fmt(wallBottom - 1.75)}V${fmt(eaveY)}c0-.663.293-1.292.8-1.72l6.75-5.692Z`;
  }

  // inner boundary of the outline (matches the original icon's second subpath at rest),
  // kept a constant stroke-width offset from the outer path so the doorframe thickness
  // stays correct as the door grows
  function buildInnerPath(doorHalfWidthTop: number, doorTop: number, roofRise: number) {
    const wallBottom = 21.497;
    const eaveY = 9.944 - roofRise;
    const apexInnerY = 3.679 - roofRise;
    const leftEaveInnerY = 9.37 - roofRise;
    const outerDoorOuterLeft = 12 - doorHalfWidthTop - 0.25;
    const innerBottomCornerY = wallBottom - 1.751;
    const innerDoorLeftX = outerDoorOuterLeft - 1.5;
    const doorSideWallTopY = doorTop + 0.25;
    const leftWallVLen = innerBottomCornerY - (eaveY - 0.001);
    const bottomHLen = innerDoorLeftX - 5;
    const doorSideVLen = innerBottomCornerY - doorSideWallTopY;
    const headerWidth = doorHalfWidthTop * 2;
    return `M12.483 ${fmt(apexInnerY)}a.75.75 0 0 0-.966 0L4.767 ${fmt(leftEaveInnerY)}a.75.75 0 0 0-.267.573v${fmt(leftWallVLen)}c0 .138.112.25.25.25h${fmt(bottomHLen)}a.25.25 0 0 0 .25-.25v-${fmt(doorSideVLen)}c0-.967.784-1.75 1.75-1.75h${fmt(headerWidth)}c.966 0 1.75.783 1.75 1.75v${fmt(doorSideVLen)}c0 .138.112.25.25.25h${fmt(bottomHLen)}a.25.25 0 0 0 .25-.25V${fmt(eaveY)}a.75.75 0 0 0-.267-.573l-6.75-5.692Z`;
  }

  function buildPath(doorHalfWidthTop: number, doorTop: number, roofRise: number) {
    const outer = buildOuterPath(doorHalfWidthTop, doorTop, roofRise);
    return filled ? outer : outer + buildInnerPath(doorHalfWidthTop, doorTop, roofRise);
  }

  // the doorway and walls are at their normal size
  const REST = { doorHalfWidthTop: 1.75, doorTop: 13.997, roofRise: 0 };
  // the doorway grows taller and wider, and the walls grow taller (pushing the roof up), while pressed
  const PRESSED = { doorHalfWidthTop: 2.75, doorTop: 11.997, roofRise: 1.5 };
  // on release, the walls overshoot shorter than their resting height before settling back
  const OVERSHOOT = { doorHalfWidthTop: 1.75, doorTop: 13.997, roofRise: -1.2 };

  function press() {
    const el = rootRef.value?.querySelector<SVGPathElement>('.house');
    if (!el) return;
    pressAnimation = el.animate(
      [
        { d: `path("${buildPath(REST.doorHalfWidthTop, REST.doorTop, REST.roofRise)}")` },
        { d: `path("${buildPath(PRESSED.doorHalfWidthTop, PRESSED.doorTop, PRESSED.roofRise)}")` },
      ],
      { duration: 150, easing: 'cubic-bezier(.167, .167, 0, 1)', fill: 'forwards' }
    );
  }

  async function release() {
    const el = rootRef.value?.querySelector<SVGPathElement>('.house');
    const animation = pressAnimation;
    if (!el || !animation) return;
    pressAnimation = undefined;
    await animation.finished.catch(() => {});
    el.animate(
      [
        {
          offset: 0,
          d: `path("${buildPath(PRESSED.doorHalfWidthTop, PRESSED.doorTop, PRESSED.roofRise)}")`,
          easing: 'cubic-bezier(.55, 0, .75, 1)',
        },
        {
          offset: 10 / 19,
          d: `path("${buildPath(OVERSHOOT.doorHalfWidthTop, OVERSHOOT.doorTop, OVERSHOOT.roofRise)}")`,
          easing: 'cubic-bezier(.35, 0, 0, 1)',
        },
        { offset: 1, d: `path("${buildPath(REST.doorHalfWidthTop, REST.doorTop, REST.roofRise)}")` },
      ],
      { duration: 317, fill: 'forwards' }
    );
  }

  const housePathToBeDrawn = computed(() => buildPath(REST.doorHalfWidthTop, REST.doorTop, REST.roofRise));

  onMounted(() => registerIconAnimation?.({ press, release }));
  onUnmounted(() => registerIconAnimation?.(undefined));
</script>

<template>
  <span ref="root" class="animated-home">
    <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
      <path class="house" :d="housePathToBeDrawn" fill="currentColor" />
    </svg>
  </span>
</template>

<style scoped>
  .animated-home {
    display: inline-flex;
  }
</style>
