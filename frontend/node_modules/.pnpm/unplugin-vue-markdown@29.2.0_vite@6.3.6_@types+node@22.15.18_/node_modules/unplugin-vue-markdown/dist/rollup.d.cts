import * as rollup from 'rollup';
import { Options } from './types.cjs';
import '@mdit-vue/plugin-component';
import '@mdit-vue/plugin-frontmatter';
import '@mdit-vue/types';
import 'markdown-it-async';
import 'unplugin-utils';

declare const _default: (options: Options) => rollup.Plugin<any> | rollup.Plugin<any>[];

export { _default as default };
