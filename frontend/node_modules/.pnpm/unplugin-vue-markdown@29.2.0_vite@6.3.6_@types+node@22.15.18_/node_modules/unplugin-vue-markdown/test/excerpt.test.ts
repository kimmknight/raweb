import { describe, expect, it } from 'vitest'
import { createMarkdown } from '../src/core/markdown'
import { resolveOptions } from '../src/core/options'

describe('excerpt', () => {
  it('rendered excerpt', async () => {
    const options = resolveOptions({
      excerpt: true,
      frontmatterOptions: {
        grayMatterOptions: {
          excerpt: true,
          excerpt_separator: '<!-- more -->',
        },
      },
    })
    const markdownToVue = createMarkdown(options)
    const md = `---
title: Hey
---

This is an excerpt which has been rendered to **HTML**.

<!-- more -->

# Hello

- A
- B
- C`
    expect((await markdownToVue('', md)).code).toMatchSnapshot()
  })

  it('raw excerpt', async () => {
    const options = resolveOptions({
      excerpt: true,
      frontmatterOptions: {
        renderExcerpt: false,
        grayMatterOptions: {
          excerpt: true,
          excerpt_separator: '<!-- more -->',
        },
      },
    })
    const markdownToVue = createMarkdown(options)
    const md = `---
title: Hey
---

This is an excerpt which is kept as **raw Markdown**.

<!-- more -->

# Hello

- A
- B
- C`
    expect((await markdownToVue('', md)).code).toMatchSnapshot()
  })
})
