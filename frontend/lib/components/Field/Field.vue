<script setup lang="ts"></script>

<template>
  <label class="input">
    <slot />
  </label>
</template>

<style scoped>
  label {
    display: flex;
    flex-direction: column;
    justify-content: flex-start;
    gap: 3px;
    margin-bottom: 10px;
  }

  label :deep(> div.split) {
    display: flex;
    flex-direction: row;
    gap: 8px;
    align-items: center;
  }
  label :deep(> div.split > button) {
    flex-shrink: 0;
  }
</style>
