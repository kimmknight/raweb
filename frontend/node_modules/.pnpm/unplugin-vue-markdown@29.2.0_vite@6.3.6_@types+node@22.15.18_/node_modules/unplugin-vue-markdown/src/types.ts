import type { ComponentPluginOptions } from '@mdit-vue/plugin-component'
import type { FrontmatterPluginOptions } from '@mdit-vue/plugin-frontmatter'
import type { MarkdownItEnv } from '@mdit-vue/types'
import type {
  MarkdownItAsync,
  MarkdownItAsyncOptions,
  PluginSimple as MarkdownItPluginSimple,
  PluginWithOptions as MarkdownItPluginWithOptions,
} from 'markdown-it-async'
import type { FilterPattern } from 'unplugin-utils'
import type { preprocessHead } from './core/head'

/** a `<meta />` property in HTML is defined with the following name/values */
export interface MetaProperty {
  key?: string
  /**
   * the "name" property used by Facebook and other providers who
   * use the Opengraph standards
   */
  property?: string
  /**
   * used by google to identify the "name" of the name/value pair
   */
  itemprop?: string
  /**
   * used by Twitter to indicate the "name" field in a meta properties
   * name/value pairing
   */
  name?: string
  /**
   * The value of the meta property
   */
  content?: any
  [key: string]: unknown
}

/**
 * Frontmatter content is represented as key/value dictionary
 */
export interface Frontmatter {
  title?: string
  name?: string
  description?: string
  meta?: MetaProperty[]
  [key: string]: unknown
}

export interface Options {
  /**
   * Explicitly set the Vue version
   *
   * @default auto detected
   */
  vueVersion?: string

  /**
   * Enable head support, need to install @unhead/vue and register to App in main.js
   *
   * @default true
   */
  headEnabled?: boolean

  /**
   * The head field in frontmatter used to be used for @unhead/vue
   *
   * When an empty string is passed, it will use the root properties of the frontmatter
   *
   * @default ''
   */
  headField?: string

  /**
   * Parse for frontmatter
   *
   * @default true
   */
  frontmatter?: boolean

  /**
   * Parse for excerpt
   *
   * If `true`, it will be passed to `frontmatterPreprocess` as `frontmatter.excerpt`, replacing the `excerpt` key in frontmatter, if there's any
   *
   * @default false
   */
  excerpt?: boolean

  /**
   * Remove custom SFC block
   *
   * @default ['route', 'i18n']
   */
  customSfcBlocks?: string[]

  /**
   * Options passed to [@mdit-vue/plugin-component](https://github.com/mdit-vue/mdit-vue/tree/main/packages/plugin-component)
   */
  componentOptions?: ComponentPluginOptions

  /**
   * Options passed to [@mdit-vue/plugin-frontmatter](https://github.com/mdit-vue/mdit-vue/tree/main/packages/plugin-frontmatter)
   */
  frontmatterOptions?: FrontmatterPluginOptions

  /**
   * Custom function to provide defaults to the frontmatter and
   * move certain attributes into the "meta" category.
   *
   * Note: _overriding this will remove built-in functionality setting
   * "meta" properties and the built-in "head" support. Do this only
   * if you know what you're doing._
   */
  frontmatterPreprocess?: (
    frontmatter: Frontmatter,
    options: ResolvedOptions,
    id: string,
    defaultHeadProcess: typeof preprocessHead,
  ) => {
    head: Record<string, any>
    frontmatter: Frontmatter
  }

  /**
   * Expose frontmatter via expose API
   *
   * @default true
   */
  exposeFrontmatter?: boolean

  /**
   * Expose excerpt via expose API
   *
   * @default false
   */
  exposeExcerpt?: boolean

  /**
   * Export frontmatter in component module
   *
   * @default true
   */
  exportFrontmatter?: boolean

  /**
   * Add `v-pre` to `<code>` tag to escape curly brackets interpolation
   *
   * @see https://github.com/unplugin/unplugin-vue-markdown/issues/14
   * @default true
   */
  escapeCodeTagInterpolation?: boolean

  /**
   * Options passed to Markdown It
   */
  markdownItOptions?: MarkdownItAsyncOptions

  /**
   * Plugins for Markdown It
   */
  markdownItUses?: (
    | MarkdownItPluginSimple
    | [MarkdownItPluginSimple | MarkdownItPluginWithOptions<any>, any]
    | any
  )[]

  /**
   * A function providing the Markdown It instance gets the ability to apply custom
   * settings/plugins
   */
  markdownItSetup?: (MarkdownIt: MarkdownItAsync) => void | Promise<void>

  /**
   * Wrap the rendered html in a div
   *
   * @default true
   */
  wrapperDiv?: boolean

  /**
   * Class names for wrapper div
   *
   * This option will be ignored if `wrapperDiv` is set to `false`
   *
   * @default 'markdown-body'
   */
  wrapperClasses?: string | string[] | undefined | null | ((id: string, code: string) => string | string[] | undefined | null)

  /**
   * Component name to wrapper with
   *
   * @default undefined
   */
  wrapperComponent?: string | undefined | null | ((id: string, code: string) => string | undefined | null)

  /**
   * Custom tranformations apply before and after the markdown transformation
   */
  transforms?: {
    before?: (code: string, id: string) => string | Promise<string>
    after?: (code: string, id: string) => string | Promise<string>
    /**
     * Return extra code to be injected into the `<script>` tag
     */
    extraScripts?: (frontmatter: Record<string, any>, id: string) => string[] | Promise<string[]>
  }

  include?: FilterPattern
  exclude?: FilterPattern
}

export interface ResolvedOptions extends Required<Options> { }

export interface MarkdownEnv extends MarkdownItEnv {
  id: string
}
