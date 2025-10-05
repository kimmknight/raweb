import type { TransformResult } from 'vite'
import type { MarkdownEnv, ResolvedOptions } from '../types'
import { toArray, uniq } from '@antfu/utils'
import { componentPlugin } from '@mdit-vue/plugin-component'
import { frontmatterPlugin } from '@mdit-vue/plugin-frontmatter'
import { preprocessHead } from './head'

const scriptSetupRE = /<\s*script([^>]*)\bsetup\b([^>]*)>([\s\S]*)<\/script>/g
const defineExposeRE = /defineExpose\s*\(/g

const EXPORTS_KEYWORDS = [
  'class',
  'default',
  'export',
  'function',
  'import',
  'let',
  'var',
  'const',
  'from',
  'as',
  'return',
  'if',
  'else',
  'switch',
  'case',
  'break',
  'for',
  'while',
  'do',
]

interface ScriptMeta {
  code: string
  attr: string
}

function extractScriptSetup(html: string) {
  const scripts: ScriptMeta[] = []
  html = html.replace(scriptSetupRE, (_, attr1, attr2, code) => {
    scripts.push({
      code,
      attr: `${attr1} ${attr2}`.trim(),
    })
    return ''
  })

  return { html, scripts }
}

function extractCustomBlock(html: string, options: ResolvedOptions) {
  const blocks: string[] = []
  for (const tag of options.customSfcBlocks) {
    html = html.replace(new RegExp(`<${tag}[^>]*\\b[^>]*>[^<]*<\\/${tag}>`, 'gm'), (code) => {
      blocks.push(code)
      return ''
    })
  }

  return { html, blocks }
}

export function createMarkdown(options: ResolvedOptions) {
  const isVue2 = options.vueVersion.startsWith('2.')

  const setupPromise = (async () => {
    const { default: MarkdownIt } = await import('markdown-it-async')

    const md = MarkdownIt({
      html: true,
      linkify: true,
      typographer: true,
      ...options.markdownItOptions,
    })

    md.use(componentPlugin, options.componentOptions)

    if (options.frontmatter || options.excerpt) {
      md.use(frontmatterPlugin, {
        ...options.frontmatterOptions,
        grayMatterOptions: {
          excerpt: options.excerpt,
          ...options.frontmatterOptions.grayMatterOptions,
        },
      })
    }

    md.linkify.set({ fuzzyLink: false })

    options.markdownItUses.forEach((e) => {
      const [plugin, options] = toArray(e)

      md.use(plugin, options)
    })

    await options.markdownItSetup(md)

    return md
  })()

  return async (id: string, raw: string): Promise<TransformResult> => {
    const md = await setupPromise

    const {
      wrapperDiv,
      wrapperClasses,
      wrapperComponent,
      transforms,
      headEnabled,
      frontmatterPreprocess,
    } = options

    raw = raw.trimStart()
    raw = await transforms.before?.(raw, id) ?? raw

    const env: MarkdownEnv = { id }
    let html = await md.renderAsync(raw, env)
    const { excerpt = '', frontmatter: data = null } = env

    if (wrapperDiv) {
      const wrapperClassesResolved = toArray(
        typeof wrapperClasses === 'function'
          ? wrapperClasses(id, raw)
          : wrapperClasses,
      )
        .filter(Boolean)
        .join(' ')

      if (wrapperClassesResolved)
        html = `<div class="${wrapperClassesResolved}">${html}</div>`
      else
        html = `<div>${html}</div>`
    }

    const wrapperComponentName = typeof wrapperComponent === 'function'
      ? wrapperComponent(id, raw)
      : wrapperComponent

    if (wrapperComponentName) {
      const attrs = [
        options.frontmatter && ':frontmatter="frontmatter"',
        options.excerpt && ':excerpt="excerpt"',
      ].filter(Boolean).join(' ')
      html = `<${wrapperComponentName} ${attrs}>${html}</${wrapperComponentName}>`
    }

    html = await transforms.after?.(html, id) ?? html

    if (options.escapeCodeTagInterpolation) {
      // escape curly brackets interpolation in <code>, #14
      html = html.replace(/<code(.*?)>/g, '<code$1 v-pre>')
    }

    const hoistScripts = extractScriptSetup(html)
    html = hoistScripts.html
    const customBlocks = extractCustomBlock(html, options)
    html = customBlocks.html

    const scriptLines: string[] = []
    let frontmatterExportsLines: string[] = []
    let excerptExportsLine = ''
    let excerptKeyOverlapping = false

    function hasExplicitExports() {
      return defineExposeRE.test(hoistScripts.scripts.map(i => i.code).join(''))
    }

    if (options.frontmatter) {
      if (options.excerpt && data) {
        if (data.excerpt !== undefined)
          excerptKeyOverlapping = true
        data.excerpt = excerpt
      }

      const { head, frontmatter } = frontmatterPreprocess(data || {}, options, id, preprocessHead)

      if (options.excerpt && !excerptKeyOverlapping && frontmatter.excerpt !== undefined)
        delete frontmatter.excerpt

      scriptLines.push(`const frontmatter = ${JSON.stringify(frontmatter)}`)

      if (options.exportFrontmatter) {
        frontmatterExportsLines = Object.entries(frontmatter)
          .map(([key, value]) => {
            if (EXPORTS_KEYWORDS.includes(key))
              key = `_${key}`
            return `export const ${key} = ${JSON.stringify(value)}`
          })
      }

      if (!isVue2 && options.exposeFrontmatter && !hasExplicitExports())
        scriptLines.push('defineExpose({ frontmatter })')

      if (!isVue2 && headEnabled && head) {
        // @ts-expect-error legacy option
        if (headEnabled === 'vueuse')
          throw new Error('unplugin-vue-markdown no longer supports @vueuse/head. Change `headEnabled` to `true` and install `@unhead/vue` instead.')
        scriptLines.push(`const head = ${JSON.stringify(head)}`)
        scriptLines.unshift(`import { useHead } from "@unhead/vue"`)
        scriptLines.push('useHead(head)')
      }

      scriptLines.push(...await transforms.extraScripts?.(frontmatter, id) || [])
    }

    if (options.excerpt) {
      scriptLines.push(`const excerpt = ${JSON.stringify(excerpt)}`)

      if (!excerptKeyOverlapping)
        excerptExportsLine = `export const excerpt = ${JSON.stringify(excerpt)}\n`

      if (!isVue2 && options.exposeExcerpt && !hasExplicitExports())
        scriptLines.push('defineExpose({ excerpt })')
    }

    scriptLines.push(...hoistScripts.scripts.map(i => i.code))

    let attrs = uniq(hoistScripts.scripts.map(i => i.attr)).join(' ').trim()
    if (attrs)
      attrs = ` ${attrs}`

    const scripts = isVue2
      ? [
          `<script${attrs}>`,
          ...scriptLines,
          ...frontmatterExportsLines,
          excerptExportsLine,
          'export default { data() { return { frontmatter } } }',
          '</script>',
        ]
      : [
          `<script setup${attrs}>`,
          ...scriptLines,
          '</script>',
          ...((frontmatterExportsLines.length || excerptExportsLine)
            ? [
                `<script${attrs}>`,
                ...frontmatterExportsLines,
                excerptExportsLine,
                '</script>',
              ]
            : []),
        ]

    const code = [
      `<template>${html}</template>`,
      ...scripts.map(i => i.trim()).filter(Boolean),
      ...customBlocks.blocks,
    ].join('\n')

    return {
      code,
      map: { mappings: '' } as any,
    }
  }
}
