<!-- animation extracted from: https://github.com/microsoft/microsoft-ui-xaml/blob/bac7a9c33ffe3696ec19121b16f166227eae4f1f/controls/dev/AnimatedIcon/AnimatedVisuals/AnimatedBackVisualSource.cpp -->
<script setup lang="ts">
  import { inject, onMounted, onUnmounted, useTemplateRef } from 'vue';
  import { registerIconAnimationKey } from './iconAnimation';

  const rootRef = useTemplateRef<HTMLElement>('root');
  const registerIconAnimation = inject(registerIconAnimationKey, undefined);

  let pressAnimation: Animation | undefined;

  function getStemElement() {
    return rootRef.value?.querySelector<SVGPathElement>('.stem') ?? undefined;
  }

  function getHeadElement() {
    return rootRef.value?.querySelector<SVGPathElement>('.chevron') ?? undefined;
  }

  function press() {
    const stem = getStemElement();
    const head = getHeadElement();
    if (!stem || !head) return;
    const easing = 'cubic-bezier(.167, .167, 0, 1)';
    pressAnimation = stem.animate([{ transform: 'translateX(0.469px) scaleX(0.7209)' }], {
      duration: 150,
      easing,
      fill: 'forwards',
    });
    head.animate([{ transform: 'translateX(4.5px)' }], { duration: 150, easing, fill: 'forwards' });
  }

  async function release() {
    const stem = getStemElement();
    const head = getHeadElement();
    const animation = pressAnimation;
    if (!stem || !head || !animation) return;
    pressAnimation = undefined;
    await animation.finished.catch(() => {});
    stem.animate(
      [
        { offset: 0, transform: 'translateX(0.469px) scaleX(0.7209)', easing: 'cubic-bezier(.55, 0, .75, 1)' },
        { offset: 10 / 19, transform: 'translateX(-1.875px) scaleX(1.0087)', easing: 'cubic-bezier(.35, 0, 0, 1)' },
        { offset: 1, transform: 'translateX(0px) scaleX(1)' },
      ],
      { duration: 317, fill: 'forwards' }
    );
    head.animate(
      [
        { offset: 0, transform: 'translateX(4.5px)', easing: 'cubic-bezier(.85, 0, .75, 1)' },
        { offset: 10 / 19, transform: 'translateX(-2.008px)', easing: 'cubic-bezier(.35, 0, 0, 1)' },
        { offset: 1, transform: 'translateX(0px)' },
      ],
      { duration: 317, fill: 'forwards' }
    );
  }

  onMounted(() => registerIconAnimation?.({ press, release }));
  onUnmounted(() => registerIconAnimation?.(undefined));
</script>

<template>
  <span ref="root" class="animated-back">
    <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
      <path
        class="chevron"
        d="M10.733 19.79a.75.75 0 0 0 1.034-1.086L5.516 12.75V11.25L11.767 5.295a.75.75 0 0 0-1.034-1.086l-7.42 7.067a.995.995 0 0 0-.3.58.754.754 0 0 0 .001.289.995.995 0 0 0 .3.579l7.419 7.067Z"
        fill="currentColor"
      />
      <path class="stem" d="M5.466 12.75H20.25a.75.75 0 0 0 0-1.5H5.466Z" fill="currentColor" />
    </svg>
  </span>
</template>

<style scoped>
  .animated-back {
    display: inline-flex;
  }

  .stem {
    transform-box: fill-box;
    transform-origin: right center;
  }
</style>
