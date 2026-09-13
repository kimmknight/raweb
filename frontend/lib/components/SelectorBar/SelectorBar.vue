<script setup lang="ts">
  import { AnimatedNavigationItemIndicator } from '$components';
</script>

<template>
  <AnimatedNavigationItemIndicator.Track orientation="horizontal" class="selector-bar" role="tablist">
    <slot></slot>
  </AnimatedNavigationItemIndicator.Track>
</template>

<style scoped>
  .selector-bar {
    display: flex;
    flex-direction: row;
    gap: 4px;
    border-bottom: 1px solid var(--wui-divider-stroke-default);
  }
</style>
