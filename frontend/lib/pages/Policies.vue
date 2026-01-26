<script setup lang="ts">
  import { Button, PolicyDialog, TextBlock } from '$components';
  import { ManagedResourceListDialog, showConfirm } from '$dialogs';
  import { useCoreDataStore } from '$stores';
  import { isUrl, notEmpty, useWebfeedData } from '$utils';
  import { useTranslation } from 'i18next-vue';
  import { onMounted, ref } from 'vue';

  const { iisBase } = useCoreDataStore();
  const { t } = useTranslation();

  const props = defineProps<{
    refreshWorkspace: () => ReturnType<typeof useWebfeedData>['refresh'];
  }>();

  const isSecureContext = window.isSecureContext;

  const data = ref<Record<string, unknown> | null>({});
  const error = ref(null);
  const loading = ref(false);
  async function fetchPolicies() {
    loading.value = true;
    return fetch(iisBase + 'api/policies')
      .then((response) => {
        if (!response.ok) {
          throw new Error('Network response was not ok');
        }
        return response.json();
      })
      .then((json) => {
        data.value = json;
        error.value = null;
      })
      .catch((err) => {
        error.value = err.message;
        data.value = null;
      })
      .finally(() => {
        loading.value = false;
      });
  }

  onMounted(() => {
    fetchPolicies();
  });

  function moveFocusUp() {
    const focusedElement = document.activeElement as HTMLElement;
    if (focusedElement) {
      const previousElement = focusedElement.previousElementSibling?.previousElementSibling as
        | HTMLElement
        | undefined;
      if (previousElement) {
        focusedElement.setAttribute('tabindex', '-1');
        previousElement.focus();
        previousElement.setAttribute('tabindex', '0');
      }
    }
  }

  function moveFocusDown() {
    const focusedElement = document.activeElement as HTMLElement;
    if (focusedElement) {
      const nextElement = focusedElement.nextElementSibling?.nextElementSibling as HTMLElement | undefined;
      if (nextElement) {
        focusedElement.setAttribute('tabindex', '-1');
        nextElement.focus();
        nextElement.setAttribute('tabindex', '0');
      }
    }
  }

  async function showAlert(message: string) {
    await showConfirm(t('policies.alertTitle'), message, '', t('dialog.ok')).catch(() => {});
  }

  async function setPolicy(key: string, value: string | boolean | null, { noRefresh = false } = {}) {
    loading.value = true;
    return fetch(iisBase + 'api/policies/' + key + '/', {
      method: 'POST',
      body: value?.toString(),
    })
      .then(async (res) => {
        if (!res.ok) {
          const errorText = await res.text();
          throw new Error(errorText || 'Network response was not ok');
        }

        if (noRefresh) {
          return;
        }
        return fetchPolicies();
      })
      .catch(async (err) => {
        await showAlert(`Error setting policy: ${err.message}`);
      })
      .finally(() => {
        loading.value = false;
      });
  }

  const policyEditorSpecs: {
    key: InstanceType<typeof PolicyDialog>['$props']['name'];
    extraKeys?: InstanceType<typeof PolicyDialog>['$props']['name'][];
    appliesTo: InstanceType<typeof PolicyDialog>['$props']['appliesTo'];
    extraFields?: InstanceType<typeof PolicyDialog>['$props']['extraFields'];
    onApply: InstanceType<typeof PolicyDialog>['$props']['onSave'];
    transformVisibleState?: (state: 'enabled' | 'disabled' | 'unset') => 'enabled' | 'disabled' | 'unset';
  }[] = [
    {
      key: 'App.FavoritesEnabled',
      appliesTo: ['Web client'],
      onApply: async (closeDialog, state: boolean | null) => {
        await setPolicy('App.FavoritesEnabled', state);
        closeDialog();
      },
    },
    {
      key: 'TerminalServerAliases',
      appliesTo: ['Web client', 'Workspace'],
      extraFields: [
        {
          key: 'aliases',
          label: 'Aliases',
          type: 'key-value',
          multiple: true,
          interpret: (value: string) => {
            const pairs = value.split(';').map((pair) => pair.trim());
            const result: [string, string][] = [];
            pairs.forEach((pair) => {
              const [key, val] = pair.split('=').map((s) => s.trim());
              if (key && val) {
                result.push([key, val]);
              }
            });
            return result;
          },
        },
      ],
      onApply: async (closeDialog, state, extraFieldsState) => {
        if (!state || !extraFieldsState) {
          await setPolicy('TerminalServerAliases', null);
          closeDialog();
          return;
        }

        if (
          !extraFieldsState.aliases ||
          !Array.isArray(extraFieldsState.aliases) ||
          extraFieldsState.aliases.length === 0 ||
          !extraFieldsState.aliases.every(
            (pair: unknown): pair is [string, string] =>
              Array.isArray(pair) &&
              pair.length === 2 &&
              typeof pair[0] === 'string' &&
              typeof pair[1] === 'string'
          )
        ) {
          await setPolicy('TerminalServerAliases', '');
          closeDialog();
          return;
        }

        const aliasesString = extraFieldsState.aliases.map(([key, val]) => `${key}=${val}`).join(';');
        await setPolicy('TerminalServerAliases', aliasesString);
        closeDialog();
      },
    },
    {
      key: 'App.CombineTerminalServersModeEnabled',
      appliesTo: ['Web client'],
      onApply: async (closeDialog, state: boolean | null) => {
        await setPolicy('App.CombineTerminalServersModeEnabled', state);
        closeDialog();
      },
    },
    {
      key: 'App.OpenConnectionsInNewWindowEnabled',
      appliesTo: ['Web client'],
      onApply: async (closeDialog, state: boolean | null) => {
        await setPolicy('App.OpenConnectionsInNewWindowEnabled', state);
        closeDialog();
      },
    },
    {
      key: 'App.FlatModeEnabled',
      appliesTo: ['Web client'],
      onApply: async (closeDialog, state: boolean | null) => {
        await setPolicy('App.FlatModeEnabled', state);
        closeDialog();
      },
    },
    {
      key: 'App.IconBackgroundsEnabled',
      appliesTo: ['Web client'],
      onApply: async (closeDialog, state: boolean | null) => {
        await setPolicy('App.IconBackgroundsEnabled', state);
        closeDialog();
      },
    },
    {
      key: 'App.HidePortsEnabled',
      appliesTo: ['Web client'],
      onApply: async (closeDialog, state: boolean | null) => {
        await setPolicy('App.HidePortsEnabled', state);
        closeDialog();
      },
    },
    {
      key: 'App.SimpleModeEnabled',
      appliesTo: ['Web client'],
      onApply: async (closeDialog, state: boolean | null) => {
        await setPolicy('App.SimpleModeEnabled', state);
        closeDialog();
      },
    },
    {
      key: 'App.ConnectionMethod.RdpFileDownload.Enabled',
      appliesTo: ['Web client'],
      onApply: async (closeDialog, state: boolean | null) => {
        await setPolicy('App.ConnectionMethod.RdpFileDownload.Enabled', state);
        closeDialog();
      },
    },
    {
      key: 'App.ConnectionMethod.RdpProtocol.Enabled',
      appliesTo: ['Web client'],
      onApply: async (closeDialog, state: boolean | null) => {
        await setPolicy('App.ConnectionMethod.RdpProtocol.Enabled', state);
        closeDialog();
      },
    },
    {
      key: 'RegistryApps.Enabled',
      appliesTo: ['Web client', 'Workspace'],
      onApply: async (closeDialog, state: boolean | null) => {
        // this is an old setting that was repurposed to mean something else
        // now, enabled means that the list of apps under TSAllowList are used
        // and disabled or unset meansd that the list of apps under
        // the app id in centralpublishedresources is used

        // HOWEVER, the GUI shows the opposite for enabled/disabled, so we need to invert it here
        await setPolicy('RegistryApps.Enabled', state === null ? null : !state);
        closeDialog();
      },
      transformVisibleState: (state) => {
        // the GUI shows the opposite for enabled/disabled, so we need to invert it here
        if (state === 'unset') {
          return 'unset';
        } else if (state === 'enabled') {
          return 'disabled';
        } else {
          return 'enabled';
        }
      },
    },
    {
      key: 'RegistryApps.FullAddressOverride',
      appliesTo: ['Web client', 'Workspace'],
      extraFields: [
        {
          key: 'origin',
          label: 'Origin',
          type: 'string',
        },
      ],
      onApply: async (closeDialog, state, extraFieldsState) => {
        if (!state || !extraFieldsState) {
          await setPolicy('RegistryApps.FullAddressOverride', null);
          closeDialog();
          return;
        }

        await setPolicy('RegistryApps.FullAddressOverride', extraFieldsState.origin.toString());
        closeDialog();
      },
    },
    {
      key: 'RegistryApps.AdditionalProperties',
      appliesTo: ['Web client', 'Workspace'],
      extraFields: [
        {
          key: 'properties',
          label: 'Properties',
          type: 'string',
          multiple: true,
          interpret: (value: string) => {
            return value
              .replaceAll('\\;', '🤮')
              .split(';')
              .map((property) => [property.trim().replaceAll('🤮', ';'), '']);
          },
        },
      ],
      onApply: async (closeDialog, state, extraFieldsState) => {
        if (!state || !extraFieldsState) {
          await setPolicy('RegistryApps.AdditionalProperties', null);
          closeDialog();
          return;
        }

        if (!extraFieldsState.properties || !Array.isArray(extraFieldsState.properties)) {
          await setPolicy('RegistryApps.AdditionalProperties', '');
          closeDialog();
          return;
        }

        const propertiesString = extraFieldsState.properties.map((p) => p[0].replaceAll(';', '\\;')).join(';');
        await setPolicy('RegistryApps.AdditionalProperties', propertiesString);
        closeDialog();
      },
    },
    {
      key: 'Workspace.ShowMultiuserResourcesUserAndGroupNames',
      appliesTo: ['Web client', 'Workspace version 2.0 or newer'],
      onApply: async (closeDialog, state: boolean | null) => {
        await setPolicy('Workspace.ShowMultiuserResourcesUserAndGroupNames', state);
        closeDialog();
      },
    },
    {
      key: 'UserCache.Enabled',
      appliesTo: ['Web client', 'Workspace'],
      onApply: async (closeDialog, state: boolean | null) => {
        await setPolicy('UserCache.Enabled', state);
        closeDialog();
      },
    },
    {
      key: 'UserCache.StaleWhileRevalidate',
      appliesTo: ['Web client', 'Workspace'],
      onApply: async (closeDialog, state, extraFields) => {
        if (!state) {
          await setPolicy('UserCache.StaleWhileRevalidate', null);
          closeDialog();
          return;
        }

        const maxAge = extraFields?.maxAge;
        if (
          typeof maxAge !== 'string' ||
          maxAge === '' ||
          isNaN(Number(maxAge)) ||
          !Number.isInteger(Number(maxAge)) ||
          Number(maxAge) < -1
        ) {
          await showAlert('Maximum cache age must be an integer greater than or equal to -1.');
          closeDialog(false);
          return;
        }

        await setPolicy('UserCache.StaleWhileRevalidate', maxAge);
        closeDialog();
      },
      extraFields: [
        {
          key: 'maxAge',
          label: 'Maximum cache age (seconds)',
          type: 'string',
        },
      ],
    },
    {
      key: 'PasswordChange.Enabled',
      appliesTo: ['Web client'],
      onApply: async (closeDialog, state: boolean | null) => {
        await setPolicy('PasswordChange.Enabled', state);
        closeDialog();
      },
    },
    {
      key: 'App.Alerts.SignedInUser',
      appliesTo: ['Web client'],
      extraFields: [
        {
          key: 'alerts',
          label: 'Alerts',
          type: 'json',
          multiple: true,
          interpret: (value: string) => {
            const json = value ? JSON.parse(value) : null;
            if (!json || !Array.isArray(json)) {
              return [];
            }
            return json;
          },
          jsonFields: {
            title: 'Title',
            message: 'Message',
            linkText: 'Link text',
            linkHref: 'Link URL',
          },
        },
      ],
      onApply: async (closeDialog, state, extraFieldsState) => {
        if (!state || !extraFieldsState) {
          await setPolicy('App.Alerts.SignedInUser', null);
          closeDialog();
          return;
        }

        if (
          !extraFieldsState.alerts ||
          !Array.isArray(extraFieldsState.alerts) ||
          extraFieldsState.alerts.length === 0
        ) {
          await setPolicy('App.Alerts.SignedInUser', '');
          closeDialog();
          return;
        }

        await setPolicy('App.Alerts.SignedInUser', JSON.stringify(extraFieldsState.alerts));
        closeDialog();
        return;
      },
    },
    {
      key: 'GuacdWebClient.Address',
      appliesTo: ['Web client'],
      transformVisibleState() {
        if (!data.value) {
          return 'unset';
        }

        const enabledValue = data.value['GuacdWebClient.Enabled'];
        if (enabledValue === undefined || enabledValue === null || enabledValue === '') {
          return 'unset';
        }
        if (enabledValue === 'true') {
          return 'enabled';
        }
        return 'disabled';
      },
      onApply: async (closeDialog, state, extraFields) => {
        // set whether the web client is enabled
        await setPolicy('GuacdWebClient.Enabled', state, { noRefresh: true });

        // for not configured, reset the value
        if (state === null) {
          // await setPolicy('GuacdWebClient.Address', null); // temporarily keep the old value until the policy is reconfigured
          closeDialog();
          return;
        }

        // for disabled, do nothing else
        if (state === false) {
          closeDialog();
          return;
        }

        // validate the fields
        const externalAddress = extraFields?.externalAddress?.[0];
        const isObject = (value: unknown): value is Record<string, unknown> =>
          typeof value === 'object' && value !== null && !Array.isArray(value);
        console.log(externalAddress, isObject(externalAddress));
        if (isObject(externalAddress)) {
          const hostname = externalAddress?.hostname;
          const port = externalAddress?.port;
          if (typeof hostname !== 'string' || hostname === '') {
            await showAlert(t('policies.GuacdWebClient.Address.errors.hostnameEmpty'));
            closeDialog(false);
            return;
          }
          if (typeof port !== 'string' || port === '') {
            await showAlert(t('policies.GuacdWebClient.Address.errors.portEmpty'));
            closeDialog(false);
            return;
          }
          if (hostname.includes('://') || !isUrl(`https://${hostname}`, { requireTopLevelDomain: true })) {
            await showAlert(t('policies.GuacdWebClient.Address.errors.hostnameInvalid'));
            closeDialog(false);
            return;
          }

          // set the policy value
          const policyValue = `${hostname}:${port}`;
          await setPolicy('GuacdWebClient.Address', policyValue, { noRefresh: true });
          closeDialog();
        }

        // set the policy value
        const useContainer = extraFields?.useContainer;
        if (typeof useContainer !== 'string' || (useContainer !== 'true' && useContainer !== 'false')) {
          await showAlert(t('policies.GuacdWebClient.Address.errors.methodInvalid'));
          closeDialog(false);
          return;
        }

        const policyValue = useContainer === 'true' ? 'container' : 'external';
        await setPolicy('GuacdWebClient.Method', policyValue);
        closeDialog();
      },
      extraFields: [
        {
          key: 'useContainer',
          label: t('policies.GuacdWebClient.Address.fields.method'),
          type: 'boolean',
          keyValueLabels: [
            t('policies.GuacdWebClient.Address.fields.useContainer'),
            t('policies.GuacdWebClient.Address.fields.useExternal'),
          ],
          interpret: () => (data.value?.['GuacdWebClient.Method'] === 'external' ? 'false' : 'true'),
        },
        {
          key: 'externalAddress',
          type: 'json',
          label: t('policies.GuacdWebClient.Address.fields.externalAddress'),
          jsonFields: {
            hostname: t('policies.GuacdWebClient.Address.fields.externalHostname'),
            port: t('policies.GuacdWebClient.Address.fields.externalPort'),
          },
          interpret: (value) => {
            return [
              {
                hostname: value?.split(':')[0] || '',
                port: value?.split(':')[1] || '',
              },
            ];
          },
        },
      ],
    },
    {
      key: 'App.Auth.MFA.Duo',
      appliesTo: ['Web client'],
      transformVisibleState() {
        if (!data.value) {
          return 'unset';
        }

        const enabledValue = data.value['App.Auth.MFA.Duo.Enabled'];
        if (enabledValue === undefined || enabledValue === null || enabledValue === '') {
          return 'unset';
        }
        if (enabledValue === 'true') {
          return 'enabled';
        }
        return 'disabled';
      },
      onApply: async (closeDialog, state, extraFields) => {
        // set whether Duo MFA is enabled
        await setPolicy('App.Auth.MFA.Duo.Enabled', state, { noRefresh: true });

        // for not configured, reset the value
        if (state === null) {
          await setPolicy('App.Auth.MFA.Duo', null);
          closeDialog();
          return;
        }

        // for disabled, do nothing else
        if (state === false) {
          closeDialog();
          return;
        }

        // if there are no connections, reset the value
        const connections = extraFields?.connections;
        const isArrayOfObjects = (toCheck: unknown): toCheck is Record<string, unknown>[] => {
          return (
            !!toCheck &&
            Array.isArray(toCheck) &&
            toCheck.every((item) => typeof item === 'object' && item !== null && !Array.isArray(item))
          );
        };
        if (!isArrayOfObjects(connections) || connections.length === 0) {
          await showAlert(t('policies.App.Auth.MFA.Duo.errors.connectionsEmpty'));
          closeDialog(false);
          return;
        }

        // validate the connection fields
        let exitEarly = false;
        for await (const connection of connections) {
          const clientId = connection?.clientId;
          const clientSecret = connection?.clientSecret;
          const hostname = connection?.hostname;
          const domainsCsv = connection?.domains;
          if (typeof clientId !== 'string' || clientId === '') {
            await showAlert(t('policies.App.Auth.MFA.Duo.errors.clientIdEmpty'));
            exitEarly = true;
            break;
          }
          if (typeof clientSecret !== 'string' || clientSecret === '') {
            await showAlert(t('policies.App.Auth.MFA.Duo.errors.clientSecretEmpty'));
            exitEarly = true;
            break;
          }
          if (typeof hostname !== 'string' || hostname === '') {
            await showAlert(t('policies.App.Auth.MFA.Duo.errors.hostnameEmpty'));
            exitEarly = true;
            break;
          }
          if (hostname.includes('://') || !isUrl(`https://${hostname}`, { requireTopLevelDomain: true })) {
            await showAlert(t('policies.App.Auth.MFA.Duo.errors.hostnameInvalid'));
            exitEarly = true;
            break;
          }
          if (typeof domainsCsv !== 'string' || domainsCsv === '') {
            await showAlert(t('policies.App.Auth.MFA.Duo.errors.domainsEmpty'));
            exitEarly = true;
            break;
          }
          const domains = domainsCsv.split(',').map((d: string) => d.trim());
          if (domains.length === 0) {
            await showAlert(t('policies.App.Auth.MFA.Duo.errors.domainsEmpty'));
            exitEarly = true;
            break;
          }
        }
        if (exitEarly) {
          closeDialog(false);
          return;
        }

        // if there are no excluded usernames, reset the value
        const excludedUsernamesObjects = extraFields?.excludedUsernames;
        if (!isArrayOfObjects(excludedUsernamesObjects) || excludedUsernamesObjects.length === 0) {
          await setPolicy('App.Auth.MFA.Duo.Excluded', null);
          return;
        }
        const excludedUsernames = excludedUsernamesObjects.map((obj) => obj.username);
        if (excludedUsernames.length === 0) {
          await setPolicy('App.Auth.MFA.Duo.Excluded', null);
          return;
        }

        // validate the usernames
        for await (const excludedUsername of excludedUsernames) {
          if (typeof excludedUsername !== 'string' || excludedUsername === '') {
            await showAlert(t('policies.App.Auth.MFA.Duo.errors.usernameEmpty'));
            exitEarly = true;
            break;
          }
          if (!excludedUsername.includes('\\')) {
            await showAlert(t('policies.App.Auth.MFA.Duo.errors.usernameMissingDomain'));
            exitEarly = true;
            break;
          }
        }
        if (exitEarly) {
          closeDialog(false);
          return;
        }

        // set the policy value
        const policyValue = connections
          .map((connection) => {
            const clientId = connection.clientId;
            const clientSecret = connection.clientSecret;
            const hostname = connection.hostname;
            const domainsCsv = connection.domains;
            return `${clientId}:${clientSecret}@${hostname}@${domainsCsv}`;
          })
          .join(';');
        await setPolicy('App.Auth.MFA.Duo', policyValue, { noRefresh: true });
        await setPolicy('App.Auth.MFA.Duo.Excluded', excludedUsernames.join(','));
        closeDialog();
      },
      extraFields: [
        {
          key: 'connections',
          label: t('policies.App.Auth.MFA.Duo.fields.connections'),
          type: 'json',
          multiple: true,
          interpret: (value) => {
            const connections = value ? parseDuoMfaPolicyValue(value) : null;
            if (!connections || !Array.isArray(connections)) {
              return [];
            }
            return connections.map((connection) => ({
              clientId: connection.clientId,
              clientSecret: connection.clientSecret,
              hostname: connection.hostname,
              domains: connection.domains.join(', '),
            }));
          },
          jsonFields: {
            clientId: t('policies.App.Auth.MFA.Duo.fields.clientId'),
            clientSecret: t('policies.App.Auth.MFA.Duo.fields.clientSecret'),
            hostname: t('policies.App.Auth.MFA.Duo.fields.hostname'),
            domains: t('policies.App.Auth.MFA.Duo.fields.domains'),
          },
        },
        {
          key: 'excludedUsernames',
          label: t('policies.App.Auth.MFA.Duo.fields.excludedUsernames'),
          type: 'json',
          multiple: true,
          interpret: () => {
            const excludedUsernamesCsv = data.value?.['App.Auth.MFA.Duo.Excluded'];
            if (typeof excludedUsernamesCsv !== 'string' || excludedUsernamesCsv === '') {
              return [];
            }
            return excludedUsernamesCsv
              .split(',')
              .map((u: string) => u.trim())
              .map((u) => ({ username: u }));
          },
          jsonFields: {
            username: '',
          },
        },
      ],
    },
    {
      key: 'WorkspaceAuth.Block',
      appliesTo: ['Workspace'],
      onApply: async (closeDialog, state: boolean | null) => {
        await setPolicy('WorkspaceAuth.Block', state);
        closeDialog();
      },
    },
    {
      key: 'LogFiles.DiscardAgeDays',
      appliesTo: ['Server'],
      onApply: async (closeDialog, state, extraFields) => {
        if (state === null) {
          await setPolicy('LogFiles.DiscardAgeDays', null);
          closeDialog();
          return;
        }

        if (state === false) {
          await setPolicy('LogFiles.DiscardAgeDays', '0');
          closeDialog();
          return;
        }

        const days = extraFields?.days;
        if (
          typeof days !== 'string' ||
          days === '' ||
          isNaN(Number(days)) ||
          !Number.isInteger(Number(days)) ||
          Number(days) < 1
        ) {
          await showAlert(t('policies.LogFiles.DiscardAgeDays.errors.daysInvalid'));
          closeDialog(false);
          return;
        }

        await setPolicy('LogFiles.DiscardAgeDays', days);
        closeDialog();
      },
      extraFields: [
        {
          key: 'days',
          label: t('policies.LogFiles.DiscardAgeDays.fields.days'),
          type: 'string',
          interpret: (currentValue: string) => {
            if (
              currentValue === undefined ||
              currentValue === null ||
              currentValue === '' ||
              currentValue === '0'
            ) {
              return '';
            }
            return currentValue.toString();
          },
        },
      ],
      transformVisibleState: () => {
        if (!data.value) {
          return 'unset';
        }

        const policyValue = data.value['LogFiles.DiscardAgeDays'];
        if (policyValue === undefined || policyValue === null || policyValue === '') {
          return 'unset';
        }
        if (policyValue === '0') {
          return 'disabled';
        }
        return 'enabled';
      },
    },
  ];

  function parseDuoMfaPolicyValue(value?: string): {
    clientId: string;
    clientSecret: string;
    hostname: string;
    domains: string[];
  }[] {
    if (!value) {
      return [];
    }

    const connectionStrings = value.split(';').map((v) => v.trim());
    if (connectionStrings.length === 0) {
      return [];
    }

    const connections = connectionStrings
      .map((connectionString) => {
        const parts = connectionString.split('@');
        if (parts.length < 2 || parts.length > 3) {
          return null;
        }

        const credentialsPart = parts[0];
        const hostnamePart = parts[1];
        const domainsPart = parts[2];
        if (!credentialsPart || !hostnamePart) {
          return null;
        }

        const credentialsParts = credentialsPart.split(':');
        if (credentialsParts.length !== 2) {
          return null;
        }

        const clientId = credentialsParts[0];
        const clientSecret = credentialsParts[1];
        const hostname = hostnamePart;
        const domains = domainsPart ? domainsPart.split(',').map((d) => d.trim()) : ['*'];

        return {
          clientId,
          clientSecret,
          hostname,
          domains,
        };
      })
      .filter(notEmpty);

    return connections;
  }
