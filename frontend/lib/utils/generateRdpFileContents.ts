type Resource = NonNullable<
  Awaited<ReturnType<typeof import('$utils/getAppsAndDevices').getAppsAndDevices>>
>['resources'][number];

type AppOrDesktopProperties = Partial<NonNullable<Resource['hosts'][number]['rdp']>>;

/**
 * Generates the contents of an RDP file based on the provided properties.
 *
 * Property names should match the expected RDP file format, and values should be strings or numbers (integers).
 * All other types, including null and undefined, are ignored.
 *
 * Property names SHOULD NOT include ':i' or ':s' suffixes. These are added automatically based on the type of the value.
 *
 * To review valid properties, visit https://kimmknight.github.io/rdpfileeditor/.
 *
 * @param properties - The properties to include in the RDP file.
 */
export function generateRdpFileContents({ rdpFileText, ...properties }: AppOrDesktopProperties) {
  let text = '';

  for (const [key, value] of Object.entries(properties)) {
    if (typeof value === 'string' || typeof value === 'number') {
      const type = key === 'password 51' ? 'b' : typeof value === 'number' ? 'i' : 's';
      text += `${key}:${type}:${value}\r\n`;
    }
  }

  return text;
}
