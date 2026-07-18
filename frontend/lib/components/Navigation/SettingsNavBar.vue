<script setup lang="ts">
  import { AnimatedNavigationItemIndicator, Button } from '$components';
  import { useCoreDataStore } from '$stores';
  import { useTranslation } from 'i18next-vue';

  const { authUser, capabilities } = useCoreDataStore();
  const { t } = useTranslation();

  const { hidden, simpleModeEnabled } = defineProps<{
    hidden?: boolean;
    simpleModeEnabled?: boolean;
  }>();

  const canManagePolicies = authUser.isAdmin;
  const canManageResources = authUser.isAdmin;

  // the nav bar should be hidden if there is only the general tab
  const shouldShowNavBar = canManagePolicies || canManageResources;
</script>

<template>
  <!-- TODO: replace this with a selector bar -->
  <AnimatedNavigationItemIndicator.Track
    v-if="shouldShowNavBar"
    orientation="horizontal"
    class="settings-nav"
    :class="{ hidden, simpleModeEnabled }"
  >
    <RouterLink to="/settings" custom v-slot="{ href, isExactActive, navigate }">
      <AnimatedNavigationItemIndicator.Selectable :selected="isExactActive" :indicatorSize="16">
        <Button :href :active="isExactActive" @click="navigate" :class="{ isExactActive }">
          {{ t('settingsHub.nav.general') }}
          <template v-slot:icon>
            <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path
                d="M12.012 2.25c.734.008 1.465.093 2.182.253a.75.75 0 0 1 .582.649l.17 1.527a1.384 1.384 0 0 0 1.927 1.116l1.401-.615a.75.75 0 0 1 .85.174 9.792 9.792 0 0 1 2.204 3.792.75.75 0 0 1-.271.825l-1.242.916a1.381 1.381 0 0 0 0 2.226l1.243.915a.75.75 0 0 1 .272.826 9.797 9.797 0 0 1-2.204 3.792.75.75 0 0 1-.848.175l-1.407-.617a1.38 1.38 0 0 0-1.926 1.114l-.169 1.526a.75.75 0 0 1-.572.647 9.518 9.518 0 0 1-4.406 0 .75.75 0 0 1-.572-.647l-.168-1.524a1.382 1.382 0 0 0-1.926-1.11l-1.406.616a.75.75 0 0 1-.849-.175 9.798 9.798 0 0 1-2.204-3.796.75.75 0 0 1 .272-.826l1.243-.916a1.38 1.38 0 0 0 0-2.226l-1.243-.914a.75.75 0 0 1-.271-.826 9.793 9.793 0 0 1 2.204-3.792.75.75 0 0 1 .85-.174l1.4.615a1.387 1.387 0 0 0 1.93-1.118l.17-1.526a.75.75 0 0 1 .583-.65c.717-.159 1.45-.243 2.201-.252Zm0 1.5a9.135 9.135 0 0 0-1.354.117l-.109.977A2.886 2.886 0 0 1 6.525 7.17l-.898-.394a8.293 8.293 0 0 0-1.348 2.317l.798.587a2.881 2.881 0 0 1 0 4.643l-.799.588c.32.842.776 1.626 1.348 2.322l.905-.397a2.882 2.882 0 0 1 4.017 2.318l.11.984c.889.15 1.798.15 2.687 0l.11-.984a2.881 2.881 0 0 1 4.018-2.322l.905.396a8.296 8.296 0 0 0 1.347-2.318l-.798-.588a2.881 2.881 0 0 1 0-4.643l.796-.587a8.293 8.293 0 0 0-1.348-2.317l-.896.393a2.884 2.884 0 0 1-4.023-2.324l-.11-.976a8.988 8.988 0 0 0-1.333-.117ZM12 8.25a3.75 3.75 0 1 1 0 7.5 3.75 3.75 0 0 1 0-7.5Zm0 1.5a2.25 2.25 0 1 0 0 4.5 2.25 2.25 0 0 0 0-4.5Z"
                fill="currentColor"
              />
            </svg>
          </template>
        </Button>
      </AnimatedNavigationItemIndicator.Selectable>
    </RouterLink>
    <RouterLink
      v-if="canManagePolicies"
      to="/settings/policies"
      custom
      v-slot="{ href, isExactActive, navigate }"
    >
      <AnimatedNavigationItemIndicator.Selectable :selected="isExactActive" :indicatorSize="16">
        <Button :href :active="isExactActive" @click="navigate" :class="{ isExactActive }">
          {{ t('settingsHub.nav.policies') }}
          <template v-slot:icon>
            <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path
                d="M12 1.999c5.487 0 9.942 4.419 10 9.892a6.064 6.064 0 0 1-1.525-.566 8.486 8.486 0 0 0-.21-1.326h-1.942a1.618 1.618 0 0 0-.648 0h-.769c.012.123.023.246.032.37a7.858 7.858 0 0 1-1.448.974c-.016-.46-.047-.908-.094-1.343H8.604a18.968 18.968 0 0 0 .135 5H12v1.5H9.06c.653 2.414 1.786 4.002 2.94 4.002.276 0 .551-.091.819-.263a6.938 6.938 0 0 0 1.197 1.56c-.652.133-1.326.203-2.016.203-5.524 0-10.002-4.478-10.002-10.001C1.998 6.477 6.476 1.998 12 1.998ZM7.508 16.501H4.785a8.532 8.532 0 0 0 4.095 3.41c-.523-.82-.954-1.846-1.27-3.015l-.102-.395ZM7.093 10H3.735l-.004.017a8.524 8.524 0 0 0-.233 1.984c0 1.056.193 2.067.545 3h3.173a20.301 20.301 0 0 1-.218-3c0-.684.033-1.354.095-2.001Zm1.788-5.91-.023.008A8.531 8.531 0 0 0 4.25 8.5h3.048c.313-1.752.86-3.278 1.583-4.41ZM12 3.499l-.116.005C10.618 3.62 9.396 5.622 8.828 8.5h6.343c-.566-2.87-1.784-4.869-3.045-4.995L12 3.5Zm3.12.59.106.175c.67 1.112 1.178 2.572 1.475 4.237h3.048a8.533 8.533 0 0 0-4.338-4.29l-.291-.121Zm7.38 8.888c-1.907-.172-3.434-1.287-4.115-1.87a.601.601 0 0 0-.77 0c-.682.583-2.21 1.698-4.116 1.87a.538.538 0 0 0-.5.523V17c0 4.223 4.094 5.716 4.873 5.962a.42.42 0 0 0 .255 0c.78-.246 4.872-1.74 4.872-5.962v-3.5a.538.538 0 0 0-.5-.523Z"
                fill="currentColor"
              />
            </svg>
          </template>
        </Button>
      </AnimatedNavigationItemIndicator.Selectable>
    </RouterLink>
    <RouterLink
      v-if="canManageResources"
      to="/settings/resources-manager"
      custom
      v-slot="{ href, isExactActive, navigate }"
    >
      <AnimatedNavigationItemIndicator.Selectable :selected="isExactActive" :indicatorSize="16">
        <Button :href :active="isExactActive" @click="navigate" :class="{ isExactActive }">
          {{ t('settingsHub.nav.resources') }}
          <template v-slot:icon>
            <svg width="24" height="24" fill="none" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path
                d="M8.75 2A1.75 1.75 0 0 0 7 3.75v3a.25.25 0 0 1-.25.25h-3A1.75 1.75 0 0 0 2 8.75v3c0 .966.784 1.75 1.75 1.75h8a1.75 1.75 0 0 0 1.75-1.75v-3a.25.25 0 0 1 .25-.25h2.5A1.75 1.75 0 0 0 18 6.75v-3A1.75 1.75 0 0 0 16.25 2h-7.5Zm7.5 5H13.5V3.5h2.75a.25.25 0 0 1 .25.25v3a.25.25 0 0 1-.25.25ZM12 7H8.483c.011-.082.017-.165.017-.25v-3a.25.25 0 0 1 .25-.25H12V7ZM7 8.5V12H3.75a.25.25 0 0 1-.25-.25v-3a.25.25 0 0 1 .25-.25H7Zm1.5 0h3.518a1.762 1.762 0 0 0-.018.25v3a.25.25 0 0 1-.25.25H8.5V8.5Zm8.75 2a1.75 1.75 0 0 0-1.75 1.75v3a.25.25 0 0 1-.25.25h-8.5A1.75 1.75 0 0 0 5 17.25v3c0 .966.783 1.75 1.75 1.75h13.5A1.75 1.75 0 0 0 22 20.25v-8a1.75 1.75 0 0 0-1.75-1.75h-3ZM17 12.25a.25.25 0 0 1 .25-.25h3a.25.25 0 0 1 .25.25v3.25h-3.518c.012-.082.018-.165.018-.25v-3ZM17 17h3.5v3.25a.25.25 0 0 1-.25.25H17V17Zm-1.5-.018V20.5h-4V17h3.75c.085 0 .168-.006.25-.018ZM10 17v3.5H6.75a.25.25 0 0 1-.25-.25v-3a.25.25 0 0 1 .25-.25H10Z"
                fill="currentColor"
              />
            </svg>
          </template>
        </Button>
      </AnimatedNavigationItemIndicator.Selectable>
    </RouterLink>
  </AnimatedNavigationItemIndicator.Track>
