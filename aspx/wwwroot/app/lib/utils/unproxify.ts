export function unproxify<T>(val: T): T {
  if (Array.isArray(val)) {
    // If the value is an array, recursively apply unproxify to each item
    return val.map(unproxify) as unknown as T;
  }
  if (val && typeof val === 'object') {
    // If the value is an object, recursively apply unproxify to each entry
    return Object.fromEntries(Object.entries(val).map(([k, v]) => [k, unproxify(v)])) as T;
  }
  return val; // Return the value if it's neither an object nor an array
}
