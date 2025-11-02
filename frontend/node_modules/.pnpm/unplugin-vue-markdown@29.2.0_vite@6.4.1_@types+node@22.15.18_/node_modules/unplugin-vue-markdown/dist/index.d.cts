import * as unplugin from 'unplugin';
import { UnpluginFactory } from 'unplugin';
import { Options } from './types.cjs';
import '@mdit-vue/plugin-component';
import '@mdit-vue/plugin-frontmatter';
import '@mdit-vue/types';
import 'markdown-it-async';
import 'unplugin-utils';

declare const unpluginFactory: UnpluginFactory<Options>;
declare const _default: unplugin.UnpluginInstance<Options, boolean>;

export { _default as default, unpluginFactory };
