export function toKebabCase(str: string) {
  return str
    .replace(/([a-z])([A-Z])/g, '$1-$2') // insert hyphen between camelCase
    .replace(/[\s_]+/g, '-') // replace spaces and underscores with hyphen
    .toLowerCase();
}
