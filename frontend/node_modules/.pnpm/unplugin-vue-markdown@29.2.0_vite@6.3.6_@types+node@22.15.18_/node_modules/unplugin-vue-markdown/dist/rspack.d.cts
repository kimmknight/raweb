import { Options } from './types.cjs';
import '@mdit-vue/plugin-component';
import '@mdit-vue/plugin-frontmatter';
import '@mdit-vue/types';
import 'markdown-it-async';
import 'unplugin-utils';

declare const _default: (options: Options) => RspackPluginInstance;

export { _default as default };