</template>

<style scoped>
  .settings-nav {
    display: flex;
    gap: 0.5rem;
    padding: 0 0 0.25rem 0;
    height: auto;
    max-height: fit-content;
    transition: padding var(--wui-control-fast-duration) allow-discrete;
    overflow: hidden;
    color: var(--wui-text-secondary);
    color: light-dark(var(--wui-text-primary), var(--wui-text-secondary));
    opacity: 1;
  }
  .settings-nav.hidden {
    height: 0;
    padding: 0;
    opacity: 0;
  }
  .settings-nav.simpleModeEnabled {
    padding-left: 0.5rem;
  }

  .settings-nav :deep(.button) {
    --wui-control-fill-default: transparent;
    box-shadow: none;
    height: 2.5rem;
    -webkit-user-drag: none;
    transition: color var(--wui-control-normal-duration);
  }
  .settings-nav :deep(.button:not(.disabled):hover) {
    background-color: var(--wui-subtle-secondary);
    color: var(--wui-text-primary);
  }
  .settings-nav :deep(.button:not(.disabled):not(.active):hover:not(:active) .icon) {
    color: var(--wui-text-secondary);
    color: light-dark(var(--wui-text-primary), var(--wui-text-secondary));
  }
  .settings-nav :deep(.button:not(.disabled):active) {
    background-color: var(--wui-subtle-tertiary);
    color: var(--wui-text-tertiary);
    will-change: line-height, background-color, color;
  }

  .settings-nav :deep(.button.isExactActive) {
    background-color: transparent !important;
  }

  /* .settings-nav :deep(.button::after) {
    content: '';
    position: absolute;
    bottom: 2px;
    block-size: 3px;
    border-radius: 3px;
    inline-size: 0;
    background-color: var(--wui-accent-default);
  }
  .settings-nav :deep(.button.isExactActive::after) {
    inline-size: 1rem;
    transition: width var(--wui-control-fast-duration) var(--wui-control-fast-out-slow-in-easing);
  } */

  .settings-nav :deep(.button:not(.active) .icon) {
    transition: color var(--wui-control-normal-duration);
  }
</style>
