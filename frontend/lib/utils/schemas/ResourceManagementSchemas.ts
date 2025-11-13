import { pascalCaseToCamelCase } from '$utils';
import { z } from 'zod';

export function objectPropertiesToCamelCase(obj: any): any {
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

const ManagedResourceFileTypeAssociationSchema = z.preprocess(
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

export enum CommandLineMode {
  Disabled = 0,
  Optional = 1,
  Enforced = 2,
}

export enum ManagedResourceSource {
  File = 0,
  TSAppAllowList = 1,
  CentralPublishedResourcesApp = 2,
}

const RemoteAppPropertiesSchema = z
  .preprocess(
    objectPropertiesToCamelCase,
    z.object({
      /** The path to RemoteApp's executable. */
      applicationPath: z.string(),
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
      /** The file types (extensions) that this RemoteApp claims to support. */
      fileTypeAssociations: ManagedResourceFileTypeAssociationSchema.array()
        .nullish()
        .transform((x) => x ?? undefined)
        .default([]),
    })
  )
  .nullable();

const BaseManagedResourceSchema = z.object({
  /** The name of the registry key or internal file name for this resource. */
  identifier: z.string(),
  /** The source of the managed resource. */
  source: z.enum(ManagedResourceSource),
  /** The display name of the resource. */
  name: z.string(),
  remoteAppProperties: RemoteAppPropertiesSchema,
  /** The icon path for the resource. */
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
  /** .resource file ONLY: whether the file contains a referenced light mode icon */
  hasLightIcon: z.boolean().nullish(),
  /** .resource file ONLY: whether the file contains a referenced dark mode icon */
  hasDarkIcon: z.boolean().nullish(),
  /** Whether the resource should appear in the workspace/webfeed. */
  includeInWorkspace: z.boolean(),
  /** The string version of the security descriptor, which defines which users can access this RemoteApp. */
  securityDescriptorSddl: z
    .string()
    .nullish()
    .transform((x) => x ?? undefined),
  /** The parsed security descriptor, containing arrays of SIDs with at least allowed and denied read access. */
  securityDescription: z
    .object({
      readAccessAllowedSids: z.string().array(),
      readAccessDeniedSids: z.string().array(),
    })
    .nullish()
    .transform((x) => x ?? undefined),
  /** The RDP file string for this resource. */
  rdpFileString: z
    .string()
    .nullish()
    .transform((x) => x ?? undefined),
});

const ManagedResourceSchema = z.preprocess(objectPropertiesToCamelCase, BaseManagedResourceSchema);

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
    fileTypeAssociations: ManagedResourceFileTypeAssociationSchema.array().default([]),
  })
);

export const ResourceManagementSchemas = {
  RegistryRemoteApp: {
    App: ManagedResourceSchema,
    AppNotPreprocessed: BaseManagedResourceSchema,
    RemoteAppProperties: RemoteAppPropertiesSchema,
    FileTypeAssociation: ManagedResourceFileTypeAssociationSchema,
    CommandLineMode,
    ManagedResourceSource,
  },
  InstalledApp: {
    App: InstalledAppSchema,
    FileTypeAssociation: ManagedResourceFileTypeAssociationSchema,
  },
};
