import type MarkdownIt, { Options, PresetName, PluginSimple as PluginSimple$1, PluginWithOptions as PluginWithOptions$1, PluginWithParams as PluginWithParams$1 } from 'markdown-it';

type PluginSimple = ((md: MarkdownItAsync) => void);
type PluginWithOptions<T = any> = ((md: MarkdownItAsync, options?: T) => void);
type PluginWithParams = ((md: MarkdownItAsync, ...params: any[]) => void);
interface MarkdownItAsyncOptions extends Omit<Options, 'highlight'> {
    /**
     * Highlighter function for fenced code blocks.
     * Highlighter `function (str, lang, attrs)` should return escaped HTML. It can
     * also return empty string if the source was not changed and should be escaped
     * externally. If result starts with <pre... internal wrapper is skipped.
     * @default null
     */
    highlight?: ((str: string, lang: string, attrs: string) => string | Promise<string>) | null | undefined;
    /**
     * Emit warning when calling `md.render` instead of `md.renderAsync`.
     *
     * @default false
     */
    warnOnSyncRender?: boolean;
}

type MarkdownItAsyncPlaceholderMap = Map<string, [promise: Promise<string>, str: string, lang: string, attrs: string]>;
declare class MarkdownItAsync extends MarkdownIt {
     // @ts-ignore
    options: MarkdownItAsyncOptions
    placeholderMap: MarkdownItAsyncPlaceholderMap;
    private disableWarn;
    constructor(presetName: PresetName, options?: MarkdownItAsyncOptions);
    constructor(options?: MarkdownItAsyncOptions);
    use(plugin: PluginSimple): this;
    use(plugin: PluginSimple$1): this;
    use<T = any>(plugin: PluginWithOptions<T>, options?: T): this;
    use<T = any>(plugin: PluginWithOptions$1<T>, options?: T): this;
    use(plugin: PluginWithParams, ...params: any[]): this;
    use(plugin: PluginWithParams$1, ...params: any[]): this;
    render(src: string, env?: any): string;
    renderAsync(src: string, env?: any): Promise<string>;
}
declare function createMarkdownItAsync(presetName: PresetName, options?: MarkdownItAsyncOptions): MarkdownItAsync;
declare function createMarkdownItAsync(options?: MarkdownItAsyncOptions): MarkdownItAsync;
declare function replaceAsync(string: string, searchValue: RegExp, replacer: (...args: string[]) => Promise<string>): Promise<string>;

export { MarkdownItAsync, type MarkdownItAsyncOptions, type MarkdownItAsyncPlaceholderMap, type MarkdownItAsyncOptions as Options, type PluginSimple, type PluginWithOptions, type PluginWithParams, createMarkdownItAsync, createMarkdownItAsync as default, replaceAsync };
