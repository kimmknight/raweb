# markdown-it-async

[![npm version][npm-version-src]][npm-version-href]
[![npm downloads][npm-downloads-src]][npm-downloads-href]
[![bundle][bundle-src]][bundle-href]
[![JSDocs][jsdocs-src]][jsdocs-href]
[![License][license-src]][license-href]

Enhance [`markdown-it`](https://github.com/markdown-it/markdown-it) to support **async** highlight function.

This package is a wrapper on top of `markdown-it` instead of a fork. Updates from `markdown-it` will be reflected without as transient dependencies.

## Usage

```ts
import MarkdownItAsync from 'markdown-it-async'

const md = MarkdownItAsync({
  async highlight(code, lang) {
    const { codeToHtml } = await import('shiki')
    return await codeToHtml(code, { lang, theme: 'vitesse-dark' })
  }
})

// Note you need to use `renderAsync` instead of `render`
const html = await md.renderAsync(markdown)
```

## Opt-in Warning

If you integrate this package into your project, and want to make sure you have every usage of `md.render` migrated to `md.renderAsync`, you can enable the `warnOnSyncRender` option.

```ts
const md = MarkdownItAsync({
  warnOnSyncRender: true
})

md.render('Hello') // This will throw a conole warning
```

## How it works?

This package is a thin wrapper around `markdown-it` to support async highlight function. It uses [the approach suggested in `markdown-it`'s docs](https://github.com/markdown-it/markdown-it/blob/master/docs/development.md#i-need-async-rule-how-to-do-it), by putting placeholders in sync mode and then replace them with async results.

The goal is to make it easy to use and absorb the "hack" into the library itself.

## Sponsors

<p align="center">
  <a href="https://cdn.jsdelivr.net/gh/antfu/static/sponsors.svg">
    <img src='https://cdn.jsdelivr.net/gh/antfu/static/sponsors.svg'/>
  </a>
</p>

## License

[MIT](./LICENSE) License © 2024-PRESENT [Anthony Fu](https://github.com/antfu)

<!-- Badges -->

[npm-version-src]: https://img.shields.io/npm/v/markdown-it-async?style=flat&colorA=080f12&colorB=1fa669
[npm-version-href]: https://npmjs.com/package/markdown-it-async
[npm-downloads-src]: https://img.shields.io/npm/dm/markdown-it-async?style=flat&colorA=080f12&colorB=1fa669
[npm-downloads-href]: https://npmjs.com/package/markdown-it-async
[bundle-src]: https://img.shields.io/bundlephobia/minzip/markdown-it-async?style=flat&colorA=080f12&colorB=1fa669&label=minzip
[bundle-href]: https://bundlephobia.com/result?p=markdown-it-async
[license-src]: https://img.shields.io/github/license/antfu/markdown-it-async.svg?style=flat&colorA=080f12&colorB=1fa669
[license-href]: https://github.com/antfu/markdown-it-async/blob/main/LICENSE
[jsdocs-src]: https://img.shields.io/badge/jsdocs-reference-080f12?style=flat&colorA=080f12&colorB=1fa669
[jsdocs-href]: https://www.jsdocs.io/package/markdown-it-async
