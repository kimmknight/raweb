<script setup lang="ts">
  import {
    Button,
    ContentDialog,
    Field,
    FieldSet,
    InfoBar,
    TextBlock,
    TextBox,
    ToggleSwitch,
  } from '$components';
  import {
    EditFileTypeAssociationsDialog,
    PickIconIndexDialog,
    RdpFilePropertiesDialog,
    RegistryRemoteAppSecurityDialog,
    showConfirm,
  } from '$dialogs';
  import { useCoreDataStore } from '$stores';
  import { generateRdpFileContents, normalizeRdpFileString, ResourceManagementSchemas } from '$utils';
  import { CommandLineMode } from '$utils/schemas/ResourceManagementSchemas';
  import { unproxify } from '$utils/unproxify';
  import { useTranslation } from 'i18next-vue';
  import { computed, ref, useTemplateRef } from 'vue';
  import z from 'zod';

  const { iisBase } = useCoreDataStore();
  const { t } = useTranslation();

  const mountDate = Date.now();
  const registryKey = defineModel<string>('registryKey');
  const name = defineModel<string>('name');
  const path = defineModel<string>('path');
  const vPath = defineModel<string>('vPath');
  const iconPath = defineModel<string>('iconPath');
  const iconIndex = defineModel<string>('iconIndex');
  const commandLine = defineModel<string>('commandLine');
  const commandLineOption = defineModel<CommandLineMode>('commandLineOption');
  const includeInWorkspace = defineModel<boolean>('includeInWorkspace');
  const rdpFileString = defineModel<string>('rdpFileString');
  const fileTypeAssociations =
    defineModel<z.infer<typeof ResourceManagementSchemas.RegistryRemoteApp.FileTypeAssociation>[]>(
      'fileTypeAssociations'
    );
  const securityDescription =
    defineModel<z.infer<typeof ResourceManagementSchemas.RegistryRemoteApp.App>['securityDescription']>(
      'securityDescription'
    );

  const emit = defineEmits<{
    (e: 'afterSave'): void;
    (e: 'onClose'): void;
  }>();

  const saving = ref(false);
  const saveError = ref<Error | null>(null);
  async function attemptSave(close: () => void) {
    saving.value = true;

    const dataToSend = ResourceManagementSchemas.RegistryRemoteApp.App.safeParse({
      key: registryKey.value,
      name: name.value,
      path: path.value,
      vPath: path.value || vPath.value || '',
      iconPath: iconPath.value || '',
      iconIndex: parseInt(iconIndex.value || '0'),
      commandLine: commandLine.value || '',
      commandLineOption: commandLineOption.value || CommandLineMode.Optional,
      includeInWorkspace: includeInWorkspace.value || true,
      fileTypeAssociations: fileTypeAssociations.value || [],
      securityDescription: securityDescription.value,
      rdpFileString: rdpFileString.value ? normalizeRdpFileString(rdpFileString.value) : undefined,
    });

    if (!dataToSend.success) {
      saveError.value = new Error(
        'Something is wrong with the data you are trying to save. Please review your changes and try again. For more details, see the browser console.'
      );
      saving.value = false;
      return;
    }

    // send the updated fields to the server
    await fetch(`${iisBase}api/management/resources/registered`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(dataToSend.data),
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
      })
      .then(() => {
        emit('afterSave');
        discardChanges();
        close();
      })
      .catch((err) => {
        if (err instanceof Error) {
          if (err.message.includes('409 Conflict')) {
            saveError.value = new Error(t('registryApps.manager.create.409'));
            return;
          }

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

  // track whether any fields have been modified
  const currentValuesObj = computed(() => {
    return {
      key: registryKey.value,
      name: name.value,
      path: path.value,
      vPath: path.value || vPath.value || '',
      iconPath: iconPath.value || '',
      iconIndex: parseInt(iconIndex.value || '0'),
      commandLine: commandLine.value || '',
      commandLineOption: commandLineOption.value || CommandLineMode.Optional,
      includeInWorkspace: includeInWorkspace.value || true,
      fileTypeAssociations: fileTypeAssociations.value || [],
      securityDescription: securityDescription.value || undefined,
    };
  });
  const initialValues = ref<typeof currentValuesObj.value>();
  const isModified = computed(() => {
    return JSON.stringify(initialValues.value) !== JSON.stringify(currentValuesObj.value);
  });
  function discardChanges() {
    if (initialValues.value) {
      const init = initialValues.value;
      registryKey.value = init.key;
      name.value = init.name;
      path.value = init.path;
      vPath.value = init.vPath;
      iconPath.value = init.iconPath;
      iconIndex.value = init.iconIndex.toString();
      commandLine.value = init.commandLine;
      commandLineOption.value = init.commandLineOption;
      includeInWorkspace.value = init.includeInWorkspace;
      fileTypeAssociations.value = init.fileTypeAssociations;
      securityDescription.value = init.securityDescription;
    }
  }

  const contentDialog = useTemplateRef<InstanceType<typeof ContentDialog> | null>('contentDialog');
  defineExpose({
    open: () => {
      unproxify(contentDialog.value)?.open();
    },
  });

  const hasRequiredFields = computed(() => {
    return (
      name.value &&
      name.value.trim().length > 0 &&
      path.value &&
      path.value.trim().length > 0 &&
      registryKey.value &&
      registryKey.value.trim().length > 0
    );
  });
</script>

<template>
  <ContentDialog
    ref="contentDialog"
    @after-open="
      () => {
        initialValues = JSON.parse(JSON.stringify(currentValuesObj));
      }
    "
    @close="
      (event) => {
        // ensure user wants to close if there are unsaved changes
        if (isModified) {
          event.preventDefault();

          showConfirm(
            t('closeDialogWithUnsavedChangesGuard.title'),
            t('closeDialogWithUnsavedChangesGuard.message'),
            'Yes',
            'No'
          )
            .then((closeConfirmDialog) => {
              // user accepted closing
              event.detail.close();
              closeConfirmDialog();
              emit('onClose');
              saveError = null;
            })
            .catch(() => {}); // user cancelled closing
        }
      }
    "
    @save-keyboard-shortcut="(close) => attemptSave(close)"
    :close-on-backdrop-click="false"
    :title="t('registryApps.manager.create.title')"
    size="max"
    max-height="760px"
    fill-height
  >
    <template #opener="{ close, open, popoverId }">
      <slot name="default" :close="close" :open="open" :popover-id="popoverId" />
    </template>

    <template #default="{ close, popoverId }">
      <InfoBar
        v-if="saveError"
        severity="critical"
        :title="t('registryApps.manager.appProperties.saveError')"
        style="margin-bottom: 12px; position: sticky; top: var(--title-height); z-index: 99"
      >
        <TextBlock>{{ saveError.message }}</TextBlock>
      </InfoBar>

      <div
        @keydown="
          (event) => {
            if (!event.target) {
              return;
            }

            const closestDialog = (event.target as HTMLElement)?.closest?.('dialog');
            if (!closestDialog) {
              return;
            }

            if (event.key === 'Enter' && !event.shiftKey && closestDialog.id === popoverId) {
              // if it is a button or link, we do not want to trigger save
              // because the button/link likely is for opening a child dialog
              const isButtonOrLink =
                (event.target as HTMLElement).tagName === 'BUTTON' ||
                (event.target as HTMLElement).tagName === 'A';
              if (isButtonOrLink) {
                return;
              }

              event.preventDefault();
              attemptSave(close);
            }
          }
        "
      >
        <FieldSet>
          <template #legend>
            <TextBlock block variant="bodyLarge">{{
              t('registryApps.manager.appProperties.sections.application')
            }}</TextBlock>
          </template>
          <Field>
            <TextBlock>{{ t('registryApps.properties.displayName') }}</TextBlock>
            <TextBox v-model:value="name"></TextBox>
          </Field>
          <Field>
            <TextBlock>{{ t('registryApps.properties.appPath') }}</TextBlock>
            <TextBox v-model:value="path"></TextBox>
          </Field>
          <Field>
            <TextBlock>{{ t('registryApps.properties.cmdLineArgs') }}</TextBlock>
            <TextBox v-model:value="commandLine"></TextBox>
          </Field>
        </FieldSet>

        <FieldSet>
          <template #legend>
            <TextBlock block variant="bodyLarge">{{
              t('registryApps.manager.appProperties.sections.icon')
            }}</TextBlock>
          </template>
          <Field>
            <TextBlock>{{ t('registryApps.properties.iconPath') }}</TextBlock>
            <div class="split">
              <TextBox v-model:value="iconPath"></TextBox>
              <img
                :src="`${iisBase}api/management/resources/icon?path=${encodeURIComponent(
                  iconPath ?? ''
                )}&index=${iconIndex || -1}&__cacheBust=${mountDate}`"
                alt=""
                width="24"
                height="24"
              />
            </div>
          </Field>
          <Field>
            <TextBlock>{{ t('registryApps.properties.iconIndex') }}</TextBlock>
            <div class="split">
              <TextBox v-model:value="iconIndex"></TextBox>
              <PickIconIndexDialog
                :icon-path="iconPath ?? ''"
                :current-index="parseInt(iconIndex ?? '0')"
                @index-selected="
                  (newIndex, newPath) => {
                    iconIndex = newIndex.toString();
                    if (newPath && iconPath !== newPath) {
                      iconPath = newPath;
                    }
                  }
                "
                #default="{ open }"
              >
                <Button :disabled="!iconPath" type="button" @click="open">
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
              </PickIconIndexDialog>
            </div>
          </Field>
        </FieldSet>
        <FieldSet>
          <template #legend>
            <TextBlock block variant="bodyLarge">{{
              t('registryApps.manager.appProperties.sections.advanced')
            }}</TextBlock>
          </template>
          <Field>
            <TextBlock block>{{ t('registryApps.properties.includeInWorkspace') }}</TextBlock>
            <ToggleSwitch v-model="includeInWorkspace">
              {{ includeInWorkspace ? t('policies.state.enabled') : t('policies.state.disabled') }}
            </ToggleSwitch>
          </Field>
          <Field no-label-focus>
            <TextBlock block>{{ t('registryApps.properties.fileTypeAssociations') }}</TextBlock>
            <div>
              <EditFileTypeAssociationsDialog
                #default="{ open }"
                v-model="fileTypeAssociations"
                :fallback-icon-path="iconPath"
                :fallback-icon-index="parseInt(iconIndex || '0')"
              >
                <Button @click="open">
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
              </EditFileTypeAssociationsDialog>
            </div>
          </Field>
          <Field no-label-focus>
            <TextBlock block>{{ t('registryApps.properties.userAssignment') }}</TextBlock>
            <div>
              <RegistryRemoteAppSecurityDialog
                #default="{ open }"
                :app-name="name"
                v-model="securityDescription"
              >
                <Button @click="open">
                  <template #icon>
                    <svg viewBox="0 0 24 24">
                      <path
                        d="M12 1.999c5.487 0 9.942 4.419 10 9.892a6.064 6.064 0 0 1-1.525-.566 8.486 8.486 0 0 0-.21-1.326h-1.942a1.618 1.618 0 0 0-.648 0h-.769c.012.123.023.246.032.37a7.858 7.858 0 0 1-1.448.974c-.016-.46-.047-.908-.094-1.343H8.604a18.968 18.968 0 0 0 .135 5H12v1.5H9.06c.653 2.414 1.786 4.002 2.94 4.002.276 0 .551-.091.819-.263a6.938 6.938 0 0 0 1.197 1.56c-.652.133-1.326.203-2.016.203-5.524 0-10.002-4.478-10.002-10.001C1.998 6.477 6.476 1.998 12 1.998ZM7.508 16.501H4.785a8.532 8.532 0 0 0 4.095 3.41c-.523-.82-.954-1.846-1.27-3.015l-.102-.395ZM7.093 10H3.735l-.004.017a8.524 8.524 0 0 0-.233 1.984c0 1.056.193 2.067.545 3h3.173a20.301 20.301 0 0 1-.218-3c0-.684.033-1.354.095-2.001Zm1.788-5.91-.023.008A8.531 8.531 0 0 0 4.25 8.5h3.048c.313-1.752.86-3.278 1.583-4.41ZM12 3.499l-.116.005C10.618 3.62 9.396 5.622 8.828 8.5h6.343c-.566-2.87-1.784-4.869-3.045-4.995L12 3.5Zm3.12.59.106.175c.67 1.112 1.178 2.572 1.475 4.237h3.048a8.533 8.533 0 0 0-4.338-4.29l-.291-.121Zm7.38 8.888c-1.907-.172-3.434-1.287-4.115-1.87a.601.601 0 0 0-.77 0c-.682.583-2.21 1.698-4.116 1.87a.538.538 0 0 0-.5.523V17c0 4.223 4.094 5.716 4.873 5.962a.42.42 0 0 0 .255 0c.78-.246 4.872-1.74 4.872-5.962v-3.5a.538.538 0 0 0-.5-.523Z"
                        fill="currentColor"
                      />
                    </svg>
                  </template>
                  {{ t('registryApps.manager.appProperties.manageUserAssignment') }}
                </Button>
              </RegistryRemoteAppSecurityDialog>
            </div>
          </Field>
          <Field>
            <TextBlock>{{ t('registryApps.properties.key') }}</TextBlock>
            <TextBox v-model:value="registryKey"></TextBox>
          </Field>
          <Field no-label-focus>
            <TextBlock block>{{ t('registryApps.properties.customizeRdpFile') }}</TextBlock>
            <div>
              <RdpFilePropertiesDialog
                #default="{ open }"
                :name
                :model-value="`${rdpFileString}
                remoteapplicationcmdline:s:${commandLine || ''}
                remoteapplicationfileextensions:s:${(fileTypeAssociations || [])
                  .map((fta) => fta.extension)
                  .join(';')}
                remoteapplicationname:s:${name || ''}
                remoteapplicationprogram:s:||${registryKey || ''}
                `"
                @update:model-value="
                  (newValue) => {
                    rdpFileString = generateRdpFileContents(newValue);
                  }
                "
                :disabled-fields="[
                  'full address:s',
                  'remoteapplicationcmdline:s',
                  'remoteapplicationfileextensions:s',
                  'remoteapplicationmode:i',
                  'remoteapplicationname:s',
                  'remoteapplicationprogram:s',
                  'workspace id:s',
                ]"
                mode="create"
                default-group="connection"
              >
                <Button @click="open">
                  <template #icon>
                    <svg viewBox="0 0 24 24">
                      <path
                        d="M21.03 2.97a3.578 3.578 0 0 1 0 5.06L9.062 20a2.25 2.25 0 0 1-.999.58l-5.116 1.395a.75.75 0 0 1-.92-.921l1.395-5.116a2.25 2.25 0 0 1 .58-.999L15.97 2.97a3.578 3.578 0 0 1 5.06 0ZM15 6.06 5.062 16a.75.75 0 0 0-.193.333l-1.05 3.85 3.85-1.05A.75.75 0 0 0 8 18.938L17.94 9 15 6.06Zm2.03-2.03-.97.97L19 7.94l.97-.97a2.079 2.079 0 0 0-2.94-2.94Z"
                        fill="currentColor"
                      />
                    </svg>
                  </template>
                  {{ t('registryApps.manager.appProperties.customizeRdpFile') }}
                </Button>
              </RdpFilePropertiesDialog>
            </div>
          </Field>
        </FieldSet>
      </div>
    </template>

    <template #footer="{ close }">
      <Button @click="attemptSave(close)" :loading="saving" :disabled="!hasRequiredFields">OK</Button>
      <Button @click="close">Cancel</Button>
    </template>
  </ContentDialog>
</template>
