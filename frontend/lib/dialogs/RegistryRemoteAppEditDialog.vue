<script setup lang="ts">
  import { Button, ContentDialog, InfoBar, TextBlock, TextBox, ToggleSwitch } from '$components';
  import { ResourceManagementSchemas } from '$utils';
  import { useQuery } from '@tanstack/vue-query';
  import { useTranslation } from 'i18next-vue';
  import { ref, watch } from 'vue';
  import z from 'zod';

  const { t } = useTranslation();

  const { registryKey } = defineProps<{
    registryKey: string;
    displayName?: string;
  }>();

  const { isPending, isFetching, isError, data, error, refetch, dataUpdatedAt } = useQuery({
    queryKey: ['remote-app-registry', registryKey],
    queryFn: async () => {
      return fetch(`/api/management/resources/registered/${registryKey}`)
        .then((res) => {
          if (!res.ok) {
            throw new Error(
              `Error fetching registered RemoteApp "${registryKey}": ${res.status} ${res.statusText}`
            );
          }
          return res.json();
        })
        .then((data) => {
          if (data === null) {
            throw new Error(`Registered RemoteApp with key "${registryKey}" not found.`);
          }
          return ResourceManagementSchemas.RegistryRemoteApp.App.parse(data);
        });
    },
    enabled: false, // do not fetch automatically
  });

  const emit = defineEmits<{
    (e: 'afterSave'): void;
    (e: 'onClose'): void;
  }>();

  // create a local copy of the data for editing
  const formData = ref<
    | (Omit<z.infer<typeof ResourceManagementSchemas.RegistryRemoteApp.App>, 'iconIndex'> & {
        iconIndex: string;
      })
    | null
  >(null);
  watch(
    [data, dataUpdatedAt],
    ([$data]) => {
      if ($data) {
        formData.value = JSON.parse(JSON.stringify($data));

        // convert iconIndex to string for TextBox
        if (formData.value) {
          formData.value.iconIndex = String($data.iconIndex);
        }
      }
    },
    { immediate: true }
  );

  /**
   * Determines which fields have been modified in the form data compared to the original data.
   */
  function getModifiedFields() {
    const updatedFields: Partial<z.infer<typeof ResourceManagementSchemas.RegistryRemoteApp.App>> = {};
    for (const key in formData.value) {
      if (key === 'iconIndex') {
        // special case: convert back to number
        if (Number(formData.value.iconIndex) !== data.value?.iconIndex) {
          updatedFields.iconIndex = Number(formData.value.iconIndex);
        }
        continue;
      }

      if (
        JSON.stringify(formData.value[key as keyof typeof formData.value]) !==
        JSON.stringify(data.value?.[key as keyof typeof data.value])
      ) {
        updatedFields[key as keyof typeof updatedFields] = formData.value[
          key as keyof typeof formData.value
        ] as any;
      }
    }
    return updatedFields;
  }

  const saving = ref(false);
  const saveError = ref<Error | null>(null);
  async function attemptSave(close: () => void) {
    if (!formData.value) {
      return;
    }

    saving.value = true;

    // determine which fields have changed
    const updatedFields = getModifiedFields();

    // nothing to update
    if (Object.keys(updatedFields).length === 0) {
      saving.value = false;
      close();
      return;
    }

    // send the updated fields to the server
    await fetch(`/api/management/resources/registered/${registryKey}`, {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(updatedFields),
    })
      .then(async (res) => {
        if (!res.ok) {
          const errorJson = await res.json().catch((e) => '(no json body)');
          if (
            errorJson &&
            typeof errorJson === 'object' &&
            ('Message' in errorJson || 'ExceptionMessage' in errorJson)
          ) {
            throw new Error(errorJson.ExceptionMessage || errorJson.Message);
          } else {
            throw new Error(
              `Error updating registered RemoteApp ${registryKey}: ${res.status} ${
                res.statusText
              } ${JSON.stringify(errorJson)}`
            );
          }
        }
        return res.json();
      })
      .then(() => {
        formData.value = null; // reset the working copy
        emit('afterSave');
        close();
      })
      .catch((err) => {
        if (err instanceof Error) {
          saveError.value = err;
        } else {
          saveError.value = new Error('An unknown error occurred while saving.');
        }
        console.error(err);
      })
      .finally(() => {
        saving.value = false;
      });
  }

  const windowConfirm = window.confirm.bind(window);
</script>

