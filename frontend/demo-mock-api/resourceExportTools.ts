import {
  TextReader,
  Uint8ArrayReader,
  Uint8ArrayWriter,
  ZipWriter,
} from '@zip.js/zip.js/lib/zip-core-external.js';
import type { z } from 'zod';
import type { registedResourceSchema } from './collectMockFiles';

type RegisteredResource = z.infer<typeof registedResourceSchema>;

/**
 * Fetches the icon for a resource and re-encodes it as a PNG, returning its bytes.
 *
 * This is necessary because the icon files served by the demo build are `.webp`, but
 * `.resource` and `.tsresource` files always contain a PNG for the icon.
 */
async function fetchIconPngBytes(identifier: string): Promise<Uint8Array | null> {
  const res = await window.fetch(
    resolve('api/resources/image/managed-resources/' + encodeURIComponent(identifier))
  );
  if (!res.ok) {
    return null;
  }

  // use a browser canvas to convert the webp to png
  const bitmap = await createImageBitmap(await res.blob());
  const canvas = document.createElement('canvas');
  canvas.width = bitmap.width;
  canvas.height = bitmap.height;
  const canvasContext = canvas.getContext('2d');
  if (!canvasContext) {
    throw new Error('Failed to get canvas context');
  }
  canvasContext.drawImage(bitmap, 0, 0);
  const pngBlob: Blob = await new Promise((resolve, reject) =>
    canvas.toBlob((blob) => (blob ? resolve(blob) : reject(new Error('Failed to generate blob'))), 'image/png')
  );

  return new Uint8Array(await pngBlob.arrayBuffer());
}

/**
 * Builds a `.tsresource` zip for a single resource.
 *
 * @returns A `Uint8Array` containing the generated file bytes.
 */
async function buildResourceZip(resource: RegisteredResource): Promise<Uint8Array<ArrayBuffer>> {
  const isDesktop = resource.RemoteAppProperties == null;
  const iconEntryName = isDesktop ? 'wallpaper.png' : 'resource.png';
  const lightIconBytes = resource.HasLightIcon ? await fetchIconPngBytes(resource.Identifier) : null;

  // a real exported info.json omits nullable fields entirely when unset rather than writing them as null,
  // so we must manually remove any undefined or null values from the object before strinifying it
  const rawInfo = {
    IconIndex: resource.IconIndex || 0,
    IconPath: lightIconBytes ? iconEntryName : resource.IconPath,
    IncludeInWorkspace: resource.IncludeInWorkspace !== false,
    Name: resource.Name,
    SecurityDescriptorSddl: resource.SecurityDescriptorSddl,
    VirtualFolders: resource.VirtualFolders,
    MacAddress: resource.MacAddress,
    __Version: 1,
  };
  const info: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(rawInfo)) {
    if (value !== undefined && value !== null) {
      info[key] = value;
    }
  }

  const zipWriter = new ZipWriter(new Uint8ArrayWriter(), { level: 0 });
  await zipWriter.add('resource.rdp', new TextReader(resource.RdpFileString));
  await zipWriter.add('info.json', new TextReader(JSON.stringify(info, null, 2)));
  if (lightIconBytes) {
    await zipWriter.add(iconEntryName, new Uint8ArrayReader(lightIconBytes));
  }
  return zipWriter.close();
}

/**
 * Builds a `Response` for the specified resource's `.tsresource` zip.
 */
async function buildSingleExportResponse(identifier: string): Promise<Response> {
  const resource = await fetchJson(resolve('api/management/resources/registered/' + identifier));
  const bytes = await buildResourceZip(resource);

  return new Response(bytes, {
    headers: {
      'Content-Type': 'application/x-tsresource',
      'Content-Disposition': `attachment; filename="${resource.Name}.tsresource"`,
    },
  });
}

/**
 * Builds a `Response` for a `.tsresourcebundle` zip containing all registered resources' `.tsresource` zips.
 */
async function buildBundleExportResponse(): Promise<Response> {
  const resources: any[] = await fetchJson(resolve('api/management/resources/registered'));

  // build a `.tsresourcebundle` zip containing each resource's `.tsresource` zip
  const bundleWriter = new ZipWriter(new Uint8ArrayWriter(), { level: 0 });
  for (const resource of resources) {
    const resourceBytes = await buildResourceZip(resource);
    await bundleWriter.add(`${resource.Name}.tsresource`, new Uint8ArrayReader(resourceBytes));
  }
  const bytes = await bundleWriter.close();

  return new Response(bytes, {
    headers: {
      'Content-Type': 'application/x-tsresourcebundle',
      'Content-Disposition': `attachment; filename="resources-export.tsresourcebundle"`,
    },
  });
}

function resolve(relativeUrlPath: string): string {
  return new URL(relativeUrlPath, document.baseURI).href;
}

async function fetchJson(url: string): Promise<any> {
  return (await window.fetch(url)).json();
}

export const resourceExportTools = {
  buildSingleExportResponse,
  buildBundleExportResponse,
};
