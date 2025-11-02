import * as vite from 'vite';
import { Options } from './types.js';
import '@mdit-vue/plugin-component';
import '@mdit-vue/plugin-frontmatter';
import '@mdit-vue/types';
import 'markdown-it-async';
import 'unplugin-utils';

declare const _default: (options: Options) => vite.Plugin<any> | vite.Plugin<any>[];

export { _default as default };
