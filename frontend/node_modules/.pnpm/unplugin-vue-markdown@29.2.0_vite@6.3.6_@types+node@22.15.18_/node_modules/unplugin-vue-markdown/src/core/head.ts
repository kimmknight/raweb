import type { Frontmatter, MetaProperty, ResolvedOptions } from '../types'

const headProperties = [
  'title',
  'meta',
  'link',
  'base',
  'style',
  'script',
  'htmlAttrs',
  'bodyAttrs',
]

export function preprocessHead<T extends Frontmatter>(frontmatter: T, options: ResolvedOptions) {
  if (!options.headEnabled)
    return frontmatter

  const head: Frontmatter = options.headField
    ? (frontmatter[options.headField] as Frontmatter || {})
    : frontmatter

  const meta = (head.meta = head.meta || []) as MetaProperty[]

  if (head.title) {
    if (!meta.find((i: any) => i.property === 'og:title'))
      meta.push({ property: 'og:title', content: head.title })

    if (!meta.find((i: any) => i.name === 'twitter:title'))
      meta.push({ name: 'twitter:title', content: head.title })
  }

  if (head.description) {
    if (!meta.find((i: any) => i.name === 'description'))
      meta.push({ name: 'description', content: head.description })

    if (!meta.find((i: any) => i.property === 'og:description'))
      meta.push({ property: 'og:description', content: head.description })

    if (!meta.find((i: any) => i.name === 'twitter:description'))
      meta.push({ name: 'twitter:description', content: head.description })
  }

  if (head.image) {
    if (!meta.find((i: any) => i.property === 'og:image'))
      meta.push({ property: 'og:image', content: head.image })

    if (!meta.find((i: any) => i.name === 'twitter:image'))
      meta.push({ name: 'twitter:image', content: head.image })

    if (!meta.find((i: any) => i.property === 'twitter:card'))
      meta.push({ name: 'twitter:card', content: 'summary_large_image' })
  }

  const result: any = {}

  for (const [key, value] of Object.entries(head)) {
    if (headProperties.includes(key))
      result[key] = value
  }

  return Object.entries(result).length === 0 ? null : result
}