</script>

<template>
  <div class="titlebar-row">
    <TextBlock variant="title">{{ t('policies.title') }}</TextBlock>
    <div class="header-actions" v-if="isSecureContext">
      <div class="actions">
        <ManagedResourceListDialog @app-or-desktop-change="props.refreshWorkspace" v-if="isSecureContext">
          <template #default="{ open }">
            <Button @click="open">{{ t('registryApps.manager.open') }}</Button>
          </template>
        </ManagedResourceListDialog>
      </div>
    </div>
  </div>

  <div class="wrapper">
    <div role="table" class="compact">
      <div role="rowgroup" class="thead">
        <div role="row">
          <span role="cell" style="width: 28px"></span>
          <span role="columnheader" class="rightPadding" style="flex-grow: 1">{{
            t('policies.table.setting')
          }}</span>
          <span role="columnheader" class="rightPadding" style="width: 140px; flex-shrink: 1">{{
            t('policies.table.state')
          }}</span>
        </div>
      </div>
      <div role="rowgroup" class="tbody">
        <PolicyDialog
          v-for="(policy, index) in policyEditorSpecs
            .sort((a, b) => {
              const titleA = t(`policies.${a.key}.title`);
              const titleB = t(`policies.${b.key}.title`);
              return titleA.localeCompare(titleB);
            })
            .map((p) => {
              return {
                ...p,
                state: (data?.[p.key] !== undefined
                  ? data[p.key] === 'false' || data[p.key] === ''
                    ? 'disabled'
                    : 'enabled'
                  : 'unset') as 'disabled' | 'enabled' | 'unset',
              };
            })
            .map((p) => {
              return {
                ...p,
                state: p.transformVisibleState ? p.transformVisibleState(p.state) : p.state,
              };
            })"
          :key="policy.key"
          :name="policy.key"
          :title="t(`policies.${policy.key}.title`)"
          :initialState="policy.state"
          :extraFields="policy.extraFields"
          :stringValue="data?.[policy.key]?.toString() || ''"
          :appliesTo="policy.appliesTo"
          @save="policy.onApply"
        >
          <template v-slot="{ openDialog }">
            <span
              role="row"
              @click.stop="() => openDialog()"
              @keydown.stop.enter.space="() => openDialog()"
              @keydown.up="() => moveFocusUp()"
              @keydown.down="() => moveFocusDown()"
              :tabindex="index === 0 ? '0' : '-1'"
              style="--focus-outline-offset: -2px"
            >
              <span role="cell" style="width: 28px">
                <svg width="16" height="16" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path
                    d="M7 12.25a.75.75 0 1 1 1.5 0 .75.75 0 0 1-1.5 0Zm.75 2.25a.75.75 0 1 0 0 1.5.75.75 0 0 0 0-1.5ZM7 18.25a.75.75 0 1 1 1.5 0 .75.75 0 0 1-1.5 0Zm3.75-6.75a.75.75 0 0 0 0 1.5h5.5a.75.75 0 0 0 0-1.5h-5.5ZM10 15.25a.75.75 0 0 1 .75-.75h5.5a.75.75 0 0 1 0 1.5h-5.5a.75.75 0 0 1-.75-.75Zm.75 2.25a.75.75 0 0 0 0 1.5h5.5a.75.75 0 0 0 0-1.5h-5.5Zm8.664-9.086-5.829-5.828a.493.493 0 0 0-.049-.04.626.626 0 0 1-.036-.03 2.072 2.072 0 0 0-.219-.18.652.652 0 0 0-.08-.044l-.048-.024-.05-.029c-.054-.031-.109-.063-.166-.087a1.977 1.977 0 0 0-.624-.138c-.02-.001-.04-.004-.059-.007A.605.605 0 0 0 12.172 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9.828a2 2 0 0 0-.586-1.414ZM18.5 20a.5.5 0 0 1-.5.5H6a.5.5 0 0 1-.5-.5V4a.5.5 0 0 1 .5-.5h6V8a2 2 0 0 0 2 2h4.5v10Zm-5-15.379L17.378 8.5H14a.5.5 0 0 1-.5-.5V4.621Z"
                    fill="currentColor"
                  />
                </svg>
              </span>
              <span role="cell" class="rightPadding" style="flex-grow: 1"
                >{{ t(`policies.${policy.key}.title`) }}
              </span>
              <span role="cell" class="rightPadding" style="width: 140px; flex-shrink: 0">{{
                (() => {
                  if (policy.state === 'enabled') return t('policies.state.enabled');
                  if (policy.state === 'disabled') return t('policies.state.disabled');
                  return t('policies.state.unset');
                })()
              }}</span>
            </span>
          </template>
        </PolicyDialog>
      </div>
    </div>
  </div>
