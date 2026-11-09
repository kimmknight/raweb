import type { getAppsAndDevices } from '$utils';

/** Gets the unique IDs of the terminal servers that host the resources of the given types */
export function getTerminalServerIds(
  data: Awaited<ReturnType<typeof getAppsAndDevices>> | undefined,
  resourceTypes: ('RemoteApp' | 'Desktop')[] = ['RemoteApp', 'Desktop']
) {
  const resourcesOfType = data?.resources.filter((resource) => resourceTypes.includes(resource.type));
  return Array.from(new Set(resourcesOfType?.flatMap((resource) => resource.hosts.map((host) => host.id))));
}
