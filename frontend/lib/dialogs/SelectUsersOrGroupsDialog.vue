<script setup lang="ts">
  import { Button, ContentDialog, Field, IconButton, TextBlock, TextBox } from '$components';
  import { SelectLocationDialog, showConfirm } from '$dialogs';
  import { useCoreDataStore } from '$stores';
  import { SecurityManagementSchemas } from '$utils';
  import { useQuery } from '@tanstack/vue-query';
  import { useTranslation } from 'i18next-vue';
  import { ref } from 'vue';
  import z from 'zod';

  const { iisBase } = useCoreDataStore();
  const { t } = useTranslation();

  const { isPending, isFetching, isError, data, error, refetch, dataUpdatedAt } = useQuery({
    queryKey: ['security-locations'],
    queryFn: async () => {
      return fetch(`${iisBase}api/management/security/locations`)
        .then(async (res) => {
          if (!res.ok) {
            await res.json().then((err) => {
              if (err && 'ExceptionMessage' in err) {
                throw new Error(err.ExceptionMessage);
              }
            });
            throw new Error(`Error fetching security locations: ${res.status} ${res.statusText}`);
          }
          return res.json();
        })
        .then((data) => {
          const locations = z.string().array().parse(data);
          selectedLocation.value = locations[0];
          return locations;
        });
    },
    enabled: false, // do not fetch automatically
  });

  const selectedLocation = ref<string | undefined>(undefined);
  const unresolvedObjectNames = ref<string>('');
  const resolvedObjects = ref<z.infer<typeof SecurityManagementSchemas.Resolved>[]>([]);

  const emit = defineEmits<{
    (e: 'apply', newObjects: typeof resolvedObjects.value): void;
    (e: 'close'): void;
  }>();

  function handleClose() {
    emit('close');

    // clear the state
    selectedLocation.value = undefined;
    unresolvedObjectNames.value = '';
    resolvedObjects.value = [];
  }

  const checkNamesLoading = ref(false);
  async function checkNames() {
    checkNamesLoading.value = true;
    for await (const name of unresolvedObjectNames.value.split(';').map((n) => n.trim())) {
      if (!name.trim()) continue;

      const searchParams = new URLSearchParams();
      searchParams.append('lookup', name);
      if (selectedLocation.value) {
        searchParams.append('domain', selectedLocation.value);
      }

      await fetch(`${iisBase}api/management/security/find-sid?${searchParams}`, {
        method: 'GET',
      })
        .then(async (res) => {
          if (!res.ok) {
            await showConfirm(
              t('registryApps.manager.selectUsersOrGroups.notFoundTitle'),
              t('registryApps.manager.selectUsersOrGroups.notFoundMessage', { name }),
              t('registryApps.manager.selectUsersOrGroups.discardNotFoundObject'),
              t('registryApps.manager.selectUsersOrGroups.keepNotFoundObject')
            )
              .then((closeConfirmDialog) => {
                // user wants to discard unfound name
                unresolvedObjectNames.value = unresolvedObjectNames.value
                  .split(';')
                  .map((n) => n.trim())
                  .filter((n) => n.toLowerCase() !== name.toLowerCase())
                  .join('; ');

                closeConfirmDialog();
              })
              .catch(() => {
                // do nothing; user wants to keep the unfound name
              });

            console.error(`Error looking up security object '${name}': ${res.status} ${res.statusText}`);
            throw new Error('');
          }
          return res.json();
        })
        .then((json) => SecurityManagementSchemas.Resolved.parse(json))
        .then((data) => {
          // remove the name from unresolved names
          unresolvedObjectNames.value = unresolvedObjectNames.value
            .split(';')
            .map((n) => n.trim())
            .filter((n) => n.toLowerCase() !== name.toLowerCase())
            .join('; ');

          // add to resolved objects
          if (!resolvedObjects.value.find((obj) => obj.sid === data.sid)) {
            resolvedObjects.value = [...resolvedObjects.value, data];
          }
        })
        .catch((err) => {
          if (err.message) {
            console.error(err);
          }
        });
    }
    checkNamesLoading.value = false;
  }

  const submitLoading = ref(false);
  async function submit(close: () => void) {
    submitLoading.value = true;
    await checkNames();
    submitLoading.value = false;

    // if there are still unresolved names, do not proceed
    if (unresolvedObjectNames.value.trim().length > 0) {
      return;
    }

    emit('apply', resolvedObjects.value);
    close();
  }

  const randomId = `select-users-or-groups-dialog-object-field-${Math.random().toString(36).substring(2, 15)}`;
