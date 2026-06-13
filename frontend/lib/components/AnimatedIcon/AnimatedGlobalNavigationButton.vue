<!-- animation extracted from: https://github.com/microsoft/microsoft-ui-xaml/blob/bac7a9c33ffe3696ec19121b16f166227eae4f1f/controls/dev/AnimatedIcon/AnimatedVisuals/AnimatedGlobalNavigationButtonVisualSource.cpp -->
<script setup lang="ts">
  import { inject, onMounted, onUnmounted, useTemplateRef } from 'vue';
  import { registerIconAnimationKey } from './iconAnimation';

  const rootRef = useTemplateRef<HTMLElement>('root');
  const registerIconAnimation = inject(registerIconAnimationKey, undefined);

  let pressAnimation: Animation | undefined;

  function getBarsElement() {
    return rootRef.value?.querySelector<SVGGElement>('.nav-bars') ?? undefined;
  }

  function press() {
    const el = getBarsElement();
    if (!el) return;
    pressAnimation = el.animate([{ transform: 'scaleX(0.521)' }], {
      duration: 150,
      easing: 'cubic-bezier(.167, .167, 0, 1)',
      fill: 'forwards',
    });
  }

  async function release() {
    const el = getBarsElement();
    const animation = pressAnimation;
    if (!el || !animation) return;
    pressAnimation = undefined;
    await animation.finished.catch(() => {});
    el.animate(
      [
        { offset: 0, transform: 'scaleX(0.521)' },
        { offset: 8 / 19, transform: 'scaleX(1.048)', easing: 'cubic-bezier(.85, 0, .75, 1)' },
        { offset: 1, transform: 'scaleX(1)', easing: 'cubic-bezier(.35, 0, 0, 1)' },
      ],
      { duration: 317, fill: 'forwards' }
    );
  }

  onMounted(() => registerIconAnimation?.({ press, release }));
  onUnmounted(() => registerIconAnimation?.(undefined));
</script>

<template>
  <span ref="root" class="animated-global-navigation-button">
    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
      <g class="nav-bars">
        <path
          d="M2.752 5.003h18.5a.75.75 0 0 1 .102 1.493l-.102.007h-18.5A.75.75 0 0 1 2.65 5.01l.102-.007h18.5-18.5Z"
          fill="currentColor"
        />
        <path
          d="M2.753 11.503h18.5a.75.75 0 0 1 .102 1.493l-.102.007h-18.5a.75.75 0 0 1-.102-1.493l.102-.007h18.5-18.5Z"
          fill="currentColor"
        />
        <path
          d="M2.753 18h18.5a.75.75 0 0 1 .102 1.493l-.102.007h-18.5a.75.75 0 0 1-.102-1.493L2.753 18h18.5-18.5Z"
          fill="currentColor"
        />
      </g>
    </svg>
  </span>
</template>

<style scoped>
  .animated-global-navigation-button {
    display: inline-flex;
  }

  .nav-bars {
    transform-box: fill-box;
    transform-origin: center;
  }
</style>
