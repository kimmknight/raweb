<script setup lang="ts"></script>

<template>
  <fieldset>
    <legend><slot name="legend"></slot></legend>
    <slot></slot>
  </fieldset>
</template>

<style scoped>
  fieldset {
    border-color: var(--wui-surface-stroke-default);
    border-width: 1px;
    border-radius: var(--wui-control-corner-radius);
    padding: 12px;
    margin-bottom: 16px;
  }
</style>
