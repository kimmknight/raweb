<script setup lang="ts">
  defineOptions({ inheritAttrs: false });
</script>

<template>
  <router-view v-slot="{ Component }">
    <component :is="Component" v-bind="$attrs" />
  </router-view>
</template>
