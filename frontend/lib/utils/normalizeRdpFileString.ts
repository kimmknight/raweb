export function normalizeRdpFileString(rdpString: string | undefined) {
  if (!rdpString) {
    return '';
  }

  return rdpString
    .split('\n')
    .map((line) => line.trim())
    .filter((line) => line.length > 0)
    .sort()
    .join('\n')
    .normalize('NFC');
}
