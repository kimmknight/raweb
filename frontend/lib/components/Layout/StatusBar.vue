<template>
  <footer class="status-bar">
    <div id="status-bar-start"></div>
    <div id="status-bar-end"></div>
  </footer>
</template>

<style scoped>
  .status-bar {
    position: relative;
    z-index: 2;
    flex-shrink: 0;
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    block-size: 30px;
    padding: 0 12px;
    box-sizing: border-box;
    box-shadow: inset 0 1px 0 var(--wui-divider-stroke-default);
    background-color: var(--wui-solid-background-tertiary);
    color: var(--wui-text-secondary);
  }
  .status-bar:not(:has(> div > *)) {
    display: none;
  }
  .status-bar > div {
    display: flex;
    align-items: center;
    gap: 2px;
  }
</style>
