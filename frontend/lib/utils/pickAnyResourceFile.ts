import { showConfirm } from '$dialogs';
import { t } from 'i18next';
import { fillEmptyMimeTypes } from './fillEmptyMimeTypes';

/**
 * Opens a file picker to select an RDP, RESOUCE, TSRESOURCE, or
 * TSRESOURCEBUNDLE file.
 *
 * This function filters selected files to only allow the
 * appropriate file types. It does not validate file content.
 */
export async function pickAnyResourceFile(allowMultiple?: false): Promise<File>;
export async function pickAnyResourceFile(allowMultiple: true): Promise<FileList>;
export async function pickAnyResourceFile(allowMultiple = false): Promise<File | FileList> {
  return new Promise<File | FileList>((resolve, reject) => {
    var input = document.createElement('input');
    input.type = 'file';
    input.accept = '.rdp,.resource,.tsresource' + (allowMultiple ? ',.tsresourcebundle' : '');
    input.multiple = allowMultiple;
    input.onchange = async (event) => {
      const files = fillEmptyMimeTypes((event.target as HTMLInputElement).files ?? undefined);
      if (files.length === 0) {
        return;
      }

      const filteredFiles = [...files].filter((file) =>
        allowMultiple
          ? ['application/x-rdp', 'application/x-tsresource', 'application/x-tsresourcebundle'].includes(
              file.type
            )
          : ['application/x-rdp', 'application/x-tsresource'].includes(file.type)
      );
      if (filteredFiles.length === 0) {
        showConfirm(
          t('registryApps.import.noValidResources.title'),
          t('registryApps.import.noValidResources.message', { count: files.length }),
          '',
          t('dialog.ok')
        );
        return;
      }

      if (!allowMultiple) {
        resolve(files[0]);
      } else {
        resolve(files);
      }
    };

    input.oncancel = () => {
      input.remove();
    };

    input.click();
  });
}
