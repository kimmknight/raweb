<script setup lang="ts">
  const { status } = defineProps<{ status?: 'paused' | 'error' }>();

  const progress = defineModel<number | undefined>('progress', {
    default: undefined,
  });
</script>

<template>
  <svg
    class="progress-bar"
    role="progressbar"
    width="100%"
    height="3"
    :aria-valuemin="typeof progress === 'number' ? 0 : undefined"
    :aria-valuemax="typeof progress === 'number' ? 100 : undefined"
    :aria-valuenow="progress"
    :class="{
      indeterminate: typeof progress !== 'number',
      'status-paused': status === 'paused',
      'status-error': status === 'error',
    }"
  >
    <rect
      v-if="typeof progress === 'number'"
      height="1"
      rx="0.5"
      y="1"
      width="100%"
      class="progress-bar-rail"
    />
    <rect v-else height="3" ry="3" class="progress-bar-track" />
    <rect
      :width="typeof progress === 'number' ? `${progress}%` : undefined"
      height="3"
      rx="1.5"
      class="progress-bar-track"
    />
  </svg>
</template>

<style scoped>
  @keyframes indeterminate-1 {
    0% {
      opacity: 1;
      transform: translateX(-100%);
    }
    95% {
      opacity: 1;
      transform: translateX(100%);
    }
    95.01% {
      opacity: 0;
    }
    100% {
      opacity: 0;
      transform: translateX(100%);
    }
  }
  @keyframes indeterminate-2 {
    0% {
      opacity: 0;
      transform: translateX(-150%);
    }
    17.49% {
      opacity: 0;
    }
    17.5% {
      opacity: 1;
      transform: translateX(-150%);
    }
    100% {
      transform: translateX(166.66%);
      opacity: 1;
    }
  }

  .progress-bar {
    display: flex;
    align-items: center;
    width: 100%;
    min-block-size: 3px;
  }

  .progress-bar-track {
    height: 3px;
    max-width: 50%;
    transition: var(--wui-control-fast-duration) linear fill;
    fill: var(--wui-accent-default);
    border-radius: var(--wui-control-corner-radius);
  }

  .progress-bar-rail {
    fill: var(--wui-control-strong-stroke-default);
    width: 100%;
    height: 1px;
    border-radius: var(--wui-control-corner-radius);
  }

  .progress-bar.indeterminate .progress-bar-rail {
    display: none;
  }

  .progress-bar.indeterminate .progress-bar-track {
    opacity: 0;
  }

  .progress-bar.indeterminate .progress-bar-track:first-of-type {
    width: 40%;
    animation: 1.88s infinite indeterminate-1;
    animation-timing-function: cubic-bezier(0.4, 0, 1, 2) !important;
  }
  .progress-bar.indeterminate .progress-bar-track:nth-of-type(2) {
    width: 60%;
    animation: 1.88s infinite indeterminate-2;
    animation-timing-function: cubic-bezier(0.4, 0, 0.6, 1) !important;
    animation-delay: 0.4s;
  }

  .progress-bar.status-paused .progress-bar-track {
    fill: var(--wui-system-caution);
  }
  .progress-bar.status-error .progress-bar-track {
    fill: var(--wui-system-danger);
  }
</style>
