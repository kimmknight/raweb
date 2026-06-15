import { ManagedResourceCreateDialog } from '$dialogs';
import { BlobReader, BlobWriter, TextWriter, ZipReader, type FileEntry } from '@zip.js/zip.js';
import { t } from 'i18next';
import { readRdpFile } from './readRdpFile';

/**
 * Parses a .tsresource file or .resource file, which is a special zip file that contains:
 * - an RDP file named "resource.rdp"
 * - a metadata file named "info.json" that contains a JSON object with additional properties for the resource
 * - optional: a light and/or dark mode icon file, specified in the metadata file
 */
export async function readTsResourceFile(file: File) {
  return new Promise<{
    isRemoteApp: NonNullable<InstanceType<typeof ManagedResourceCreateDialog>['$props']['isRemoteApp']>;
    data: NonNullable<InstanceType<typeof ManagedResourceCreateDialog>['$props']['initialData']>;
  }>(async (resolve, reject) => {
    const validExtensions = ['.tsresource', '.resource'];

    const fileName = file.name;
    const fileExtension = fileName.substring(fileName.lastIndexOf('.')).toLowerCase();
    if (!validExtensions.includes(fileExtension)) {
      reject(t('registryApps.manager.tsResourceUploadFail.invalidFileType'));
      return;
    }

    const fileNameWithoutExtension = fileName.substring(0, fileName.length - fileExtension.length);

    // get the entries within the zip file
    const zipFileReader = new BlobReader(file);
    const zipReader = new ZipReader(zipFileReader);
    const entries = await zipReader.getEntries();

    // at a minimum, all tsresource files MUST have the RDP file and the metadata file
    const rdpFileEntry = entries.find((entry) => entry.filename === 'resource.rdp');
    const metadataFileEntry = entries.find((entry) => entry.filename === 'info.json');
    if (!rdpFileEntry || rdpFileEntry.directory) {
      reject(t('registryApps.manager.tsResourceUploadFail.missingRdp'));
      return;
    }
    if (!metadataFileEntry || metadataFileEntry.directory) {
      reject(t('registryApps.manager.tsResourceUploadFail.missingMetadata'));
      return;
    }

    const metadata = await parseMetadataEntry(metadataFileEntry);
    const creationData = await parseRdpEntry(rdpFileEntry, metadata, fileNameWithoutExtension);

    // if the metadata specifies a path that is inside the file,
    // attempt to read the light and dark mode icons as blobs
    // from the file as well
    const isContainedIconPath =
      metadata.iconPath && !metadata.iconPath.startsWith('/') && !metadata.iconPath.startsWith('\\');
    if (metadata.iconPath && isContainedIconPath) {
      const lightIconEntry = entries.find((entry) => entry.filename === metadata.iconPath);
      if (lightIconEntry && !lightIconEntry.directory) {
        creationData.data.lightIconBlob = await lightIconEntry.getData(new BlobWriter());
      }

      const darkIconPath = metadata.iconPath.replace(/(\.[^.]*)$/, '-dark$1');
      const darkIconEntry = entries.find((entry) => entry.filename === darkIconPath);
      if (darkIconEntry && !darkIconEntry.directory) {
        creationData.data.darkIconBlob = await darkIconEntry.getData(new BlobWriter());
      }
    }

    resolve(creationData);
  });
}

/**
 * Reads the metadata entry from the tsresource file and
 * parses it into an object.
 */
async function parseMetadataEntry(entry: FileEntry) {
  try {
    const json = await entry.getData(new TextWriter());
    const object = JSON.parse(json);

    const version = parseMetadataVersion(object.__Version);
    const iconIndex = parseIconIndex(object.IconIndex);
    const iconPath = parseIconPath(object.IconPath);
    const includeInWorkspace = parseIncludeInWorkspace(object.IncludeInWorkspace);
    const name = parseName(object.Name);
    const securityDescriptorSddl = parseSecurityDescriptorSddl(object.SecurityDescriptorSddl);
    const virtualFolders = parseVirtualFolders(object.VirtualFolders);

    return {
      version,
      iconIndex,
      iconPath,
      includeInWorkspace,
      name,
      securityDescriptorSddl,
      virtualFolders,
    };
  } catch (error) {
    console.error('Error parsing metadata entry:', error);
    throw t('registryApps.manager.tsResourceUploadFail.invalidMetadata');
  }
}

