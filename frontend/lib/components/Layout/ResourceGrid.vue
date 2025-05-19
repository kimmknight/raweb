<script setup lang="ts">
  const { mode } = defineProps<{
    mode: 'card' | 'list' | 'grid' | 'tile' | undefined;
  }>();
</script>

<template>
  <div class="grid" :class="`mode-${mode}`">
    <slot></slot>
  </div>
</template>

<style scoped>
  .grid {
    display: grid;
  }
  .grid.mode-card {
    grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  }
  .grid.mode-grid {
    grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
  }
  .grid.mode-list {
    grid-template-columns: 1fr;
  }
  .grid.mode-tile {
    grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  }
  @media (max-width: 599px) {
    .grid.mode-tile {
      grid-template-columns: 1fr;
    }
  }
</style>
