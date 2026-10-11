import { prefixUserNS } from '$utils';
import { ManagedResourceSource } from '$utils/schemas/ResourceManagementSchemas';
import { computed, ref, type ComputedRef, type Ref } from 'vue';

type Resource = NonNullable<
  Awaited<ReturnType<typeof import('./getAppsAndDevices.ts').getAppsAndDevices>>
>['resources'][number];

/**
 * Info about the active guacd web client session's server keyboard layout.
 *
 * This is set by `useServerKeyboardLayout()` while a session is active so
 * the titlebar can show a button for changing it.
 */
export const activeGuacdSessionLayout = ref<{
  /** The host for this session, which is used to store the remembered choice in localStorage. */
  hostId: string;
  /** True if the layout is auto-detected server-side and cannot be changed from the web client. */
  isAutoDetected: boolean;
  /** The guacd `server-layout` value currently in effect for the session. */
  current: string;
  /** Applies a newly chosen layout to the active session and reconnects. */
  applyLayout: (layout: string) => void;
} | null>(null);

interface ServerLayoutEntry {
  host: string;
  /** The layout remembered for this host's connections ("always"). */
  savedLayout?: string;
  /** The layout last picked in the dialog for this host, even if not remembered ("just once"). */
  lastSelectedLayout?: string;
}

function storageKey() {
  return prefixUserNS('serverLayout:data'); // by ending with :data, the key will be cleared on sign out
}

function readServerLayoutEntries(): ServerLayoutEntry[] {
  try {
    const raw = localStorage.getItem(storageKey());
    return raw ? JSON.parse(raw) : [];
  } catch {
    return [];
  }
}

function writeServerLayoutEntries(entries: ServerLayoutEntry[]) {
  localStorage.setItem(storageKey(), JSON.stringify(entries));
}

export function getServerLayoutEntry(hostId: string): ServerLayoutEntry | undefined {
  return readServerLayoutEntries().find((entry) => entry.host === hostId);
}

/**
 * Merges the given changes into the stored entry for a host.
 * The entry is created or modified as needed.
 *
 * Omit a property to leave it unchanged. Pass `undefined` explicitly to clear it.
 */
export function updateServerLayoutEntry(
  hostId: string,
  changes: Partial<Pick<ServerLayoutEntry, 'savedLayout' | 'lastSelectedLayout'>>
) {
  const entries = readServerLayoutEntries();
  const existing = entries.find((entry) => entry.host === hostId);
  const updated: ServerLayoutEntry = {
    host: hostId,
    savedLayout: existing?.savedLayout,
    lastSelectedLayout: existing?.lastSelectedLayout,
    ...changes,
  };
  const rest = entries.filter((entry) => entry.host !== hostId);
  writeServerLayoutEntries(updated.savedLayout || updated.lastSelectedLayout ? [...rest, updated] : rest);
}

/**
 * A composable for managing the server keyboard layout for a given resource and host.
 * It determines whether the layout is auto-detected, already known, or needs to be chosen by the user.
 *
 * @param resourceId The ID of the resource for which to manage the server layout.
 * @param hostId The ID of the host for which to manage the server layout.
 * @param resources A computed reference to the list of available resources.
 * @returns An object containing the computed properties and functions for managing the server layout.
 */
export function useServerKeyboardLayout(
  resourceId: Ref<string> | ComputedRef<string>,
  hostId: Ref<string> | ComputedRef<string>,
  resources: ComputedRef<Resource[] | undefined>
) {
  const resource = computed(() => resources.value?.find((r) => r.id === resourceId.value));
  const host = computed(() => resource.value?.hosts.find((h) => h.id === hostId.value));

  const isRegistryBasedResource = computed(() => {
    const source = resource.value?.source.source;
    return (
      source === ManagedResourceSource.TSAppAllowList ||
      source === ManagedResourceSource.CentralPublishedResourcesApp ||
      source === ManagedResourceSource.CentralPublishedResourcesDesktop
    );
  });

  const storedServerLayout = computed(() => {
    const value = host.value?.rdp?.['raweb server layout'];
    return typeof value === 'string' && value ? value : undefined;
  });

  /**
   * Determines the server keyboard layout to use for the connection by prompting the user via
   * `promptForLayout` if it is not already known.
   *
   * `promptForLayout` is responsible for remembering the choice in localStorage.
   */
  async function determineServerLayout(promptForLayout: () => Promise<string>): Promise<string | undefined> {
    if (isRegistryBasedResource.value) {
      return undefined;
    }
    if (storedServerLayout.value) {
      return storedServerLayout.value;
    }

    const remembered = getServerLayoutEntry(hostId.value)?.savedLayout;
    if (remembered) {
      return remembered;
    }

    return await promptForLayout();
  }

  return { isRegistryBasedResource, storedServerLayout, determineServerLayout };
}