<template>
  <ContentDialog
    @open="() => refetch()"
    @close="
      (event) => {
        // ensure user wants to close if there are unsaved changes
        const modified = getModifiedFields();
        if (Object.keys(modified).length > 0) {
          const confirmClose = windowConfirm(t('closeDialogWithUnsavedChangesGuard'));
          if (!confirmClose) {
            event.preventDefault(); // cancel the close event if the user cancelled
            return;
          }
        }

        emit('onClose');
        formData = null; // discard the working copy
        saveError = null;
      }
    "
    @save-keyboard-shortcut="(close) => attemptSave(close)"
    :close-on-backdrop-click="false"
    :title="(displayName || data?.name) + ' ' + t('registryApps.manager.appProperties.title')"
    size="max"
    max-height="760px"
    fill-height
    :updating="isFetching"
    :loading="isPending"
    :error="isError && error !== null ? error : false"
  >
    <template #opener="{ close, open, popoverId }">
      <slot name="default" :close="close" :open="open" :popover-id="popoverId" />
    </template>

    <template #default="{ close }">
      <InfoBar
        v-if="saveError"
        severity="critical"
        :title="t('registryApps.manager.appProperties.saveError')"
        style="margin-bottom: 12px; position: sticky; top: var(--title-height); z-index: 99"
      >
        <TextBlock>{{ saveError.message }}</TextBlock>
      </InfoBar>

      <form
        v-if="formData"
        @keydown="
          (event) => {
            if (event.key === 'Enter' && !event.shiftKey) {
              event.preventDefault();
              attemptSave(close);
            }
          }
        "
      >
        <fieldset>
          <legend>
            <TextBlock block variant="bodyLarge">{{
              t('registryApps.manager.appProperties.sections.application')
            }}</TextBlock>
          </legend>
          <label class="input">
            <TextBlock>{{ t('registryApps.properties.displayName') }}</TextBlock>
            <TextBox v-model:value="formData.name"></TextBox>
          </label>
          <label class="input">
            <TextBlock>{{ t('registryApps.properties.appPath') }}</TextBlock>
            <TextBox v-model:value="formData.path"></TextBox>
          </label>
          <label class="input">
            <TextBlock>{{ t('registryApps.properties.cmdLineArgs') }}</TextBlock>
            <TextBox v-model:value="formData.commandLine"></TextBox>
          </label>
        </fieldset>

        <fieldset>
          <legend>
            <TextBlock block variant="bodyLarge">{{
              t('registryApps.manager.appProperties.sections.icon')
            }}</TextBlock>
          </legend>
          <label class="input">
            <TextBlock>{{ t('registryApps.properties.iconPath') }}</TextBlock>
            <TextBox v-model:value="formData.iconPath"></TextBox>
          </label>
          <label class="input">
            <TextBlock>{{ t('registryApps.properties.iconIndex') }}</TextBlock>
            <div class="split">
              <TextBox v-model:value="formData.iconIndex"></TextBox>
              <Button disabled type="button">
                <template #icon>
                  <svg viewBox="0 0 24 24">
                    <path
                      d="M21 6.25A3.25 3.25 0 0 0 17.75 3H6.25A3.25 3.25 0 0 0 3 6.25v4.507a5.495 5.495 0 0 1 1.5-.882V6.25c0-.966.784-1.75 1.75-1.75h11.5c.966 0 1.75.784 1.75 1.75v11.5c0 .209-.037.409-.104.595l-5.822-5.702-.128-.116a2.251 2.251 0 0 0-2.243-.38c.259.425.461.889.598 1.38a.75.75 0 0 1 .724.188L18.33 19.4a1.746 1.746 0 0 1-.581.099h-4.775l.512.513c.278.277.443.626.495.987h3.768A3.25 3.25 0 0 0 21 17.75V6.25Z"
                      fill="currentColor"
                    />
                    <path
                      d="M17.504 8.752a2.252 2.252 0 1 0-4.504 0 2.252 2.252 0 0 0 4.504 0Zm-3.004 0a.752.752 0 1 1 1.504 0 .752.752 0 0 1-1.504 0ZM9.95 17.89a4.5 4.5 0 1 0-1.145.976l2.915 2.914a.75.75 0 1 0 1.06-1.06l-2.83-2.83ZM6.5 18a3 3 0 1 1 0-6 3 3 0 0 1 0 6Z"
                      fill="currentColor"
                    />
                  </svg>
                </template>
                {{ t('registryApps.manager.appProperties.selectIcon') }}
              </Button>
            </div>
          </label>
        </fieldset>
        <fieldset>
          <legend>
            <TextBlock block variant="bodyLarge">{{
              t('registryApps.manager.appProperties.sections.advanced')
            }}</TextBlock>
          </legend>
          <label class="input">
            <TextBlock block>{{ t('registryApps.properties.includeInWorkspace') }}</TextBlock>
            <ToggleSwitch v-model="formData.includeInWorkspace">
              {{ formData.includeInWorkspace ? t('policies.state.enabled') : t('policies.state.disabled') }}
            </ToggleSwitch>
          </label>
          <label class="input">
            <TextBlock block>{{ t('registryApps.properties.fileTypeAssociations') }}</TextBlock>
            <div>
              <Button disabled type="button">
                <template #icon>
                  <svg viewBox="0 0 24 24">
                    <path
                      d="M12 14a2 2 0 1 0 0-4 2 2 0 0 0 0 4ZM6 12a5.999 5.999 0 1 1 11.986.368l-2.66 2.66a4.499 4.499 0 1 0-.301.301l-2.535 2.535c-.04.04-.08.082-.118.124A5.999 5.999 0 0 1 6.001 12Zm5.998-8.5a8.501 8.501 0 0 1 8.443 7.512 3.293 3.293 0 0 1 1.529.237C21.587 6.077 17.269 2 11.999 2 6.477 2 2 6.477 2 12c0 5.186 3.947 9.45 9 9.951-.002-.177.018-.36.065-.545l.233-.934A8.501 8.501 0 0 1 12 3.5Zm7.1 9.17-5.9 5.9a2.685 2.685 0 0 0-.707 1.248l-.457 1.83a1.087 1.087 0 0 0 1.317 1.319l1.83-.458a2.684 2.684 0 0 0 1.248-.706l5.9-5.902a2.285 2.285 0 0 0-3.23-3.231Z"
                      fill="currentColor"
                    />
                  </svg>
                </template>
                {{ t('registryApps.manager.appProperties.configureFileTypeAssociations') }}
              </Button>
            </div>
          </label>
          <label class="input">
            <TextBlock block>{{ t('registryApps.properties.security') }}</TextBlock>
            <div>
              <Button disabled type="button">
                <template #icon>
                  <svg viewBox="0 0 24 24">
                    <path
                      d="M12 1.999c5.487 0 9.942 4.419 10 9.892a6.064 6.064 0 0 1-1.525-.566 8.486 8.486 0 0 0-.21-1.326h-1.942a1.618 1.618 0 0 0-.648 0h-.769c.012.123.023.246.032.37a7.858 7.858 0 0 1-1.448.974c-.016-.46-.047-.908-.094-1.343H8.604a18.968 18.968 0 0 0 .135 5H12v1.5H9.06c.653 2.414 1.786 4.002 2.94 4.002.276 0 .551-.091.819-.263a6.938 6.938 0 0 0 1.197 1.56c-.652.133-1.326.203-2.016.203-5.524 0-10.002-4.478-10.002-10.001C1.998 6.477 6.476 1.998 12 1.998ZM7.508 16.501H4.785a8.532 8.532 0 0 0 4.095 3.41c-.523-.82-.954-1.846-1.27-3.015l-.102-.395ZM7.093 10H3.735l-.004.017a8.524 8.524 0 0 0-.233 1.984c0 1.056.193 2.067.545 3h3.173a20.301 20.301 0 0 1-.218-3c0-.684.033-1.354.095-2.001Zm1.788-5.91-.023.008A8.531 8.531 0 0 0 4.25 8.5h3.048c.313-1.752.86-3.278 1.583-4.41ZM12 3.499l-.116.005C10.618 3.62 9.396 5.622 8.828 8.5h6.343c-.566-2.87-1.784-4.869-3.045-4.995L12 3.5Zm3.12.59.106.175c.67 1.112 1.178 2.572 1.475 4.237h3.048a8.533 8.533 0 0 0-4.338-4.29l-.291-.121Zm7.38 8.888c-1.907-.172-3.434-1.287-4.115-1.87a.601.601 0 0 0-.77 0c-.682.583-2.21 1.698-4.116 1.87a.538.538 0 0 0-.5.523V17c0 4.223 4.094 5.716 4.873 5.962a.42.42 0 0 0 .255 0c.78-.246 4.872-1.74 4.872-5.962v-3.5a.538.538 0 0 0-.5-.523Z"
                      fill="currentColor"
                    />
                  </svg>
                </template>
                {{ t('registryApps.manager.appProperties.managePermissions') }}
              </Button>
            </div>
          </label>
          <label class="input">
            <TextBlock>{{ t('registryApps.properties.key') }}</TextBlock>
            <TextBox v-model:value="formData.key"></TextBox>
          </label>
        </fieldset>
      </form>
    </template>

    <template #footer="{ close }">
      <Button @click="attemptSave(close)" :loading="saving">OK</Button>
      <Button @click="close">Cancel</Button>
    </template>
  </ContentDialog>
</template>

<style scoped>
  fieldset > label {
    display: flex;
    flex-direction: column;
    justify-content: flex-start;
    gap: 3px;
    margin-bottom: 10px;
  }

  label > div.split {
    display: flex;
    flex-direction: row;
    gap: 8px;
  }
  label > div.split > button {
    flex-shrink: 0;
  }

  fieldset {
    border-color: var(--wui-surface-stroke-default);
    border-width: 1px;
    border-radius: var(--wui-control-corner-radius);
    padding: 12px;
    margin-bottom: 16px;
  }
</style>
