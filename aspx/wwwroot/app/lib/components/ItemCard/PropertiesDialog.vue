<script setup lang="ts">
  import Button from '$components/Button/Button.vue';
  import ContentDialog from '$components/ContentDialog/ContentDialog.vue';
  import TextBlock from '$components/TextBlock/TextBlock.vue';
  import { raw } from '$utils';
  import { computed, ref, useTemplateRef } from 'vue';
  import TerminalServerPickerDialog from './TerminalServerPickerDialog.vue';

  const terminalServerAliases = window.__terminalServerAliases;

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

  const propertiesDialog = useTemplateRef<typeof ContentDialog>('propertiesDialog');
  const openDialog = computed(() => raw(propertiesDialog.value)?.open);
  const closeDialog = computed(() => raw(propertiesDialog.value)?.close);
  const dialogIsOpen = computed(() => raw(propertiesDialog.value)?.isOpen);

  const properties = computed(() => {
    if (!selectedTerminalServer.value) return null;
    const foundHost = resource.hosts.find((host) => host.id === selectedTerminalServer.value);
    if (!foundHost) return null;
    const { rdpFileText, ...properties } = foundHost.rdp || {};
    return properties;
  });

  function open() {
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
  function handleTerminalServerSelectorDialogClose(detail: TerminalServerPickerDialogOnCloseDetail) {
    // Step 2
    // set the selected terminal server and open the properties dialog
    selectedTerminalServer.value = detail.selectedTerminalServer;
    if (!dialogIsOpen.value && openDialog.value) {
      openDialog.value();
    }
  }

  function resetSelectedTerminalServer() {
    // Step 3
    // reset the selected terminal server when the properties dialog is closed
    selectedTerminalServer.value = '';
  }

  defineExpose({ openDialog: open, closeDialog, dialogIsOpen });
</script>

<template>
  <ContentDialog
    :title="`Properties`"
    ref="propertiesDialog"
    size="max"
    @beforeClose="resetSelectedTerminalServer"
    @contextmenu.stop
  >
    <div v-if="properties" class="properties">
      <div class="property">
        <TextBlock variant="bodyStrong">{{ resource.type === 'Desktop' ? 'Device' : 'Application' }}</TextBlock>
        <TextBlock variant="caption">{{ resource.title }}</TextBlock>
      </div>
      <div class="property">
        <TextBlock variant="bodyStrong">Terminal server</TextBlock>
        <TextBlock variant="caption">
          {{ terminalServerAliases[selectedTerminalServer || ''] ?? selectedTerminalServer }}
        </TextBlock>
      </div>
      <div class="property" v-for="(value, key) in properties" :key="key">
        <TextBlock variant="bodyStrong">{{ key }}</TextBlock>
        <TextBlock variant="caption" v-if="key === ''" style="font-size: italic">empty</TextBlock>
        <TextBlock variant="caption" v-else>{{ value }}</TextBlock>
      </div>
    </div>

    <template v-slot:footer>
      <Button @click="closeDialog">Close</Button>
    </template>
  </ContentDialog>

  <TerminalServerPickerDialog
    :resource="resource"
    ref="tsPickerDialog"
    force
    @close="handleTerminalServerSelectorDialogClose"
  />
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
