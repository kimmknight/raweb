import { reactive } from 'vue';

type Resource = NonNullable<
  Awaited<ReturnType<typeof import('$utils/getAppsAndDevices').getAppsAndDevices>>
>['resources'][number];

/** The sizes of icon images by URL. A null size means that the image could not be loaded. */
const iconSizes = reactive(new Map<string, number | null>());
const requestedIcons = new Set<string>();

/**
 * Gets the size of an image in bytes. The image is only loaded the first time that its size is requested.
 *
 * This does not wait for the image. The first call starts loading it and returns undefined, and later calls
 * return its size (or null if it could not be loaded). The sizes are reactive, so a computed value or a render
 * function that calls this runs again once the size is known.
 */
function getImageSize(url: string) {
  if (!requestedIcons.has(url)) {
    requestedIcons.add(url);
    fetch(url)
      .then((response) => (response.ok ? response.blob() : Promise.reject(new Error(response.statusText))))
      .then((blob) => iconSizes.set(url, blob.size))
      .catch(() => iconSizes.set(url, null));
  }
  return iconSizes.get(url);
}

/**
 * Gets the size of a workspace resource: its generated RDP file plus its icon image.
 *
 * For a resource on several terminal servers, the RDP file of the first terminal server is used.
 * If there is no image, only the RDP file is counted. Otherwise, this returns undefined until the image size is
 * known (see `getImageSize`).
 *
 * Use this function from a computed value or render function that can run again once the image size is known.
 */
export function getResourceSize(resource: Resource) {
  const rdpFileSize = resource.hosts[0]?.rdpFileSize ?? 0;

  const icon = resource.icons.find((icon) => icon.type === 'png');
  if (!icon) {
    return rdpFileSize;
  }

  const url = new URL(icon.url.href);
  url.searchParams.set('format', 'png'); // the same icon that the resource cards show
  const imageSize = getImageSize(url.href);
  return imageSize === undefined ? undefined : rdpFileSize + (imageSize ?? 0);
}

/**
 * Formats a size in bytes for people to read, such as `1.5 KiB`.
 */
export function formatFileSize(bytes: number | undefined) {
  if (bytes === undefined) {
    return '';
  }

  const units = ['B', 'KiB', 'MiB', 'GiB'];
  let value = bytes;
  let unitIndex = 0;
  while (value >= 1024 && unitIndex < units.length - 1) {
    value /= 1024;
    unitIndex++;
  }
  const formatted = value.toLocaleString(undefined, {
    maximumFractionDigits: unitIndex === 0 ? 0 : 1,
  });
  return `${formatted} ${units[unitIndex]}`;
}
