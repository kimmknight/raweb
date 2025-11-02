import z from 'zod';
import { objectPropertiesToCamelCase } from './ResourceManagementSchemas.ts';

export enum PrincipalKind {
  User,
  Group,
  Computer,
  Other,
}

const ResolvedSecurityIdentifierSchema = z.preprocess(
  objectPropertiesToCamelCase,
  z.object({
    sid: z.string(),
    domain: z.string().nullish(),
    userPrincipalName: z.string().nullish(),
    userName: z.string(),
    displayName: z.string().nullish(),
    principalKind: z.enum(PrincipalKind),
    expandedDisplayName: z.string(),
  })
);

const ResolvedSidsResultSchema = z.preprocess(
  objectPropertiesToCamelCase,
  z.object({
    resolvedSids: z.array(ResolvedSecurityIdentifierSchema),
    invalidOrUnfoundSids: z.array(z.string()),
  })
);

export const SecurityManagementSchemas = {
  Resolved: ResolvedSecurityIdentifierSchema,
  ResolvedMany: ResolvedSidsResultSchema,
};
