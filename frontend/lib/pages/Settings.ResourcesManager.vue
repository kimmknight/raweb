<script setup lang="ts">
  import { Button } from '$components';
  import { ManagedResourceListDialog } from '$dialogs';
  import { useCoreDataStore } from '$stores';
  import { useWebfeedData } from '$utils';
  import { useTranslation } from 'i18next-vue';

  const { needsSignInAgain } = useCoreDataStore();
  const { t } = useTranslation();

  const props = defineProps<{
    refreshWorkspace: () => ReturnType<typeof useWebfeedData>['refresh'];
  }>();

  const isSecureContext = window.isSecureContext;
</script>

<template>
  <ManagedResourceListDialog
    @app-or-desktop-change="props.refreshWorkspace"
    v-if="isSecureContext && !needsSignInAgain"
  >
    <template #default="{ open }">
      <Button @click="open">{{ t('registryApps.manager.open') }}</Button>
    </template>
  </ManagedResourceListDialog>
</template>
