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

  const isLocalAdministrator = window.__authUser.isLocalAdministrator;

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
    <TextBlock variant="title">{{ $t('settings.title') }}</TextBlock>
  </div>
  <section>
    <div class="section-title-row">
      <TextBlock variant="subtitle">{{ $t('settings.favorites.title') }}</TextBlock>
    </div>
    <div class="favorites">
      <InfoBar severity="caution" v-if="!supportsAnchorPositions">
        {{ $t('settings.favorites.disabledNoAnchorPos') }}
      </InfoBar>
      <ToggleSwitch
        v-model="favoritesEnabled"
        :disabled="simpleModeEnabled || policies?.favoritesEnabled !== '' || !supportsAnchorPositions"
      >
        {{ $t('settings.favorites.switch') }}
      </ToggleSwitch>
      <div class="button-row">
        <Button @click="exportFavorites" :disabled="!supportsAnchorPositions">{{
          $t('settings.favorites.export')
        }}</Button>
        <Button @click="importFavorites" :disabled="!supportsAnchorPositions">{{
          $t('settings.favorites.import')
        }}</Button>
      </div>
    </div>
  </section>
  <section>
    <div class="section-title-row">
      <TextBlock variant="subtitle">{{ $t('settings.flatMode.title') }}</TextBlock>
    </div>
    <div class="favorites">
      <TextBlock>
        {{ $t('settings.flatMode.desc') }}
      </TextBlock>
      <ToggleSwitch v-model="flatModeEnabled" :disabled="policies?.flatModeEnabled !== ''">
        {{ $t('settings.flatMode.switch') }}
      </ToggleSwitch>
    </div>
  </section>
  <section>
    <div class="section-title-row">
      <TextBlock variant="subtitle">{{ $t('settings.iconBackgrounds.title') }}</TextBlock>
    </div>
    <div class="favorites">
      <TextBlock>
        {{ $t('settings.iconBackgrounds.desc') }}
      </TextBlock>
      <ToggleSwitch v-model="iconBackgroundsEnabled" :disabled="policies?.iconBackgroundsEnabled !== ''">
        {{ $t('settings.iconBackgrounds.switch') }}
      </ToggleSwitch>
    </div>
  </section>
  <section>
    <div class="section-title-row">
      <TextBlock variant="subtitle">{{ $t('settings.combineTerminalServersMode.title') }}</TextBlock>
    </div>
    <div class="favorites">
      <InfoBar severity="caution" v-if="!canUseDialogs">
        <span v-html="$t('settings.combineTerminalServersMode.disabledNoDialogs')"></span>
      </InfoBar>
      <TextBlock>
        {{ $t('settings.combineTerminalServersMode.desc') }}
      </TextBlock>
      <ToggleSwitch
        v-model="combineTerminalServersModeEnabled"
        :disabled="policies?.combineTerminalServersModeEnabled !== '' || !canUseDialogs"
      >
        {{ $t('settings.combineTerminalServersMode.switch') }}
      </ToggleSwitch>
    </div>
  </section>
  <section>
    <div class="section-title-row">
      <TextBlock variant="subtitle">{{ $t('settings.simpleMode.title') }}</TextBlock>
    </div>
    <div class="favorites">
      <TextBlock>
        {{ $t('settings.simpleMode.desc') }}
      </TextBlock>
      <TextBlock>
        {{ $t('settings.simpleMode.desc2') }}
      </TextBlock>
      <ToggleSwitch v-model="simpleModeEnabled" :disabled="policies?.simpleModeEnabled !== ''">
        {{ $t('settings.simpleMode.switch') }}
      </ToggleSwitch>
    </div>
  </section>
  <section>
    <div class="section-title-row">
      <TextBlock variant="subtitle">{{ $t('settings.workspaceUrl.title') }}</TextBlock>
    </div>
    <div class="worksapce">
      <TextBlock variant="body">{{ workspaceUrl }}</TextBlock>
      <div class="button-row">
        <Button @click="copyWorkspaceUrl">{{ $t('settings.workspaceUrl.copy') }}</Button>
      </div>
    </div>
  </section>
  <section>
    <div class="section-title-row">
      <TextBlock variant="subtitle">{{ $t('settings.about.title') }}</TextBlock>
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
        <TextBlock variant="subtitle">{{ $t('appName') }}</TextBlock>
      </div>
      <TextBlock>
        {{ $t('settings.about.appDesc') }}
      </TextBlock>
      <Button variant="hyperlink" href="https://github.com/kimmknight/raweb">{{
        $t('settings.about.learnMore')
      }}</Button>
    </div>
    <Button style="margin-top: 8px" href="#policies" v-if="simpleModeEnabled && isLocalAdministrator">
      Manage policies
    </Button>
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
