<script setup lang="ts">
  import RailButton from '$components/Navigation/RailButton.vue';
  import { useCoreDataStore } from '$stores';
  import { favoritesEnabled } from '$utils';

  const { authUser } = useCoreDataStore();

  // TODO [Anchors]: Remove this when all major browsers support CSS Anchor Positioning
  const supportsAnchorPositions = CSS.supports('position-area', 'center center');

  const base = document.querySelector('base')?.getAttribute('href') || '/';
  const docsUrl = __DOCS_EXCLUDED__ ? 'https://kimmknight.github.io/raweb/wiki-redirect' : base + 'docs';
  function openHelpPopup() {
    const popup = window.open(docsUrl, 'help', 'width=1000,height=600,menubar=0,status=0');
    if (popup) {
      popup.focus();
    } else {
      alert('Please allow popups for this application');
    }
  }
</script>

<template>
  <div class="nav-rail">
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
      </ul>
    </nav>

    <div class="bottom">
      <!-- Policies -->
      <RouterLink
        v-if="authUser.isLocalAdministrator"
        to="/policies"
        custom
        v-slot="{ href, isActive, navigate }"
      >
        <RailButton :href="href" :active="isActive" @click="navigate">
          <template v-slot:icon>
            <!-- policies svg normal -->
            <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path
                d="M12 1.999c5.487 0 9.942 4.419 10 9.892a6.064 6.064 0 0 1-1.525-.566 8.486 8.486 0 0 0-.21-1.326h-1.942a1.618 1.618 0 0 0-.648 0h-.769c.012.123.023.246.032.37a7.858 7.858 0 0 1-1.448.974c-.016-.46-.047-.908-.094-1.343H8.604a18.968 18.968 0 0 0 .135 5H12v1.5H9.06c.653 2.414 1.786 4.002 2.94 4.002.276 0 .551-.091.819-.263a6.938 6.938 0 0 0 1.197 1.56c-.652.133-1.326.203-2.016.203-5.524 0-10.002-4.478-10.002-10.001C1.998 6.477 6.476 1.998 12 1.998ZM7.508 16.501H4.785a8.532 8.532 0 0 0 4.095 3.41c-.523-.82-.954-1.846-1.27-3.015l-.102-.395ZM7.093 10H3.735l-.004.017a8.524 8.524 0 0 0-.233 1.984c0 1.056.193 2.067.545 3h3.173a20.301 20.301 0 0 1-.218-3c0-.684.033-1.354.095-2.001Zm1.788-5.91-.023.008A8.531 8.531 0 0 0 4.25 8.5h3.048c.313-1.752.86-3.278 1.583-4.41ZM12 3.499l-.116.005C10.618 3.62 9.396 5.622 8.828 8.5h6.343c-.566-2.87-1.784-4.869-3.045-4.995L12 3.5Zm3.12.59.106.175c.67 1.112 1.178 2.572 1.475 4.237h3.048a8.533 8.533 0 0 0-4.338-4.29l-.291-.121Zm7.38 8.888c-1.907-.172-3.434-1.287-4.115-1.87a.601.601 0 0 0-.77 0c-.682.583-2.21 1.698-4.116 1.87a.538.538 0 0 0-.5.523V17c0 4.223 4.094 5.716 4.873 5.962a.42.42 0 0 0 .255 0c.78-.246 4.872-1.74 4.872-5.962v-3.5a.538.538 0 0 0-.5-.523Z"
                fill="currentColor"
              />
            </svg>
          </template>
          <template v-slot:icon-active>
            <!-- policies svg active -->
            <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path
                d="M8.904 16.5H12v.5c0 1.728.59 3.072 1.383 4.09-.439.584-.912.91-1.383.91-1.197 0-2.401-2.095-3.038-5.205l-.058-.294Zm-.23-1.499H12v-1.5c0-.85.676-1.453 1.409-1.52.773-.07 1.484-.33 2.084-.64a25.619 25.619 0 0 0-.069-1.34H8.575l-.033.488A25.982 25.982 0 0 0 8.674 15Zm13.127-5c.124.611.193 1.243.2 1.89-1.33-.287-2.416-1.073-2.965-1.544a1.576 1.576 0 0 0-.707-.346h3.472ZM3.065 16.5h4.306c.365 2.082.983 3.854 1.792 5.093a10.028 10.028 0 0 1-5.951-4.814l-.147-.279Zm-.867-6.5h4.87a28.205 28.205 0 0 0 .033 4.42l.058.58H2.456a10.048 10.048 0 0 1-.258-5Zm12.746-7.424-.108-.17A10.027 10.027 0 0 1 21.372 8.5H16.78c-.316-2.416-.957-4.492-1.837-5.923ZM9.04 2.444l.122-.037c-.88 1.35-1.535 3.329-1.883 5.654l-.061.44H2.628A10.028 10.028 0 0 1 9.04 2.443ZM12 2.002c1.318 0 2.645 2.542 3.214 6.183l.046.315H8.74c.54-3.809 1.904-6.498 3.26-6.498Zm10.5 10.975c-1.907-.172-3.434-1.287-4.115-1.87a.601.601 0 0 0-.77 0c-.682.583-2.21 1.698-4.116 1.87a.538.538 0 0 0-.5.523V17c0 4.223 4.094 5.716 4.873 5.962a.42.42 0 0 0 .255 0c.78-.246 4.872-1.74 4.872-5.962v-3.5a.538.538 0 0 0-.5-.523Z"
                fill="currentColor"
              />
            </svg>
          </template>
          {{ $t('policies.title') }}
        </RailButton>
      </RouterLink>

      <!-- Settings -->
      <RouterLink to="/settings" custom v-slot="{ href, isActive, navigate }">
        <RailButton :href="href" :active="isActive" @click="navigate">
          <template v-slot:icon>
            <!-- settings svg normal -->
            <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path
                d="M12.012 2.25c.734.008 1.465.093 2.182.253a.75.75 0 0 1 .582.649l.17 1.527a1.384 1.384 0 0 0 1.927 1.116l1.401-.615a.75.75 0 0 1 .85.174 9.792 9.792 0 0 1 2.204 3.792.75.75 0 0 1-.271.825l-1.242.916a1.381 1.381 0 0 0 0 2.226l1.243.915a.75.75 0 0 1 .272.826 9.797 9.797 0 0 1-2.204 3.792.75.75 0 0 1-.848.175l-1.407-.617a1.38 1.38 0 0 0-1.926 1.114l-.169 1.526a.75.75 0 0 1-.572.647 9.518 9.518 0 0 1-4.406 0 .75.75 0 0 1-.572-.647l-.168-1.524a1.382 1.382 0 0 0-1.926-1.11l-1.406.616a.75.75 0 0 1-.849-.175 9.798 9.798 0 0 1-2.204-3.796.75.75 0 0 1 .272-.826l1.243-.916a1.38 1.38 0 0 0 0-2.226l-1.243-.914a.75.75 0 0 1-.271-.826 9.793 9.793 0 0 1 2.204-3.792.75.75 0 0 1 .85-.174l1.4.615a1.387 1.387 0 0 0 1.93-1.118l.17-1.526a.75.75 0 0 1 .583-.65c.717-.159 1.45-.243 2.201-.252Zm0 1.5a9.135 9.135 0 0 0-1.354.117l-.109.977A2.886 2.886 0 0 1 6.525 7.17l-.898-.394a8.293 8.293 0 0 0-1.348 2.317l.798.587a2.881 2.881 0 0 1 0 4.643l-.799.588c.32.842.776 1.626 1.348 2.322l.905-.397a2.882 2.882 0 0 1 4.017 2.318l.11.984c.889.15 1.798.15 2.687 0l.11-.984a2.881 2.881 0 0 1 4.018-2.322l.905.396a8.296 8.296 0 0 0 1.347-2.318l-.798-.588a2.881 2.881 0 0 1 0-4.643l.796-.587a8.293 8.293 0 0 0-1.348-2.317l-.896.393a2.884 2.884 0 0 1-4.023-2.324l-.11-.976a8.988 8.988 0 0 0-1.333-.117ZM12 8.25a3.75 3.75 0 1 1 0 7.5 3.75 3.75 0 0 1 0-7.5Zm0 1.5a2.25 2.25 0 1 0 0 4.5 2.25 2.25 0 0 0 0-4.5Z"
                fill="currentColor"
              />
            </svg>
          </template>
          <template v-slot:icon-active>
            <!-- settings svg active -->
            <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path
                d="M12.012 2.25c.734.008 1.465.093 2.182.253a.75.75 0 0 1 .582.649l.17 1.527a1.384 1.384 0 0 0 1.927 1.116l1.401-.615a.75.75 0 0 1 .85.174 9.792 9.792 0 0 1 2.204 3.792.75.75 0 0 1-.271.825l-1.242.916a1.381 1.381 0 0 0 0 2.226l1.243.915a.75.75 0 0 1 .272.826 9.797 9.797 0 0 1-2.204 3.792.75.75 0 0 1-.848.175l-1.407-.617a1.38 1.38 0 0 0-1.926 1.114l-.169 1.526a.75.75 0 0 1-.572.647 9.518 9.518 0 0 1-4.406 0 .75.75 0 0 1-.572-.647l-.168-1.524a1.382 1.382 0 0 0-1.926-1.11l-1.406.616a.75.75 0 0 1-.849-.175 9.798 9.798 0 0 1-2.204-3.796.75.75 0 0 1 .272-.826l1.243-.916a1.38 1.38 0 0 0 0-2.226l-1.243-.914a.75.75 0 0 1-.271-.826 9.793 9.793 0 0 1 2.204-3.792.75.75 0 0 1 .85-.174l1.4.615a1.387 1.387 0 0 0 1.93-1.118l.17-1.526a.75.75 0 0 1 .583-.65c.717-.159 1.45-.243 2.201-.252ZM12 9a3 3 0 1 0 0 6 3 3 0 0 0 0-6Z"
                fill="currentColor"
              />
            </svg>
          </template>
          {{ $t('settings.title') }}
        </RailButton>
      </RouterLink>

      <!-- Wiki (external link, not router) -->
      <RailButton :href="docsUrl" target="_blank" @click.prevent="openHelpPopup">
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
  nav li a {
    text-decoration: none;
    color: #000;
    font-size: 14px;
    display: flex;
    align-items: center;
    justify-content: center;
    inline-size: var(--button-size);
    block-size: var(--button-size);
  }
</style>
