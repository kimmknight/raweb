<script setup lang="ts">
  const { size = 32 } = defineProps<{ size?: number }>();
</script>

<template>
  <svg
    tabindex="-1"
    class="progress-ring indeterminate"
    :width="size"
    :height="size"
    viewBox="0 0 16 16"
    role="status"
  >
    <circle cx="50%" cy="50%" r="7" stroke-dasharray="3" stroke-dashoffset="NaN" class="svelte-32f9k0"></circle>
  </svg>
</template>

<style scoped>
  circle {
    fill: none;
    stroke: var(--wui-accent-default);
    stroke-width: 1.5;
    stroke-linecap: round;
    stroke-dasharray: 43.97;
    transform: rotate(-90deg);
    transform-origin: 50% 50%;
    transition: all var(--wui-control-normal-duration) linear;
    animation: root-splash-progress-ring-indeterminate 2s linear infinite;
  }

  @keyframes root-splash-progress-ring-indeterminate {
    0% {
      stroke-dasharray: 0.01px 43.97px;
      transform: rotate(0);
    }

    50% {
      stroke-dasharray: 21.99px 21.99px;
      transform: rotate(450deg);
    }

    100% {
      stroke-dasharray: 0.01px 43.97px;
      transform: rotate(3turn);
    }
  }
</style>
