---
title: Hello
meta:
  - name: description
    content: Hello World
test: test
---

# Hello world!

Front matter: {{ frontmatter }}

<style>
h1 {
  color: cadetblue;
}
</style>

<script setup>
import Counter2 from '../Counter2.vue'
</script>

<Counter />
<br>
<Counter2 />

<router-link to="/">Home</router-link>

<route>
{
  meta: {
    layout: 'home'
  }
}
</route>
