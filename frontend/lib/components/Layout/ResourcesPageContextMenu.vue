<script setup lang="ts">
  import { MenuFlyout, MenuFlyoutDivider, MenuFlyoutItem, MenuFlyoutSubItem } from '$components/MenuFlyout';
  import { BulkImportDialog, ManagedResourceCreateDiscoveryDialog, showConfirm } from '$dialogs';
  import {
    add,
    arrowRight,
    databaseSearch,
    desktopAdd,
    documentArrowIn,
    filter,
    refresh,
    resourcesManager,
    server,
    sort as sortIcon,
    tiles,
  } from '$icons';
  import { useCoreDataStore } from '$stores';
  import { getAppsAndDevices, pickAnyResourceFile, PreventableEvent } from '$utils';
  import { useTranslation } from 'i18next-vue';
  import { storeToRefs } from 'pinia';
  import { computed } from 'vue';
  import { useRouter } from 'vue-router';
  import { getTerminalServerIds } from './getTerminalServerIds';
  import SortMenuItems from './SortMenuItems.vue';
  import TerminalServerMenuItems from './TerminalServerMenuItems.vue';
  import ViewMenuItems from './ViewMenuItems.vue';

  const {
    data: workspaceData,
    resourceTypes = ['RemoteApp', 'Desktop'],
    refreshWorkspace,
    refreshLabel,
  } = defineProps<{
    data?: Awaited<ReturnType<typeof getAppsAndDevices>>;
    resourceTypes?: ('RemoteApp' | 'Desktop')[];
    refreshWorkspace: () => Promise<unknown>;
    /** The text of the refresh item. Defaults to "Refresh workspace" or its translation. */
    refreshLabel?: string;
  }>();

  const mode = defineModel<'card' | 'list' | 'grid' | 'tile'>('mode', { required: true });
  const sortName = defineModel<'Name' | 'Terminal server' | 'Date modified'>('sortName', { required: true });
  const sortOrder = defineModel<'asc' | 'desc'>('sortOrder', { required: true });
  const terminalServersFilter = defineModel<string[]>('terminalServersFilter', { required: false });

  const { t } = useTranslation();
  const router = useRouter();
  const { authUser, needsSignInAgain, capabilities, machineName } = storeToRefs(useCoreDataStore());

  const includesApps = computed(() => resourceTypes.includes('RemoteApp'));
  const includesDevices = computed(() => resourceTypes.includes('Desktop'));

  const allTerminalServers = computed(() =>
    terminalServersFilter.value ? getTerminalServerIds(workspaceData, resourceTypes) : []
  );

  // only administrators in a secure context can manage resources,
  // and pages without any resource types (e.g. custom pages) have nothing to create or manage
  const canManageResources = computed(
    () =>
      window.isSecureContext &&
      !needsSignInAgain.value &&
      authUser.value.isAdmin &&
      (includesApps.value || includesDevices.value)
  );

  /**
   * Gets the starting data for a new resource that the creation dialog will fill in.
   */
  function getEmptyResourceData(isRemoteApp: boolean) {
    return {
      isRemoteApp,
      data: {
        identifier: crypto.randomUUID(),
        includeInWorkspace: true,
        virtualFolders: ['/'],
      },
    };
  }

  /**
   * Shows the error dialog for a resource file that could not be used.
   */
  function showFileError(error: unknown) {
    showConfirm(t('registryApps.manager.rdpUploadFail.title'), error as string, '', t('dialog.ok'));
  }

  /**
   * Refreshes the workspace after a resource was created before letting the creation dialog close.
   */
  async function handleResourceChange(event: PreventableEvent<{ next: () => void }>) {
    event.preventDefault();
    await refreshWorkspace();

    // wrap in setTimeout so that the updated resources list can fully render
    // before the dialog is closed
    setTimeout(() => {
      event.detail.next();
    }, 0);
  }
</script>

