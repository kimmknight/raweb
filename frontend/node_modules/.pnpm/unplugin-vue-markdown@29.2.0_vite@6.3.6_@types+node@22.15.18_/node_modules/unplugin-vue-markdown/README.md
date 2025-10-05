# unplugin-vue-markdown

[![NPM version](https://img.shields.io/npm/v/unplugin-vue-markdown?color=a1b858)](https://www.npmjs.com/package/unplugin-vue-markdown)

Compile Markdown to Vue component.

- 📚 Use Markdown as Vue components.
- 💚 Use Vue components in Markdown.
- 🔌 Supports Vite, Webpack, Vue CLI and more, powered by [unplugin](https://github.com/unjs/unplugin).
- ⚡️ The same transformation as [VitePress](https://vitepress.vuejs.org/).

## Install

```bash
npm i unplugin-vue-markdown
```

<details>
<summary>Vite</summary><br>

```ts
// vite.config.ts
import Vue from '@vitejs/plugin-vue'
import Markdown from 'unplugin-vue-markdown/vite'

export default defineConfig({
  plugins: [
    Vue({
      include: [/\.vue$/, /\.md$/], // <-- allows Vue to compile Markdown files
    }),
    Markdown({ /* options */ }),
  ],
})
```

Example: [`examples/vite`](./examples/vite/)

<br>
</details>

<details>
<summary>Webpack</summary><br>

```ts
// webpack.config.js
const Markdown = require('unplugin-vue-markdown/webpack')
const { VueLoaderPlugin } = require('vue-loader')

module.exports = {
  /* ... */
  module: {
    rules: [
      // ... other rules
      {
        test: /\.(vue|md)$/,
        loader: 'vue-loader'
      }
    ]
  },
  plugins: [
    new VueLoaderPlugin(),
    Markdown({ /* options */ })
  ]
}
```

<br>
</details>

<details>
<summary>Vue CLI</summary><br>

```ts
// vue.config.js
const Markdown = require('unplugin-vue-markdown/webpack')

module.exports = {
  parallel: false, // Disable thread-loader which will cause errors, we are still investigating the root cause
  chainWebpack: (config) => {
    config.module
      .rule('vue')
      .test(/\.(vue|md)$/) // <-- allows Vue to compile Markdown files

    config
      .plugin('markdown')
      .use(Markdown({
        markdownItUses: [
          prism,
        ],
      }))
  },
}
```

Example: [`examples/vue-cli`](./examples/vue-cli/)

<br>
</details>

## Import Markdown as Vue components

```html
<template>
  <HelloWorld />
</template>

<script>
import HelloWorld from './README.md'

export default {
  components: {
    HelloWorld,
  },
}
</script>
```

## Use Vue Components inside Markdown

You can even use Vue components inside your markdown, for example

```html
<Counter :init='5'/>
```

<Counter :init='5'/>

Note you can either register the components globally, or use the `<script setup>` tag to register them locally.

```ts
import { createApp } from 'vue'
import App from './App.vue'
import Counter from './Counter.vue'

const app = createApp(App)

// register global
app.component('Counter', Counter) // <--

app.mount()
```

```html
<script setup>
import { Counter } from './Counter.vue'
</script>

<Counter :init='5'/>
```

Or you can use [`unplugin-vue-components`](#work-with-unplugin-vue-components) for auto components registration.

## Frontmatter

Frontmatter will be parsed and inject into Vue's instance data `frontmatter` field.

For example:

```md
---
name: My Cool App
---

# Hello World

This is {{frontmatter.name}}
```

Will be rendered as

```html
<h1>Hello World</h1>
<p>This is My Cool App</p>
```

It will also be passed to the wrapper component's props if you have set `wrapperComponent` option.

## Document head and meta

To manage document head and meta, you would need to install [`@unhead/vue`](https://unhead.harlanzw.com/integrations/vue/setup) and do some setup.

```bash
npm i @unhead/vue
```

```js
// vite.config.js
import Vue from '@vitejs/plugin-vue'
import Markdown from 'unplugin-vue-markdown/vite'

export default {
  plugins: [
    Vue({
      include: [/\.vue$/, /\.md$/],
    }),
    Markdown({
      headEnabled: true // <--
    })
  ]
}
```

```js
// src/main.js
import { createHead } from '@unhead/vue/client' // <--
import { createApp } from 'vue'

const app = createApp(App)

const head = createHead() // <--
app.use(head) // <--
```

Then you can use frontmatter to control the head. For example:

```yaml
---
title: My Cool App
meta:
  - name: description
    content: Hello World
---
```

For more options available, please refer to [`@unhead/vue`'s docs](https://unhead.harlanzw.com/integrations/vue/setup).

## Options

`unplugin-vue-markdown` uses [`markdown-it`](https://github.com/markdown-it/markdown-it) under the hood, see [`markdown-it`'s docs](https://markdown-it.github.io/markdown-it/) for more details

```ts
// vite.config.js
import MarkdownItAnchor from 'markdown-it-anchor'
import MarkdownItPrism from 'markdown-it-prism'
import Markdown from 'unplugin-vue-markdown/vite'

export default {
  plugins: [
    Markdown({
      // default options passed to markdown-it
      // see: https://markdown-it.github.io/markdown-it/
      markdownItOptions: {
        html: true,
        linkify: true,
        typographer: true,
      },
      // A function providing the Markdown It instance gets the ability to apply custom settings/plugins
      markdownItSetup(md) {
        // for example
        md.use(MarkdownItAnchor)
        md.use(MarkdownItPrism)
      },
      // Class names for the wrapper div
      wrapperClasses: 'markdown-body'
    })
  ],
}
```

See [the tsdoc](./src/types.ts) for more advanced options

## Example

See the [/examples](./examples).

Or the pre-configured Markdown template [Vitesse](https://github.com/antfu/vitesse).

## Integrations

### Work with [vite-plugin-pages](https://github.com/hannoeru/vite-plugin-pages)

```ts
import Vue from '@vitejs/plugin-vue'
import Markdown from 'unplugin-vue-markdown/vite'
import Pages from 'vite-plugin-pages'

export default {
  plugins: [
    Vue({
      include: [/\.vue$/, /\.md$/],
    }),
    Pages({
      extensions: ['vue', 'md'],
    }),
    Markdown()
  ],
}
```

Put your markdown under `./src/pages/xx.md`, then you can access the page via route `/xx`.

### Work with [unplugin-vue-components](https://github.com/antfu/unplugin-vue-components)

`unplugin-vue-components` allows you to do on-demand components auto-importing without worrying about registration.

```ts
import Vue from '@vitejs/plugin-vue'
import Components from 'unplugin-vue-components/vite'
import Markdown from 'unplugin-vue-markdown/vite'

export default {
  plugins: [
    Vue({
      include: [/\.vue$/, /\.md$/],
    }),
    Markdown(),
    // should be placed after `Markdown()`
    Components({
      // allow auto load markdown components under `./src/components/`
      extensions: ['vue', 'md'],

      // allow auto import and register components used in markdown
      include: [/\.vue$/, /\.vue\?vue/, /\.md$/],
    })
  ],
}
```

Components under `./src/components` can be directly used in markdown components, and markdown components can also be put under `./src/components` to be auto imported.

## TypeScript Shim

```ts
declare module '*.vue' {
  import type { ComponentOptions } from 'vue'

  const Component: ComponentOptions
  export default Component
}

declare module '*.md' {
  import type { ComponentOptions } from 'vue'

  const Component: ComponentOptions
  export default Component
}
```

## License

MIT License © 2020-PRESENT [Anthony Fu](https://github.com/antfu)
