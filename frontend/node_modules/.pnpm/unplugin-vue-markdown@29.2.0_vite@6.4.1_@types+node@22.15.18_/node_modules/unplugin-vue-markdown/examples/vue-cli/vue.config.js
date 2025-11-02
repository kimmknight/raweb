const { defineConfig } = require('@vue/cli-service')
const prism = require('markdown-it-prism')
const Markdown = require('unplugin-vue-markdown/webpack')

module.exports = defineConfig({
  transpileDependencies: true,
  parallel: false,
  chainWebpack: (config) => {
    config.module
      .rule('vue')
      .test(/\.(vue|md)$/)

    config
      .plugin('markdown')
      .use(Markdown({
        markdownItUses: [
          prism,
        ],
      }))
  },
})