</template>

<style scoped>
  .titlebar-row {
    user-select: none;
    margin-bottom: 16px;
  }

  .header-actions {
    margin: 12px 0 8px 0;
  }

  .header-actions,
  .actions {
    display: flex;
    flex-direction: row;
    gap: 8px;
  }

  div.wrapper {
    box-shadow: 0 0 0 1px var(--wui-divider-stroke-default);
    border-radius: var(--wui-control-corner-radius);
    width: 100%;
    height: calc(100% - 94px);
    overflow: auto;
  }

  div[role='table'] {
    width: auto;
    font-family: var(--wui-font-family-text);
    font-size: var(--wui-font-size-body);
    font-weight: 400;
    line-height: 20px;
  }

  /* row style */
  [role='row'] {
    display: flex;
    flex-direction: row;
    flex-wrap: nowrap;
    align-items: center;
    justify-content: flex-start;
    user-select: none;
  }
  span[role='row'] {
    text-decoration: none;
    color: inherit;
    cursor: default;
    transition: var(--wui-control-faster-duration) ease background;
  }
  span[role='row']:hover {
    background-color: var(--wui-control-fill-secondary);
  }
  span[role='row']:active {
    background-color: var(--wui-control-fill-tertiary);
    color: var(--wui-text-secondary);
  }

  /* row size */
  div[role='rowgroup'] [role='row'] {
    min-height: 40px;
    height: unset;
  }
  div[role='table'].compact div[role='rowgroup'] [role='row'] {
    min-height: 32px;
    /* height: 30px; */
  }

  /* header row */
  div[role='rowgroup'].thead div[role='row'] {
    border-bottom: 1px solid var(--wui-divider-stroke-default);
    min-height: 42px;
    height: 42px;
  }
  div[role='rowgroup'].thead {
    position: sticky;
    top: 0;
    background-color: var(--wui-solid-background-tertiary);
    z-index: 1;
  }
  div[role='table'].compact div[role='rowgroup'].thead div[role='row'] {
    min-height: 36px;
    height: 36px;
  }

  /* cell */
  span[role='columnheader'],
  span[role='cell'] {
    padding: 4px 0 4px 10px;
    box-sizing: border-box;
    height: 100%;
    display: flex;
    flex-direction: row;
    align-items: center;
  }

  span[role='cell'].rightPadding {
    padding-right: 10px;
  }
</style>
