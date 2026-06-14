import { ManagedResourceCreateDialog } from '$dialogs';
import { readRdpFile } from './readRdpFile';

/**
 * Opens a file picker to select an RDP file and reads its content.
 */
export async function pickRDPFile() {
  return new Promise<{
    isRemoteApp: InstanceType<typeof ManagedResourceCreateDialog>['$props']['isRemoteApp'];
    data: InstanceType<typeof ManagedResourceCreateDialog>['$props']['initialData'];
  }>((resolve, reject) => {
    var input = document.createElement('input');
    input.type = 'file';
    input.accept = '.rdp';
    input.multiple = false;
    input.onchange = (event) => {
      const file = (event.target as HTMLInputElement).files?.[0];
      if (!file) {
        return;
      }

      readRdpFile(file)
        .then((data) => {
          resolve(data);
        })
        .catch(reject)
        .finally(() => {
          input.remove();
        });
    };

    input.oncancel = () => {
      input.remove();
    };

    input.click();
  });
}
