<script setup lang="ts">
  import { AnimatedIcon, MenuFlyout, MenuFlyoutDivider, MenuFlyoutItem } from '$components';
  import { AnimatedNavigationItemIndicator } from '$components/Navigation/AnimatedNavigationItemIndicator';
  import RailButton from '$components/Navigation/RailButton.vue';
  import { BulkImportDialog, ManagedResourceCreateDiscoveryDialog, showConfirm } from '$dialogs';
  import { useCoreDataStore } from '$stores';
  import {
    favoritesEnabled,
    openHelpPopup,
    pickAnyResourceFile,
    PreventableEvent,
    useWebfeedData,
  } from '$utils';
  import { useTranslation } from 'i18next-vue';
  import { storeToRefs } from 'pinia';
  import { useRouter } from 'vue-router';

  const { docsUrl } = useCoreDataStore();
  const { authUser, needsSignInAgain, capabilities } = storeToRefs(useCoreDataStore());
  const { t } = useTranslation();
  const router = useRouter();

  // TODO [Anchors]: Remove this when all major browsers support CSS Anchor Positioning
  const supportsAnchorPositions = CSS.supports('position-area', 'center center');

  const { hidden = false, refreshWorkspace } = defineProps<{
    hidden?: boolean;
    refreshWorkspace: ReturnType<typeof useWebfeedData>['refresh'];
  }>();

  const isSecureContext = window.isSecureContext;
  const randomUUID = isSecureContext
    ? crypto.randomUUID.bind(crypto)
    : () => {
        throw new Error('crypto.randomUUID is not available in an insecure context');
      };

  async function handleAppOrDesktopChange(event: PreventableEvent<{ next: () => void }>) {
    event.preventDefault();
    await refreshWorkspace();

    // wrap in setTimeout so that the updated resources list can fully render
    // before the dialog is closed
    setTimeout(() => {
      event.detail.next();
    }, 0);
  }
</script>

