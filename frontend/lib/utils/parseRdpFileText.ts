import { groupResourceProperties, type getAppsAndDevices } from '$utils';

type Resource = NonNullable<Awaited<ReturnType<typeof getAppsAndDevices>>>['resources'][number];
type AppOrDesktopProperties = Partial<NonNullable<Resource['hosts'][number]['rdp']>>;

/**
 * Parses the text content of an RDP file into grouped resource properties.
 */
export function parseRdpFileText(rdpFileContent: string, includeMissing = false) {
  // parse the RDP file content into properties
  const rdpFileProperties: AppOrDesktopProperties = {};
  const lines = rdpFileContent.split('\n');
  for (const line of lines) {
    const parts = line.trim().split(':');
    const key = parts.slice(0, 2).join(':');
    const value = parts.slice(2).join(':');
    rdpFileProperties[key as keyof AppOrDesktopProperties] = value;
  }

  // group the resource properties
  const resourceProperties = groupResourceProperties(rdpFileProperties, includeMissing);
  return {
    ...resourceProperties,
    raw: rdpFileProperties,
  };
}
