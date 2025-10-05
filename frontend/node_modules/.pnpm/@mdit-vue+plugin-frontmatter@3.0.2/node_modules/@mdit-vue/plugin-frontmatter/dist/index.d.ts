import grayMatter from "gray-matter";
import { PluginWithOptions } from "markdown-it";

//#region src/types.d.ts
type GrayMatterOptions = grayMatter.GrayMatterOption<string, GrayMatterOptions>;
/**
 * Options of @mdit-vue/plugin-frontmatter
 */
interface FrontmatterPluginOptions {
  /**
   * Options of gray-matter
   *
   * @see https://github.com/jonschlinkert/gray-matter#options
   */
  grayMatterOptions?: GrayMatterOptions;
  /**
   * Render the excerpt or not
   *
   * @default true
   */
  renderExcerpt?: boolean;
}
declare module '@mdit-vue/types' {
  interface MarkdownItEnv {
    /**
     * The raw Markdown content without frontmatter
     */
    content?: string;
    /**
     * The excerpt that extracted by `@mdit-vue/plugin-frontmatter`
     *
     * - Would be the rendered HTML when `renderExcerpt` is enabled
     * - Would be the raw Markdown when `renderExcerpt` is disabled
     */
    excerpt?: string;
    /**
     * The frontmatter that extracted by `@mdit-vue/plugin-frontmatter`
     */
    frontmatter?: Record<string, unknown>;
  }
}
//#endregion
//#region src/frontmatter-plugin.d.ts
/**
 * Get markdown frontmatter and excerpt
 *
 * Extract them into env
 */
declare const frontmatterPlugin: PluginWithOptions<FrontmatterPluginOptions>;
//#endregion
export { FrontmatterPluginOptions, frontmatterPlugin };