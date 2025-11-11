<script setup lang="ts">
  const props = defineProps<{
    noLabelFocus?: boolean;
  }>();

  // adds support for focusing any associated element even if it is not a form element
  function handleClickLabel(event: MouseEvent) {
    if (props.noLabelFocus) {
      event.preventDefault();
      return;
    }

    const forElement = (event.currentTarget as HTMLLabelElement).getAttribute('for');
    if (forElement) {
      const formElement = document.getElementById(forElement);
      if (formElement) {
        formElement.focus();
      }
    }
  }
</script>

<template>
  <component :is="props.noLabelFocus ? 'div' : 'label'" class="label" @click="handleClickLabel">
    <slot />
  </component>
</template>

<style scoped>
  .label {
    display: flex;
    flex-direction: column;
    justify-content: flex-start;
    gap: 3px;
    margin-bottom: 10px;
  }

  .label :deep(> div.split) {
    display: flex;
    flex-direction: row;
    gap: 8px;
    align-items: center;
  }
  .label :deep(> div.split > button) {
    flex-shrink: 0;
  }

  .label :deep(> div.stack) {
    display: flex;
    flex-direction: column;
    gap: 4px;
  }
</style>
