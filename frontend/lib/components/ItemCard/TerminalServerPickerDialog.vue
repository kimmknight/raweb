<script setup lang="ts">
  import { Button, ContentDialog, PickerItem } from '$components';
  import { useCoreDataStore } from '$stores';
  import { generateRdpFileContents, openHelpPopup, raw } from '$utils';
  import { computed, ref, useTemplateRef } from 'vue';

  type Resource = NonNullable<
    Awaited<ReturnType<typeof import('$utils').getAppsAndDevices>>
  >['resources'][number];

  interface OnCloseParameters {
    selectedTerminalServer: string;
    downloadRdpFile: (_window?: typeof window) => void;
    getRdpFileContents?: () => string | undefined;
  }

  const props = defineProps<{
    resource: Resource;
    /** Show the dialog even if a preferred terminal server for this app has already been selected */
    force?: boolean;
    /**
     * The terminal servers to offer. Defaults to every terminal server that hosts the
     * resource. Provide a subset when only some of them support the action being taken,
     * such as waking the device over the network.
     */
    hosts?: Resource['hosts'];
    /** Overrides the dialog title. Defaults to the title used when connecting. */
    title?: string;
  }>();

  const emit = defineEmits<{
    (e: 'close', params: OnCloseParameters): void;
  }>();

  const { docsUrl } = useCoreDataStore();

  const tsPickerDialog = useTemplateRef<typeof ContentDialog>('tsPickerDialog');
  const openDialog = computed(() => raw(tsPickerDialog.value)?.open);
  const closeDialog = computed(() => raw(tsPickerDialog.value)?.close);
  const dialogIsOpen = computed(() => raw(tsPickerDialog.value)?.isOpen);
  const popoverId = computed(() => raw(tsPickerDialog.value)?.popoverId);
  /** The terminal servers offered by this instance of the picker */
  const availableHosts = computed(() => props.hosts ?? props.resource.hosts);

  const selectedTerminalServer = ref(availableHosts.value[0]?.id || '');

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
      getRdpFileContents: () => buildRdpFile(foundHost),
    });
  }

  function handleSubmitKeydown(evt: KeyboardEvent) {
    if (evt.key === 'Enter' || evt.key === ' ') {
      evt.preventDefault();
      submit();
    }
  }

  const { authUser, terminalServerAliases } = useCoreDataStore();

  function buildRdpFile(host: Resource['hosts'][number]) {
    if (host.rdp && !host.rdp.Signed) {
      // attempt to infer the domain name from the host URL
      const maybeDomainHost = `${host.rdp['full address']}`.split('.').slice(1).join('.');
      const maybeDomainNetBios = `${host.rdp['full address']}`.split('.')[1];

      // use the current authenticated user's username if the rdp file
      // does not already specify a username
      const username = (() => {
        if (host.rdp.username) return host.rdp.username;
        if (authUser && authUser.username) {
          // // only include the domain if it is not already specified in the rdp file
          // // and if the host URL has a domain part (not just an IP address or netbios name)
          // if (maybeDomainHost && !host.rdp.domain) return `${authUser.username}@${maybeDomainHost}`;
          return `${authUser.username}`;
        }
      })();

      // TODO: offer a mechanism for embedding a password in the RDP file on Windows clients
      // (probably through a settings page that allows a user to set passwords for )
      // create password: https://github.com/RedAndBlueEraser/rdp-file-password-encryptor/blob/master/rdp-file-password-encryptor.ps1
      // or use ("MySuperSecretPassword!" | ConvertTo-SecureString -AsPlainText -Force) | ConvertFrom-SecureString
      // property: password 51:b:<result>

      const rdpFileText = generateRdpFileContents({
        ...host.rdp,
        domain: host.rdp.domain || maybeDomainNetBios,
        username,
        'workspace id': host.name, // shown by Windows in parenthesis after the app name in the taskbar (instead of "(Remote)")
      });

      return rdpFileText;
    }
  }

  function downloadRdpFile(title: string, host: Resource['hosts'][number], _window: typeof window) {
    // attempt to build the RDP file contents from the selected host, but
    // fall back to the download URL if the rdp file properties could not be found
    const rdpFileText = buildRdpFile(host);
    let downloadUrl = '';
    if (rdpFileText) {
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
    if (availableHosts.value.length === 0) return;

    // TODO: if the resource has a preferred terminal server, select it and submit it
    // without showing the dialog (unless the force prop is true)

    // the offered terminal servers can change between openings, so make sure the
    // selection still refers to one of them
    const selectionIsAvailable = availableHosts.value.some((host) => host.id === selectedTerminalServer.value);
    if (!selectionIsAvailable) {
      selectedTerminalServer.value = availableHosts.value[0].id;
    }

    // if there is only one terminal server, select it and submit it
    // without showing the dialog
    if (availableHosts.value.length === 1) {
      selectedTerminalServer.value = availableHosts.value[0].id;
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
    :title="title ?? $t('resource.tsPicker.title', { resourceTitle: props.resource.title })"
    ref="tsPickerDialog"
    class="ts-picker-dialog"
    acrylic
    @contextmenu.stop
    @keydown.stop
    @click.stop
  >
    <div class="picker-items">
      <PickerItem
        v-for="host in availableHosts"
        :key="popoverId + host.id"
        :name="`${popoverId}-host-${resource.id}`"
        :value="host.id"
        v-model="selectedTerminalServer"
        @dblclick="submit"
      >
        {{ terminalServerAliases[host.name] ?? host.name }}
      </PickerItem>
    </div>

    <Button
      variant="hyperlink"
      :href="docsUrl + '/settings/combined-mode/'"
      @click.prevent="openHelpPopup(docsUrl + '/settings/combined-mode/')"
      style="margin: 0 0.375rem"
    >
      {{ $t('resource.tsPicker.help') }}
    </Button>

    <template v-slot:footer>
      <Button @click="submit" @keydown.stop="handleSubmitKeydown">{{ $t('dialog.once') }}</Button>
    </template>
  </ContentDialog>
</template>

<style>
  .ts-picker-dialog .picker-items {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
    padding-bottom: 0.75rem;
  }
</style>
