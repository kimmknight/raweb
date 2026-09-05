import { existsSync } from 'node:fs';
import { glob, readdir, readFile } from 'node:fs/promises';
import path from 'node:path';
import z from 'zod';

export interface MockFile {
  /**
   * URL path relative to the app base with no leading slash.
   *
   * @example `api/app-init-details.json` or `webfeed.aspx`.
   **/
  urlPath: string;
  /**
   * Absolute path to the local file to read for this URL. Omit when `content` is set instead.
   **/
  filePath?: string;
  /**
   * Text content to serve instead of using `filePath`.
   **/
  content?: string;
  /**
   * A valid mime type.
   **/
  contentType: string;
  /**
   * Whether the file is text encoded in UTF-8.
   **/
  isText: boolean;
}

/**
 * Builds the full list of mock API files to be served by the demo
 * build for RAWeb. This is used by the `demoMockApiPlugin`.
 * @returns
 */
export async function collectMockFiles(mockFilesDirectory = import.meta.dirname): Promise<MockFile[]> {
  const apiDir = path.join(mockFilesDirectory, 'api');
  const webfeedDir = path.join(mockFilesDirectory, 'webfeed');
  const systemIconsDir = path.join(mockFilesDirectory, 'system-icons');

  const registered = await readRegisteredResources(apiDir);

  return [
    ...(await collectApiFiles(apiDir)),
    ...(await collectWebfeedFile(webfeedDir)),
    ...(await collectRdpFiles(webfeedDir)),
    ...(await collectWebfeedResourceIconFiles(webfeedDir)),
    ...(await collectSystemIconFiles(systemIconsDir)),
    ...collectRegisteredResourceFiles(registered),
  ];
}

/**
 * Parses the `management/resources/registered.json` file to get the
 * list of registered resources for the demo build.
 *
 * @param apiDir The directory of the mock API files.
 */
async function readRegisteredResources(apiDir: string): Promise<unknown[]> {
  const registeredPath = path.join(apiDir, 'management/resources/registered.json');
  if (!existsSync(registeredPath)) {
    return [];
  }

  const json = JSON.parse(await readFile(registeredPath, 'utf-8'));
  const jsonArray = Array.isArray(json) ? json : [];

  const parsed = registedResourceSchema.array().safeParse(jsonArray);
  if (!parsed.success) {
    console.error('Failed to parse registered resources:', z.treeifyError(parsed.error));
  }

  return parsed.success ? parsed.data : [];
}

/**
 * A schema for the registered resources in the `management/resources/registered.json` file.
 */
export const registedResourceSchema = z.object({
  Identifier: z.string(),
  Source: z.number().optional(),
  Name: z.string(),
  RemoteAppProperties: z
    .object({
      ApplicationPath: z.string(),
      CommandLine: z.string().nullish(),
      CommandLineOption: z.number().optional(),
      FileTypeAssociations: z
        .array(
          z.object({
            Extension: z.string(),
            IconPath: z.string().nullish(),
            IconIndex: z.number().optional(),
          })
        )
        .optional(),
    })
    .nullish(),
  IconPath: z.string().nullish(),
  IconIndex: z.number().optional(),
  HasLightIcon: z.boolean().optional(),
  HasDarkIcon: z.boolean().optional(),
  IncludeInWorkspace: z.boolean().optional(),
  SecurityDescriptorSddl: z.string().nullish(),
  SecurityDescription: z
    .object({
      ReadAccessAllowedSids: z.array(z.string()),
      ReadAccessDeniedSids: z.array(z.string()),
    })
    .optional(),
  RdpFileString: z.string(),
  VirtualFolders: z.array(z.string()),
  MacAddress: z.string().nullish(),
});

/**
 * Creates GET responses for each registered resource in the demo build. The `registered.json` file
 * contains the list of resources and their GET response bodies.
 *
 * The response will be available at `GET api/management/resources/registered/{identifier}`.
 */
function collectRegisteredResourceFiles(registered: unknown[]): MockFile[] {
  return registered
    .filter(
      (resource): resource is { Identifier: string } =>
        typeof resource === 'object' &&
        !!resource &&
        'Identifier' in resource &&
        typeof resource?.Identifier === 'string'
    )
    .map((resource) => ({
      urlPath: `api/management/resources/registered/${resource.Identifier}`,
      content: JSON.stringify(resource, null, 2),
      contentType: 'application/json',
      isText: true,
    }));
}

/**
 * Infers the content type (mime type) for a file based on its extension.
 *
 * If the extension is not recognized, it defaults to `application/octet-stream`.
 */
function inferContentTypeFromExtension(filePath: string): string {
  switch (path.extname(filePath).toLowerCase()) {
    case '.json':
      return 'application/json';
    case '.xml':
      return 'application/xml';
    case '.rdp':
      return 'application/x-rdp';
    case '.png':
      return 'image/png';
    case '.webp':
      return 'image/webp';
    case '.js':
      return 'text/javascript';
    case '.css':
      return 'text/css';
    default:
      return 'application/octet-stream';
  }
}

