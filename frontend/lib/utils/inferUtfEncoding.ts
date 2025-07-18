/**
 * Looks at the byte-order mark (BOM) of a given ArrayBuffer to infer the UTF encoding.
 *
 * This function supports detecting UTF-8, UTF-16, and UTF-32 encodings. A buffer
 * with no recognized BOM is assumed to be UTF-8.
 */
export function inferUtfEncoding(
  buffer: Uint8Array
): 'utf-8' | 'utf-16le' | 'utf-16be' | 'utf-32le' | 'utf-32be' {
  if (buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf) {
    return 'utf-8';
  }

  if (buffer[0] == 0xff && buffer[1] == 0xfe && buffer[2] == 0x00 && buffer[3] == 0x00) {
    return 'utf-32le';
  }

  if (buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0xfe && buffer[3] == 0xff) {
    return 'utf-32be';
  }

  if (buffer[0] == 0xff && buffer[1] == 0xfe) {
    return 'utf-16le';
  }

  if (buffer[0] == 0xfe && buffer[1] == 0xff) {
    return 'utf-16be';
  }

  return 'utf-8'; // default to utf-8 if no known BOM is found
}
