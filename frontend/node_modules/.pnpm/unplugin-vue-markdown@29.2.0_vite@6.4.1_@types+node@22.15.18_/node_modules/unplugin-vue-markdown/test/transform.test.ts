import { describe, expect, it } from 'vitest'
import { createMarkdown } from '../src/core/markdown'
import { resolveOptions } from '../src/core/options'

describe('transform', async () => {
  const options = resolveOptions({})
  const markdownToVue = createMarkdown(options)

  it('basic', async () => {
    const md = `---
title: Hey
---

# Hello

- A
- B
- C
`
    expect((await markdownToVue('', md)).code).toMatchSnapshot()
  })

  it('style', async () => {
    const md = `
# Hello

<style>h1 { color: red }</style>
`
    expect((await markdownToVue('', md)).code).toMatchSnapshot()
  })

  it('script setup', async () => {
    const md = `
# Hello

<script setup lang="ts">
import Foo from './Foo.vue'
</script>
`
    expect((await markdownToVue('', md)).code).toMatchSnapshot()
  })

  it('exposes frontmatter', async () => {
    const md = `---
title: Hey
---

# Hello`
    expect((await markdownToVue('', md)).code).toMatchSnapshot()
  })

  it('couldn\'t expose frontmatter', async () => {
    const md = `---
title: Hey
---

<script setup>
defineExpose({ test: 'test'})
</script>
`
    expect((await markdownToVue('', md)).code).toMatchSnapshot()
  })

  it('escapeCodeTagInterpolation', async () => {
    const md = `
<div>{{hello}}</div>

\`\`\`ts
<div>{{hello}}</div>
\`\`\`
`
    expect((await markdownToVue('', md)).code).toMatchSnapshot()
  })

  it('frontmatter interpolation', async () => {
    const md = `
---
name: 'My Cool App'
---

# Hello World

This is {{frontmatter.name}}
`
    expect((await markdownToVue('', md)).code).toMatchSnapshot()
  })

  it('vue directives', async () => {
    const md = `
---
name: 'My Cool App'
---

<script setup lang="ts">
function onClick() {
  // ...
}
</script>

<button @click="onClick"></button>
`
    expect((await markdownToVue('', md)).code).toMatchSnapshot()
  })

  it('export keyword frontmatters', async () => {
    const md = `
---
class: 'text'
default: 'foo'
---

Hello
`
    expect((await markdownToVue('', md)).code).toMatchSnapshot()
  })

  it('code escape', async () => {
    const md = `
Hello \`{{ world }}\`

\`\`\`js
console.log(\`{{ world }}\`)
\`\`\`
`
    expect((await markdownToVue('', md)).code).toMatchSnapshot()
  })
})
