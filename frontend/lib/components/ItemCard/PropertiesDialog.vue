<script setup lang="ts">
  import { RdpFilePropertiesDialog } from '$dialogs';
  import { useCoreDataStore } from '$stores';
  import { raw } from '$utils';
  import { computed, ref, useTemplateRef } from 'vue';
  import TerminalServerPickerDialog from './TerminalServerPickerDialog.vue';

  const { terminalServerAliases } = useCoreDataStore();

  type Resource = NonNullable<
    Awaited<ReturnType<typeof import('$utils').getAppsAndDevices>>
  >['resources'][number];

  const { resource } = defineProps<{
    resource: Resource;
  }>();

  const tsPickerDialog = useTemplateRef<typeof TerminalServerPickerDialog>('tsPickerDialog');
  const openTsPickerDialog = computed(() => raw(tsPickerDialog.value)?.openDialog);
  const tsPickerDialogIsOpen = computed(() => raw(tsPickerDialog.value)?.isOpen);
  const selectedTerminalServer = ref<Resource['hosts'][number]['id']>();

  const properties = computed(() => {
    if (!selectedTerminalServer.value) return undefined;
    const foundHost = resource.hosts.find((host) => host.id === selectedTerminalServer.value);
    if (!foundHost) return undefined;
    return foundHost.rdp || undefined;
  });
  const isSignedRdpFile = computed(() => {
    return properties.value ? 'signature' in properties.value : false;
  });
  const isRemoteApp = computed(() => {
    return properties.value
      ? 'remoteapplicationmode' in properties.value && properties.value.remoteapplicationmode === 1
      : false;
  });

  const editMode = ref(false);
  function open(_editMode = false) {
    editMode.value = _editMode;

    // Step 1
    // open ther terminal server picker to determine which terminal
    // server's app/desktop properties should be shown
    if (!tsPickerDialogIsOpen.value && openTsPickerDialog.value) {
      openTsPickerDialog.value();
    }
  }

  type TerminalServerPickerDialogProps = InstanceType<typeof TerminalServerPickerDialog>['$props'];
  type TerminalServerPickerDialogOnCloseDetail = Parameters<
    NonNullable<TerminalServerPickerDialogProps['onClose']>
  >[0];
  function handleTerminalServerSelectorDialogClose(
    detail: TerminalServerPickerDialogOnCloseDetail,
    openPropsDialog: () => void
  ) {
    // Step 2
    // set the selected terminal server and open the properties dialog
    selectedTerminalServer.value = detail.selectedTerminalServer;
    openPropsDialog();
  }

  function resetSelectedTerminalServer() {
    // Step 3
    // reset the selected terminal server when the properties dialog is closed
    selectedTerminalServer.value = '';
  }

  const shouldRefreshWorkspace = ref(false);
  function handleAfterClose() {
    resetSelectedTerminalServer();
    if (shouldRefreshWorkspace.value) {
      emit('requestWorkspaceRefresh');
      shouldRefreshWorkspace.value = false;
    }
  }

  const emit = defineEmits<{
    (e: 'requestWorkspaceRefresh'): void;
  }>();

  defineExpose({ openDialog: open });
</script>

<template>
  <RdpFilePropertiesDialog
    :terminal-server="terminalServerAliases[selectedTerminalServer || ''] ?? selectedTerminalServer"
    :model-value="properties"
    @update:modelValue="shouldRefreshWorkspace = true"
    @after-close="handleAfterClose"
    @after-remove-from-registry="
      (close) => {
        shouldRefreshWorkspace = true;
        close();
      }
    "
    @after-save-to-registry="shouldRefreshWorkspace = true"
    :mode="editMode && !isSignedRdpFile ? 'edit' : 'view'"
    :source="resource.source"
    :hidden-groups="isRemoteApp ? undefined : ['remoteapp']"
    allow-edit-dialog
    :name="resource.title"
    #default="{ open }"
  >
    <TerminalServerPickerDialog
      :resource="resource"
      ref="tsPickerDialog"
      force
      @close="(params) => handleTerminalServerSelectorDialogClose(params, open)"
    />
  </RdpFilePropertiesDialog>
</template>

<style scoped>
  .properties {
    display: flex;
    flex-direction: column;
    gap: 8px;

    position: relative;
    --offset: -24px;
    left: var(--offset);
    width: 100%;
    box-sizing: content-box;
    max-height: calc(var(--content-height) - 156px - 96px);
    overflow: auto;
    padding: 0 calc(var(--offset) * -1);
  }

  .property {
    display: flex;
    flex-direction: column;
    gap: 4px;
  }

  .property > .type-caption {
    user-select: text;
  }
</style>
