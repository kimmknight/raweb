import { ManagedResourceCreateDialog } from '$dialogs';
import { BlobReader, BlobWriter, ZipReader } from '@zip.js/zip.js';
import { t } from 'i18next';
import { readTsResourceFile } from './readTsResourceFile';

/**
 * Parses a .tsresourcebundle file, which is a special zip file that contains
 * many .tsresource files.
 */
export async function readTsResourceBundleFile(file: File) {
  return new Promise<
    {
      isRemoteApp: NonNullable<InstanceType<typeof ManagedResourceCreateDialog>['$props']['isRemoteApp']>;
      data: NonNullable<InstanceType<typeof ManagedResourceCreateDialog>['$props']['initialData']>;
    }[]
  >(async (resolve, reject) => {
    const validExtensions = ['.tsresourcebundle'];

    const fileName = file.name;
    const fileExtension = fileName.substring(fileName.lastIndexOf('.')).toLowerCase();
    if (!validExtensions.includes(fileExtension)) {
      reject(t('registryApps.manager.tsResourceBundleUploadFail.invalidFileType'));
      return;
    }

    // get the entries within the zip file
    const zipFileReader = new BlobReader(file);
    const zipReader = new ZipReader(zipFileReader);
    const entries = await zipReader.getEntries();

    // parse each entry as a .tsresource file and aggregate the results
    const creationDataArray = [];
    for await (const entry of entries) {
      if (!entry.directory && entry.filename.toLowerCase().endsWith('.tsresource')) {
        try {
          const entryBlob = await entry.getData(new BlobWriter());
          const creationData = await readTsResourceFile(new File([entryBlob], entry.filename));
          creationDataArray.push(creationData);
        } catch (error) {
          throw new Error(
            t('registryApps.manager.tsResourceBundleUploadFail.invalidTsResource', { fileName: entry.filename })
          );
        }
      }
    }

    resolve(creationDataArray);
  });
}