/**
 * Reads the RDP entry from the tsresource file, extracts creation
 * information from it, and combines it with the metadata to construct
 * the full creation data for the managed resource.
 */
async function parseRdpEntry(
  entry: FileEntry,
  metadata: Awaited<ReturnType<typeof parseMetadataEntry>>,
  nameFallback: string
) {
  try {
    const rdpFileText = await entry.getData(new TextWriter());

    const parsed = await readRdpFile(new File([rdpFileText], 'resource.rdp', { type: 'application/x-rdp' }));

    parsed.data.name = metadata.name || parsed.data.name || nameFallback;
    parsed.data.iconIndex = metadata.iconIndex;
    parsed.data.iconPath = metadata.iconPath;
    parsed.data.includeInWorkspace = metadata.includeInWorkspace;
    if (metadata.securityDescriptorSddl) {
      parsed.data.securityDescription = parseSecurityDescriptorSddlAces(metadata.securityDescriptorSddl);
    }
    parsed.data.virtualFolders = metadata.virtualFolders;

    return parsed;
  } catch (error) {
    console.error('Error parsing RDP entry:', error);
    throw t('registryApps.manager.tsResourceUploadFail.invalidRdp');
  }
}

function parseMetadataVersion(version: unknown): 1 {
  if (version !== 1) {
    throw t('registryApps.manager.tsResourceUploadFail.unsupportedVersion', { version });
  }

  return version;
}

function parseIconIndex(iconIndex: unknown): number {
  if (typeof iconIndex !== 'number' || !Number.isInteger(iconIndex) || iconIndex < 0) {
    throw t('registryApps.manager.tsResourceUploadFail.invalidIconIndex', { iconIndex });
  }

  return iconIndex;
}

function parseIconPath(iconPath: unknown): string | undefined {
  if (iconPath === undefined) {
    return undefined;
  }

  if (typeof iconPath !== 'string') {
    throw t('registryApps.manager.tsResourceUploadFail.invalidIconPath', { iconPath });
  }

  return iconPath;
}

function parseIncludeInWorkspace(includeInWorkspace: unknown): boolean {
  if (includeInWorkspace === undefined) {
    return false;
  }

  if (typeof includeInWorkspace !== 'boolean') {
    throw t('registryApps.manager.tsResourceUploadFail.invalidIncludeInWorkspace', { includeInWorkspace });
  }

  return includeInWorkspace;
}

function parseName(name: unknown): string | undefined {
  if (name === undefined) {
    return undefined;
  }

  if (typeof name !== 'string') {
    throw t('registryApps.manager.tsResourceUploadFail.invalidName', { name });
  }

  return name;
}

function parseSecurityDescriptorSddl(sddl: unknown): string | undefined {
  if (sddl === undefined) {
    return undefined;
  }

  if (typeof sddl !== 'string') {
    throw t('registryApps.manager.tsResourceUploadFail.invalidSecurityDescriptorSddl');
  }

  return sddl;
}

function parseSecurityDescriptorSddlAces(sddl: string): {
  readAccessAllowedSids: string[];
  readAccessDeniedSids: string[];
} {
  const readAccessAllowedSids: string[] = [];
  const readAccessDeniedSids: string[] = [];

  const aceRegex = /\((A|D);[^;]*;[^;]*;[^;]*;[^;]*;([^)]+)\)/g;

  for (const match of sddl.matchAll(aceRegex)) {
    const [, aceType, sid] = match;

    if (aceType === 'A') {
      readAccessAllowedSids.push(sid);
    } else if (aceType === 'D') {
      readAccessDeniedSids.push(sid);
    }
  }

  return { readAccessAllowedSids, readAccessDeniedSids };
}

function parseVirtualFolders(virtualFolders: unknown): string[] | undefined {
  if (virtualFolders === undefined) {
    return ['/'];
  }

  if (!Array.isArray(virtualFolders) || !virtualFolders.every((folder) => typeof folder === 'string')) {
    throw t('registryApps.manager.tsResourceUploadFail.invalidVirtualFolders');
  }

  return virtualFolders;
}