<template>
  <AnimatedNavigationItemIndicator.Track
    class="nav-rail nav-rail-flex"
    :class="{ hidden }"
    :aria-hidden="hidden"
    :inert="hidden"
  >
    <nav>
      <ul>
        <!-- Favorites -->
        <li v-if="favoritesEnabled && supportsAnchorPositions">
          <RouterLink to="/favorites" custom v-slot="{ href, isActive, navigate }">
            <AnimatedNavigationItemIndicator.Selectable :selected="isActive" :indicatorHeight="24">
              <RailButton :href="href" :active="isActive" @click="navigate">
                <template v-slot:icon>
                  <svg
                    width="24"
                    height="24"
                    fill="none"
                    viewBox="0 0 24 24"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      d="M10.788 3.103c.495-1.004 1.926-1.004 2.421 0l2.358 4.777 5.273.766c1.107.161 1.549 1.522.748 2.303l-3.816 3.72.901 5.25c.19 1.103-.968 1.944-1.959 1.424l-4.716-2.48-4.715 2.48c-.99.52-2.148-.32-1.96-1.424l.901-5.25-3.815-3.72c-.801-.78-.359-2.142.748-2.303L8.43 7.88l2.358-4.777Zm1.21.936L9.74 8.615a1.35 1.35 0 0 1-1.016.738l-5.05.734 3.654 3.562c.318.31.463.757.388 1.195l-.862 5.03 4.516-2.375a1.35 1.35 0 0 1 1.257 0l4.516 2.374-.862-5.029a1.35 1.35 0 0 1 .388-1.195l3.654-3.562-5.05-.734a1.35 1.35 0 0 1-1.016-.738l-2.259-4.576Z"
                      fill="currentColor"
                    />
                  </svg>
                </template>
                <template v-slot:icon-active>
                  <svg
                    width="24"
                    height="24"
                    fill="none"
                    viewBox="0 0 24 24"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      d="M10.788 3.103c.495-1.004 1.926-1.004 2.421 0l2.358 4.777 5.273.766c1.107.161 1.549 1.522.748 2.303l-3.816 3.72.901 5.25c.19 1.103-.968 1.944-1.959 1.424l-4.716-2.48-4.715 2.48c-.99.52-2.148-.32-1.96-1.424l.901-5.25-3.815-3.72c-.801-.78-.359-2.142.748-2.303L8.43 7.88l2.358-4.777Z"
                      fill="currentColor"
                    />
                  </svg>
                </template>
                {{ $t('favorites.title') }}
              </RailButton>
            </AnimatedNavigationItemIndicator.Selectable>
          </RouterLink>
        </li>

        <!-- Devices -->
        <li>
          <RouterLink to="/devices" custom v-slot="{ href, isActive, navigate }">
            <AnimatedNavigationItemIndicator.Selectable :selected="isActive" :indicatorHeight="24">
              <RailButton :href="href" :active="isActive" @click="navigate">
                <template v-slot:icon>
                  <svg
                    width="24"
                    height="24"
                    fill="none"
                    viewBox="0 0 24 24"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      d="M6.75 22a.75.75 0 0 1-.102-1.493l.102-.007h1.749v-2.498H4.25a2.25 2.25 0 0 1-2.245-2.096L2 15.752V5.25a2.25 2.25 0 0 1 2.096-2.245L4.25 3h15.499a2.25 2.25 0 0 1 2.245 2.096l.005.154v10.502a2.25 2.25 0 0 1-2.096 2.245l-.154.005h-4.25V20.5h1.751a.75.75 0 0 1 .102 1.494L17.25 22H6.75Zm7.248-3.998h-4l.001 2.498h4l-.001-2.498ZM19.748 4.5H4.25a.75.75 0 0 0-.743.648L3.5 5.25v10.502c0 .38.282.694.648.743l.102.007h15.499a.75.75 0 0 0 .743-.648l.007-.102V5.25a.75.75 0 0 0-.648-.743l-.102-.007Z"
                      fill="currentColor"
                    />
                  </svg>
                </template>
                <template v-slot:icon-active>
                  <svg
                    width="24"
                    height="24"
                    fill="none"
                    viewBox="0 0 24 24"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      d="M6.75 22a.75.75 0 0 1-.102-1.493l.102-.007h1.749v-2.498H4.25a2.25 2.25 0 0 1-2.245-2.096L2 15.752V5.25a2.25 2.25 0 0 1 2.096-2.245L4.25 3h15.499a2.25 2.25 0 0 1 2.245 2.096l.005.154v10.502a2.25 2.25 0 0 1-2.096 2.245l-.154.005h-4.25V20.5h1.751a.75.75 0 0 1 .102 1.494L17.25 22H6.75Zm7.248-3.998h-4l.001 2.498h4l-.001-2.498Z"
                      fill="currentColor"
                    />
                  </svg>
                </template>
                {{ $t('devices.title') }}
              </RailButton>
            </AnimatedNavigationItemIndicator.Selectable>
          </RouterLink>
        </li>

        <!-- Apps -->
        <li>
          <RouterLink to="/apps" custom v-slot="{ href, isActive, navigate }">
            <AnimatedNavigationItemIndicator.Selectable :selected="isActive" :indicatorHeight="24">
              <RailButton :href="href" :active="isActive" @click="navigate">
                <template v-slot:icon>
                  <svg
                    width="24"
                    height="24"
                    fill="none"
                    viewBox="0 0 24 24"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      d="m18.492 2.33 3.179 3.179a2.25 2.25 0 0 1 0 3.182l-2.584 2.584A2.25 2.25 0 0 1 21 13.5v5.25A2.25 2.25 0 0 1 18.75 21H5.25A2.25 2.25 0 0 1 3 18.75V5.25A2.25 2.25 0 0 1 5.25 3h5.25a2.25 2.25 0 0 1 2.225 1.915L15.31 2.33a2.25 2.25 0 0 1 3.182 0ZM4.5 18.75c0 .414.336.75.75.75l5.999-.001.001-6.75H4.5v6Zm8.249.749h6.001a.75.75 0 0 0 .75-.75V13.5a.75.75 0 0 0-.75-.75h-6.001v6.75Zm-2.249-15H5.25a.75.75 0 0 0-.75.75v6h6.75v-6a.75.75 0 0 0-.75-.75Zm2.25 4.81v1.94h1.94l-1.94-1.94Zm3.62-5.918-3.178 3.178a.75.75 0 0 0 0 1.061l3.179 3.179a.75.75 0 0 0 1.06 0l3.18-3.179a.75.75 0 0 0 0-1.06l-3.18-3.18a.75.75 0 0 0-1.06 0Z"
                      fill="currentColor"
                    />
                  </svg>
                </template>
                <template v-slot:icon-active>
                  <svg
                    width="24"
                    height="24"
                    fill="none"
                    viewBox="0 0 24 24"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      d="m18.492 2.33 3.179 3.179a2.25 2.25 0 0 1 0 3.182l-2.423 2.422A2.501 2.501 0 0 1 21 13.5v5a2.5 2.5 0 0 1-2.5 2.5h-13A2.5 2.5 0 0 1 3 18.5v-13A2.5 2.5 0 0 1 5.5 3h5c1.121 0 2.07.737 2.387 1.754L15.31 2.33a2.25 2.25 0 0 1 3.182 0ZM11 13H5v5.5a.5.5 0 0 0 .5.5H11v-6Zm7.5 0H13v6h5.5a.5.5 0 0 0 .5-.5v-5a.5.5 0 0 0-.5-.5Zm-4.06-2.001L13 9.559v1.44h1.44Zm-3.94-6h-5a.5.5 0 0 0-.5.5V11h6V5.5a.5.5 0 0 0-.5-.5Z"
                      fill="currentColor"
                    />
                  </svg>
                </template>
                {{ $t('apps.title') }}
              </RailButton>
            </AnimatedNavigationItemIndicator.Selectable>
          </RouterLink>
        </li>

        <!-- Add -->
        <div
          v-if="
            isSecureContext && !needsSignInAgain && authUser.isLocalAdministrator && supportsAnchorPositions
          "
          class="nav-rail-flex bottom"
          :style="`opacity: ${
            !router.currentRoute.value.path.startsWith('/settings') &&
            router.currentRoute.value.name !== 'webGuacd'
              ? 1
              : 0
          }; transition: opacity var(--wui-control-fast-duration) ease-in-out;`"
        >
          <MenuFlyoutDivider style="margin-block: 0.25rem" />
          <li>
            <ManagedResourceCreateDiscoveryDialog
              #default="{ open: openDiscoveryDialog }"
              @after-save="handleAppOrDesktopChange"
            >
              <BulkImportDialog
                #default="{ open: openCreationDialog, handleFileInput }"
                @after-save="handleAppOrDesktopChange"
              >
                <MenuFlyout placement="right" anchor="start">
                  <template v-slot="{ popoverId }">
                    <RailButton :popovertarget="popoverId">
                      <template v-slot:icon>
                        <svg
                          width="24"
                          height="24"
                          fill="none"
                          viewBox="0 0 24 24"
                          xmlns="http://www.w3.org/2000/svg"
                        >
                          <path
                            d="M11.75 3a.75.75 0 0 1 .743.648l.007.102.001 7.25h7.253a.75.75 0 0 1 .102 1.493l-.102.007h-7.253l.002 7.25a.75.75 0 0 1-1.493.101l-.007-.102-.002-7.249H3.752a.75.75 0 0 1-.102-1.493L3.752 11h7.25L11 3.75a.75.75 0 0 1 .75-.75Z"
                            fill="currentColor"
                          />
                        </svg>
                      </template>

                      {{ $t('registryApps.manager.addNavRail') }}
                    </RailButton>
                  </template>
                  <template #menu>
                    <MenuFlyoutItem
                      @click="
                        () => {
                          openCreationDialog({
                            isRemoteApp: false,
                            data: {
                              identifier: randomUUID(),
                              includeInWorkspace: true,
                              virtualFolders: ['/'],
                            },
                          });
                        }
                      "
                    >
                      {{ t('registryApps.manager.addNavRailAddDesktop') }}
                      <template #icon>
                        <svg viewBox="0 0 24 24">
                          <path
                            d="M 17.5,12 C 20.53765,12 23,14.46235 23,17.5 23,20.53765 20.53765,23 17.5,23 14.46235,23 12,20.53765 12,17.5 12,14.46235 14.46235,12 17.5,12 Z m 0,2.75 a 0.4125,0.4125 0 0 0 -0.40865,0.3564 l -0.0038,0.0561 v 1.925 h -1.925 a 0.4125,0.4125 0 0 0 -0.0561,0.82115 l 0.0561,0.0038 h 1.925 v 1.925 a 0.4125,0.4125 0 0 0 0.82115,0.0561 l 0.0038,-0.0561 v -1.925 h 1.925 a 0.4125,0.4125 0 0 0 0.0561,-0.82115 l -0.0561,-0.0038 h -1.925 v -1.925 A 0.4125,0.4125 0 0 0 17.5,14.75 Z"
                            fill="currentColor"
                          />
                          <path
                            d="M 4.25,3 4.0957031,3.00586 A 2.25,2.25 0 0 0 2,5.25 V 15.751953 L 2.00586,15.90625 A 2.25,2.25 0 0 0 4.25,18.001953 H 8.4980469 V 20.5 H 6.75 L 6.6484375,20.5078 A 0.75,0.75 0 0 0 6.75,22 h 6.113281 A 6.4615383,6.4615383 0 0 1 11.777344,20.5 H 9.9980469 V 18.001953 H 11.058594 A 6.4615383,6.4615383 0 0 1 11.039062,17.5 6.4615383,6.4615383 0 0 1 11.115234,16.501953 H 4.25 l -0.1015625,-0.0078 C 3.7824375,16.445141 3.5,16.131953 3.5,15.751953 V 5.25 L 3.50781,5.1484375 A 0.75,0.75 0 0 1 4.25,4.5 h 15.498047 l 0.103515,0.00781 A 0.75,0.75 0 0 1 20.498047,5.25 v 6.527344 a 6.4615383,6.4615383 0 0 1 1.5,1.083984 V 5.25 l -0.0039,-0.1542969 A 2.25,2.25 0 0 0 19.748047,3 Z"
                            fill="currentColor"
                          />
                        </svg>
                      </template>
                    </MenuFlyoutItem>
                    <MenuFlyoutItem
                      @click="
                        () => {
                          if (capabilities.supportsListInstalledApps) {
                            openDiscoveryDialog();
                          } else {
                            openCreationDialog({
                              isRemoteApp: true,
                              data: {
                                identifier: randomUUID(),
                                includeInWorkspace: true,
                                virtualFolders: ['/'],
                              },
                            });
                          }
                        }
                      "
                    >
                      {{ t('registryApps.manager.addNavRailAddApp') }}
                      <template #icon>
                        <svg viewBox="0 0 24 24">
                          <path
                            d="M 16.236328,2.3359375 A 2.25,2.25 0 0 0 14.644531,2.9941406 L 12.060547,5.5800781 A 2.25,2.25 0 0 0 9.835938,3.6640625 h -5.25 a 2.25,2.25 0 0 0 -2.25,2.25 V 19.414063 a 2.25,2.25 0 0 0 2.25,2.25 h 7.972656 A 6.4615383,6.4615383 0 0 1 11.039063,17.5 6.4615383,6.4615383 0 0 1 12.083984,13.976563 v -0.5625 h 0.410157 a 6.4615383,6.4615383 0 0 1 3.072265,-2.080079 L 12.527344,8.2949219 a 0.75,0.75 0 0 1 0,-1.0605469 l 3.177734,-3.1796875 a 0.75,0.75 0 0 1 1.060547,0 l 3.179687,3.1796875 a 0.75,0.75 0 0 1 0,1.0605469 L 17.193359,11.044922 A 6.4615383,6.4615383 0 0 1 17.5,11.039062 6.4615383,6.4615383 0 0 1 19.117187,11.24414 l 1.888672,-1.8886719 a 2.25,2.25 0 0 0 0,-3.1816406 L 17.826172,2.9941406 A 2.25,2.25 0 0 0 16.236328,2.3359375 Z m -11.65039,2.828125 h 5.25 a 0.75,0.75 0 0 1 0.75,0.75 v 6.0000005 h -6.75 V 5.9140625 a 0.75,0.75 0 0 1 0.75,-0.75 z m 7.5,4.8085937 1.939453,1.9414068 h -1.939453 z m -8.25,3.4414068 h 6.75 l -0.002,6.75 H 4.585892 c -0.414001,0 -0.75,-0.336 -0.75,-0.75 z"
                            fill="currentColor"
                          />
                          <path
                            d="M 17.5,12 C 20.53765,12 23,14.46235 23,17.5 23,20.53765 20.53765,23 17.5,23 14.46235,23 12,20.53765 12,17.5 12,14.46235 14.46235,12 17.5,12 Z m 0,2.75 a 0.4125,0.4125 0 0 0 -0.40865,0.3564 l -0.0038,0.0561 v 1.925 h -1.925 a 0.4125,0.4125 0 0 0 -0.0561,0.82115 l 0.0561,0.0038 h 1.925 v 1.925 a 0.4125,0.4125 0 0 0 0.82115,0.0561 l 0.0038,-0.0561 v -1.925 h 1.925 a 0.4125,0.4125 0 0 0 0.0561,-0.82115 l -0.0561,-0.0038 h -1.925 v -1.925 A 0.4125,0.4125 0 0 0 17.5,14.75 Z"
                            fill="currentColor"
                          />
                        </svg>
                      </template>
                    </MenuFlyoutItem>
                    <MenuFlyoutDivider />
                    <MenuFlyoutItem
                      @click="
                        pickAnyResourceFile()
                          .then(handleFileInput)
                          .catch((error) => {
                            showConfirm(
                              t('registryApps.manager.rdpUploadFail.title'),
                              error,
                              '',
                              t('dialog.ok')
                            );
                          })
                      "
                    >
                      {{ t('registryApps.manager.fromFile') }}
                      <template #icon>
                        <svg viewBox="0 0 24 24">
                          <path
                            d="m6.747 3 10.506.002a3.752 3.752 0 0 1 3.745 3.551l.005.2v4.492a.75.75 0 0 1-1.493.102l-.007-.102V6.752c0-1.19-.925-2.165-2.096-2.245l-.154-.005L6.747 4.5a2.249 2.249 0 0 0-2.242 2.057l-.008.159.002 10.536c.001 1.19.926 2.165 2.097 2.245l.154.005h4.496a.75.75 0 0 1 .102 1.493l-.102.007H6.75a3.752 3.752 0 0 1-3.745-3.55l-.006-.2-.001-10.5.004-.203a3.749 3.749 0 0 1 3.546-3.544l.2-.005ZM9.75 9h6.504a.75.75 0 0 1 .102 1.493l-.102.007-4.694-.001 7.224 7.22a.75.75 0 0 1 .073.977l-.073.084a.75.75 0 0 1-.977.073l-.084-.073-7.223-7.22v4.691a.75.75 0 0 1-.648.743l-.102.007a.75.75 0 0 1-.743-.648L9 16.25V9.734c0-.025.002-.05.005-.076l.021-.108.035-.096.005-.012a.721.721 0 0 1 .153-.223l.044-.04.081-.06.06-.035.095-.042.067-.02.062-.013L9.72 9h6.533H9.75Z"
                            fill="currentColor"
                          />
                        </svg>
                      </template>
                    </MenuFlyoutItem>
                    <MenuFlyoutDivider />
                    <RouterLink to="/settings/resources-manager" custom v-slot="{ href, navigate }">
                      <MenuFlyoutItem @click="() => navigate()">
                        {{ t('registryApps.manager.addNavRailGoTo') }}
                        <template #icon>
                          <svg
                            width="24"
                            height="24"
                            fill="none"
                            viewBox="0 0 24 24"
                            xmlns="http://www.w3.org/2000/svg"
                          >
                            <path
                              d="M8.75 2A1.75 1.75 0 0 0 7 3.75v3a.25.25 0 0 1-.25.25h-3A1.75 1.75 0 0 0 2 8.75v3c0 .966.784 1.75 1.75 1.75h8a1.75 1.75 0 0 0 1.75-1.75v-3a.25.25 0 0 1 .25-.25h2.5A1.75 1.75 0 0 0 18 6.75v-3A1.75 1.75 0 0 0 16.25 2h-7.5Zm7.5 5H13.5V3.5h2.75a.25.25 0 0 1 .25.25v3a.25.25 0 0 1-.25.25ZM12 7H8.483c.011-.082.017-.165.017-.25v-3a.25.25 0 0 1 .25-.25H12V7ZM7 8.5V12H3.75a.25.25 0 0 1-.25-.25v-3a.25.25 0 0 1 .25-.25H7Zm1.5 0h3.518a1.762 1.762 0 0 0-.018.25v3a.25.25 0 0 1-.25.25H8.5V8.5Zm8.75 2a1.75 1.75 0 0 0-1.75 1.75v3a.25.25 0 0 1-.25.25h-8.5A1.75 1.75 0 0 0 5 17.25v3c0 .966.783 1.75 1.75 1.75h13.5A1.75 1.75 0 0 0 22 20.25v-8a1.75 1.75 0 0 0-1.75-1.75h-3ZM17 12.25a.25.25 0 0 1 .25-.25h3a.25.25 0 0 1 .25.25v3.25h-3.518c.012-.082.018-.165.018-.25v-3ZM17 17h3.5v3.25a.25.25 0 0 1-.25.25H17V17Zm-1.5-.018V20.5h-4V17h3.75c.085 0 .168-.006.25-.018ZM10 17v3.5H6.75a.25.25 0 0 1-.25-.25v-3a.25.25 0 0 1 .25-.25H10Z"
                              fill="currentColor"
                            />
                          </svg>
                        </template>
                      </MenuFlyoutItem>
                    </RouterLink>
                  </template>
                </MenuFlyout>
              </BulkImportDialog>
            </ManagedResourceCreateDiscoveryDialog>
          </li>
        </div>

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
        <AnimatedNavigationItemIndicator.Selectable :selected="isActive" :indicatorHeight="24">
          <RailButton :href="href" :active="isActive" @click="navigate">
            <template v-slot:icon>
              <AnimatedIcon.Settings :filled="isActive" />
            </template>
            {{ $t('settings.title') }}
          </RailButton>
        </AnimatedNavigationItemIndicator.Selectable>
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
  </AnimatedNavigationItemIndicator.Track>
</template>

<style scoped>
  .nav-rail {
    --width: 72px;
    --button-size: 64px;

    width: var(--width);
    height: 100%;
    box-sizing: border-box;
    view-transition-name: disabled;

    flex-grow: 0;
    flex-shrink: 0;
  }
  .nav-rail-flex {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: space-between;
    padding: 0 0 4px 0;
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
