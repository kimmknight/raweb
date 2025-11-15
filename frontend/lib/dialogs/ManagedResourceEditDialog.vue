<script setup lang="ts">
  import {
    Button,
    ContentDialog,
    Field,
    FieldSet,
    IconButton,
    InfoBar,
    TextBlock,
    TextBox,
    ToggleSwitch,
  } from '$components';
  import {
    EditFileTypeAssociationsDialog,
    ManagedResourceSecurityDialog,
    PickIconIndexDialog,
    RdpFilePropertiesDialog,
    showConfirm,
  } from '$dialogs';
  import { useCoreDataStore } from '$stores';
  import {
    buildManagedIconPath,
    generateRdpFileContents,
    normalizeRdpFileString,
    pickImageFile,
    ResourceManagementSchemas,
    useObjectUrl,
  } from '$utils';
  import { ManagedResourceSource } from '$utils/schemas/ResourceManagementSchemas';
  import { useQuery } from '@tanstack/vue-query';
  import { useTranslation } from 'i18next-vue';
  import { computed, ref, watch } from 'vue';
  import z from 'zod';

  const { iisBase, capabilities } = useCoreDataStore();
  const { t } = useTranslation();

  const { identifier, displayName } = defineProps<{
    identifier: string;
    displayName?: string;
  }>();

  const { isPending, isFetching, isError, data, error, refetch, dataUpdatedAt } = useQuery({
    queryKey: ['remote-app-registry', identifier],
    queryFn: async () => {
      return fetch(`${iisBase}api/management/resources/registered/${identifier}`)
        .then(async (res) => {
          if (!res.ok) {
            await res.json().then((err) => {
              if (err && 'ExceptionMessage' in err) {
                throw new Error(err.ExceptionMessage);
              }
            });
            throw new Error(
              `Error fetching registered RemoteApp "${identifier}": ${res.status} ${res.statusText}`
            );
          }
          return res.json();
        })
        .then((data) => {
          if (data === null) {
            throw new Error(`Registered RemoteApp with key "${identifier}" not found.`);
          }
          return ResourceManagementSchemas.RegistryRemoteApp.App.parse(data);
        });
    },
    enabled: false, // do not fetch automatically
  });

  const emit = defineEmits<{
    (e: 'afterSave'): void;
    (e: 'afterDelete'): void;
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

  const isRemoteApp = computed(() => {
    return !!data.value?.remoteAppProperties;
  });

  const isManagedFileResource = computed(() => {
    return data.value?.source === ManagedResourceSource.File;
  });

  const externalAddress = computed(() => {
    if (!isManagedFileResource || !data.value?.rdpFileString) {
      return null;
    }

    const address = data.value.rdpFileString.match(/full address:s:(.+)/)?.[1];
    if (!address) {
      return null;
    }

    const addressContainsPort = address?.includes(':');
    if (addressContainsPort) {
      return address;
    }

    const port = data.value.rdpFileString.match(/server port:i:(\d+)/)?.[1];
    if (port) {
      return `${address}:${port}`;
    }
    return address;
  });

  /**
   * Determines which fields have been modified in the form data compared to the original data.
   */
  async function getModifiedFields() {
    const updatedFields: Partial<
      z.infer<typeof ResourceManagementSchemas.RegistryRemoteApp.App> & {
        managedIconLightBase64?: string;
        managedIconDarkBase64?: string;
      }
    > = {};

    for (const key in formData.value) {
      // special case: convert back to number
      if (key === 'iconIndex') {
        if (Number(formData.value.iconIndex) !== data.value?.iconIndex) {
          updatedFields.iconIndex = Number(formData.value.iconIndex);
        }
        continue;
      }

      // special case: strip out empty values and normalize order
      if (key === 'rdpFileString') {
        const original = normalizeRdpFileString(data.value?.rdpFileString);
        const modified = normalizeRdpFileString(formData.value.rdpFileString);
        if (original !== modified) {
          updatedFields.rdpFileString = modified;
        }
        continue;
      }

      // ignore hasLightIcon and hasDarkIcon; they are read-only indicators from the API
      if (key === 'hasLightIcon' || key === 'hasDarkIcon') {
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

    // check for uploaded icons
    if (uploadedLightIconBlob.value) {
      const pngBase64 = await uploadedLightIconBlob.value
        .arrayBuffer()
        .then((buffer) => new Uint8Array(buffer))
        .then((bytes) => bytes.toBase64?.());

      // '' indicates reset to default, but the reset is unnecessary if there is no existing icon
      const noChangeRequired = pngBase64 === '' && !data.value?.hasLightIcon;

      if (!noChangeRequired) {
        updatedFields.managedIconLightBase64 = pngBase64;
      }
    }
    if (uploadedDarkIconBlob.value) {
      const pngBase64 = await uploadedDarkIconBlob.value
        .arrayBuffer()
        .then((buffer) => new Uint8Array(buffer))
        .then((bytes) => bytes.toBase64?.());

      // '' indicates reset to default, but the reset is unnecessary if there is no existing icon
      const noChangeRequired = pngBase64 === '' && !data.value?.hasDarkIcon;

      if (!noChangeRequired) {
        updatedFields.managedIconDarkBase64 = pngBase64;
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
    const updatedFields = await getModifiedFields();

    // nothing to update
    if (Object.keys(updatedFields).length === 0) {
      saving.value = false;
      close();
      return;
    }

    // send the updated fields to the server
    await fetch(`${iisBase}api/management/resources/registered/${identifier}`, {
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
              `Error updating registered RemoteApp ${identifier}: ${res.status} ${
                res.statusText
              } ${JSON.stringify(errorJson)}`
            );
          }
        }
        return res.json();
      })
      .then(() => {
        // reset the working copy
        formData.value = null;
        saveError.value = null;
        uploadedLightIconBlob.value = null;
        uploadedDarkIconBlob.value = null;

        // emit save event and close dialog
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

  function attemptDelete(close: () => void) {
    showConfirm(
      t('registryApps.manager.remove.title', {
        app_name: (displayName || data.value?.name || identifier) + (isManagedFileResource ? 'ᵠ' : ''),
      }),
      t('registryApps.manager.remove.message'),
      'Yes',
      'No'
    ).then(async (done) => {
      fetch(`${iisBase}api/management/resources/registered/${identifier}`, {
        method: 'DELETE',
      }).then(async (res) => {
        if (res.ok) {
          emit('afterDelete');
          close();
          return done();
        }

        const errorJson = await res.json().catch((e) => '(no json body)');
        if (
          errorJson &&
          typeof errorJson === 'object' &&
          ('Message' in errorJson || 'ExceptionMessage' in errorJson)
        ) {
          done(new Error(errorJson.ExceptionMessage || errorJson.Message));
        } else {
          done(
            new Error(
              `Error deleting registered RemoteApp ${identifier}: ${res.status} ${
                res.statusText
              } ${JSON.stringify(errorJson)}`
            )
          );
        }
      });
    });
  }

  function iconPath(theme: 'light' | 'dark', useDefault: true): string;
  function iconPath(theme?: 'light' | 'dark', useDefault?: boolean): string | null;
  function iconPath(theme: 'light' | 'dark' = 'light', useDefault = false): string | null {
    if (!data.value || !formData.value) {
      return '';
    }

    // if we want the default icon, get it from the server by passing an empty icon path
    if (useDefault) {
      return `${iisBase}${buildManagedIconPath(
        { iconPath: '', iconIndex: 0, isRemoteApp: false, isManagedFileResource: false },
        dataUpdatedAt.value,
        theme
      )}`;
    }

    // if no icon is set for the theme, return null
    if (isManagedFileResource.value) {
      if (theme === 'light' && !formData.value.hasLightIcon) {
        return null;
      }
      if (theme === 'dark' && !formData.value.hasDarkIcon) {
        return null;
      }
    }

    // if an icon was uploaded for the theme, prefer the uploaded version over the possiblly existing one on the server
    if (
      isManagedFileResource.value &&
      theme === 'light' &&
      formData.value.hasLightIcon &&
      uploadedLightIconBlob.value
    ) {
      return uploadedLightIconUrl.value || null;
    }
    if (
      isManagedFileResource.value &&
      theme === 'dark' &&
      formData.value.hasDarkIcon &&
      uploadedDarkIconBlob.value
    ) {
      return uploadedDarkIconUrl.value || null;
    }

    // otherwise, use the icon on the server
    return `${iisBase}${buildManagedIconPath(
      isManagedFileResource.value
        ? {
            identifier: useDefault ? '' : data.value.identifier,
            isRemoteApp: isRemoteApp.value,
            isManagedFileResource: true,
          }
        : {
            iconPath: useDefault ? '' : formData.value?.iconPath,
            iconIndex: formData.value.iconIndex,
            isRemoteApp: !!formData.value.remoteAppProperties,
            isManagedFileResource: false,
          },
      dataUpdatedAt.value,
      theme
    )}`;
  }

  const browserSupportsImageUpload = 'toBase64' in Uint8Array.prototype;
  const uploadedLightIconBlob = ref<Blob | null>(null);
  const uploadedDarkIconBlob = ref<Blob | null>(null);
  const uploadedLightIconUrl = useObjectUrl(uploadedLightIconBlob);
  const uploadedDarkIconUrl = useObjectUrl(uploadedDarkIconBlob);
  const processingLightIcon = ref(false);
  const processingDarkIcon = ref(false);
  function resetLightIconToDefault() {
    uploadedLightIconBlob.value = new Blob([''], { type: 'image/jpeg' });
    if (formData.value) {
      formData.value.hasLightIcon = false;
    }
  }
  function resetDarkIconToDefault() {
    uploadedDarkIconBlob.value = new Blob([''], { type: 'image/jpeg' });
    if (formData.value) {
      formData.value.hasDarkIcon = false;
    }
  }
</script>

<template>
  <ContentDialog
    @open="() => refetch()"
    @close="
      async (event) => {
        event.preventDefault();

        // ensure user wants to close if there are unsaved changes
        const modified = await getModifiedFields();
        if (Object.keys(modified).length > 0) {
          await showConfirm(
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

              // discard the working copy
              formData = null;
              saveError = null;
              uploadedLightIconBlob = null;
              uploadedDarkIconBlob = null;
            })
            .catch(() => {}); // user cancelled closing
        } else {
          event.detail.close();

          // discard the working copy
          formData = null;
          saveError = null;
          uploadedLightIconBlob = null;
          uploadedDarkIconBlob = null;
        }
      }
    "
    @save-keyboard-shortcut="(close) => attemptSave(close)"
    :close-on-backdrop-click="false"
    :title="
      (displayName || data?.name) +
      (isManagedFileResource ? 'ᵠ ' : ' ') +
      t('registryApps.manager.appProperties.title')
    "
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
        v-if="formData"
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
        <!-- Desktop name and address -->
        <FieldSet v-if="!isRemoteApp">
          <template #legend>
            <TextBlock block variant="bodyLarge">{{
              t('registryApps.manager.appProperties.sections.desktop')
            }}</TextBlock>
          </template>
          <Field>
            <TextBlock>{{ t('registryApps.properties.displayName') }}</TextBlock>
            <TextBox v-model:value="formData.name"></TextBox>
          </Field>
          <Field v-if="isManagedFileResource">
            <TextBlock>{{ t('registryApps.properties.externalAddress') }}</TextBlock>
            <TextBox :value="externalAddress?.toString()" disabled></TextBox>
          </Field>
        </FieldSet>

        <!-- RemoteApp name, paths, and address -->
        <FieldSet v-if="isRemoteApp && formData.remoteAppProperties">
          <template #legend>
            <TextBlock block variant="bodyLarge">{{
              t('registryApps.manager.appProperties.sections.application')
            }}</TextBlock>
          </template>
          <Field>
            <TextBlock>{{ t('registryApps.properties.displayName') }}</TextBlock>
            <TextBox v-model:value="formData.name"></TextBox>
          </Field>
          <Field>
            <TextBlock>{{ t('registryApps.properties.appPath') }}</TextBlock>
            <TextBox v-model:value="formData.remoteAppProperties.applicationPath"></TextBox>
          </Field>
          <Field>
            <TextBlock>{{ t('registryApps.properties.cmdLineArgs') }}</TextBlock>
            <TextBox v-model:value="formData.remoteAppProperties.commandLine"></TextBox>
          </Field>
          <Field v-if="externalAddress">
            <TextBlock>{{ t('registryApps.properties.externalAddress') }}</TextBlock>
            <TextBox :value="externalAddress?.toString()" disabled></TextBox>
          </Field>
        </FieldSet>

        <!-- registry RemoteApp icons -->
        <FieldSet v-if="isRemoteApp && !isManagedFileResource">
          <template #legend>
            <TextBlock block variant="bodyLarge">{{
              t('registryApps.manager.appProperties.sections.icon')
            }}</TextBlock>
          </template>
          <Field>
            <TextBlock>{{ t('registryApps.properties.iconPath') }}</TextBlock>
            <div class="split">
              <TextBox v-model:value="formData.iconPath"></TextBox>
              <img :src="iconPath() || iconPath('light', true)" alt="" width="24" height="24" />
            </div>
          </Field>
          <Field>
            <TextBlock>{{ t('registryApps.properties.iconIndex') }}</TextBlock>
            <div class="split">
              <TextBox v-model:value="formData.iconIndex"></TextBox>
              <PickIconIndexDialog
                :icon-path="formData.iconPath ?? ''"
                :current-index="parseInt(formData.iconIndex)"
                @index-selected="
                  (newIndex, newPath) => {
                    if (formData) {
                      formData.iconIndex = newIndex.toString();
                      if (newPath && formData.iconPath !== newPath) {
                        formData.iconPath = newPath;
                      }
                    }
                  }
                "
                #default="{ open }"
              >
                <Button :disabled="!formData.iconPath" type="button" @click="open">
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

        <!-- managed file app icons -->
        <FieldSet v-if="isManagedFileResource">
          <template #legend>
            <TextBlock block variant="bodyLarge" v-if="isRemoteApp">
              {{ t('registryApps.manager.appProperties.sections.icon') }}
            </TextBlock>
            <TextBlock block variant="bodyLarge" v-else>
              {{ t('registryApps.manager.appProperties.sections.wallpaper') }}
            </TextBlock>
          </template>

          <div class="split">
            <Field no-label-focus class="group">
              <TextBlock block variant="body">
                {{ t('registryApps.properties.managedIcon.light') }}
              </TextBlock>
              <div class="stack">
                <img
                  :src="uploadedLightIconUrl || iconPath('light') || iconPath('light', true)"
                  alt=""
                  height="36"
                  :style="`
                    height: ${isRemoteApp ? 36 : 140}px;
                    width: ${isRemoteApp ? 36 : 224}px;
                    object-fit: ${isRemoteApp ? 'contain' : 'cover'};
                    border-radius: ${isRemoteApp ? 0 : 'var(--wui-control-corner-radius)'};
                  `"
                  @error="(event) => {
                    (event.target as HTMLImageElement).src = iconPath('light', true);
                  }"
                />
                <IconButton class="dismiss" @click="resetLightIconToDefault" v-if="formData.hasLightIcon">
                  <svg viewBox="0 0 24 24">
                    <path
                      d="m4.397 4.554.073-.084a.75.75 0 0 1 .976-.073l.084.073L12 10.939l6.47-6.47a.75.75 0 1 1 1.06 1.061L13.061 12l6.47 6.47a.75.75 0 0 1 .072.976l-.073.084a.75.75 0 0 1-.976.073l-.084-.073L12 13.061l-6.47 6.47a.75.75 0 0 1-1.06-1.061L10.939 12l-6.47-6.47a.75.75 0 0 1-.072-.976l.073-.084-.073.084Z"
                      fill="currentColor"
                    ></path>
                  </svg>
                </IconButton>
                <Button
                  :loading="processingLightIcon"
                  :disabled="!browserSupportsImageUpload"
                  @click="
                    () => {
                      processingLightIcon = true;
                      pickImageFile()
                        .then((blob) => {
                          uploadedLightIconBlob = blob;
                          if (formData) formData.hasLightIcon = true;
                        })
                        .finally(() => {
                          processingLightIcon = false;
                        });
                    }
                  "
                >
                  {{
                    isRemoteApp
                      ? t('registryApps.manager.appProperties.selectIcon')
                      : t('registryApps.manager.appProperties.selectWallpaper')
                  }}
                </Button>
              </div>
            </Field>

            <Field no-label-focus class="group">
              <TextBlock block variant="body">
                {{ t('registryApps.properties.managedIcon.dark') }}
              </TextBlock>
              <div class="stack">
                <img
                  :key="formData.hasLightIcon?.toString()"
                  :src="uploadedDarkIconUrl || iconPath('dark') || iconPath('light') || iconPath('dark', true)"
                  alt=""
                  height="36"
                  :style="`
                    height: ${isRemoteApp ? 36 : 140}px;
                    width: ${isRemoteApp ? 36 : 224}px;
                    object-fit: ${isRemoteApp ? 'contain' : 'cover'};
                    border-radius: ${isRemoteApp ? 0 : 'var(--wui-control-corner-radius)'};
                  `"
                  @error="(event) => {
                    (event.target as HTMLImageElement).src = iconPath('dark') || iconPath('light') || iconPath('dark', true);
                  }"
                />
                <IconButton class="dismiss" @click="resetDarkIconToDefault" v-if="formData.hasDarkIcon">
                  <svg viewBox="0 0 24 24">
                    <path
                      d="m4.397 4.554.073-.084a.75.75 0 0 1 .976-.073l.084.073L12 10.939l6.47-6.47a.75.75 0 1 1 1.06 1.061L13.061 12l6.47 6.47a.75.75 0 0 1 .072.976l-.073.084a.75.75 0 0 1-.976.073l-.084-.073L12 13.061l-6.47 6.47a.75.75 0 0 1-1.06-1.061L10.939 12l-6.47-6.47a.75.75 0 0 1-.072-.976l.073-.084-.073.084Z"
                      fill="currentColor"
                    ></path>
                  </svg>
                </IconButton>
                <Button
                  :loading="processingDarkIcon"
                  :disabled="!browserSupportsImageUpload"
                  @click="
                    () => {
                      processingDarkIcon = true;
                      pickImageFile()
                        .then((blob) => {
                          uploadedDarkIconBlob = blob;
                          if (formData) formData.hasDarkIcon = true;
                        })
                        .finally(() => {
                          processingDarkIcon = false;
                        });
                    }
                  "
                >
                  {{
                    isRemoteApp
                      ? t('registryApps.manager.appProperties.selectIcon')
                      : t('registryApps.manager.appProperties.selectWallpaper')
                  }}
                </Button>
              </div>
            </Field>
          </div>
        </FieldSet>

        <!-- advanced properties -->
        <FieldSet>
          <template #legend>
            <TextBlock block variant="bodyLarge">
              {{ t('registryApps.manager.appProperties.sections.advanced') }}
            </TextBlock>
          </template>
          <Field>
            <TextBlock block>{{ t('registryApps.properties.includeInWorkspace') }}</TextBlock>
            <ToggleSwitch v-model="formData.includeInWorkspace">
              {{ formData.includeInWorkspace ? t('policies.state.enabled') : t('policies.state.disabled') }}
            </ToggleSwitch>
          </Field>
          <Field no-label-focus v-if="isRemoteApp && formData.remoteAppProperties">
            <TextBlock block>{{ t('registryApps.properties.fileTypeAssociations') }}</TextBlock>
            <div>
              <EditFileTypeAssociationsDialog
                #default="{ open }"
                v-model="formData.remoteAppProperties.fileTypeAssociations"
                :app-name="formData.name + (isManagedFileResource ? 'ᵠ ' : ' ')"
                :resource-identifier="data?.identifier || formData.identifier"
                :is-managed-file-resource="isManagedFileResource"
                :fallback-icon-path="formData.iconPath"
                :fallback-icon-index="parseInt(formData.iconIndex)"
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
              <ManagedResourceSecurityDialog
                #default="{ open }"
                :app-name="formData.name + (isManagedFileResource ? 'ᵠ ' : ' ')"
                v-model="formData.securityDescription"
              >
                <Button @click="open">
                  <template #icon>
                    <svg viewBox="0 0 24 24">
                      <path
                        d="M4 13.999 13 14a2 2 0 0 1 1.995 1.85L15 16v1.5C14.999 21 11.284 22 8.5 22c-2.722 0-6.335-.956-6.495-4.27L2 17.5v-1.501c0-1.054.816-1.918 1.85-1.995L4 14ZM15.22 14H20c1.054 0 1.918.816 1.994 1.85L22 16v1c-.001 3.062-2.858 4-5 4a7.16 7.16 0 0 1-2.14-.322c.336-.386.607-.827.802-1.327A6.19 6.19 0 0 0 17 19.5l.267-.006c.985-.043 3.086-.363 3.226-2.289L20.5 17v-1a.501.501 0 0 0-.41-.492L20 15.5h-4.051a2.957 2.957 0 0 0-.595-1.34L15.22 14H20h-4.78ZM4 15.499l-.1.01a.51.51 0 0 0-.254.136.506.506 0 0 0-.136.253l-.01.101V17.5c0 1.009.45 1.722 1.417 2.242.826.445 2.003.714 3.266.753l.317.005.317-.005c1.263-.039 2.439-.308 3.266-.753.906-.488 1.359-1.145 1.412-2.057l.005-.186V16a.501.501 0 0 0-.41-.492L13 15.5l-9-.001ZM8.5 3a4.5 4.5 0 1 1 0 9 4.5 4.5 0 0 1 0-9Zm9 2a3.5 3.5 0 1 1 0 7 3.5 3.5 0 0 1 0-7Zm-9-.5c-1.654 0-3 1.346-3 3s1.346 3 3 3 3-1.346 3-3-1.346-3-3-3Zm9 2c-1.103 0-2 .897-2 2s.897 2 2 2 2-.897 2-2-.897-2-2-2Z"
                        fill="currentColor"
                      />
                    </svg>
                  </template>
                  {{ t('registryApps.manager.appProperties.manageUserAssignment') }}
                </Button>
              </ManagedResourceSecurityDialog>
            </div>
          </Field>
          <Field v-if="data?.source !== ManagedResourceSource.CentralPublishedResourcesDesktop">
            <TextBlock>{{ t('registryApps.properties.key') }}</TextBlock>
            <TextBox v-model:value="formData.identifier"></TextBox>
          </Field>
          <Field no-label-focus v-if="capabilities.supportsCentralizedPublishing">
            <TextBlock block>{{ t('registryApps.properties.customizeRdpFile') }}</TextBlock>
            <div>
              <RdpFilePropertiesDialog
                #default="{ open }"
                :name="formData.name + (isManagedFileResource ? 'ᵠ ' : ' ')"
                :model-value="formData.rdpFileString"
                @update:model-value="
                  (newValue) => {
                    if (formData) {
                      formData.rdpFileString = generateRdpFileContents(newValue);
                    }
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
                :hidden-groups="isRemoteApp ? undefined : ['remoteapp']"
                mode="edit"
                :source="formData?.source !== undefined ? { source: formData.source } : undefined"
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
        <FieldSet v-if="data?.source !== ManagedResourceSource.CentralPublishedResourcesDesktop">
          <template #legend>
            <TextBlock block variant="bodyLarge">{{
              t('registryApps.manager.appProperties.sections.dangerZone')
            }}</TextBlock>
          </template>
          <Field>
            <div>
              <Button @click="attemptDelete(close)">
                <template #icon>
                  <svg viewBox="0 0 24 24">
                    <path
                      d="M12 1.75a3.25 3.25 0 0 1 3.245 3.066L15.25 5h5.25a.75.75 0 0 1 .102 1.493L20.5 6.5h-.796l-1.28 13.02a2.75 2.75 0 0 1-2.561 2.474l-.176.006H8.313a2.75 2.75 0 0 1-2.714-2.307l-.023-.174L4.295 6.5H3.5a.75.75 0 0 1-.743-.648L2.75 5.75a.75.75 0 0 1 .648-.743L3.5 5h5.25A3.25 3.25 0 0 1 12 1.75Zm6.197 4.75H5.802l1.267 12.872a1.25 1.25 0 0 0 1.117 1.122l.127.006h7.374c.6 0 1.109-.425 1.225-1.002l.02-.126L18.196 6.5ZM13.75 9.25a.75.75 0 0 1 .743.648L14.5 10v7a.75.75 0 0 1-1.493.102L13 17v-7a.75.75 0 0 1 .75-.75Zm-3.5 0a.75.75 0 0 1 .743.648L11 10v7a.75.75 0 0 1-1.493.102L9.5 17v-7a.75.75 0 0 1 .75-.75Zm1.75-6a1.75 1.75 0 0 0-1.744 1.606L10.25 5h3.5A1.75 1.75 0 0 0 12 3.25Z"
                      fill="currentColor"
                    />
                  </svg>
                </template>
                {{
                  isRemoteApp
                    ? t('registryApps.manager.appProperties.removeApp')
                    : t('registryApps.manager.appProperties.removeDesktop')
                }}
              </Button>
            </div>
          </Field>
        </FieldSet>
      </div>
    </template>

    <template #footer="{ close }">
      <Button @click="attemptSave(close)" :loading="saving">OK</Button>
      <Button @click="close">Cancel</Button>
    </template>
  </ContentDialog>
</template>
