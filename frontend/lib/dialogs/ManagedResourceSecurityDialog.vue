<script setup lang="ts">
  import { Button, ContentDialog, IconButton, TextBlock } from '$components';
  import { SelectUsersOrGroupsDialog, showConfirm } from '$dialogs';
  import { useCoreDataStore } from '$stores';
  import { PreventableEvent, ResourceManagementSchemas, SecurityManagementSchemas } from '$utils';
  import { unproxify } from '$utils/unproxify';
  import { useTranslation } from 'i18next-vue';
  import { computed, ref, useTemplateRef, watchEffect } from 'vue';
  import z from 'zod';

  const { iisBase } = useCoreDataStore();
  const { t } = useTranslation();

  const { appName } = defineProps<{
    appName?: string;
  }>();

  type SecurityDescription = z.infer<
    typeof ResourceManagementSchemas.RegistryRemoteApp.App
  >['securityDescription'];
  const access = defineModel<SecurityDescription>();
  const initialAccess = ref<SecurityDescription>();
  const isModified = computed(() => {
    if (!access.value && !initialAccess.value) return false;
    if (!!access.value && !initialAccess.value) return true;
    if (!access.value && !!initialAccess.value) return true;
    return JSON.stringify(access.value) !== JSON.stringify(initialAccess.value);
  });

  const dialogRef = useTemplateRef('dialog');
  const isDialogOpen = computed(() => unproxify(dialogRef.value)?.isOpen || false);

  const openedAt = ref(Date.now());
  function handleAfterOpen() {
    openedAt.value = Date.now();
    shouldResetOnClose.value = true;
    if (access.value) {
      initialAccess.value = JSON.parse(JSON.stringify(access.value));
    }
  }

  // resolve the sids to display names
  const isResolvedAllowedPending = ref(true);
  const isResolvedAllowedLoading = ref(false);
  const resolvedAllowedError = ref<Error | undefined>(undefined);
  const resolvedAllowed = ref<z.infer<typeof SecurityManagementSchemas.Resolved>[]>([]);
  const resolvedDenied = ref<z.infer<typeof SecurityManagementSchemas.Resolved>[]>([]);
  const isResolvedDeniedPending = ref(true);
  const isResolvedDeniedLoading = ref(false);
  const resolvedDeniedError = ref<Error | undefined>(undefined);
  async function resolveSids(sids: string[]) {
    if (sids.length === 0) return [];

    return fetch(`${iisBase}api/management/security/resolve-sids`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(sids),
    })
      .then(async (res) => {
        if (!res.ok) {
          const err = await res.json().catch(() => null);
          if (err && 'ExceptionMessage' in err) throw new Error(err.ExceptionMessage);
          throw new Error(`Error resolving SIDs: ${res.status} ${res.statusText}`);
        }
        return res.json();
      })
      .then((data) => {
        const resolved = SecurityManagementSchemas.ResolvedMany.parse(data);
        return [
          ...resolved.resolvedSids,
          ...resolved.invalidOrUnfoundSids.map(
            (sid) =>
              ({
                sid,
                userName: sid,
                expandedDisplayName: sid,
                principalKind: 0,
              } satisfies z.infer<typeof SecurityManagementSchemas.Resolved>)
          ),
        ];
      });
  }
  watchEffect(async () => {
    if (!isDialogOpen.value) {
      resolvedAllowed.value = [];
      return;
    }

    if (!access.value) {
      resolvedAllowed.value = [];
      isResolvedAllowedPending.value = false;
      return;
    }

    try {
      isResolvedAllowedLoading.value = true;
      const data = await resolveSids(access.value.readAccessAllowedSids || []);
      resolvedAllowed.value = data;
      isResolvedAllowedPending.value = false;
    } catch (err) {
      console.error('Failed to resolve allowed SIDs:', err);
      resolvedAllowed.value = [];
      resolvedAllowedError.value = new Error(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      isResolvedAllowedLoading.value = false;
    }
  });
  watchEffect(async () => {
    if (!isDialogOpen.value) {
      resolvedDenied.value = [];
      return;
    }

    if (!access.value) {
      resolvedDenied.value = [];
      isResolvedDeniedPending.value = false;
      return;
    }

    try {
      isResolvedDeniedLoading.value = true;
      const data = await resolveSids(access.value.readAccessDeniedSids || []);
      resolvedDenied.value = data;
      isResolvedDeniedPending.value = false;
    } catch (err) {
      console.error('Failed to resolve denied SIDs:', err);
      resolvedDenied.value = [];
      resolvedDeniedError.value = new Error(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      isResolvedDeniedLoading.value = false;
    }
  });

  var shouldResetOnClose = ref(true);
  function handleClose(close: () => void, parentCloseEvent: PreventableEvent) {
    // if the close event from the ContentDialog was prevented, do nothing
    if (parentCloseEvent.defaultPrevented) return;

    // if there were modifications, confirm with the user before discarding changes
    if (isModified.value && shouldResetOnClose.value) {
      parentCloseEvent.preventDefault();

      showConfirm(
        t('closeDialogWithUnsavedChangesGuard.title'),
        t('closeDialogWithUnsavedChangesGuard.message'),
        'Yes',
        'No'
      )
        .then((closeConfirmDialog) => {
          // user wants to discard changes
          close();

          // reset to initial state
          access.value = JSON.parse(JSON.stringify(initialAccess.value));

          closeConfirmDialog();
          initialAccess.value = undefined;
        })
        .catch(() => {
          // do nothing; user cancelled
        });
    } else {
      close();
      initialAccess.value = undefined;
    }

    isResolvedAllowedPending.value = true;
    isResolvedDeniedPending.value = true;
  }

  function closeWithPreservedChanges(close: () => void) {
    shouldResetOnClose.value = false;
    close();
  }
</script>

<template>
  <ContentDialog
    @close="handleClose($event.detail.close, $event)"
    @after-open="handleAfterOpen()"
    @save-keyboard-shortcut="closeWithPreservedChanges"
    ref="dialog"
    :close-on-backdrop-click="false"
    :title="t('registryApps.manager.securityEditor.title', { app_name: appName })"
    size="standard"
    max-height="640px"
    fill-height
    :updating="isResolvedAllowedLoading || isResolvedDeniedLoading"
    :loading="isResolvedAllowedPending || isResolvedDeniedPending"
    :error="
      isResolvedAllowedPending || isResolvedDeniedPending
        ? resolvedAllowedError ?? resolvedDeniedError
        : undefined
    "
  >
    <template #opener="{ close, open, popoverId }">
      <slot name="default" :close="close" :open="open" :popover-id="popoverId" />
    </template>

    <template #default>
      <div class="header-form">
        <SelectUsersOrGroupsDialog
          #default="{ open }"
          @apply="
            (result) => {
              if (access) {
                access.readAccessAllowedSids = Array.from(
                  new Set([...(access?.readAccessAllowedSids || []), ...result.map((obj) => obj.sid)])
                );
              } else {
                access = {
                  readAccessAllowedSids: result.map((obj) => obj.sid),
                  readAccessDeniedSids: [],
                };
              }
            }
          "
        >
          <Button @click="open">
            {{ t('registryApps.manager.securityEditor.add') }}
          </Button>
        </SelectUsersOrGroupsDialog>
        <Button
          @click="
            () => {
              if (access) {
                access.readAccessAllowedSids = [];
                access.readAccessDeniedSids = [];
              }
            }
          "
          >{{ t('registryApps.manager.securityEditor.removeAll') }}</Button
        >
      </div>

      <TextBlock style="margin-bottom: calc(var(--inner-padding) / 2)">
        {{ t('registryApps.manager.securityEditor.description') }}
      </TextBlock>

      <TextBlock v-if="resolvedAllowed.length === 0 && resolvedDenied.length === 0">
        {{ t('registryApps.manager.securityEditor.undefined') }}
      </TextBlock>

      <template v-else>
        <TextBlock variant="bodyLarge">
          {{ t('registryApps.manager.securityEditor.allowedUsersGroupsLabel') }}
        </TextBlock>
        <div class="obj-list" v-if="resolvedAllowed.length > 0">
          <div class="obj" v-for="obj of resolvedAllowed">
            <TextBlock>{{ obj.displayName || obj.userPrincipalName || obj.userName || obj.sid }}</TextBlock>
            <IconButton
              :title="t('registryApps.manager.securityEditor.denyReadAccess')"
              @click="
                () => {
                  if (access) {
                    access.readAccessAllowedSids = access.readAccessAllowedSids?.filter(
                      (sid) => sid !== obj.sid
                    );
                    access.readAccessDeniedSids = Array.from(
                      new Set([...(access.readAccessDeniedSids || []), obj.sid])
                    );
                  }
                }
              "
            >
              <svg viewBox="0 0 24 24">
                <path
                  d="M3.75 5a.75.75 0 0 0-.75.75V11c0 5.001 2.958 8.676 8.725 10.948a.75.75 0 0 0 .55 0c.144-.057.286-.114.426-.173a6.536 6.536 0 0 1-1.667-1.756C6.64 17.962 4.5 14.975 4.5 11V6.478c2.577-.152 5.08-1.09 7.5-2.8 2.42 1.71 4.923 2.648 7.5 2.8v4.254c.54.282 1.037.638 1.475 1.054.017-.258.025-.52.025-.786V5.75a.75.75 0 0 0-.75-.75c-2.663 0-5.258-.943-7.8-2.85a.75.75 0 0 0-.9 0C9.008 4.057 6.413 5 3.75 5Z"
                  fill="currentColor"
                />
                <path
                  d="M16.5 22a5.5 5.5 0 1 0 0-11 5.5 5.5 0 0 0 0 11Zm-3.309-3.252a4 4 0 0 1 5.557-5.557l-5.557 5.557Zm1.06 1.06 5.558-5.556a4 4 0 0 1-5.557 5.557Z"
                  fill="currentColor"
                />
              </svg>
            </IconButton>
            <IconButton
              :title="t('registryApps.manager.securityEditor.remove')"
              @click="
                () => {
                  if (access) {
                    access.readAccessAllowedSids = access.readAccessAllowedSids?.filter(
                      (sid) => sid !== obj.sid
                    );
                  }
                }
              "
            >
              <svg viewBox="0 0 24 24">
                <path
                  d="m4.397 4.554.073-.084a.75.75 0 0 1 .976-.073l.084.073L12 10.939l6.47-6.47a.75.75 0 1 1 1.06 1.061L13.061 12l6.47 6.47a.75.75 0 0 1 .072.976l-.073.084a.75.75 0 0 1-.976.073l-.084-.073L12 13.061l-6.47 6.47a.75.75 0 0 1-1.06-1.061L10.939 12l-6.47-6.47a.75.75 0 0 1-.072-.976l.073-.084-.073.084Z"
                  fill="currentColor"
                />
              </svg>
            </IconButton>
          </div>
        </div>
        <TextBlock v-else style="margin-bottom: calc(var(--inner-padding) / 2)"
          >{{ t('registryApps.manager.securityEditor.undefinedAllowed') }}
        </TextBlock>

        <TextBlock variant="bodyLarge">
          {{ t('registryApps.manager.securityEditor.deniedUsersGroupsLabel') }}
        </TextBlock>
        <div class="obj-list" v-if="resolvedDenied.length > 0">
          <div class="obj" v-for="obj of resolvedDenied">
            <TextBlock>{{ obj.displayName || obj.userPrincipalName || obj.userName || obj.sid }}</TextBlock>
            <IconButton
              :title="t('registryApps.manager.securityEditor.allowReadAccess')"
              @click="
                () => {
                  if (access) {
                    access.readAccessDeniedSids = access.readAccessDeniedSids?.filter((sid) => sid !== obj.sid);
                    access.readAccessAllowedSids = Array.from(
                      new Set([...(access.readAccessAllowedSids || []), obj.sid])
                    );
                  }
                }
              "
            >
              <svg viewBox="0 0 24 24">
                <path
                  d="M3 5.75A.75.75 0 0 1 3.75 5c2.663 0 5.258-.943 7.8-2.85a.75.75 0 0 1 .9 0C14.992 4.057 17.587 5 20.25 5a.75.75 0 0 1 .75.75V11c0 .338-.014.67-.04.996a6.467 6.467 0 0 0-1.465-.684c.003-.103.005-.207.005-.312V6.478c-2.577-.152-5.08-1.09-7.5-2.8-2.42 1.71-4.923 2.648-7.5 2.8V11c0 4.149 2.332 7.221 7.125 9.285a6.506 6.506 0 0 0 1.005 1.52l-.355.143a.75.75 0 0 1-.55 0C5.958 19.676 3 16 3 11V5.75ZM23 17.5a5.5 5.5 0 1 1-11 0 5.5 5.5 0 0 1 11 0Zm-2.146-2.354a.5.5 0 0 0-.708 0L16.5 18.793l-1.646-1.647a.5.5 0 0 0-.708.708l2 2a.5.5 0 0 0 .708 0l4-4a.5.5 0 0 0 0-.708Z"
                  fill="currentColor"
                />
              </svg>
            </IconButton>
            <IconButton
              :title="t('registryApps.manager.securityEditor.remove')"
              @click="
                () => {
                  if (access) {
                    access.readAccessDeniedSids = access.readAccessDeniedSids?.filter((sid) => sid !== obj.sid);
                  }
                }
              "
            >
              <svg viewBox="0 0 24 24">
                <path
                  d="m4.397 4.554.073-.084a.75.75 0 0 1 .976-.073l.084.073L12 10.939l6.47-6.47a.75.75 0 1 1 1.06 1.061L13.061 12l6.47 6.47a.75.75 0 0 1 .072.976l-.073.084a.75.75 0 0 1-.976.073l-.084-.073L12 13.061l-6.47 6.47a.75.75 0 0 1-1.06-1.061L10.939 12l-6.47-6.47a.75.75 0 0 1-.072-.976l.073-.084-.073.084Z"
                  fill="currentColor"
                />
              </svg>
            </IconButton>
          </div>
        </div>
        <TextBlock v-else style="margin-bottom: calc(var(--inner-padding) / 2)"
          >{{ t('registryApps.manager.securityEditor.undefinedDenied') }}
        </TextBlock>
      </template>
    </template>

    <template #footer="{ close }">
      <Button @click="closeWithPreservedChanges(close)">{{ t('dialog.ok') }}</Button>
      <Button @click="close">{{ t('dialog.cancel') }}</Button>
    </template>
  </ContentDialog>
</template>

<style scoped>
  .obj-list {
    display: flex;
    flex-direction: column;
    gap: 8px;
    margin-top: calc(var(--inner-padding) / 4);
    margin-bottom: calc(var(--inner-padding) / 2);
  }
  .obj {
    display: flex;
    align-items: stretch;
    gap: 8px;
    width: 100%;
    background-color: var(--wui-card-background-default);
    box-shadow: inset 0 0 0 1px var(--wui-control-stroke-default);
    border-radius: var(--wui-control-corner-radius);
  }
  .obj > .text-block {
    padding: 10px;
    flex-grow: 1;
  }
  .obj > .button {
    flex-shrink: 0;
  }
  .obj > .button:not(:hover):not(:active):not(.disabled) {
    background-color: transparent;
    box-shadow: none;
  }

  .header-form + * {
    margin-top: calc(var(--inner-padding) / 2);
  }

  .header-form {
    position: sticky;
    top: var(--title-height);
    z-index: 9;
    background-color: var(--wui-background-default);
    border-bottom: 1px solid var(--wui-surface-stroke-default);
    margin-left: calc(-1 * var(--inner-padding));
    margin-right: calc(-1 * var(--inner-padding));
    padding: 0 var(--inner-padding) calc(var(--inner-padding) / 2) var(--inner-padding);
    display: flex;
    flex-direction: row;
    gap: 8px;
    flex-wrap: wrap;
  }
  .header-form::before {
    content: '';
    position: absolute;
    background-color: var(--wui-solid-background-base);
    inset: 0;
    top: -28px;
    z-index: -1;
  }
  .header-form::after {
    content: '';
    position: absolute;
    background-color: var(--wui-layer-default);
    inset: 0;
    top: -28px;
    z-index: -1;
  }
</style>
