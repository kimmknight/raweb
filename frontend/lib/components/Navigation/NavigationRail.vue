<script setup lang="ts">
  import { AnimatedIcon } from '$components';
  import RailButton from '$components/Navigation/RailButton.vue';
  import { useCoreDataStore } from '$stores';
  import { favoritesEnabled, openHelpPopup } from '$utils';

  const { authUser, docsUrl } = useCoreDataStore();

  // TODO [Anchors]: Remove this when all major browsers support CSS Anchor Positioning
  const supportsAnchorPositions = CSS.supports('position-area', 'center center');

  const { hidden = false } = defineProps<{
    hidden?: boolean;
  }>();
</script>

<template>
  <div class="nav-rail" :class="{ hidden }" :aria-hidden="hidden" :inert="hidden">
    <nav>
      <ul>
        <!-- Favorites -->
        <li v-if="favoritesEnabled && supportsAnchorPositions">
          <RouterLink to="/favorites" custom v-slot="{ href, isActive, navigate }">
            <RailButton :href="href" :active="isActive" @click="navigate">
              <template v-slot:icon>
                <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path
                    d="M10.788 3.103c.495-1.004 1.926-1.004 2.421 0l2.358 4.777 5.273.766c1.107.161 1.549 1.522.748 2.303l-3.816 3.72.901 5.25c.19 1.103-.968 1.944-1.959 1.424l-4.716-2.48-4.715 2.48c-.99.52-2.148-.32-1.96-1.424l.901-5.25-3.815-3.72c-.801-.78-.359-2.142.748-2.303L8.43 7.88l2.358-4.777Zm1.21.936L9.74 8.615a1.35 1.35 0 0 1-1.016.738l-5.05.734 3.654 3.562c.318.31.463.757.388 1.195l-.862 5.03 4.516-2.375a1.35 1.35 0 0 1 1.257 0l4.516 2.374-.862-5.029a1.35 1.35 0 0 1 .388-1.195l3.654-3.562-5.05-.734a1.35 1.35 0 0 1-1.016-.738l-2.259-4.576Z"
                    fill="currentColor"
                  />
                </svg>
              </template>
              <template v-slot:icon-active>
                <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path
                    d="M10.788 3.103c.495-1.004 1.926-1.004 2.421 0l2.358 4.777 5.273.766c1.107.161 1.549 1.522.748 2.303l-3.816 3.72.901 5.25c.19 1.103-.968 1.944-1.959 1.424l-4.716-2.48-4.715 2.48c-.99.52-2.148-.32-1.96-1.424l.901-5.25-3.815-3.72c-.801-.78-.359-2.142.748-2.303L8.43 7.88l2.358-4.777Z"
                    fill="currentColor"
                  />
                </svg>
              </template>
              {{ $t('favorites.title') }}
            </RailButton>
          </RouterLink>
        </li>

        <!-- Devices -->
        <li>
          <RouterLink to="/devices" custom v-slot="{ href, isActive, navigate }">
            <RailButton :href="href" :active="isActive" @click="navigate">
              <template v-slot:icon>
                <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path
                    d="M6.75 22a.75.75 0 0 1-.102-1.493l.102-.007h1.749v-2.498H4.25a2.25 2.25 0 0 1-2.245-2.096L2 15.752V5.25a2.25 2.25 0 0 1 2.096-2.245L4.25 3h15.499a2.25 2.25 0 0 1 2.245 2.096l.005.154v10.502a2.25 2.25 0 0 1-2.096 2.245l-.154.005h-4.25V20.5h1.751a.75.75 0 0 1 .102 1.494L17.25 22H6.75Zm7.248-3.998h-4l.001 2.498h4l-.001-2.498ZM19.748 4.5H4.25a.75.75 0 0 0-.743.648L3.5 5.25v10.502c0 .38.282.694.648.743l.102.007h15.499a.75.75 0 0 0 .743-.648l.007-.102V5.25a.75.75 0 0 0-.648-.743l-.102-.007Z"
                    fill="currentColor"
                  />
                </svg>
              </template>
              <template v-slot:icon-active>
                <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path
                    d="M6.75 22a.75.75 0 0 1-.102-1.493l.102-.007h1.749v-2.498H4.25a2.25 2.25 0 0 1-2.245-2.096L2 15.752V5.25a2.25 2.25 0 0 1 2.096-2.245L4.25 3h15.499a2.25 2.25 0 0 1 2.245 2.096l.005.154v10.502a2.25 2.25 0 0 1-2.096 2.245l-.154.005h-4.25V20.5h1.751a.75.75 0 0 1 .102 1.494L17.25 22H6.75Zm7.248-3.998h-4l.001 2.498h4l-.001-2.498Z"
                    fill="currentColor"
                  />
                </svg>
              </template>
              {{ $t('devices.title') }}
            </RailButton>
          </RouterLink>
        </li>

        <!-- Apps -->
        <li>
          <RouterLink to="/apps" custom v-slot="{ href, isActive, navigate }">
            <RailButton :href="href" :active="isActive" @click="navigate">
              <template v-slot:icon>
                <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path
                    d="m18.492 2.33 3.179 3.179a2.25 2.25 0 0 1 0 3.182l-2.584 2.584A2.25 2.25 0 0 1 21 13.5v5.25A2.25 2.25 0 0 1 18.75 21H5.25A2.25 2.25 0 0 1 3 18.75V5.25A2.25 2.25 0 0 1 5.25 3h5.25a2.25 2.25 0 0 1 2.225 1.915L15.31 2.33a2.25 2.25 0 0 1 3.182 0ZM4.5 18.75c0 .414.336.75.75.75l5.999-.001.001-6.75H4.5v6Zm8.249.749h6.001a.75.75 0 0 0 .75-.75V13.5a.75.75 0 0 0-.75-.75h-6.001v6.75Zm-2.249-15H5.25a.75.75 0 0 0-.75.75v6h6.75v-6a.75.75 0 0 0-.75-.75Zm2.25 4.81v1.94h1.94l-1.94-1.94Zm3.62-5.918-3.178 3.178a.75.75 0 0 0 0 1.061l3.179 3.179a.75.75 0 0 0 1.06 0l3.18-3.179a.75.75 0 0 0 0-1.06l-3.18-3.18a.75.75 0 0 0-1.06 0Z"
                    fill="currentColor"
                  />
                </svg>
              </template>
              <template v-slot:icon-active>
                <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path
                    d="m18.492 2.33 3.179 3.179a2.25 2.25 0 0 1 0 3.182l-2.423 2.422A2.501 2.501 0 0 1 21 13.5v5a2.5 2.5 0 0 1-2.5 2.5h-13A2.5 2.5 0 0 1 3 18.5v-13A2.5 2.5 0 0 1 5.5 3h5c1.121 0 2.07.737 2.387 1.754L15.31 2.33a2.25 2.25 0 0 1 3.182 0ZM11 13H5v5.5a.5.5 0 0 0 .5.5H11v-6Zm7.5 0H13v6h5.5a.5.5 0 0 0 .5-.5v-5a.5.5 0 0 0-.5-.5Zm-4.06-2.001L13 9.559v1.44h1.44Zm-3.94-6h-5a.5.5 0 0 0-.5.5V11h6V5.5a.5.5 0 0 0-.5-.5Z"
                    fill="currentColor"
                  />
                </svg>
              </template>
              {{ $t('apps.title') }}
            </RailButton>
          </RouterLink>
        </li>

        <!-- Client -->
        <!-- <li
          :style="`opacity: ${
            router.currentRoute.value.name === 'webGuacd' ? 1 : 0
          }; transition: opacity var(--wui-control-fast-duration);`"
        >
          <RailButton :active="true">
            <template v-slot:icon>
              <svg viewBox="0 0 192 192" xmlns="http://www.w3.org/2000/svg" fill="none">
                <path
                  stroke="currentColor"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="12"
                  d="M96 170c40.869 0 74-33.131 74-74 0-40.87-33.131-74-74-74-40.87 0-74 33.13-74 74 0 40.869 33.13 74 74 74Z"
                />
                <path
                  stroke="currentColor"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="12"
                  d="M126 52 98 80l28 28M66 84l28 28-28 28"
                />
              </svg>
            </template>
            <template v-slot:icon-active>
              <svg viewBox="0 0 192 192" xmlns="http://www.w3.org/2000/svg" fill="currentColor">
                <path
                  stroke="currentColor"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="12"
                  d="M96 170c40.869 0 74-33.131 74-74 0-40.87-33.131-74-74-74-40.87 0-74 33.13-74 74 0 40.869 33.13 74 74 74Z"
                />
                <path
                  stroke="var(--wui-solid-background-base)"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="12"
                  d="M126 52 98 80l28 28M66 84l28 28-28 28"
                />
              </svg>
            </template>
            {{ $t('client.title') }}
          </RailButton>
        </li> -->
      </ul>
    </nav>

    <div class="bottom">
      <!-- Settings -->
      <RouterLink to="/settings" custom v-slot="{ href, isActive, navigate }">
        <RailButton :href="href" :active="isActive" @click="navigate">
          <template v-slot:icon>
            <AnimatedIcon.Settings :filled="isActive" />
          </template>
          {{ $t('settings.title') }}
        </RailButton>
      </RouterLink>

      <!-- Wiki (external link, not router) -->
      <RailButton :href="docsUrl" target="_blank" @click.prevent="openHelpPopup(docsUrl)">
        <template v-slot:icon>
          <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path
              d="M13.25 7a1 1 0 1 1-2 0 1 1 0 0 1 2 0ZM11.5 9.75v5a.75.75 0 0 0 1.5 0v-5a.75.75 0 0 0-1.5 0Z"
              fill="currentColor"
            />
            <path
              d="M4 4.5A2.5 2.5 0 0 1 6.5 2H18a2.5 2.5 0 0 1 2.5 2.5v14.25a.75.75 0 0 1-.75.75H5.5a1 1 0 0 0 1 1h13.25a.75.75 0 0 1 0 1.5H6.5A2.5 2.5 0 0 1 4 19.5v-15ZM19 18V4.5a1 1 0 0 0-1-1H6.5a1 1 0 0 0-1 1V18H19Z"
              fill="currentColor"
            />
          </svg>
        </template>
        {{ $t('wiki.title') }}
      </RailButton>
    </div>
  </div>
</template>

<style scoped>
  .nav-rail {
    --width: 72px;
    --button-size: 64px;

    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: space-between;
    width: var(--width);
    height: 100%;
    padding: 0 0 4px 0;
    box-sizing: border-box;
    view-transition-name: disabled;
  }

  .nav-rail.hidden {
    --width: 0;
    opacity: 0;
    touch-action: none;
    pointer-events: none;
  }

  nav {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
  }
  nav ul,
  .bottom {
    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 4px;
  }
  nav li {
    margin: 0;
    padding: 0;
  }
  nav li a,
  nav li button {
    text-decoration: none;
    appearance: none;
    border: none;
    color: #000;
    font-size: 14px;
    display: flex;
    align-items: center;
    justify-content: center;
    inline-size: var(--button-size);
    block-size: var(--button-size);
  }
</style>
