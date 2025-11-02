<script setup lang="ts">
  import { computed, useSlots } from 'vue';
  import SplitButton from './SplitButton.vue';
  import type { StandardButtonProps } from './StandardButton.vue';
  import StandardButton from './StandardButton.vue';

  const props = defineProps<StandardButtonProps>();
  const slots = useSlots();

  const isSplitButton = computed(() => 'menu' in slots);
  const componentToRender = computed(() => (isSplitButton.value ? SplitButton : StandardButton));
</script>

<template>
  <component :is="componentToRender" :="props">
    <template v-if="$slots.icon" #icon>
      <slot name="icon"></slot>
    </template>
    <template v-if="$slots.default" #default>
      <slot></slot>
    </template>
    <template v-if="$slots['icon-end']" #icon-end>
      <slot name="icon-end"></slot>
    </template>
    <template v-if="$slots.menu" #menu>
      <slot name="menu"></slot>
    </template>
  </component>
</template>
