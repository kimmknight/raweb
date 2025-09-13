<script setup lang="ts">
  import { Button, ContentDialog, InfoBar, TextBlock, ToggleSwitch } from '$components';
  import {
    combineTerminalServersModeEnabled,
    favoritesEnabled,
    flatModeEnabled,
    iconBackgroundsEnabled,
    simpleModeEnabled,
    useFavoriteResources,
    useUpdateDetails,
  } from '$utils';
  import { hidePortsEnabled } from '$utils/hidePorts';
  import { onMounted, ref, type UnwrapRef } from 'vue';

  const { update } = defineProps<{
    update: UnwrapRef<ReturnType<typeof useUpdateDetails>['updateDetails']>;
  }>();

  const username = window.__authUser.username;
  const isLocalAdministrator = window.__authUser.isLocalAdministrator;

  // TODO: requestClose: remove this logic once all browsers have supported this for some time
  const canUseDialogs = HTMLDialogElement.prototype.requestClose !== undefined;

  // TODO [Anchors]: Remove this when all major browsers support CSS Anchor Positioning
  const supportsAnchorPositions = CSS.supports('position-area', 'center center');

  const workspaceUrl = `${window.location.origin}${window.__iisBase}webfeed.aspx`;

  const policies = ref(window.__policies);
  const coreVersion = window.__coreVersion;
  const webVersion = (() => {
    const version = window.__webVersion;
    return (
      version.slice(0, 4) +
      '.' +
      version.slice(5, 7) +
      '.' +
      version.slice(8, 10) +
      '.' +
      version.slice(11, 13) +
      version.slice(14, 16)
    );
  })();

  async function findRadcTxtRecord(
    hostname = window.location.hostname
  ): Promise<{ TTL: number; data: string; name: string; type: number; hostname: string } | null> {
    const isValidHostname = /^[a-zA-Z0-9.-]+$/.test(hostname);
    if (!isValidHostname || hostname.split('.').length < 2) {
      return null;
    }

    return await fetch(`https://cloudflare-dns.com/dns-query?name=_msradc.${hostname}&type=TXT`, {
      headers: {
        Accept: 'application/dns-json',
      },
    })
      .then((response) => response.json())
      .then((json) => {
        if (
          json &&
          json.Answer &&
          Array.isArray(json.Answer) &&
          json.Answer.length > 0 &&
          json.Answer[0].type === 16
        ) {
          return {
            hostname,
            ...json.Answer[0],
            data: json.Answer[0].data.slice(1, -1), // remove quotes from TXT record value
          };
        }
        throw new Error('No TXT record found');
      })
      .catch(async () => {
        // if the request fails, that usually means the record
        // does not exist, so we try the parent (sub)domain
        return await findRadcTxtRecord(hostname.split('.').slice(1).join('.'));
      });
  }
  const foundRadcRecord = ref<Awaited<ReturnType<typeof findRadcTxtRecord>>>(null);
  const workspaceEmail = ref<string | null>(null);
  onMounted(async () => {
    foundRadcRecord.value = await findRadcTxtRecord();
    if (username && foundRadcRecord.value && foundRadcRecord.value.data === workspaceUrl) {
      workspaceEmail.value = username + '@' + foundRadcRecord.value.hostname;
    }
  });

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

  function copyWorkspaceUrl(mode: 'url' | 'email' = 'url') {
    navigator.clipboard.writeText(mode === 'url' ? workspaceUrl : workspaceEmail.value || '').catch((err) => {
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
      <TextBlock variant="subtitle">{{ $t('settings.hidePorts.title') }}</TextBlock>
    </div>
    <div class="favorites">
      <TextBlock>
        {{ $t('settings.hidePorts.desc') }}
      </TextBlock>
      <ToggleSwitch v-model="hidePortsEnabled" :disabled="policies?.hidePortsEnabled !== ''">
        {{ $t('settings.hidePorts.switch') }}
      </ToggleSwitch>
    </div>
  </section>
  <section>
    <div class="section-title-row">
      <TextBlock variant="subtitle">{{ $t('settings.workspaceUrl.title') }}</TextBlock>
    </div>
    <div class="worksapce">
      <TextBlock variant="body" tag="div" style="display: block">
        {{ workspaceUrl }}
      </TextBlock>
      <TextBlock variant="body" v-if="workspaceEmail" tag="div" style="display: block">
        {{ workspaceEmail }}
      </TextBlock>
      <div class="button-row">
        <Button @click="copyWorkspaceUrl('url')">{{ $t('settings.workspaceUrl.copy') }}</Button>
        <Button @click="copyWorkspaceUrl('email')" v-if="workspaceEmail">
          {{ $t('settings.workspaceUrl.copyEmail') }}
        </Button>
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
      <div>
        <div>
          <TextBlock> {{ $t('settings.about.coreVersion') }}: {{ coreVersion }} </TextBlock>
        </div>
        <div>
          <TextBlock> {{ $t('settings.about.webVersion') }}: {{ webVersion }} </TextBlock>
        </div>
      </div>
      <div class="updates" v-if="isLocalAdministrator">
        <template v-if="update.loading">
          <TextBlock>
            {{ $t('settings.about.updates.checking') }}
          </TextBlock>
        </template>
        <template v-else-if="update.status === 429">
          <TextBlock>
            {{ $t('settings.about.updates.rateLimited') }}
          </TextBlock>
        </template>
        <template v-else-if="update.details">
          <TextBlock>
            {{ $t('settings.about.updates.available') }}: {{ update.details.name }} ({{
              update.details.version
            }})
          </TextBlock>
          <div class="button-row">
            <ContentDialog size="max" v-if="update.details" :title="update.details.name">
              <template #opener="{ open }">
                <Button @click="() => open()">View details</Button>
              </template>
              <div class="gfm" v-html="update.details.notes"></div>
              <template v-slot:footer="{ close }">
                <Button :href="update.details.html_url" target="_blank">View on GitHub</Button>
                <Button @click="close">Close</Button>
              </template>
            </ContentDialog>
          </div>
        </template>
        <template v-else>
          <TextBlock>
            {{ $t('settings.about.updates.upToDate') }}
          </TextBlock>
        </template>
      </div>
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

  .updates {
    margin-top: 8px;
  }

  .updates .button-row {
    margin-top: 8px;
  }
</style>

<style>
  .gfm {
    font-size: 14px;
    font-family: var(--wui-font-family-text);
    font-size: var(--wui-font-size-body);
    line-height: var(--wui-line-height-body);
    --note-color: light-dark(rgb(79, 140, 201), rgb(79, 140, 201));
    --important-color: light-dark(#8250df, #8957e5);
  }
  .gfm h2 {
    font-size: 16px !important;
    font-weight: 500 !important;
  }
  .gfm h3 {
    font-size: 15px !important;
    font-weight: 500 !important;
  }
  .gfm strong {
    font-weight: 600 !important;
  }
  .gfm .markdown-alert {
    padding: 0.5rem 1rem;
    margin-bottom: 1rem;
    color: inherit;
    border-left: 0.25em solid #3d444d;
  }
  .gfm .markdown-alert > :first-child {
    margin-top: 0;
  }
  .gfm .markdown-alert > :last-child {
    margin-bottom: 0;
  }
  .gfm .markdown-alert .markdown-alert-title {
    display: flex;
    font-weight: 500;
    align-items: center;
    line-height: 1;
  }
  .gfm .octicon {
    fill: currentColor;
    margin-right: 0.5rem;
  }
  .gfm .markdown-alert.markdown-alert-important .markdown-alert-title {
    color: var(--important-color);
  }
  .gfm .markdown-alert.markdown-alert-important {
    border-left-color: var(--important-color);
  }
  .gfm .markdown-alert.markdown-alert-note .markdown-alert-title {
    color: var(--note-color);
  }
  .gfm .markdown-alert.markdown-alert-note {
    border-left-color: var(--note-color);
  }
  .gfm pre {
    overflow: auto;
    user-select: text;
    border: 1px solid var(--wui-control-stroke-default);
    background-color: var(--wui-solid-background-base);
    padding: 12px;
    border-radius: var(--wui-control-corner-radius);
  }
  .gfm code:not(.gfm pre code) {
    background-color: var(--wui-solid-background-base);
    border-radius: var(--wui-control-corner-radius);
    padding: 2px;
  }
  .gfm a {
    color: var(--wui-accent-text-primary);
  }
</style>
