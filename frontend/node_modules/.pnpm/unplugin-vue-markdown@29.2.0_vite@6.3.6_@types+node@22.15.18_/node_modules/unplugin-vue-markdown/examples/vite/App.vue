<script setup lang='ts'>
import { ref } from 'vue'
import ComponentA from './README.md'

const current = ref()
</script>

<template>
  <router-view v-slot="{ Component }">
    <component :is="Component" ref="current" />
  </router-view>
  <div v-if="current">
    accessing frontmatter from outside via expose API:
    {{ current.frontmatter }}
  </div>
  <ComponentA style="padding: 20px 40px" />
</template>
