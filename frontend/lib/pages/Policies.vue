<script setup lang="ts">
  import { PolicyDialog, TextBlock } from '$components';
  import { useCoreDataStore } from '$stores';
  import { useTranslation } from 'i18next-vue';
  import { onMounted, ref } from 'vue';

  const { iisBase } = useCoreDataStore();
  const { t } = useTranslation();

  const data = ref<Record<string, unknown> | null>({});
  const error = ref(null);
  const loading = ref(false);
  async function fetchPolicies() {
    loading.value = true;
    return fetch(iisBase + 'policies.asmx/GetAppSettings')
      .then((response) => {
        if (!response.ok) {
          throw new Error('Network response was not ok');
        }
        return response.text();
      })
      .then((text) => {
        const parser = new DOMParser();
        const xmlDoc = parser.parseFromString(text, 'text/xml');
        return xmlDoc.documentElement.firstChild?.textContent ?? null;
      })
      .then((serialized) => {
        const json = JSON.parse(serialized || '{}');
        return json;
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

  async function setPolicy(key: string, value: string | boolean | null) {
    loading.value = true;
    return fetch(iisBase + 'policies.asmx/SetAppSetting', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
      },
      body: new URLSearchParams({
        key: key,
        value: value === null ? 'nil' : value.toString(),
      }),
    })
      .then(() => {
        return fetchPolicies();
      })
      .catch((err) => {
        alert(`Error setting policy: ${err.message}`);
      })
      .finally(() => {
        loading.value = false;
      });
  }

  const policyEditorSpecs = [
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
      key: 'RegistryApps.Enabled',
      appliesTo: ['Web client', 'Workspace'],
      onApply: async (closeDialog, state: boolean | null) => {
        await setPolicy('RegistryApps.Enabled', state);
        closeDialog();
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
  ] satisfies Array<{
    key: InstanceType<typeof PolicyDialog>['$props']['name'];
    appliesTo: InstanceType<typeof PolicyDialog>['$props']['appliesTo'];
    extraFields?: InstanceType<typeof PolicyDialog>['$props']['extraFields'];
    onApply: InstanceType<typeof PolicyDialog>['$props']['onSave'];
  }>;
</script>

<template>
  <div class="titlebar-row">
    <TextBlock variant="title">{{ $t('policies.title') }}</TextBlock>
  </div>

  <div class="wrapper">
    <div role="table" class="compact">
      <div role="rowgroup" class="thead">
        <div role="row">
          <span role="cell" style="width: 28px"></span>
          <span role="columnheader" class="rightPadding" style="flex-grow: 1">{{
            $t('policies.table.setting')
          }}</span>
          <span role="columnheader" class="rightPadding" style="width: 140px; flex-shrink: 1">{{
            $t('policies.table.state')
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
                state:
                  (data?.[p.key] !== undefined
                    ? data[p.key] === 'false' || data[p.key] === ''
                      ? 'disabled'
                      : 'enabled'
                    : 'unset') as 'disabled' | 'enabled' | 'unset',
              };
            })"
          :key="policy.key"
          :name="policy.key"
          :title="$t(`policies.${policy.key}.title`)"
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
                >{{ $t(`policies.${policy.key}.title`) }}
              </span>
              <span role="cell" class="rightPadding" style="width: 140px; flex-shrink: 0">{{
                data?.[policy.key] !== undefined
                  ? data[policy.key] === 'false' || data[policy.key] === ''
                    ? $t('policies.state.disabled')
                    : $t('policies.state.enabled')
                  : $t('policies.state.unset')
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

  div.wrapper {
    box-shadow: 0 0 0 1px var(--wui-divider-stroke-default);
    border-radius: var(--wui-control-corner-radius);
    width: 100%;
    height: calc(100% - 52px);
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
