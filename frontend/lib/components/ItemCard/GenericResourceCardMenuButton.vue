<script setup lang="ts">
  import IconButton from '$components/IconButton/IconButton.vue';
  import PropertiesDialog from '$components/ItemCard/PropertiesDialog.vue';
  import TerminalServerPickerDialog from '$components/ItemCard/TerminalServerPickerDialog.vue';
  import { MenuFlyout, MenuFlyoutDivider, MenuFlyoutItem } from '$components/MenuFlyout';
  import { showConfirm } from '$dialogs';
  import { useCoreDataStore, usePopupWindow } from '$stores';
  import {
    favoritesEnabled,
    generateRdpUri,
    openConnectionsInNewWindowEnabled,
    openHelpPopup,
    raw,
    simpleModeEnabled,
    useFavoriteResourceTerminalServers,
  } from '$utils';
  import { useTranslation } from 'i18next-vue';
  import { computed, ref, useTemplateRef } from 'vue';
  import { useRouter } from 'vue-router';
  import MethodPickerDialog from './MethodPickerDialog.vue';

  const { terminalServerAliases, capabilities, iisBase, docsUrl } = useCoreDataStore();
  const { t } = useTranslation();
  const router = useRouter();

  type Resource = NonNullable<
    Awaited<ReturnType<typeof import('$utils').getAppsAndDevices>>
  >['resources'][number];

  const {
    resource,
    class: className,
    placement,
    hideDefaultConnect = false,
    tabindex = -1,
  } = defineProps<{
    resource: Resource;
    class?: string;
    placement: 'top' | 'bottom';
    hideDefaultConnect?: boolean;
    tabindex?: number;
  }>();

  // mounting the menu items and their related dialogs is very
  // expensive when loading many resource cards, so we only mount
  // them when the user has interacted with the menu button
  const menuInteracted = ref(false);
  function activateMenu() {
    menuInteracted.value = true;
  }

  // TODO: requestClose: remove this logic once all browsers have supported this for some time
  const canUseDialogs = HTMLDialogElement.prototype.requestClose !== undefined;

  // TODO [Anchors]: Remove this when all major browsers support CSS Anchor Positioning
  const supportsAnchorPositions = CSS.supports('position-area', 'center center');

  const tsPickerDialog = useTemplateRef<typeof TerminalServerPickerDialog>('tsPickerDialog');
  const openTsPickerDialog = computed(() => raw(tsPickerDialog.value)?.openDialog);
  type TSOnCloseParameters = Parameters<
    NonNullable<InstanceType<typeof TerminalServerPickerDialog>['onClose']>
  >[0];
  const downloadRdpFile = ref<TSOnCloseParameters['downloadRdpFile'] | null>(null);
  const getRdpFileContents = ref<TSOnCloseParameters['getRdpFileContents'] | null>(null);

  const wakeTsPickerDialog = useTemplateRef<typeof TerminalServerPickerDialog>('wakeTsPickerDialog');
  const openWakeTsPickerDialog = computed(() => raw(wakeTsPickerDialog.value)?.openDialog);

  const propertiesDialog = useTemplateRef<typeof PropertiesDialog>('propertiesDialog');
  const openPropertiesDialog = computed(() => raw(propertiesDialog.value)?.openDialog);

  const methodPickerDialog = useTemplateRef<typeof MethodPickerDialog>('methodPickerDialog');
  const openMethodPickerDialog = computed(() => raw(methodPickerDialog.value)?.openDialog);
  const forceShowMethodPicker = ref(false);

  const { favoriteTerminalServers, setFavorite } = useFavoriteResourceTerminalServers(resource);

  function connect(method?: 'forcePicker') {
    forceShowMethodPicker.value = method === 'forcePicker';
    openTsPickerDialog.value?.();
  }

  function requestWorkspaceRefresh() {
    console.log('Requesting workspace refresh from GenericResourceCardMenuButton');
    emit('requestWorkspaceRefresh');
  }

  const emit = defineEmits<{
    (e: 'requestWorkspaceRefresh'): void;
  }>();

  defineExpose({ connect, activateMenu });

  const hostId = ref<string>();

  const { openWindow } = usePopupWindow();

  /**
   * Opens the web client for the given resource and host.
   *
   * If the "Open web client connections in a new window" setting is enabled,
   * this function opens the web client in a new window. Otherwise, this function
   * opens the web client in the same window.
   */
  function goToWebClient(resourceId: string, hostId: string) {
    // timeout to allow dialog to close before navigation
    setTimeout(() => {
      const webGuacdRoute = {
        name: 'webGuacd',
        params: { resourceId, hostId: hostId.replace(':', '‾') },
      };
      const webGuacdUrl = router.resolve(webGuacdRoute).href;

      const windowId = `web-guacd-${resourceId}-${hostId}`;

      if (openConnectionsInNewWindowEnabled.value) {
        openWindow(webGuacdUrl, windowId, 'width=1200,height=1000,menubar=0,status=0');
      } else {
        router.push(webGuacdRoute);
      }
    }, 200);
  }

  /**
   * The terminal servers that can wake this resource's device over the network.
   *
   * Each resource file stores its own MAC address for a resource, so when terminal
   * servers are combined, the same resource may be wakeable on some terminal servers
   * and not on others. The RAWeb server advertises this per host via the resource file URL.
   */
  const wakeableHosts = computed(() => resource.hosts.filter((host) => host.supportsWake));

  /**
   * Whether to offer the wake option at all.
   *
   * The option is hidden unless at least one terminal server advertises that it can
   * wake the device. Only managed .resource files with a MAC address configured ever
   * advertise this, so registry resources and plain .rdp files never show the option.
   */
  const canWakeUp = computed(() => {
    return canUseDialogs && wakeableHosts.value.length > 0;
  });

  const wakeUpHelpHref = `${docsUrl}/wake-on-lan/`;

  async function showWakeOutcome(titleKey: string, descriptionKey: string) {
    return await showConfirm(
      t(titleKey, { name: resource.title }),
      t(descriptionKey, { name: resource.title }),
      '',
      t('dialog.ok'),
      // the dialog only reports the outcome, so dismissing it loses nothing
      { helpAction: () => openHelpPopup(wakeUpHelpHref), closeOnBackdropClick: true }
    ).catch(() => null);
  }

  /**
   * Builds the wake endpoint URL for a terminal server by inserting the wake segment
   * into that host's resource file URL.
   *
   * The request has to go to the RAWeb server that published this host's copy of the
   * resource because that is the server holding the MAC address and the one on
   * a network from which the magic packet is most likely to be configure to be able
   * to reach the device.
   */
  function buildWakeUrl(host: Resource['hosts'][number]) {
    const marker = '/api/resources/';
    const url = new URL(host.url);

    const markerIndex = url.pathname.indexOf(marker);
    if (markerIndex === -1) {
      return null;
    }

    const appBasePath = url.pathname.slice(0, markerIndex);
    const relativeResourcePath = url.pathname.slice(markerIndex + marker.length);

    url.search = '';
    url.hash = '';
    url.pathname = `${appBasePath}${marker}wake/${relativeResourcePath}`;
    return url;
  }

  /**
   * Starts the wake flow, which prompts for a terminal server when more than one of them
   * can wake the device.
   */
  function wakeUp() {
    openWakeTsPickerDialog.value?.();
  }

  /**
   * Asks the selected terminal server's RAWeb server to broadcast a magic packet
   * and then reports the outcome in a dialog.
   */
  async function sendWakeRequest(terminalServerId: string) {
    const host = wakeableHosts.value.find((candidate) => candidate.id === terminalServerId);
    if (!host) {
      return;
    }

    const wakeUrl = buildWakeUrl(host);
    if (!wakeUrl) {
      console.error(`Could not build a wake URL from the resource URL ${host.url.href}`);
      return showWakeOutcome('resource.wakeDialog.error.title', 'resource.wakeDialog.error.description');
    }

    await fetch(wakeUrl, { method: 'POST', credentials: 'include' })
      .then((res) => {
        if (res.ok) {
          return showWakeOutcome(
            'resource.wakeDialog.success.title',
            'resource.wakeDialog.success.description'
          );
        }

        // the server reports a missing MAC address as a 400 error
        if (res.status === 400) {
          return showWakeOutcome(
            'resource.wakeDialog.notConfigured.title',
            'resource.wakeDialog.notConfigured.description'
          );
        }

        throw new Error(`Wake-on-LAN request failed: ${res.status} ${res.statusText}`);
      })
      .catch((err) => {
        console.error(err);
        return showWakeOutcome('resource.wakeDialog.error.title', 'resource.wakeDialog.error.description');
      });
  }