<template>
  <ManagedResourceCreateDiscoveryDialog
    #default="{ open: openDiscoveryDialog }"
    @after-save="handleResourceChange"
  >
    <BulkImportDialog
      #default="{ open: openCreationDialog, handleFileInput }"
      @after-save="handleResourceChange"
    >
      <MenuFlyout>
        <template #menu>
          <MenuFlyoutSubItem>
            <template #icon><span v-swap="tiles"></span></template>
            {{ t('actions.view.label2') }}
            <template #menu><ViewMenuItems v-model:mode="mode" /></template>
          </MenuFlyoutSubItem>
          <MenuFlyoutSubItem>
            <template #icon><span v-swap="sortIcon"></span></template>
            {{ t('actions.sort.label2') }}
            <template #menu>
              <SortMenuItems
                v-model:sortName="sortName"
                v-model:sortOrder="sortOrder"
                :showTerminalServer="allTerminalServers.length > 0"
              />
            </template>
          </MenuFlyoutSubItem>
          <MenuFlyoutSubItem v-if="terminalServersFilter">
            <template #icon><span v-swap="filter"></span></template>
            {{ t('actions.filter.label2') }}
            <template #menu>
              <MenuFlyout>
                <MenuFlyoutSubItem>
                  <template #icon><span v-swap="server"></span></template>
                  {{ t('actions.ts.label') }}
                  <template #menu>
                    <TerminalServerMenuItems
                      v-model:terminalServersFilter="terminalServersFilter"
                      :allTerminalServers="allTerminalServers"
                    />
                  </template>
                </MenuFlyoutSubItem>
              </MenuFlyout>
            </template>
          </MenuFlyoutSubItem>
          <MenuFlyoutDivider v-if="!canManageResources" />
          <MenuFlyoutItem @click="refreshWorkspace()">
            {{ refreshLabel ?? t('actions.refreshWorkspace') }}
            <template #icon><span v-swap="refresh"></span></template>
          </MenuFlyoutItem>
          <template v-if="canManageResources">
            <MenuFlyoutDivider />
            <MenuFlyoutSubItem>
              <template #icon><span v-swap="add"></span></template>
              {{ t('actions.new.label') }}
              <template #menu>
                <template v-if="includesApps">
                  <MenuFlyoutItem v-if="capabilities.supportsListInstalledApps" @click="openDiscoveryDialog">
                    {{ t('actions.new.appFromServer', { serverName: machineName }) }}
                    <template #icon><span v-swap="databaseSearch"></span></template>
                  </MenuFlyoutItem>
                  <MenuFlyoutItem @click="pickAnyResourceFile().then(handleFileInput).catch(showFileError)">
                    {{ t('actions.new.appFromFile') }}
                    <template #icon><span v-swap="documentArrowIn"></span></template>
                  </MenuFlyoutItem>
                </template>
                <MenuFlyoutDivider v-if="includesApps && includesDevices" />
                <template v-if="includesDevices">
                  <MenuFlyoutItem @click="openCreationDialog(getEmptyResourceData(false))">
                    {{ t('actions.new.device') }}
                    <template #icon><span v-swap="desktopAdd"></span></template>
                  </MenuFlyoutItem>
                  <MenuFlyoutItem @click="pickAnyResourceFile().then(handleFileInput).catch(showFileError)">
                    {{ t('actions.new.deviceFromFile') }}
                    <template #icon><span v-swap="documentArrowIn"></span></template>
                  </MenuFlyoutItem>
                </template>
              </template>
            </MenuFlyoutSubItem>
            <MenuFlyoutDivider />
            <MenuFlyoutItem @click="router.push('/settings/resources-manager')">
              {{ t('registryApps.manager.open') }}
              <template #icon><span v-swap="resourcesManager"></span></template>
              <!-- indicates that the item navigates to another page -->
              <template #icon-end><span v-swap="arrowRight"></span></template>
            </MenuFlyoutItem>
          </template>
        </template>
      </MenuFlyout>
    </BulkImportDialog>
  </ManagedResourceCreateDiscoveryDialog>
</template>
