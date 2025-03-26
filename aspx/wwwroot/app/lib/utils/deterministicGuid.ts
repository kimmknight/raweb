export async function deterministicGuid(input: string): Promise<string> {
  const data = new TextEncoder().encode(input);
  const hashBuffer = await crypto.subtle.digest('SHA-1', data);
  const hash = new Uint8Array(hashBuffer).slice(0, 16);

  // set version to 5 (name-based)
  hash[6] = (hash[6] & 0x0f) | 0x50;
  // set variant (RFC4122)
  hash[8] = (hash[8] & 0x3f) | 0x80;

  const toHex = (b: number) => b.toString(16).padStart(2, '0');
  const hex = [...hash].map(toHex);

  return `${hex.slice(0, 4).join('')}-${hex.slice(4, 6).join('')}-${hex.slice(6, 8).join('')}-${hex
    .slice(8, 10)
    .join('')}-${hex.slice(10, 16).join('')}`;
}