</script>

<template>
  <ContentDialog
    @open="() => refetch()"
    @close="handleClose"
    :close-on-backdrop-click="false"
    :title="t('registryApps.manager.selectUsersOrGroups.title')"
    size="max"
    max-height="390px"
    :updating="isFetching"
  >
    <template #opener="{ close, open, popoverId }">
      <slot name="default" :close="close" :open="open" :popover-id="popoverId" />
    </template>

    <template #default="{ close, popoverId }">
      <Field>
        <TextBlock>{{ t('registryApps.manager.selectUsersOrGroups.locationsField') }}</TextBlock>
        <div class="split">
          <TextBox :value="selectedLocation" disabled></TextBox>
          <SelectLocationDialog
            v-model="selectedLocation"
            :locations="data ?? []"
            :key="dataUpdatedAt"
            #default="{ open }"
          >
            <Button style="width: 106px" :disabled="isFetching" @click="open">Locations...</Button>
          </SelectLocationDialog>
        </div>
      </Field>
      <Field :for="randomId">
        <TextBlock>{{ t('registryApps.manager.selectUsersOrGroups.objectNamesField') }}</TextBlock>
        <div class="split" style="align-items: flex-start">
          <TextBox
            text-area
            v-model:value="unresolvedObjectNames"
            style="min-height: 66px"
            :id="randomId"
            @submit="submit(close)"
          >
            <template #before-text-area>
              <span class="valid-object" v-for="obj in resolvedObjects"
                ><IconButton @click="resolvedObjects = resolvedObjects.filter((o) => o.sid !== obj.sid)">
                  <svg viewBox="0 0 24 24">
                    <path
                      d="m4.397 4.554.073-.084a.75.75 0 0 1 .976-.073l.084.073L12 10.939l6.47-6.47a.75.75 0 1 1 1.06 1.061L13.061 12l6.47 6.47a.75.75 0 0 1 .072.976l-.073.084a.75.75 0 0 1-.976.073l-.084-.073L12 13.061l-6.47 6.47a.75.75 0 0 1-1.06-1.061L10.939 12l-6.47-6.47a.75.75 0 0 1-.072-.976l.073-.084-.073.084Z"
                      fill="currentColor"
                    /></svg></IconButton
                >{{ obj.expandedDisplayName }}</span
              >
            </template>
          </TextBox>
          <Button
            style="width: 106px"
            @click="checkNames"
            :disabled="isFetching"
            :loading="checkNamesLoading && !submitLoading"
            >Check names</Button
          >
        </div>
      </Field>
    </template>

    <template #footer="{ close }">
      <Button @click="submit(close)" :loading="submitLoading">{{ t('dialog.ok') }}</Button>
      <Button @click="close">{{ t('dialog.cancel') }}</Button>
    </template>
  </ContentDialog>
</template>

<style scoped>
  .valid-object {
    text-decoration: underline;
    position: relative;
    padding-left: 24px;
  }

  .valid-object Button {
    padding: 2px;
    min-block-size: 20px;
    position: absolute;
    left: -4px;
    top: 1px;
  }

  .valid-object Button svg {
    width: 12px;
    height: 12px;
  }

  .valid-object::after {
    content: '; ';
    text-decoration: none;
    display: inline-block; /* Ensures underline doesn't propagate */
  }
</style>
