import { ManagedResourceCreateDialog } from '$dialogs';
import { hashString, parseRdpFileText, ResourceManagementSchemas } from '$utils';
import { t } from 'i18next';

/**
 * Reads the content of an RDP file and extracts relevant information for
 * creating a managed resource.
 */
export async function readRdpFile(file: File) {
  return new Promise<{
    isRemoteApp: NonNullable<InstanceType<typeof ManagedResourceCreateDialog>['$props']['isRemoteApp']>;
    data: NonNullable<InstanceType<typeof ManagedResourceCreateDialog>['$props']['initialData']>;
  }>((resolve, reject) => {
    const fileName = file.name;
    if (!fileName.toLowerCase().endsWith('.rdp')) {
      reject(t('registryApps.manager.rdpUploadFail.invalidFileType'));
      return;
    }
    const fileNameWithoutExtension = fileName.substring(0, fileName.length - 4);

    const reader = new FileReader();
    reader.onload = async (readerEvent) => {
      var rdpFileContent = readerEvent.target?.result;
      if (!rdpFileContent || typeof rdpFileContent !== 'string') {
        return;
      }

      // extract relevant properties
      const resourceProperties = parseRdpFileText(rdpFileContent);
      const address = resourceProperties.connection['full address:s'];
      const isRemoteApp = resourceProperties.remoteapp['remoteapplicationmode:i'] === 1;
      const path = resourceProperties.remoteapp['remoteapplicationprogram:s'];
      const commandLineArguments = resourceProperties.remoteapp['remoteapplicationcmdline:s'];
      const aplicationDisplayName = resourceProperties.remoteapp['remoteapplicationname:s'];
      const fileTypeAssociations =
        resourceProperties.remoteapp['remoteapplicationfileextensions:s']
          ?.split(',')
          .map((ext) => {
            return { extension: ext.trim(), iconPath: undefined, iconIndex: null };
          })
          .filter(
            (extObj) =>
              extObj.extension.length > 0 && !extObj.extension.includes('*') && extObj.extension.startsWith('.')
          ) || [];

      // if the RDP file claims to be for a RemoteApp, ensure that the path to the RemoteApp is provided
      if (isRemoteApp && !path) {
        reject(t('registryApps.manager.rdpUploadFail.missingAppPath'));
        return;
      }

      // ensure that the connection address is provided
      if (!address) {
        reject(t('registryApps.manager.rdpUploadFail.missingConnectionAddress'));
        return;
      }

      // construct data for the creation dialog
      const creationData = {
        isRemoteApp,
        data: {
          identifier: await hashString(path + (commandLineArguments || '') + address),
          name: aplicationDisplayName?.trim() || fileNameWithoutExtension,
          path,
          commandLine: commandLineArguments,
          commandLineOption: ResourceManagementSchemas.RegistryRemoteApp.CommandLineMode.Optional,
          includeInWorkspace: true,
          fileTypeAssociations: fileTypeAssociations,
          rdpFileString: rdpFileContent,
        } satisfies InstanceType<typeof ManagedResourceCreateDialog>['initialData'],
      };

      resolve(creationData);
    };
    reader.onerror = (event) => {
      reject(new Error('Error reading file'));
    };
    reader.readAsText(file, 'UTF-8');
  });
}
