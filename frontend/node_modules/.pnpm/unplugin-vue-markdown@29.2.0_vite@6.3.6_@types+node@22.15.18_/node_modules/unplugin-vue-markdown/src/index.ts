import type { UnpluginFactory } from 'unplugin'
import type { Options } from './types'
import { createUnplugin } from 'unplugin'
import { createFilter } from 'unplugin-utils'
import { createMarkdown } from './core/markdown'
import { resolveOptions } from './core/options'

const cssIdRE = /\.(css|postcss|sass|scss|less|stylus|styl)($|\?)/

export const unpluginFactory: UnpluginFactory<Options> = (userOptions = {}) => {
  const options = resolveOptions(userOptions)
  const markdownToVue = createMarkdown(options)

  const filter = createFilter(
    userOptions.include || /\.md$|\.md\?vue/,
    userOptions.exclude || cssIdRE,
  )

  return {
    name: 'unplugin-vue-markdown',
    enforce: 'pre',
    transformInclude(id) {
      return filter(id)
    },
    async transform(raw, id) {
      try {
        return await markdownToVue(id, raw)
      }
      catch (e: any) {
        this.error(e)
      }
    },
    vite: {
      async handleHotUpdate(ctx) {
        if (!filter(ctx.file))
          return

        const defaultRead = ctx.read
        ctx.read = async function () {
          return (await markdownToVue(ctx.file, await defaultRead())).code
        }
      },
    },
  }
}

export default /* #__PURE__ */ createUnplugin(unpluginFactory)
