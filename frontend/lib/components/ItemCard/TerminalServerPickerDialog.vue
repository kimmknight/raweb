<script setup lang="ts">
  import Button from '$components/Button/Button.vue';
  import ContentDialog from '$components/ContentDialog/ContentDialog.vue';
  import TextBlock from '$components/TextBlock/TextBlock.vue';
  import { generateRdpFileContents, raw } from '$utils';
  import { computed, ref, useTemplateRef } from 'vue';

  type Resource = NonNullable<
    Awaited<ReturnType<typeof import('$utils').getAppsAndDevices>>
  >['resources'][number];

  interface OnCloseParameters {
    selectedTerminalServer: string;
    downloadRdpFile: (_window?: typeof window) => void;
  }

  const props = defineProps<{
    resource: Resource;
    /** Show the dialog even if a preferred terminal server for this app has already been selected */
    force?: boolean;
  }>();

  const emit = defineEmits<{
    (e: 'close', params: OnCloseParameters): void;
  }>();

  const tsPickerDialog = useTemplateRef<typeof ContentDialog>('tsPickerDialog');
  const openDialog = computed(() => raw(tsPickerDialog.value)?.open);
  const closeDialog = computed(() => raw(tsPickerDialog.value)?.close);
  const dialogIsOpen = computed(() => raw(tsPickerDialog.value)?.isOpen);
  const popoverId = computed(() => raw(tsPickerDialog.value)?.popoverId);
  const selectedTerminalServer = ref(props.resource.hosts[0]?.id || '');

  function submit() {
    closeDialog.value?.();

    const foundHost = props.resource.hosts.find((host) => host.id === selectedTerminalServer.value);
    if (!foundHost) return;

    // TODO: add functionality for saving a preference for this app/desktop

    emit('close', {
      selectedTerminalServer: selectedTerminalServer.value,
      downloadRdpFile: (_window: typeof window = window) =>
        downloadRdpFile(
          `${props.resource.title} (${terminalServerAliases[foundHost.name] ?? foundHost.name})`,
          foundHost,
          _window
        ),
    });
  }

  function handleSubmitKeydown(evt: KeyboardEvent) {
    if (evt.key === 'Enter' || evt.key === ' ') {
      evt.preventDefault();
      submit();
    }
  }

  const authUser = window.__authUser;
  const terminalServerAliases = window.__terminalServerAliases;

  function downloadRdpFile(title: string, host: Resource['hosts'][number], _window: typeof window) {
    // attempt to build the RDP file contents from the selected host, but
    // fall back to the download URL if the rdp file properties could not be found
    let downloadUrl = '';
    if (host.rdp) {
      // attempt to infer the domain name from the host URL
      const maybeDomainHost = `${host.rdp['full address']}`.split('.').slice(1).join('.');
      const maybeDomainNetBios = `${host.rdp['full address']}`.split('.')[1];

      // use the current authenticated user's username if the rdp file
      // does not already specify a username
      const username = (() => {
        if (host.rdp.username) return host.rdp.username;
        if (authUser && authUser.username) {
          // only include the domain if it is not already specified in the rdp file
          // and if the host URL has a domain part (not just an IP address or netbios name)
          if (maybeDomainHost && !host.rdp.domain) return `${authUser.username}@${maybeDomainHost}`;
          return `${authUser.username}`;
        }
      })();

      // TODO: offer a mekanism for embedding a password in the RDP file on Windows clients
      // (probably through a settings page that allows a user to set passwords for )
      // create password: https://github.com/RedAndBlueEraser/rdp-file-password-encryptor/blob/master/rdp-file-password-encryptor.ps1
      // or use ("MySuperSecretPassword!" | ConvertTo-SecureString -AsPlainText -Force) | ConvertFrom-SecureString
      // property: password 51:b:<result>

      const rdpFileText = generateRdpFileContents({
        ...host.rdp,
        domain: host.rdp.domain || maybeDomainNetBios,
        username,
      });
      const blob = new Blob([rdpFileText], { type: 'application/x-rdp' });
      downloadUrl = URL.createObjectURL(blob);
    } else {
      downloadUrl = host.url.href;
    }

    // trigger the download
    const a = _window.document.createElement('a');
    a.href = downloadUrl;
    a.download = `${title}.rdp`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(downloadUrl);
  }

  function open() {
    if (props.resource.hosts.length === 0) return;

    // TODO: if the resource has a preferred terminal server, select it and submit it
    // without showing the dialog (unless the force prop is true)

    // if there is only one terminal server, select it and submit it
    // without showing the dialog
    if (props.resource.hosts.length === 1) {
      selectedTerminalServer.value = props.resource.hosts[0].id;
      submit();
      return;
    }

    openDialog.value?.();
  }

  // Expose the openDialog method to the parent component
  defineExpose({ openDialog: open, isOpen: dialogIsOpen });
</script>

<template>
  <ContentDialog
    :title="`Select a terminal server for ${props.resource.title}`"
    ref="tsPickerDialog"
    @contextmenu.stop
    @keydown.stop
    @click.stop
  >
    <div v-for="host in resource.hosts" :key="popoverId + host.id" class="picker-item" @dblclick="submit">
      <input
        type="radio"
        :name="`${popoverId}-host-${resource.id}`"
        :value="host.id"
        :id="`${popoverId}-host-${host.id}`"
        v-model="selectedTerminalServer"
      />
      <label :for="`${popoverId}-host-${host.id}`">
        <TextBlock variant="body">{{ terminalServerAliases[host.name] ?? host.name }}</TextBlock>
      </label>
    </div>

    <template v-slot:footer>
      <Button @click="submit" @keydown.stop="handleSubmitKeydown">Just once</Button>
    </template>
  </ContentDialog>
</template>

<style scoped>
  /* Move the dialog-related styles here if any */
  .picker-item {
    color: currentColor;
    inline-size: 100%;
    block-size: 40px;
    position: relative;
    user-select: none;
    -webkit-user-drag: none;
    position: relative;
    border-radius: var(--wui-control-corner-radius);
  }
  .picker-item:hover {
    background-color: var(--wui-subtle-secondary);
    color: var(--wui-text-secondary);
  }
  .picker-item:active {
    background-color: var(--wui-subtle-tertiary);
    color: var(--wui-text-tertiary);
    will-change: line-height, background-color, color;
  }
  .picker-item:has(input:checked) {
    background-color: var(--wui-subtle-secondary);
  }
  .picker-item:has(input:checked):hover {
    background-color: var(--wui-subtle-tertiary);
  }

  .picker-item input {
    position: absolute;
    height: 100%;
    width: 100%;
    margin: 0;
    appearance: none;
  }
  .picker-item input:checked::after {
    content: '';
    position: absolute;
    width: 3px;
    height: 38%;
    top: 31%;
    left: 0;
    background-color: var(--wui-accent-default);
    border-radius: var(--wui-control-corner-radius);
  }

  .picker-item label {
    position: absolute;
    inset: 0;
    display: flex;
    align-items: center;
    padding: 16px;
  }
</style>
