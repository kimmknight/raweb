import type { MetaProperty, ResolvedOptions } from '../src/types'
import { readFile } from 'node:fs/promises'
import { describe, expect, it } from 'vitest'
import { createMarkdown } from '../src/core/markdown'
import { resolveOptions } from '../src/core/options'

const frontmatterPreprocess: ResolvedOptions['frontmatterPreprocess'] = (fm) => {
  const frontmatter = {
    title: 'default title',
    description: 'default description',
    ...fm,
  }
  const meta: MetaProperty[] = [
    { property: 'og:title', name: 'twitter:title', itemprop: 'title', content: frontmatter.title },
    {
      property: 'og:description',
      name: 'twitter:description',
      itemprop: 'description',
      content: frontmatter.description,
    },
  ]
  return {
    head: { ...frontmatter, meta },
    frontmatter: { ...frontmatter, meta },
  }
}

describe('provide bespoke frontmatter processor', () => {
  it('inline markdown is used over default properties', async () => {
    const markdownToVue = createMarkdown(resolveOptions({ frontmatterPreprocess }))
    const md = (await markdownToVue('', await readFile('test/fixtures/simple.md', 'utf-8'))).code

    // Positive tests
    expect(
      md.includes('Hello World'),
      'the title attribute is retained over the default \'title\' value',
    ).toBeTruthy()
    expect(
      md.includes('testing is the path to true happiness'),
      'description property is also retained',
    ).toBeTruthy()
    // Negative tests
    expect(
      md.includes('default title'),
      'the title attribute is retained over the default \'title\' value',
    ).toBeFalsy()
    expect(md.includes('default description'), 'default description is ignored').toBeFalsy()

    // Meta props
    expect(md.includes('og:title')).toBeTruthy()
    expect(md.includes('og:description')).toBeTruthy()
  })
})
