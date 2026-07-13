/**
 * Reads a FileList, and for any file that has an empty type, fills it
 * with:
 * - 'application/x-rdp' for .rdp files
 * - 'application/x-tsresource' for .tsresource and .resource files
 * - 'application/x-tsresourcebundle' for .tsresourcebundle files
 * - 'application/octet-stream' for all other files
 */
export function fillEmptyMimeTypes(files?: FileList): FileList {
  if (!files) {
    return new DataTransfer().files;
  }

  const filledFiles = Array.from(files).map((file) => {
    if (file.type) {
      return file;
    }

    const extension = file.name.split('.').pop()?.toLowerCase();

    let type = 'application/octet-stream';
    if (extension === 'tsresource' || extension === 'resource') {
      type = 'application/x-tsresource';
    } else if (extension === 'tsresourcebundle') {
      type = 'application/x-tsresourcebundle';
    } else if (extension === 'rdp') {
      type = 'application/x-rdp';
    }

    return new File([file], file.name, { type });
  });

  const dt = new DataTransfer();
  filledFiles.forEach((file) => dt.items.add(file));
  return dt.files;
}
