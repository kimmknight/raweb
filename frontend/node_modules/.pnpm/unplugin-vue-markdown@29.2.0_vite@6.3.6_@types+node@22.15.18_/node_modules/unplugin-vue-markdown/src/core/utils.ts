import { createRequire } from 'node:module'

const _require = typeof require === 'undefined'
  ? createRequire(import.meta.url)
  : require

export function getVueVersion(defaultVersion = '3.2.0') {
  try {
    let v = _require('vue')
    if (v.default)
      v = v.default
    return v.version || defaultVersion
  }
  catch {
    return defaultVersion
  }
}
