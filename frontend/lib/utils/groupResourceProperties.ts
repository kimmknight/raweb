import type { getAppsAndDevices } from './getAppsAndDevices.ts';

type Resource = NonNullable<Awaited<ReturnType<typeof getAppsAndDevices>>>['resources'][number];
type AppOrDesktopProperties = Partial<NonNullable<Resource['hosts'][number]['rdp']>>;

/**
 * Groups resource (RDP file) properties into categories for easier management.
 *
 * As part of this process, types will be coerced appropriately, e.g., converting byte strings to Uint8Array for binary data
 * or ensuring numeric values are stored as numbers.
 */
export function groupResourceProperties(rdpFileData: AppOrDesktopProperties, includeMissing = false) {
  const groupedProperties: GroupedAppOrDesktopProperties = {
    connection: {},
    display: {},
    gateway: {},
    hardware: {},
    remoteapp: {},
    session: {},
    signature: {},
    raweb: {},
  };

  for (const [key, value] of Object.entries(rdpFileData)) {
    // skip undefined values
    if (value === undefined) {
      continue;
    }

    // find the group for the current key
    let foundGroup: keyof typeof groups | null = null;
    let foundKey: AnyPropertyGroupValue[number] | null = null;
    for (const [groupName, properties] of Object.entries(groups) as [
      keyof typeof groups,
      AnyPropertyGroupValue
    ][]) {
      // if the key does not have a type suffix, check for all types
      const possibleKeys = key.includes(':') ? [key] : [key + ':s', key + ':i', key + ':b'];

      // find a matching key and determine its type
      for (const possibleKey of possibleKeys as unknown as AnyPropertyGroupValue) {
        if ((properties as unknown as string[]).includes(possibleKey)) {
          foundGroup = groupName;
          foundKey = possibleKey;
          break;
        }
      }
    }

    if (!foundGroup || !foundKey) {
      continue; // skip properties that don't belong to any group
    }

    // coerce the value type as needed
    let coercedValue: string | number | Uint8Array;
    const foundKeyType = foundKey.split(':')[1];
    if (foundKeyType === 'i') {
      coercedValue = typeof value === 'number' ? value : parseInt(value as string, 10);
    } else if (foundKeyType === 'b') {
      // convert hex-encoded byte string to Uint8Array
      const hex = value.toString().trim();
      const byteArray = new Uint8Array(hex.length / 2);
      for (let i = 0; i < hex.length; i += 2) {
        byteArray[i / 2] = parseInt(hex.slice(i, i + 2), 16);
      }
      coercedValue = byteArray;
    } else {
      coercedValue = value.toString();
    }

    // assign the coerced value to the appropriate key and group
    // @ts-expect-error - typescript cannot infer that foundKey matches the expected type here
    groupedProperties[foundGroup][foundKey] = coercedValue;
  }

  if (includeMissing) {
    // ensure all properties are present, even if undefined
    for (const [groupName, properties] of Object.entries(groups) as [
      keyof typeof groups,
      AnyPropertyGroupValue
    ][]) {
      for (const propertyKey of properties as unknown as AnyProperty[]) {
        // @ts-expect-error - typescript cannot infer that propertyKey matches the expected type here
        if (groupedProperties[groupName][propertyKey] === undefined) {
          // @ts-expect-error - typescript cannot infer that propertyKey matches the expected type here
          groupedProperties[groupName][propertyKey] = undefined;
        }
      }
    }
  }

  return groupedProperties;
}

// originally adapted from https://github.com/kimmknight/rdpfileeditor/blob/main/rdpoptions.json
const groups = {
  connection: [
    'alternate full address:s',
    'authentication level:i',
    'autoreconnect max retries:i',
    'autoreconnection enabled:i',
    'bandwidthautodetect:i',
    'compression:i',
    'connection type:i',
    'domain:s',
    'enablecredsspsupport:i',
    'enablerdsaadauth:i',
    'full address:s',
    'kdcproxyname:s',
    'negotiate security layer:i',
    'networkautodetect:i',
    'password 51:b',
    'prompt for credentials:i',
    'prompt for credentials on client:i',
    'promptcredentialonce:i',
    'server port:i',
    'targetisaadjoined:i',
    'username:s',
  ],
  display: [
    'allow desktop composition:i',
    'allow font smoothing:i',
    'bitmapcachepersistenable:i',
    'bitmapcachesize:i',
    'desktopheight:i',
    'desktop size id:i',
    'desktopscalefactor:i',
    'desktopwidth:i',
    'disable full window drag:i',
    'disable menu anims:i',
    'disable themes:i',
    'disable wallpaper:i',
    'dynamic resolution:i',
    'enablesuperpan:i',
    'encode redirected video capture:i',
    'maximizetocurrentdisplays:i',
    'redirectdirectx:i',
    'screen mode id:i',
    'selectedmonitors:s',
    'session bpp:i',
    'singlemoninwindowedmode:i',
    'smart sizing:i',
    'span monitors:i',
    'superpanaccelerationfactor:i',
    'use multimon:i',
    'videoplaybackmode:i',
    'winposstr:s',
  ],
  gateway: [
    'gatewaycredentialssource:i',
    'gatewayhostname:s',
    'gatewayprofileusagemethod:i',
    'gatewayusagemethod:i',
  ],
  hardware: [
    'audiocapturemode:i',
    'audiomode:i',
    'audioqualitymode:i',
    'camerastoredirect:s',
    'devicestoredirect:s',
    'drivestoredirect:s',
    'redirected video capture encoding quality:i',
    'redirectcomports:i',
    'redirectlocation:i',
    'redirectposdevices:i',
    'redirectprinters:i',
    'redirectsmartcards:i',
    'usbdevicestoredirect:s',
  ],
  remoteapp: [
    'disableremoteappcapscheck:i',
    'remoteapplicationcmdline:s',
    'remoteapplicationfile:s',
    'remoteapplicationfileextensions:s',
    'remoteapplicationexpandcmdline:i',
    'remoteapplicationexpandworkingdir:i',
    'remoteapplicationicon:s',
    'remoteapplicationmode:i',
    'remoteapplicationname:s',
    'remoteapplicationprogram:s',
    'workspace id:s',
  ],
  session: [
    'administrative session:i',
    'alternate shell:s',
    'disable ctrl+alt+del:i',
    'disableconnectionsharing:i',
    'displayconnectionbar:i',
    'keyboardhook:i',
    'pinconnectionbar:i',
    'public mode:i',
    'redirectclipboard:i',
    'redirectwebauthn:i',
    'shell working directory:s',
  ],
  signature: ['signscope:s', 'signature:s'],
  raweb: ['raweb source type:i', 'raweb external flag:i'],
} as const satisfies Record<string, string[]>;

type ParseEntry<S extends string> = S extends `${string}:s`
  ? string
  : S extends `${string}:i`
  ? number
  : S extends `${string}:b`
  ? Uint8Array
  : never;

type GroupedAppOrDesktopProperties = {
  [K in keyof typeof groups]: {
    [P in (typeof groups)[K][number]]?: ParseEntry<P>;
  };
};

type AnyPropertyGroupValue = (typeof groups)[keyof typeof groups];
type AnyProperty = AnyPropertyGroupValue[number];
