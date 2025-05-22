<script setup lang="ts">
  import { Button, InfoBar, TextBlock, ToggleSwitch } from '$components';
  import {
    combineTerminalServersModeEnabled,
    favoritesEnabled,
    flatModeEnabled,
    iconBackgroundsEnabled,
    simpleModeEnabled,
    useFavoriteResources,
  } from '$utils';
  import { ref } from 'vue';

  // TODO: requestClose: remove this logic once all browsers have supported this for some time
  const canUseDialogs = HTMLDialogElement.prototype.requestClose !== undefined;

  // TODO [Anchors]: Remove this when all major browsers support CSS Anchor Positioning
  const supportsAnchorPositions = CSS.supports('position-area', 'center center');

  const workspaceUrl = `${window.location.origin}${window.__iisBase}webfeed.aspx`;

  const policies = ref(window.__policies);

  const { favoriteResources } = useFavoriteResources();

  function exportFavorites() {
    const favorites = JSON.stringify(favoriteResources.value, null, 2);
    const blob = new Blob([favorites], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'favorites.json';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }

  function importFavorites() {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'application/json';
    input.onchange = (event) => {
      const file = (event.target as HTMLInputElement).files?.[0];
      if (!file) return;
      const reader = new FileReader();
      reader.onload = (e) => {
        try {
          const favorites = JSON.parse(e.target?.result as string);
          if (!Array.isArray(favorites)) {
            throw new Error('Invalid favorites format. Expected an array.');
          }
          if (!favorites.every((fav) => Array.isArray(fav) && fav.length === 3)) {
            throw new Error(
              'Invalid favorites format. Each favorite should be an array of [id, type, terminalServerId].'
            );
          }
          favoriteResources.value = favorites;
        } catch (error) {
          console.error('Error parsing favorites:', error);
        }
        document.body.removeChild(input);
      };
      reader.readAsText(file);
    };
    input.click();
  }

  function copyWorkspaceUrl() {
    navigator.clipboard.writeText(workspaceUrl).catch((err) => {
      console.error('Failed to copy workspace URL: ', err);
    });
  }
</script>

<template>
  <div class="titlebar-row">
    <TextBlock variant="title">Settings</TextBlock>
  </div>
  <section>
    <div class="section-title-row">
      <TextBlock variant="subtitle">Favorites</TextBlock>
    </div>
    <div class="favorites">
      <InfoBar severity="caution" v-if="!supportsAnchorPositions">
        This setting is disabled beacuse your browser does not support CSS Anchor Positioning.
      </InfoBar>
      <ToggleSwitch
        v-model="favoritesEnabled"
        :disabled="simpleModeEnabled || policies?.favoritesEnabled !== '' || !supportsAnchorPositions"
      >
        Enable favorites
      </ToggleSwitch>
      <div class="button-row">
        <Button @click="exportFavorites" :disabled="!supportsAnchorPositions">Export</Button>
        <Button @click="importFavorites" :disabled="!supportsAnchorPositions">Import</Button>
      </div>
    </div>
  </section>
  <section>
    <div class="section-title-row">
      <TextBlock variant="subtitle">Flatten folders</TextBlock>
    </div>
    <div class="favorites">
      <TextBlock>
        On views that support folders, flatten folders to show all apps and desktops in a single list.
      </TextBlock>
      <ToggleSwitch v-model="flatModeEnabled" :disabled="policies?.flatModeEnabled !== ''">
        Enable flat mode
      </ToggleSwitch>
    </div>
  </section>
  <section>
    <div class="section-title-row">
      <TextBlock variant="subtitle">Icon backgrounds</TextBlock>
    </div>
    <div class="favorites">
      <TextBlock>
        Add a square background with padding to app and desktop icons for better visibility.
      </TextBlock>
      <ToggleSwitch v-model="iconBackgroundsEnabled" :disabled="policies?.iconBackgroundsEnabled !== ''">
        Enable icon backgrounds
      </ToggleSwitch>
    </div>
  </section>
  <section>
    <div class="section-title-row">
      <TextBlock variant="subtitle">Combine apps across servers</TextBlock>
    </div>
    <div class="favorites">
      <InfoBar severity="caution" v-if="!canUseDialogs">
        This setting is disabled because your browser does not support the <code>requestClose()</code> method on
        <code>&lt;dialog &#47;&gt;</code> elements
      </InfoBar>
      <TextBlock>
        Show only one icon for each app, regardless of the number of terminal servers they are hosted on. If
        multiple terminal servers are available, a prompt to select one will be shown when launching the app.
      </TextBlock>
      <ToggleSwitch
        v-model="combineTerminalServersModeEnabled"
        :disabled="policies?.combineTerminalServersModeEnabled !== '' || !canUseDialogs"
      >
        Enable combined apps
      </ToggleSwitch>
    </div>
  </section>
  <section>
    <div class="section-title-row">
      <TextBlock variant="subtitle">Simple mode</TextBlock>
    </div>
    <div class="favorites">
      <TextBlock>
        Enable simple mode to use a compact, combined, single-page list of apps and desktops.
      </TextBlock>
      <TextBlock>
        The flatten folders option is supported. All other pages and features will be disabled.
      </TextBlock>
      <ToggleSwitch v-model="simpleModeEnabled" :disabled="policies?.simpleModeEnabled !== ''">
        Enable simple mode
      </ToggleSwitch>
    </div>
  </section>
  <section>
    <div class="section-title-row">
      <TextBlock variant="subtitle">Workspace URL</TextBlock>
    </div>
    <div class="worksapce">
      <TextBlock variant="body">{{ workspaceUrl }}</TextBlock>
      <div class="button-row">
        <Button @click="copyWorkspaceUrl">Copy workspace URL</Button>
      </div>
    </div>
  </section>
  <section>
    <div class="section-title-row">
      <TextBlock variant="subtitle">About</TextBlock>
    </div>
    <div class="about">
      <div class="logo">
        <svg
          xmlns="http://www.w3.org/2000/svg"
          width="48"
          height="48"
          viewBox="0 0 64 64"
          class="splash-app-logo"
        >
          <!-- Transparent background -->
          <rect width="64" height="64" fill="none" />

          <!-- Grid of apps -->
          <rect x="8" y="8" width="20" height="20" rx="4" fill="#42A5F5" />
          <rect x="36" y="8" width="20" height="20" rx="4" fill="#FFCA28" />
          <rect x="8" y="36" width="20" height="20" rx="4" fill="#EF5350" />
          <circle cx="46" cy="46" r="10" fill="#66BB6A" />
        </svg>
        <TextBlock variant="subtitle">RemoteApps</TextBlock>
      </div>
      <TextBlock>
        A web interface for your RemoteApps and Desktops hosted on Windows 10, 11 and Server. Powered by RAWeb.
      </TextBlock>
      <Button variant="hyperlink" href="https://github.com/kimmknight/raweb">Learn more on GitHub</Button>
    </div>
  </section>
</template>

<style scoped>
  .titlebar-row,
  section {
    user-select: none;
  }

  section {
    margin: 24px 0 8px 0;
  }
  section:last-of-type {
    padding-bottom: 36px;
  }

  .section-title-row {
    margin: 24px 0 8px 0;
  }

  .button-row {
    display: flex;
    flex-direction: row;
    gap: 8px;
  }

  .favorites {
    display: flex;
    flex-direction: column;
    gap: 8px;
    margin-bottom: 8px;
  }

  .worksapce .button-row {
    margin-top: 8px;
  }

  .about {
    background-color: var(--wui-card-background-default);
    border: 1px solid var(--wui-card-stroke-default);
    border-radius: var(--wui-overlay-corner-radius);
    padding: 16px;
  }

  .about .logo {
    display: flex;
    flex-direction: row;
    align-items: center;
    gap: 16px;
    margin-bottom: 8px;
  }
</style>
