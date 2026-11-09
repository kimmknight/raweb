<script setup lang="ts">
  import { MenuFlyoutDivider, MenuFlyoutItem } from '$components/MenuFlyout';
  import { checkmark } from '$icons';
  import { useCoreDataStore } from '$stores';

  const { terminalServerAliases } = useCoreDataStore();

  const { allTerminalServers } = defineProps<{ allTerminalServers: string[] }>();

  const terminalServersFilter = defineModel<string[]>('terminalServersFilter', { required: true });

  function toggle(terminalServer: string) {
    if (terminalServersFilter.value.includes(terminalServer)) {
      terminalServersFilter.value = terminalServersFilter.value.filter((ts) => ts !== terminalServer);
    } else {
      terminalServersFilter.value = [...terminalServersFilter.value, terminalServer];
    }
  }
</script>

<template>
  <MenuFlyoutItem
    v-for="terminalServer in allTerminalServers"
    :key="terminalServer"
    :indented="!terminalServersFilter.includes(terminalServer)"
    @click="toggle(terminalServer)"
  >
    <template v-slot:icon v-if="terminalServersFilter.includes(terminalServer)">
      <span v-swap="checkmark"></span>
    </template>
    {{ terminalServerAliases[terminalServer] ?? terminalServer }}
  </MenuFlyoutItem>
  <MenuFlyoutDivider />
  <MenuFlyoutItem @click="() => (terminalServersFilter = [])" :indented="terminalServersFilter.length > 0">
    <template v-slot:icon v-if="terminalServersFilter.length === 0">
      <span v-swap="checkmark"></span>
    </template>
    {{ $t('actions.ts.all') }}
  </MenuFlyoutItem>
</template>
