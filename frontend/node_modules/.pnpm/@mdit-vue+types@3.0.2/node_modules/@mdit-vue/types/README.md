# @mdit-vue/types

[![npm](https://badgen.net/npm/v/@mdit-vue/types)](https://www.npmjs.com/package/@mdit-vue/types)

Shared types definition of mdit-vue.

## Install

```sh
npm i @mdit-vue/types
```

## Usage

Add more properties to `env` via [TypeScript module augmentation](https://www.typescriptlang.org/docs/handbook/declaration-merging.html#module-augmentation):

```ts
declare module '@mdit-vue/types' {
  interface MarkdownItEnv {
    foo?: string[];
  }
}
```

```ts
import type { MarkdownItEnv } from '@mdit-vue/types';
import MarkdownIt from 'markdown-it';

const md = MarkdownIt({ html: true });
const env: MarkdownItEnv = {};
const rendered = md.render('raw markdown text', env);
```
