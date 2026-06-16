import type { getAppsAndDevices } from './getAppsAndDevices';
import type { groupResourceProperties } from './groupResourceProperties';

type Resource = NonNullable<Awaited<ReturnType<typeof getAppsAndDevices>>>['resources'][number];
type AppOrDesktopProperties = Partial<NonNullable<Resource['hosts'][number]['rdp']>>;
type GroupedAppOrDesktopProperties = ReturnType<typeof groupResourceProperties>;

/**
 * Restricts `T` to only the keys present in `Shape`, at the top level and one level of
 * nesting (i.e. group names and the property names within each group). Any extra key is
 * typed as `never`, so passing an object with extra properties (e.g. the `raw` property
 * from `parseRdpFileText`, or an unrecognized property within a group) is a type error.
 */
type StrictShape<T, Shape> = {
  [K in keyof T]: K extends keyof Shape
    ? Shape[K] extends Record<string, any>
      ? T[K] extends Record<string, any>
        ? { [P in keyof T[K]]: P extends keyof Shape[K] ? T[K][P] : never }
        : T[K]
      : T[K]
    : never;
};

/**
 * Flattens the grouped resource properties back into a single-level object, excluding any
 * properties that are in the disabledFields list or have invalid values.
 *
 * Empty strings, empty binary strings, and NaN values are excluded from the flattened properties.
 * These are values that would not be valid in an RDP file and should not be included when
 * emitting the updated full set of properties.
 */
export function flattenGroupedRdpProperties<T extends GroupedAppOrDesktopProperties>(
  _resourceProperties: StrictShape<T, GroupedAppOrDesktopProperties>,
  disabledFields: string[] = []
): AppOrDesktopProperties {
  // narrow back to the concrete shape so the loop below can rely on typeof narrowing;
  // the generic `StrictShape` param type above is only needed to validate the call site
  const resourceProperties = _resourceProperties as GroupedAppOrDesktopProperties;

  const flattenedProperties: AppOrDesktopProperties = {};
  for (const group of Object.values(resourceProperties)) {
    for (const [key, value] of Object.entries(group)) {
      const stringOrNumberValue =
        value === undefined
          ? undefined
          : typeof value === 'string'
            ? value.trim()
            : typeof value === 'number'
              ? value
              : Array.from(value, (b) => b.toString(16).padStart(2, '0')).join('');

      // Only set the property if it has a valid value and is not in the disabledFields list.
      if (
        stringOrNumberValue !== undefined &&
        stringOrNumberValue !== '' &&
        !Number.isNaN(stringOrNumberValue) &&
        !disabledFields.includes(key)
      ) {
        flattenedProperties[key as keyof AppOrDesktopProperties] = stringOrNumberValue;
      }
    }
  }
  return flattenedProperties;
}
