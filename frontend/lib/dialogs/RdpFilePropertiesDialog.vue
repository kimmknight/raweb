<script setup lang="ts">
  import { Button, ContentDialog, Field, NavigationPane, TextBlock, TextBox } from '$components';
  import { TreeItem } from '$components/NavigationView/NavigationTypes';
  import { ManagedResourceEditDialog } from '$dialogs';
  import { useCoreDataStore } from '$stores';
  import {
    generateRdpFileContents,
    getAppsAndDevices,
    groupResourceProperties,
    notEmpty,
    resourceGroupNames,
  } from '$utils';
  import { UnmanagedResourceSource } from '$utils/getAppsAndDevices';
  import { ManagedResourceSource } from '$utils/schemas/ResourceManagementSchemas';
  import { useTranslation } from 'i18next-vue';
  import { capitalize, computed, ref, useTemplateRef, watch } from 'vue';

  type Resource = NonNullable<Awaited<ReturnType<typeof getAppsAndDevices>>>['resources'][number];
  type AppOrDesktopProperties = Partial<NonNullable<Resource['hosts'][number]['rdp']>>;
  type GroupName = (typeof resourceGroupNames)[number];

  const { t } = useTranslation();
  const { authUser, capabilities } = useCoreDataStore();

  const {
    modelValue,
    mode = 'view',
    defaultGroup = 'raweb',
    hiddenGroups = [],
    name = '',
    terminalServer = '',
    source: _,
    disabledFields = [],
    allowEditDialog = false,
  } = defineProps<{
    mode?: 'view' | 'edit' | 'create';
    modelValue: AppOrDesktopProperties | string | undefined;
    defaultGroup?: GroupName;
    hiddenGroups?: GroupName[];
    name?: string;
    terminalServer?: string;
    source?: Resource['source'];
    disabledFields?: string[];
    allowEditDialog?: boolean;
  }>();
  const { source, managementIdentifier } = _ || {};

  // update resource properties when modelValue changes
  const resourceProperties = ref<Record<
    GroupName,
    Record<string, string | number | Uint8Array | undefined>
  > | null>(null);
  watch(
    () => modelValue,
    (newValue) => {
      let newValueObject: AppOrDesktopProperties = {};
      if (typeof newValue === 'string') {
        const lines = newValue.split('\n');
        for (const line of lines) {
          const parts = line.trim().split(':');
          const key = parts.slice(0, 2).join(':');
          const value = parts.slice(2).join(':');
          newValueObject[key as keyof AppOrDesktopProperties] = value;
        }
        newValueObject.rdpFileText = newValue;
      } else {
        newValueObject = newValue || {};
      }

      resourceProperties.value = groupResourceProperties(newValueObject, mode !== 'view');
    },
    { immediate: true }
  );

  const emit = defineEmits<{
    (e: 'update:modelValue', result: AppOrDesktopProperties): void; // emited on save/apply (when dialog is closed)
    (e: 'onClose'): void;
    (e: 'afterClose'): void;
    (e: 'afterSaveToRegistry'): void;
    (e: 'afterRemoveFromRegistry', close: () => void): void;
  }>();

  function flattenProperties(_resourceProperties: NonNullable<typeof resourceProperties.value>) {
    const flattenedProperties: AppOrDesktopProperties = {};
    for (const group of Object.values(_resourceProperties)) {
      for (const [key, value] of Object.entries(group)) {
        const stringOrNumberValue =
          value === undefined
            ? undefined
            : typeof value === 'string'
            ? value.trim()
            : typeof value === 'number'
            ? value
            : Array.from(value, (b) => b.toString(16).padStart(2, '0')).join('');

        if (stringOrNumberValue !== undefined) {
          flattenedProperties[key as keyof AppOrDesktopProperties] = stringOrNumberValue;
        }
      }
    }
    return flattenedProperties;
  }

  function emitChangesAndClose(closeDialog: () => void) {
    if (!capabilities.supportsCentralizedPublishing) {
      return;
    }

    if (resourceProperties.value) {
      const flattenedProperties = flattenProperties(resourceProperties.value);

      const rdpFileTextLines: string[] = [];
      for (const [key, value] of Object.entries(flattenedProperties)) {
        if (value !== undefined) {
          rdpFileTextLines.push(`${key}:${value}`);
        }
      }
      flattenedProperties.rdpFileText = rdpFileTextLines.join('\n');

      closeDialog();
      setTimeout(() => {
        emit('update:modelValue', flattenedProperties);
      }, 300);
    }
  }

  const isOpen = ref(false);

  const menuItemIconsMap = {
    connection:
      '<svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path d="M21.707 3.707a1 1 0 0 0-1.414-1.414L18.496 4.09a4.252 4.252 0 0 0-5.251.604l-1.068 1.069a1.75 1.75 0 0 0 0 2.474l3.585 3.586a1.75 1.75 0 0 0 2.475 0l1.068-1.068a4.252 4.252 0 0 0 .605-5.25l1.797-1.798ZM10.707 11.707a1 1 0 0 0-1.414-1.414l-1.47 1.47-.293-.293a.75.75 0 0 0-1.06 0l-1.775 1.775a4.252 4.252 0 0 0-.605 5.25l-1.797 1.798a1 1 0 1 0 1.414 1.414l1.798-1.797a4.252 4.252 0 0 0 5.25-.605l1.775-1.775a.75.75 0 0 0 0-1.06l-.293-.293 1.47-1.47a1 1 0 0 0-1.414-1.414l-1.47 1.47-1.586-1.586 1.47-1.47Z" fill="currentColor"/></svg>',
    display:
      '<svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path d="M6.75 22a.75.75 0 0 1-.102-1.493l.102-.007h1.749v-2.498H4.25a2.25 2.25 0 0 1-2.245-2.096L2 15.752V5.25a2.25 2.25 0 0 1 2.096-2.245L4.25 3h15.499a2.25 2.25 0 0 1 2.245 2.096l.005.154v10.502a2.25 2.25 0 0 1-2.096 2.245l-.154.005h-4.25V20.5h1.751a.75.75 0 0 1 .102 1.494L17.25 22H6.75Zm7.248-3.998h-4l.001 2.498h4l-.001-2.498ZM19.748 4.5H4.25a.75.75 0 0 0-.743.648L3.5 5.25v10.502c0 .38.282.694.648.743l.102.007h15.499a.75.75 0 0 0 .743-.648l.007-.102V5.25a.75.75 0 0 0-.648-.743l-.102-.007Z" fill="currentColor"/></svg>',
    gateway:
      '<svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path d="M22.002 12C22.002 6.477 17.524 2 12 2 6.476 1.999 2 6.477 2 12.001c0 5.186 3.947 9.45 9.001 9.952V20.11c-.778-.612-1.478-1.905-1.939-3.61h1.94V15H8.737a18.969 18.969 0 0 1-.135-5h6.794c.068.64.105 1.31.105 2h1.5c0-.684-.033-1.353-.095-2h3.358c.154.64.237 1.31.237 2h1.5ZM4.786 16.5h2.722l.102.396c.317 1.17.748 2.195 1.27 3.015a8.532 8.532 0 0 1-4.094-3.41ZM3.736 10h3.358a20.847 20.847 0 0 0-.095 2c0 1.043.075 2.051.217 3H4.043a8.483 8.483 0 0 1-.544-3c0-.682.08-1.347.232-1.983L3.736 10Zm5.122-5.902.023-.008C8.16 5.222 7.611 6.748 7.298 8.5H4.25c.905-2 2.56-3.587 4.608-4.402Zm3.026-.594L12 3.5l.126.006c1.262.126 2.48 2.125 3.045 4.995H8.83c.568-2.878 1.79-4.88 3.055-4.996Zm3.343.76-.107-.174.291.121a8.533 8.533 0 0 1 4.339 4.29h-3.048c-.298-1.665-.806-3.125-1.475-4.237Z" fill="currentColor"/><path d="M12 19a1 1 0 0 0 1 1h3v2h-.5a.5.5 0 1 0 0 1h4a.5.5 0 0 0 0-1H19v-2h3a1 1 0 0 0 1-1v-5a1 1 0 0 0-1-1h-9a1 1 0 0 0-1 1v5Z" fill="currentColor"/></svg>',
    hardware:
      '<svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path d="M12.92 3.316c.806-.717 2.08-.145 2.08.934v15.496c0 1.078-1.274 1.65-2.08.934l-4.492-3.994a.75.75 0 0 0-.498-.19H4.25A2.25 2.25 0 0 1 2 14.247V9.75a2.25 2.25 0 0 1 2.25-2.25h3.68a.75.75 0 0 0 .498-.19l4.491-3.993Zm.58 1.49L9.425 8.43A2.25 2.25 0 0 1 7.93 9H4.25a.75.75 0 0 0-.75.75v4.497c0 .415.336.75.75.75h3.68a2.25 2.25 0 0 1 1.495.57l4.075 3.623V4.807ZM16.499 8.25a.75.75 0 0 1 .75-.75h3.503a.75.75 0 0 1 .75.75v1.797a.75.75 0 0 1 .498.707v5a.75.75 0 0 1-.75.75h-.236v2.746a.75.75 0 0 1-1.5 0v-2.747h-.982v2.747a.75.75 0 0 1-1.5 0v-2.747h-.282a.75.75 0 0 1-.75-.75v-5a.75.75 0 0 1 .499-.707V8.25Zm4.001 3.254h-3v3.5h3v-3.5ZM17.999 9v1h2.003V9h-2.003Z" fill="currentColor"/></svg>',
    remoteapp:
      '<svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path d="m17.751 3 .185.005a3.25 3.25 0 0 1 3.06 3.06l.005.185v11.5l-.005.184A3.25 3.25 0 0 1 17.751 21H6.25a3.25 3.25 0 0 1-3.245-3.066L3 17.75V6.25a3.25 3.25 0 0 1 3.066-3.245L6.25 3h11.501ZM19.5 8H4.501L4.5 17.75a1.75 1.75 0 0 0 1.606 1.744l.144.006h11.501l.144-.006a1.75 1.75 0 0 0 1.6-1.593l.006-.151L19.5 8Zm-9.25 1.5a.75.75 0 0 1 .743.648l.007.102v7a.75.75 0 0 1-.648.743L10.25 18h-3.5a.75.75 0 0 1-.743-.648L6 17.25v-7a.75.75 0 0 1 .648-.743L6.75 9.5h3.5ZM9.5 11h-2v5.5h2V11Zm6.75 1.503a.75.75 0 0 1 .102 1.493l-.102.007h-3.496a.75.75 0 0 1-.101-1.493l.101-.007h3.496Zm1-3.003a.75.75 0 0 1 .102 1.493L17.25 11h-4.496a.75.75 0 0 1-.101-1.493l.101-.007h4.496Z" fill="currentColor"/></svg>',
    session:
      '<svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path d="M8.75 13.5a3.251 3.251 0 0 1 3.163 2.498L21.25 16a.75.75 0 0 1 .102 1.493l-.102.007h-9.337a3.251 3.251 0 0 1-6.326 0H2.75a.75.75 0 0 1-.102-1.493L2.75 16h2.837a3.251 3.251 0 0 1 3.163-2.5Zm0 1.5a1.75 1.75 0 0 0-1.652 1.172l-.021.063-.039.148a1.756 1.756 0 0 0 .02.815l.04.13.025.069a1.75 1.75 0 0 0 3.28-.069l.04-.13-.018.06a1.75 1.75 0 0 0 .048-.815l-.03-.137-.02-.07-.047-.134A1.75 1.75 0 0 0 8.75 15Zm6.5-11a3.251 3.251 0 0 1 3.163 2.5h2.837a.75.75 0 0 1 .102 1.493L21.25 8h-2.837a3.251 3.251 0 0 1-6.326 0H2.75a.75.75 0 0 1-.102-1.493L2.75 6.5l9.337-.002A3.251 3.251 0 0 1 15.25 4Zm0 1.5a1.75 1.75 0 0 0-1.652 1.173l-.021.062-.038.148a1.757 1.757 0 0 0 .019.815l.04.13.025.069a1.75 1.75 0 0 0 3.28-.068l.04-.131-.018.06a1.75 1.75 0 0 0 .048-.815l-.03-.137-.02-.07-.047-.134A1.75 1.75 0 0 0 15.25 5.5Z" fill="currentColor"/></svg>',
    signature:
      '<svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path d="M10 2a4 4 0 0 1 4 4v2h1.75A2.25 2.25 0 0 1 18 10.25V11c-.319 0-.637.11-.896.329l-.107.1c-.164.17-.33.323-.496.457L16.5 10.25a.75.75 0 0 0-.75-.75H4.25a.75.75 0 0 0-.75.75v9.5c0 .414.336.75.75.75h9.888a6.024 6.024 0 0 0 1.54 1.5H4.25A2.25 2.25 0 0 1 2 19.75v-9.5A2.25 2.25 0 0 1 4.25 8H6V6a4 4 0 0 1 4-4Zm8.284 10.122c.992 1.036 2.091 1.545 3.316 1.545.193 0 .355.143.392.332l.008.084v2.501c0 2.682-1.313 4.506-3.873 5.395a.385.385 0 0 1-.253 0c-2.476-.86-3.785-2.592-3.87-5.13L14 16.585v-2.5c0-.23.18-.417.4-.417 1.223 0 2.323-.51 3.318-1.545a.389.389 0 0 1 .566 0ZM10 13.5a1.5 1.5 0 1 1 0 3 1.5 1.5 0 0 1 0-3Zm0-10A2.5 2.5 0 0 0 7.5 6v2h5V6A2.5 2.5 0 0 0 10 3.5Z" fill="currentColor"/></svg>',
    raweb:
      '<svg xmlns="http://www.w3.org/2000/svg" width="144" height="144" viewBox="-4 -4 54 54"><rect x="0" y="0" width="20" height="20" rx="4" fill="transparent" stroke="currentColor" stroke-width="3"/><rect x="28" y="0" width="20" height="20" rx="4" fill="transparent" stroke="currentColor" stroke-width="3"/><rect x="0" y="28" width="20" height="20" rx="4" fill="transparent" stroke="currentColor" stroke-width="3"/><circle cx="38" cy="38" r="10" fill="transparent" stroke="currentColor" stroke-width="3"/></svg>',
  };

  const menuItems = computed(() => {
    return (openEditDialog?: () => void) => {
      return [
        {
          name: capitalize(t(`resource.props.sections.raweb`)),
          icon: menuItemIconsMap['raweb'],
          onClick: () => {
            transitionToNewGroup('raweb');
          },
          selected: currentGroup.value === 'raweb',
        } satisfies TreeItem,
        // always show other sections as long as they are not empty or hidden
        ...Object.entries(resourceProperties.value || {})
          .filter(([group]) => !hiddenGroups.includes(group as GroupName))
          .map(([group, properties]) => {
            if (properties === null || Object.keys(properties).length === 0 || group === 'raweb') {
              return null;
            }

            return {
              name: capitalize(t(`resource.props.sections.${group}`)),
              icon: menuItemIconsMap[group as keyof typeof menuItemIconsMap] || '',
              onClick: () => {
                transitionToNewGroup(group as keyof ReturnType<typeof groupResourceProperties>);
              },
              selected: group === currentGroup.value,
            } satisfies TreeItem;
          }),
        authUser.isLocalAdministrator &&
        managementIdentifier &&
        mode === 'view' &&
        allowEditDialog &&
        openEditDialog
          ? ({
              name: 'footer',
              type: 'navigation',
              children: [
                {
                  name: 'hr',
                },
                {
                  name: 'Edit',
                  disabled: managementIdentifier === undefined,
                  onClick: () => {
                    if (openEditDialog) {
                      openEditDialog();
                    }
                  },
                  icon: '<svg viewBox="0 0 24 24"><path d="M21.03 2.97a3.578 3.578 0 0 1 0 5.06L9.062 20a2.25 2.25 0 0 1-.999.58l-5.116 1.395a.75.75 0 0 1-.92-.921l1.395-5.116a2.25 2.25 0 0 1 .58-.999L15.97 2.97a3.578 3.578 0 0 1 5.06 0ZM15 6.06 5.062 16a.75.75 0 0 0-.193.333l-1.05 3.85 3.85-1.05A.75.75 0 0 0 8 18.938L17.94 9 15 6.06Zm2.03-2.03-.97.97L19 7.94l.97-.97a2.079 2.079 0 0 0-2.94-2.94Z" fill="currentColor"/></svg>',
                },
              ],
            } satisfies TreeItem)
          : null,
      ].filter(notEmpty) satisfies TreeItem[];
    };
  });

  const currentGroup = ref<keyof ReturnType<typeof groupResourceProperties> | 'raweb' | null>(null);
  const contentElem = useTemplateRef<HTMLElement>('content-area');
  async function transitionToNewGroup(newGroup: keyof ReturnType<typeof groupResourceProperties> | 'raweb') {
    const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (prefersReducedMotion || !contentElem.value || newGroup === currentGroup.value) {
      currentGroup.value = newGroup;
      return;
    }

    // animate out
    const outAnimation = contentElem.value.animate([{ opacity: 1 }, { opacity: 0 }], {
      duration: 130,
      easing: 'cubic-bezier(0.16, 1, 0.3, 1)',
      fill: 'both',
    });

    // when out animation is finished, switch group
    await outAnimation.finished;
    currentGroup.value = newGroup;
    contentElem.value.scrollTop = 0;

    // animate in the new content
    contentElem.value.animate([{ opacity: 0 }, { opacity: 1 }], {
      duration: 210,
      easing: 'cubic-bezier(0.16, 1, 0.3, 1)',
      fill: 'both',
    });
    contentElem.value.animate(
      [
        { transform: 'translateY(10px)', opacity: 0 },
        { transform: 'translateY(0)', opacity: 1 },
      ],
      {
        duration: 380,
        easing: 'cubic-bezier(0.16, 1, 0.3, 1)',
        fill: 'both',
      }
    );
  }

  // if the current selected group is not available, switch to the next available group
  watch(
    [menuItems, isOpen],
    ([_$menuItems]) => {
      if (!isOpen.value) {
        return;
      }

      const $menuItems = _$menuItems();

      const noMenuItemIsSelected = !$menuItems
        .filter((item) => item.type === undefined)
        .find((item) => item.selected);
      if (!noMenuItemIsSelected) {
        return;
      }

      const defaultGroupKey = defaultGroup;
      const defaultGroupMenuItem = $menuItems.find(
        (item) => item.name === capitalize(t(`resource.props.sections.${defaultGroupKey}`))
      );
      if (defaultGroupMenuItem) {
        transitionToNewGroup(defaultGroupKey);
        return;
      }

      const secondaryDefaultGroupKey = 'remoteapp';
      const secondaryDefaultGroupMenuItem = $menuItems.find(
        (item) => item.name === capitalize(t(`resource.props.sections.${secondaryDefaultGroupKey}`))
      );
      if (secondaryDefaultGroupMenuItem) {
        transitionToNewGroup(secondaryDefaultGroupKey);
        return;
      }

      const nextDefaultGroupKey =
        $menuItems.length > 0
          ? (Object.keys(resourceProperties.value || {})[0] as keyof ReturnType<typeof groupResourceProperties>)
          : null;
      const nextDefaultGroupMenuItem = $menuItems.find(
        (item) => item.name === capitalize(t(`resource.props.sections.${nextDefaultGroupKey}`))
      );
      if (nextDefaultGroupKey && nextDefaultGroupMenuItem) {
        transitionToNewGroup(nextDefaultGroupKey);
        return;
      }
    },
    { immediate: true }
  );

  function handleAfterClose() {
    setTimeout(() => {
      isOpen.value = false;
      currentGroup.value = null;
      emit('afterClose');
    }, 300);
  }

  function isUint8Array(value: any): value is Uint8Array {
    return value instanceof Uint8Array;
  }
  function uint8ArrayToHexString(arr: Uint8Array | undefined): string {
    return Array.from(arr || [], (b) => b.toString(16).padStart(2, '0')).join('');
  }
  function hexStringToUint8Array(hex: string): Uint8Array {
    return new Uint8Array(hex.match(/.{1,2}/g)?.map((byte) => parseInt(byte, 16)) || []);
  }

  function downloadRdpFile() {
    if (!resourceProperties.value) {
      return;
    }
    const flattenedProperties = flattenProperties(resourceProperties.value);
    const rdpFileString = generateRdpFileContents(flattenedProperties);

    const blob = new Blob([rdpFileString], { type: 'application/x-rdp' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${name || 'connection'}.rdp`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }
</script>

<template>
  <ContentDialog
    @after-open="() => (isOpen = true)"
    @close="emit('onClose')"
    @after-close="handleAfterClose"
    :close-on-backdrop-click="mode === 'view'"
    size="maxer"
    max-height="680px"
    fill-height
    class="rdp-properties-content-dialog"
  >
    <template #opener="{ close, open, popoverId }">
      <slot name="default" :close="close" :open="open" :popover-id="popoverId" />
    </template>

    <template #default="{ popoverId, close }">
      <div class="wrapper">
        <div class="nav-area">
          <ManagedResourceEditDialog
            v-if="allowEditDialog"
            :key="popoverId + managementIdentifier"
            :identifier="managementIdentifier || ''"
            :display-name="
              name || resourceProperties?.['remoteapp']['remoteapplicationname:s']?.toString() || ''
            "
            @after-delete="emit('afterRemoveFromRegistry', close)"
            @after-save="emit('afterSaveToRegistry')"
            #default="{ open: openEditDialog }"
            :source="_"
          >
            <NavigationPane
              :menu-items="menuItems(openEditDialog)"
              :header-text="capitalize(t('resource.props.title', { name: '', section: '' }).trim())"
            />
          </ManagedResourceEditDialog>
          <NavigationPane
            v-else
            :menu-items="menuItems()"
            :header-text="capitalize(t('resource.props.title', { name: '', section: '' }).trim())"
          />
        </div>
        <div class="content-area" ref="content-area">
          <TextBlock variant="subtitle" class="title" v-if="currentGroup">
            {{
              t('resource.props.title', {
                name: name || resourceProperties?.['remoteapp']['remoteapplicationname:s'],
                section: currentGroup === 'raweb' ? '' : t(`resource.props.sections.${currentGroup}`),
              })
            }}
          </TextBlock>

          <template v-if="currentGroup === 'raweb'">
            <Field>
              <TextBlock>{{ t('resource.props.type') }}</TextBlock>
              <TextBox
                :value="
                  (() => {
                    if (!resourceProperties) {
                      return t('resource.props.unknownType');
                    }

                    const isRemoteApp = !!resourceProperties?.['remoteapp']['remoteapplicationprogram:s'];

                    if (isRemoteApp) {
                      return capitalize(t('application'));
                    }

                    return capitalize(t('device'));
                  })()
                "
                disabled
              />
            </Field>

            <Field>
              <TextBlock>{{ t('resource.props.location') }}</TextBlock>
              <TextBox
                :value="
                  (() => {
                    if (source === ManagedResourceSource.File) {
                      return 'App_Data/managed-resources';
                    }
                    if (source === ManagedResourceSource.TSAppAllowList) {
                      return 'HKLM\\...Terminal Server\\TSAppAllowList\\Applications';
                    }
                    if (source === ManagedResourceSource.CentralPublishedResourcesApp) {
                      return 'HKLM\\...Terminal Server\\CentralPublishedResources\\...\\Applications';
                    }
                    if (source === UnmanagedResourceSource.UnmanagedFile) {
                      return 'App_Data/resources';
                    }
                    if (source === UnmanagedResourceSource.UnmanagedMultiuserFile) {
                      return 'App_Data/multuser-resources';
                    }
                    return '';
                  })()
                "
                disabled
              />
            </Field>

            <Field>
              <TextBlock>{{ t('resource.props.ts') }}</TextBlock>
              <TextBox
                :value="terminalServer || (() => {
                const address = resourceProperties?.connection['full address:s'] as string | undefined;
                const addressContainsPort = address?.includes(':');
                if (addressContainsPort) {
                  return address
                }
                const port = resourceProperties?.connection['server port:i'];
                return port ? `${address}:${port}` : address || '';
              })()"
                disabled
              />
            </Field>
          </template>

          <template v-else-if="resourceProperties && currentGroup">
            <Field
              v-for="{ key, label, description } in Object.keys(resourceProperties[currentGroup] || {})
                .map((key) => {
                  return {
                    key,
                    label: t(`resource.props.properties.${key.replace(':', '__')}.label`, {
                      defaultValue: key,
                    }),
                    description: t(`resource.props.properties.${key.replace(':', '__')}.description`, {
                      defaultValue: key,
                    }),
                  };
                })
                .sort((a, b) => a.label.localeCompare(b.label))"
              :key="key"
            >
              <TextBlock :title="description" style="cursor: help">
                {{ label }}
              </TextBlock>
              <TextBox
                v-if="key.endsWith('i')"
                :disabled="
                  mode === 'view' || disabledFields.includes(key) || !capabilities.supportsCentralizedPublishing
                "
                :value="resourceProperties[currentGroup][key]?.toString()"
                @update:value="
                  (newValue) => {
                    if (resourceProperties && currentGroup) {
                      resourceProperties[currentGroup][key] = parseInt(newValue);
                    }
                  }
                "
                type="number"
              />
              <TextBox
                v-if="key.endsWith('s')"
                :disabled="
                  mode === 'view' ||
                  key === 'signature:s' ||
                  disabledFields.includes(key) ||
                  !capabilities.supportsCentralizedPublishing
                "
                :value="resourceProperties[currentGroup][key]?.toString()"
                @update:value="
                  (newValue) => {
                    if (resourceProperties && currentGroup) {
                      resourceProperties[currentGroup][key] = newValue;
                    }
                  }
                "
              />
              <TextBox
                v-else-if="key.endsWith('b')"
                :disabled="
                  mode === 'view' || disabledFields.includes(key) || !capabilities.supportsCentralizedPublishing
                "
                :value="uint8ArrayToHexString(isUint8Array(resourceProperties[currentGroup][key]) ? resourceProperties[currentGroup][key] as Uint8Array
                  : undefined)"
                @update:value="
                  (newValue) => {
                    if (resourceProperties && currentGroup) {
                      resourceProperties[currentGroup][key] = hexStringToUint8Array(newValue);
                    }
                  }
                "
                :type="typeof resourceProperties[currentGroup][key] === 'number' ? 'number' : 'string'"
              />
            </Field>
          </template>
        </div>
      </div>
    </template>

    <template #footer-left v-if="mode !== 'view' && capabilities.supportsCentralizedPublishing">
      <Button @click="downloadRdpFile">Download</Button>
    </template>

    <template #footer="{ close }">
      <template v-if="mode === 'view' || !capabilities.supportsCentralizedPublishing">
        <Button @click="close">{{ t('dialog.close') }}</Button>
      </template>
      <template v-else>
        <Button @click="emitChangesAndClose(close)" :disabled="!capabilities.supportsCentralizedPublishing">{{
          t('dialog.ok')
        }}</Button>
        <Button @click="close">{{ t('dialog.cancel') }}</Button>
      </template>
    </template>
  </ContentDialog>
</template>

<style>
  .rdp-properties-content-dialog > .content-dialog-inner > .content-dialog-body {
    padding: 0 !important;
    overflow: hidden !important;
  }
</style>

<style scoped>
  .wrapper {
    display: flex;
    flex-direction: row;
    height: 100%;
    width: 680px;
    position: relative;
  }

  .nav-area {
    background-color: var(--wui-solid-background-base);
  }

  .nav-area :deep(aside) {
    height: 100%;
  }

  .nav-area :deep(aside:not(.collapsed)) {
    width: 160px;
  }

  .content-area {
    padding: var(--inner-padding);
    width: 0;
    flex-grow: 1;
    overflow: auto;
  }

  .title {
    padding-bottom: calc(var(--inner-padding) / 2);
  }
</style>
