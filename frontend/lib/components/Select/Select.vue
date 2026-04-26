<script setup lang="ts">
  const { disabled, alwaysContrastText } = defineProps<{
    disabled?: boolean;
    /** Use the visible text styles even when disabled. */
    alwaysContrastText?: boolean;
  }>();

  const model = defineModel({ default: '' });

  function update(event: Event) {
    const newValue = (event.target as HTMLSelectElement).value;
    model.value = newValue;
  }
</script>

<template>
  <select
    :disabled="disabled"
    :value="model"
    @change="update"
    class="fluent-select"
    :class="{ alwaysContrastText }"
  >
    <button>
      <selectedcontent></selectedcontent>
    </button>
    <!-- The slot should contain <option> elements. -->
    <slot></slot>
  </select>
</template>

<style scoped>
  select,
  ::picker(select) {
    appearance: base-select;
    --menu-flyout-transition-offset: -14px;
  }

  select {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    user-select: none;
    font-family: var(--wui-font-family-text);
    font-size: var(--wui-font-size-body);
    font-weight: normal;
    color: var(--wui-text-primary);
    line-height: 20px;
    position: relative;
    box-sizing: border-box;
    padding-block: 4px 6px;
    padding-inline: 11px;
    text-decoration: none;
    border: none;
    cursor: default;
    border-radius: var(--wui-control-corner-radius);
    transition: background var(--wui-control-faster-duration) ease;
    min-height: 30px;

    box-shadow:
      inset 0 0 0 1px var(--wui-control-stroke-default),
      inset 0 -1px 0 0 var(--wui-control-stroke-secondary-overlay);
    background-color: var(--wui-control-fill-default);
    color: var(--text-primary);
    background-clip: padding-box;
  }
  select:hover:not(:disabled) {
    background-color: var(--wui-control-fill-secondary);
  }
  select:active:not(:disabled) {
    background-color: var(--wui-control-fill-tertiary);
    color: var(--wui-text-secondary);
  }
  select:disabled {
    background-color: var(--wui-control-fill-disabled);
  }
  select:not(.alwaysContrastText):disabled {
    color: var(--wui-text-disabled);
  }
  select.alwaysContrastText:disabled {
    color: light-dark(
      oklch(from var(--wui-text-primary) calc(l + 0.2) c h),
      oklch(from var(--wui-text-primary) calc(l - 0.1) c h)
    );
  }

  select::picker-icon {
    transition: rotate 200ms cubic-bezier(0.16, 1, 0.3, 1);
    flex-shrink: 0;
    flex-grow: 0;
    content: '';
    display: inline-block;
    width: 12px;
    height: 12px;
    background-color: currentColor;
    mask-image: url("data:image/svg+xml,%3Csvg width='24' height='24' fill='none' viewBox='0 0 24 24' xmlns='http://www.w3.org/2000/svg'%3E%3Cpath d='M4.22 8.47a.75.75 0 0 1 1.06 0L12 15.19l6.72-6.72a.75.75 0 1 1 1.06 1.06l-7.25 7.25a.75.75 0 0 1-1.06 0L4.22 9.53a.75.75 0 0 1 0-1.06Z' fill='black'/%3E%3C/svg%3E");
    mask-size: contain;
    mask-repeat: no-repeat;
  }
  select:disabled::picker-icon {
    mask-image: none;
    background-color: transparent;
  }

  select:open::picker-icon {
    rotate: 180deg;
    transition-delay: calc(var(--wui-control-normal-duration) / 2);
  }

  ::picker(select) {
    user-select: none;
    font-family: var(--wui-font-family-text);
    font-size: var(--wui-font-size-body);
    font-weight: normal;
    line-height: 20px;
    margin: 0;
    padding: 0;
    padding-block: 2px;
    box-sizing: border-box;
    color: var(--wui-text-primary);
    border-radius: var(--wui-overlay-corner-radius);
    border: 1px solid var(--wui-surface-stroke-flyout);
    background-color: var(--wui-solid-background-quarternary);
    background-clip: padding-box;
    opacity: 0;
    box-shadow: none;
  }
  :open::picker(select) {
    opacity: 1;
    box-shadow: var(--wui-flyout-shadow);
    transition:
      opacity var(--wui-control-normal-duration) var(--wui-control-fast-out-slow-in-easing) allow-discrete,
      display var(--wui-control-normal-duration) var(--wui-control-fast-out-slow-in-easing) allow-discrete,
      overlay var(--wui-control-normal-duration) var(--wui-control-fast-out-slow-in-easing) allow-discrete,
      box-shadow var(--wui-control-normal-duration) var(--wui-control-fast-out-slow-in-easing)
        calc(var(--wui-control-normal-duration) / 2);
  }
  @starting-style {
    :open::picker(select) {
      opacity: 0;
      box-shadow: none;
      transform: translateY(var(--menu-flyout-transition-offset, -50%));
    }
  }

  select :deep(option) {
    transform: translateY(var(--menu-flyout-transition-offset, -50%));
  }
  select:open :deep(option) {
    transform: translateY(0);
    transition:
      transform var(--wui-control-normal-duration) var(--wui-control-fast-out-slow-in-easing),
      background var(--wui-control-faster-duration) ease;
  }
  @starting-style {
    select:open :deep(option) {
      transform: translateY(var(--menu-flyout-transition-offset, -50%));
    }
  }

  select :deep(option) {
    display: flex;
    inline-size: calc(100% - 8px);
    position: relative;
    box-sizing: border-box;
    flex: 0 0 auto;
    margin: 2px 4px 4px 4px;
    padding-inline: 12px;
    border-radius: var(--wui-control-corner-radius);
    outline: none;
    background-color: var(--wui-subtle-transparent);
    color: var(--wui-text-primary);
    cursor: default;
    min-block-size: 29px;
    white-space: normal;
  }

  select :deep(option):last-child {
    margin-bottom: 2px;
  }

  select :deep(option):focus-visible {
    outline: var(--focus-outline);
    outline-offset: var(--focus-outline-offset);
  }

  select :deep(option):hover,
  select :deep(option):checked {
    background-color: var(--wui-subtle-secondary);
  }

  select :deep(option):active {
    background-color: var(--wui-subtle-tertiary);
  }

  select :deep(option)::checkmark {
    content: '';
    display: inline-block;
    flex-shrink: 0;
    flex-grow: 0;
    width: 16px;
    height: 16px;
    background-color: currentColor;
    mask-image: url("data:image/svg+xml,%3Csvg width='24' height='24' fill='none' viewBox='0 0 24 24' xmlns='http://www.w3.org/2000/svg'%3E%3Cpath d='M4.53 12.97a.75.75 0 0 0-1.06 1.06l4.5 4.5a.75.75 0 0 0 1.06 0l11-11a.75.75 0 0 0-1.06-1.06L8.5 16.94l-3.97-3.97Z' fill='black'/%3E%3C/svg%3E");
    mask-size: contain;
    mask-repeat: no-repeat;
  }
</style>
