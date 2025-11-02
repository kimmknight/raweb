<script setup lang="ts">
  import { MenuFlyout } from '$components';
  import { chevronDown } from '$icons';
  import { useAttrs } from 'vue';
  import type { StandardButtonProps } from './StandardButton.vue';
  import StandardButton from './StandardButton.vue';

  const props = defineProps<StandardButtonProps>();
  const attrs = useAttrs();
  defineOptions({ inheritAttrs: false });
  console.log(attrs);
</script>

<template>
  <div class="split-button">
    <StandardButton :="{ ...props, ...attrs }">
      <template v-if="$slots.icon" #icon>
        <slot name="icon"></slot>
      </template>
      <template v-if="$slots.default" #default>
        <slot></slot>
      </template>
    </StandardButton>
    <MenuFlyout placement="bottom" anchor="end">
      <template #default="{ popoverId }">
        <StandardButton
          :variant="props.variant"
          @click="$emit('click2')"
          :popovertarget="popoverId"
          @click.stop
        >
          <template #icon-end>
            <slot name="icon-end"><span v-swap="chevronDown"></span></slot>
          </template>
        </StandardButton>
      </template>
      <template v-slot:menu>
        <slot name="menu"></slot>
      </template>
    </MenuFlyout>
  </div>
</template>

<style scoped>
  .split-button {
    display: inline-flex;
    flex-direction: row;
    align-items: center;
    justify-content: center;
    flex-wrap: nowrap;
    gap: 0;
  }

  .split-button :deep(.button:first-of-type) {
    border-top-right-radius: 0;
    border-bottom-right-radius: 0;
  }
  .split-button :deep(.button:last-of-type) {
    border-top-left-radius: 0;
    border-bottom-left-radius: 0;
    padding-inline: 6px;
    min-width: 30px;
    box-shadow: inset 0 1px 0 0 var(--wui-control-stroke-default),
      inset -1px -1px 0 0 var(--wui-control-stroke-default),
      inset 0 -1px 0 0 var(--wui-control-stroke-secondary-overlay);
  }
  .split-button :deep(.button:last-of-type > svg) {
    margin-inline-start: 6px;
    margin-inline-end: 6px;
  }
</style>
