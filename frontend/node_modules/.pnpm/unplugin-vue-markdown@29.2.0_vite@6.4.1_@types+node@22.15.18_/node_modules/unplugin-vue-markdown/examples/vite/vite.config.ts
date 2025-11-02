import Vue from '@vitejs/plugin-vue'
import prism from 'markdown-it-prism'
import Markdown from 'unplugin-vue-markdown/vite'
import { defineConfig } from 'vite'
import Inspect from 'vite-plugin-inspect'
import Pages from 'vite-plugin-pages'

export default defineConfig({
  plugins: [
    Vue({
      include: [/\.vue$/, /\.md$/],
    }),
    Markdown({
      markdownItOptions: {

      },
      headEnabled: true,
      markdownItUses: [
        prism,
      ],
    }),
    Pages({
      pagesDir: 'pages',
      extensions: ['vue', 'md'],
    }),
    Inspect(),
  ],
  build: {
    sourcemap: true,
  },
})