/**
 * Directly serves everything under `api/**`. For all filex except app-init-details.json,
 * the file extension is stripped from the URL path since real RAWeb API endpoints don't have one.
 */
async function collectApiFiles(apiDir: string): Promise<MockFile[]> {
  if (!existsSync(apiDir)) {
    return [];
  }

  const entries = await readdir(apiDir, { withFileTypes: true, recursive: true });
  return entries
    .filter((entry) => entry.isFile())
    .map((entry) => {
      const filePath = path.join(entry.parentPath, entry.name);
      const relativePath = path.relative(apiDir, filePath).replaceAll('\\', '/');

      const shouldKeepExtension =
        relativePath === 'app-init-details.json' || relativePath.startsWith('inject/file/');

      const urlPath = 'api/' + (shouldKeepExtension ? relativePath : relativePath.replace(/\.[^./]+$/, ''));

      return {
        urlPath,
        filePath,
        contentType: inferContentTypeFromExtension(filePath),
        isText: true,
      };
    });
}

/**
 * Mocks the MS-TWSP version 2.0 workspace endpoint.
 *
 * This registers the webfeed.xml file to be served at `webfeed.aspx` and /api/workspace.
 */
async function collectWebfeedFile(webfeedDir: string): Promise<MockFile[]> {
  const filePath = path.join(webfeedDir, 'webfeed.xml');
  if (!existsSync(filePath)) {
    return [];
  }

  return [
    {
      urlPath: 'webfeed.aspx',
      filePath,
      contentType: 'application/x-msts-radc+xml; radc_schema_version=2.0',
      isText: true,
    },
    {
      urlPath: 'api/workspace',
      filePath,
      contentType: 'application/x-msts-radc+xml; radc_schema_version=2.0',
      isText: true,
    },
  ];
}

/**
 * Gets filenames from a directory that match the specified glob pattern.
 *
 * The filenames are returned as paths relative to the specified directory.
 *
 * If the directory does not exist, an empty array is returned.
 */
async function globFilenames(pattern: string, dir: string): Promise<string[]> {
  if (!existsSync(dir)) {
    return [];
  }

  const filenames: string[] = [];

  for await (const relativePath of glob(pattern, { cwd: dir })) {
    filenames.push(relativePath);
  }

  return filenames;
}

/**
 * Registers each RDP file in the `webfeed/resources` directory to be served at `api/resources/managed-resources/{filename}`.
 */
async function collectRdpFiles(webfeedDir: string): Promise<MockFile[]> {
  const resourcesDir = path.join(webfeedDir, 'resources');

  return (await globFilenames('*.rdp', resourcesDir)).map((relativePath) => ({
    urlPath: `api/resources/managed-resources/${relativePath.replace(/\.rdp$/i, '')}`,
    filePath: path.join(resourcesDir, relativePath),
    contentType: 'application/x-rdp',
    isText: true,
  }));
}

/**
 * Registers each workspace resource icon in the `webfeed/resources` directory
 * to be served at `api/resources/image/managed-resources/{filename}`.
 *
 * Framed resource icons (those with filenames ending in `-framed`) are served at
 * `api/resources/image/managed-resources-framed/{filename-without-framed}`.
 */
async function collectWebfeedResourceIconFiles(webfeedDir: string): Promise<MockFile[]> {
  const resourcesDir = path.join(webfeedDir, 'resources');

  return (await globFilenames('*.webp', resourcesDir)).map((relativePath) => {
    const extension = path.extname(relativePath);
    const baseName = relativePath.slice(0, -extension.length);

    // matches if the filename ends with '-framed', which means that it
    // is a wallpaper contained in a desktop/monitor frame
    const framedMatch = /^(.+)-framed$/i.exec(baseName);

    const urlPath = framedMatch
      ? `api/resources/image/managed-resources-framed/${framedMatch[1]}`
      : `api/resources/image/managed-resources/${baseName}`;

    return {
      urlPath,
      filePath: path.join(resourcesDir, relativePath),
      contentType: inferContentTypeFromExtension(relativePath),
      isText: false,
    };
  });
}

/**
 * Registers each system icon in the `system-icons` directory to be served at
 * `demo-icons/{filename}`.
 *
 * The mapping of system icon file paths to their corresponding filenames is
 * defined in the `system-icons/map.json` file.
 * Some system icons might be references to resource icons in the
 * `webfeed/resources` directory and are prefixed with `webfeed:` in the map
 * file.
 */
async function collectSystemIconFiles(systemIconsDir: string): Promise<MockFile[]> {
  return (await globFilenames('*.webp', systemIconsDir)).map((relativePath) => ({
    urlPath: `demo-icons/${relativePath}`,
    filePath: path.join(systemIconsDir, relativePath),
    contentType: inferContentTypeFromExtension(relativePath),
    isText: false,
  }));
}
