import { pascalCaseToCamelCase } from '$utils';
import { z } from 'zod';

function objectPropertiesToCamelCase(obj: any): any {
  if (typeof obj !== 'object' || obj === null || Array.isArray(obj)) {
    return obj;
  }

  return Object.fromEntries(
    Object.entries(obj).map(([key, value]) => {
      const camelCaseKey = pascalCaseToCamelCase(key);
      return [camelCaseKey, objectPropertiesToCamelCase(value)];
    })
  );
}

const RegistryRemoteAppFileTypeAssociationSchema = z.preprocess(
  objectPropertiesToCamelCase,
  z.object({
    extension: z.string(),
    iconPath: z
      .string()
      .nullish()
      .transform((x) => x ?? undefined),
    iconIndex: z.number().nullish().default(0),
  })
);

enum CommandLineMode {
  Disabled = 0,
  Optional = 1,
  Enforced = 2,
}

const RegistryRemoteAppSchema = z.preprocess(
  objectPropertiesToCamelCase,
  z.object({
    /** The name of the registry key for this RemoteApp. */
    key: z.string(),
    /** The display name of the RemoteApp. */
    name: z.string(),
    /** The path to RemoteApp's executable. */
    path: z.string(),
    vPath: z
      .string()
      .nullish()
      .transform((x) => x ?? undefined),
    /** The icon path for the RemoteApp. */
    iconPath: z
      .string()
      .nullish()
      .transform((x) => x ?? undefined),
    /** The index of the icon from the iconPath. If the icon path is a plain image, this index does not matter. */
    iconIndex: z
      .number()
      .nullish()
      .transform((x) => x ?? undefined)
      .default(0),
    /** The command line arguments for the RemoteApp. */
    commandLine: z
      .string()
      .nullish()
      .transform((x) => x ?? undefined),
    /** Whether the command line arguments are used. */
    commandLineOption: z
      .enum(CommandLineMode)
      .nullish()
      .transform((x) => x ?? undefined)
      .default(CommandLineMode.Optional),
    /** Whether the RemoteApp should appear in the workspace/webfeed. */
    includeInWorkspace: z.boolean(),
    /** The file types (extensions) that this RemoteApp claims to support. */
    fileTypeAssociations: RegistryRemoteAppFileTypeAssociationSchema.array()
      .nullish()
      .transform((x) => x ?? undefined)
      .default([]),
    /** The string version of the security descriptor, which defines which users can access this RemoteApp. */
    securityDescriptorSddl: z
      .string()
      .nullish()
      .transform((x) => x ?? undefined),
  })
);

const InstalledAppSchema = z.preprocess(
  objectPropertiesToCamelCase,
  z.object({
    /** The full name of the installed app. */
    displayName: z.string(),
    /** If the app was discovered via the Start Menu and it was in a subfolder, this is the relative subfolder path. Otherwise, it is an empty string. */
    displayFolder: z.string(),
    /** The path to the icon on the system. */
    path: z.string(),
    /** The command line arguments for the app's discovered shortcut. */
    commandLineArguments: z.string().nullish(),
    /** The path to the icon for the app. */
    iconPath: z.string().nullish(),
    /** The index of the icon from the iconPath. If the icon path is a plain image, this index does not matter. */
    iconIndex: z.number().nullish().default(0),
    /** The file types (extensions) that this app claims to support. */
    fileTypeAssociations: RegistryRemoteAppFileTypeAssociationSchema.array().default([]),
  })
);

export const ResourceManagementSchemas = {
  RegistryRemoteApp: {
    App: RegistryRemoteAppSchema,
    FileTypeAssociation: RegistryRemoteAppFileTypeAssociationSchema,
    CommandLineMode,
  },
  InstalledApp: {
    App: InstalledAppSchema,
    FileTypeAssociation: RegistryRemoteAppFileTypeAssociationSchema,
  },
};
