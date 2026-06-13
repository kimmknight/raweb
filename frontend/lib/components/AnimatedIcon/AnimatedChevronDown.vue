<!-- animation extracted from: https://github.com/microsoft/microsoft-ui-xaml/blob/bac7a9c33ffe3696ec19121b16f166227eae4f1f/controls/dev/AnimatedIcon/AnimatedVisuals/AnimatedChevronDownSmallVisualSource.cpp -->
<script setup lang="ts">
  import { chevronDown } from '$icons';
  import { inject, onMounted, onUnmounted, useTemplateRef } from 'vue';
  import { registerIconAnimationKey } from './iconAnimation';

  const rootRef = useTemplateRef<HTMLElement>('root');
  const registerIconAnimation = inject(registerIconAnimationKey, undefined);

  let pressAnimation: Animation | undefined;

  function press() {
    const el = rootRef.value;
    if (!el) return;
    pressAnimation = el.animate([{ transform: 'translateY(2px)' }], {
      duration: 150,
      easing: 'cubic-bezier(.167, .167, .65, 1)',
      fill: 'forwards',
    });
  }

  async function release() {
    const el = rootRef.value;
    const animation = pressAnimation;
    if (!el || !animation) return;
    pressAnimation = undefined;
    await animation.finished.catch(() => {});
    el.animate(
      [
        { offset: 0, transform: 'translateY(2px)' },
        { offset: 5 / 19, transform: 'translateY(-0.8px)', easing: 'cubic-bezier(.55, 0, .75, 1)' },
        { offset: 1, transform: 'translateY(0px)', easing: 'cubic-bezier(.35, 0, 0, 1)' },
      ],
      { duration: 317, fill: 'forwards' }
    );
  }

  onMounted(() => registerIconAnimation?.({ press, release }));
  onUnmounted(() => registerIconAnimation?.(undefined));
</script>

<template>
  <span ref="root" class="animated-chevron-down" v-html="chevronDown"></span>
</template>

<style scoped>
  .animated-chevron-down {
    display: inline-flex;
  }
</style>