</script>

<template>
  <MenuFlyout :placement="placement" v-if="supportsAnchorPositions">
    <template v-slot="{ popoverId }">
      <IconButton
        :popovertarget="popoverId"
        @click.stop
        @keydown.stop
        @pointerdown="activateMenu"
        @focus="activateMenu"
        :tabindex
        :class="className"
      >
        <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
          <path
            d="M8 12a2 2 0 1 1-4 0 2 2 0 0 1 4 0ZM14 12a2 2 0 1 1-4 0 2 2 0 0 1 4 0ZM18 14a2 2 0 1 0 0-4 2 2 0 0 0 0 4Z"
            fill="currentColor"
          />
        </svg>
      </IconButton>
    </template>
    <template v-slot:menu v-if="menuInteracted">
      <MenuFlyoutItem @click="() => connect()" v-if="!hideDefaultConnect">
        {{ t('resource.menu.connect') }}
        <template v-slot:icon>
          <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path
              d="M7.608 4.615a.75.75 0 0 0-1.108.659v13.452a.75.75 0 0 0 1.108.659l12.362-6.726a.75.75 0 0 0 0-1.318L7.608 4.615ZM5 5.274c0-1.707 1.826-2.792 3.325-1.977l12.362 6.726c1.566.853 1.566 3.101 0 3.953L8.325 20.702C6.826 21.518 5 20.432 5 18.726V5.274Z"
              fill="currentColor"
            />
          </svg>
        </template>
      </MenuFlyoutItem>
      <MenuFlyoutItem
        @click="() => connect('forcePicker')"
        :indented="!hideDefaultConnect"
        v-if="canUseDialogs"
      >
        {{ t('resource.menu.connectWith') }}…
        <template v-slot:icon v-if="hideDefaultConnect">
          <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path
              d="M7.608 4.615a.75.75 0 0 0-1.108.659v13.452a.75.75 0 0 0 1.108.659l12.362-6.726a.75.75 0 0 0 0-1.318L7.608 4.615ZM5 5.274c0-1.707 1.826-2.792 3.325-1.977l12.362 6.726c1.566.853 1.566 3.101 0 3.953L8.325 20.702C6.826 21.518 5 20.432 5 18.726V5.274Z"
              fill="currentColor"
            />
          </svg>
        </template>
      </MenuFlyoutItem>
      <MenuFlyoutItem @click="wakeUp" v-if="canWakeUp">
        {{ t('resource.menu.wakeUp') }}
        <template v-slot:icon>
          <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path
              d="M8.205 4.844a1 1 0 0 1 .844 1.813A6.997 6.997 0 0 0 12 20a6.998 6.998 0 0 0 2.965-13.336 1 1 0 0 1 .848-1.812A8.996 8.996 0 0 1 21 13.003C21 17.973 16.97 22 12 22s-9-4.028-9-8.996a8.996 8.996 0 0 1 5.205-8.16ZM12 2a1 1 0 0 1 .993.883L13 3v7a1 1 0 0 1-1.993.118L11 10V3a1 1 0 0 1 1-1Z"
              fill="currentColor"
            />
          </svg>
        </template>
      </MenuFlyoutItem>
      <MenuFlyoutDivider v-if="favoritesEnabled && !simpleModeEnabled"></MenuFlyoutDivider>
      <template v-for="host in resource.hosts" :key="host.id" v-if="favoritesEnabled && !simpleModeEnabled">
        <MenuFlyoutItem v-if="favoriteTerminalServers.includes(host.id)" @click="setFavorite(host.id, false)">
          <span class="dual-line-menu-item">
            <span>{{ t('resource.menu.favRemove') }}</span>
            <span v-if="resource.hosts.length > 1">{{ terminalServerAliases[host.name] ?? host.name }}</span>
          </span>
          <template v-slot:icon>
            <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path
                d="M3.28 2.22a.75.75 0 1 0-1.06 1.06l4.804 4.805-3.867.561c-1.107.161-1.55 1.522-.748 2.303l3.815 3.719-.9 5.251c-.19 1.103.968 1.944 1.959 1.423l4.715-2.479 4.716 2.48c.99.52 2.148-.32 1.96-1.424l-.04-.223 2.085 2.084a.75.75 0 0 0 1.061-1.06L3.28 2.22Zm13.518 15.639.345 2.014-4.516-2.374a1.35 1.35 0 0 0-1.257 0l-4.516 2.374.862-5.03a1.35 1.35 0 0 0-.388-1.194l-3.654-3.562 4.673-.679 8.45 8.45ZM20.323 10.087l-3.572 3.482 1.06 1.06 3.777-3.68c.8-.781.359-2.142-.748-2.303l-5.273-.766-2.358-4.777c-.495-1.004-1.926-1.004-2.421 0L9.3 6.118l1.12 1.12 1.578-3.2 2.259 4.577a1.35 1.35 0 0 0 1.016.738l5.05.734Z"
                fill="currentColor"
              />
            </svg>
          </template>
        </MenuFlyoutItem>
        <MenuFlyoutItem v-else @click="setFavorite(host.id, true)">
          <span class="dual-line-menu-item">
            <span>{{ t('resource.menu.favAdd') }}</span>
            <span v-if="resource.hosts.length > 1">{{ terminalServerAliases[host.name] ?? host.name }}</span>
          </span>
          <template v-slot:icon>
            <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path
                d="M10.788 3.103c.495-1.004 1.926-1.004 2.421 0l2.358 4.777 5.273.766c1.107.161 1.549 1.522.748 2.303l-3.816 3.72.901 5.25c.19 1.103-.968 1.944-1.959 1.424l-4.716-2.48-4.715 2.48c-.99.52-2.148-.32-1.96-1.424l.901-5.25-3.815-3.72c-.801-.78-.359-2.142.748-2.303L8.43 7.88l2.358-4.777Zm1.21.936L9.74 8.615a1.35 1.35 0 0 1-1.016.738l-5.05.734 3.654 3.562c.318.31.463.757.388 1.195l-.862 5.03 4.516-2.375a1.35 1.35 0 0 1 1.257 0l4.516 2.374-.862-5.029a1.35 1.35 0 0 1 .388-1.195l3.654-3.562-5.05-.734a1.35 1.35 0 0 1-1.016-.738l-2.259-4.576Z"
                fill="currentColor"
              />
            </svg>
          </template>
        </MenuFlyoutItem>
      </template>
      <MenuFlyoutDivider></MenuFlyoutDivider>
      <MenuFlyoutItem @click="() => openPropertiesDialog()" v-if="canUseDialogs">
        {{ t('resource.menu.props') }}
        <template v-slot:icon>
          <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path
              d="M10.5 7.751a5.75 5.75 0 0 1 8.38-5.114.75.75 0 0 1 .186 1.197L16.301 6.6l1.06 1.06 2.779-2.778a.75.75 0 0 1 1.193.179 5.75 5.75 0 0 1-6.422 8.284l-7.365 7.618a3.05 3.05 0 0 1-4.387-4.24l7.475-7.734a5.766 5.766 0 0 1-.134-1.238Zm5.75-4.25a4.25 4.25 0 0 0-4.067 5.489.75.75 0 0 1-.178.74l-7.768 8.035a1.55 1.55 0 1 0 2.23 2.156l7.676-7.941a.75.75 0 0 1 .775-.191 4.25 4.25 0 0 0 5.466-5.03l-2.492 2.492a.75.75 0 0 1-1.061 0L14.71 7.13a.75.75 0 0 1 0-1.06l2.466-2.467a4.268 4.268 0 0 0-.926-.102Z"
              fill="currentColor"
            />
          </svg>
        </template>
      </MenuFlyoutItem>
    </template>
  </MenuFlyout>

  <TerminalServerPickerDialog
    v-if="menuInteracted"
    :resource="resource"
    ref="tsPickerDialog"
    @close="
      ({ downloadRdpFile: dl, getRdpFileContents: gfc, selectedTerminalServer }) => {
        downloadRdpFile = dl;
        getRdpFileContents = gfc;

        const foundHost = resource.hosts.find((host) => host.id === selectedTerminalServer);
        hostId = foundHost?.id;
        const isSignedRdpFile = foundHost?.rdp?.signature;

        // signed RDP files are too long - see https://issues.chromium.org/issues/41322340#comment3
        const allowedMethods = isSignedRdpFile || !canUseDialogs ? ['rdpFile'] : ['rdpFile', 'rdpProtocolUri'];
        if (canUseDialogs && hostId && capabilities.supportsGuacdWebClient) {
          allowedMethods.push('webGuacd');
        }

        openMethodPickerDialog(forceShowMethodPicker, allowedMethods);
      }
    "
  />

  <MethodPickerDialog
    v-if="menuInteracted"
    :resourceTitle="resource.title"
    ref="methodPickerDialog"
    :allowRememberMethod="supportsAnchorPositions"
    @close="
      ({ selectedMethod }) => {
        if (selectedMethod === 'rdpFile') {
          downloadRdpFile?.();
        }
        if (selectedMethod === 'rdpProtocolUri') {
          const contents = getRdpFileContents?.();
          if (contents) {
            const uri = generateRdpUri(contents, true);
          }
        }
        if (selectedMethod === 'webGuacd' && hostId) {
          goToWebClient(resource.id, hostId);
        }
        downloadRdpFile = null;
        getRdpFileContents = null;
        hostId = undefined;
      }
    "
  />

  <TerminalServerPickerDialog
    v-if="menuInteracted && canWakeUp"
    :resource="resource"
    :hosts="wakeableHosts"
    :title="t('resource.wakeDialog.pickerTitle', { resourceTitle: resource.title })"
    ref="wakeTsPickerDialog"
    @close="({ selectedTerminalServer }) => sendWakeRequest(selectedTerminalServer)"
  />

  <PropertiesDialog
    v-if="menuInteracted"
    :resource="resource"
    ref="propertiesDialog"
    @requestWorkspaceRefresh="requestWorkspaceRefresh"
  />
</template>

<style scoped>
  .dual-line-menu-item {
    display: flex;
    flex-direction: column;
  }
  .dual-line-menu-item > span + span {
    opacity: 0.5;
    font-size: 10px;
    line-height: 10px;
    padding-bottom: 2px;
  }
</style>
