import * as esbuild from 'esbuild';
import { Options } from './types.cjs';
import '@mdit-vue/plugin-component';
import '@mdit-vue/plugin-frontmatter';
import '@mdit-vue/types';
import 'markdown-it-async';
import 'unplugin-utils';

declare const _default: (options: Options) => esbuild.Plugin;

export { _default as default };
