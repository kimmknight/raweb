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
    iconPath: z.string().optional(),
    iconIndex: z.number().optional().default(0),
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
    vPath: z.string().nullable(),
    /** The icon path for the RemoteApp. */
    iconPath: z.string().nullable(),
    /** The index of the icon from the iconPath. If the icon path is a plain image, this index does not matter. */
    iconIndex: z.number().nullable().default(0),
    /** The command line arguments for the RemoteApp. */
    commandLine: z.string().nullable(),
    /** Whether the command line arguments are used. */
    commandLineOption: z.enum(CommandLineMode).nullable().default(CommandLineMode.Optional),
    /** Whether the RemoteApp should appear in the workspace/webfeed. */
    includeInWorkspace: z.boolean(),
    /** The file types (extensions) that this RemoteApp claims to support. */
    fileTypeAssociations: RegistryRemoteAppFileTypeAssociationSchema.array().nullable().default([]),
    /** The string version of the security descriptor, which defines which users can access this RemoteApp. */
    securityDescriptorSddl: z.string().nullable(),
  })
);

export const ResourceManagementSchemas = {
  RegistryRemoteApp: {
    App: RegistryRemoteAppSchema,
    FileTypeAssociation: RegistryRemoteAppFileTypeAssociationSchema,
    CommandLineMode,
  },
};
