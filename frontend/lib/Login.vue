<script setup lang="ts">
  import { Button, InfoBar, ProgressRing, TextBlock, TextBox, Titlebar } from '$components';
  import { useCoreDataStore } from '$stores';
  import { registerServiceWorker, removeSplashScreen } from '$utils';
  import { useTranslation } from 'i18next-vue';
  import { computed, onMounted, ref, watchEffect } from 'vue';
  import { i18nextPromise } from './i18n';

  const { appBase: base, iisBase, envMachineName, machineName, policies } = useCoreDataStore();

  const { t } = useTranslation();

  const sslError = ref(false);
  const returnUrl = ref<string | null>(null);

  onMounted(() => {
    // store the return URL from the query string
    const searchParams = new URLSearchParams(window.location.search);
    const returnPathOrHref = searchParams.get('ReturnUrl');
    if (returnPathOrHref) {
      returnUrl.value = new URL(returnPathOrHref, window.location.origin).href;
    } else {
      returnUrl.value = base;
    }
    window.history.replaceState({}, '', window.location.pathname); // remove the query string from the URL

    // use anonymous authentication if it is enabled in always mode
    if (policies.anonymousAuthentication === 'always') {
      proceedAsAnonymous();
    }

    registerServiceWorker().then((response) => {
      if (response === 'SSL_ERROR') {
        sslError.value = true;
      }
    });
  });

  // track whether i18n is ready
  const i18nReady = ref(false);
  i18nextPromise.then(() => {
    i18nReady.value = true;
  });

  // remove the splash screen once everything is ready
  const canRemoveSplashScreen = computed(() => {
    return i18nReady.value;
  });
  watchEffect(() => {
    if (canRemoveSplashScreen.value) {
      setTimeout(() => {
        removeSplashScreen();
      }, 300);
    }
  });

  async function authenticateUser(username: string, password: string, returnUrl?: string) {
    // attempt to sign in with the provided credentials
    const response = await fetch(iisBase + 'api/auth/authenticate', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        username: username,
        password: password,
      }),
    })
      .then((res): Promise<CredentialsResponse> => res.json())
      .catch((): InvalidCredentialsResponse => ({ success: false }))
      .finally(() => {
        submitting.value = false;
      });

    // show the correct domain and username in the form
    usernameValue.value =
      response.domain + '\\' + (response.success ? response.username : username.split('\\')[1] || username);

    // if the response indicates invalid credentials, show an error message
    if (!response.success) {
      errorMessage.value = response.error ? t(response.error) : t('login.incorrectUsernameOrPassword');
      submitting.value = false;
      return;
    }

    // if the credentials were valid, the server should have set the auth cookie,
    // so redirect to the return URL or the main application page
    const redirectUrl = returnUrl ? decodeURIComponent(returnUrl) : base;
    window.location.href = redirectUrl;
  }

  type InvalidCredentialsResponse = {
    success: false;
    error?: string;
    domain?: string;
  };

  type ValidCredentialsResponse = {
    success: true;
    username: string;
    domain: string;
  };

  type CredentialsResponse = InvalidCredentialsResponse | ValidCredentialsResponse;

  const errorMessage = ref<string | null>(null);
  const submitting = ref(false);
  const usernameValue = ref<string>('');
  const passwordValue = ref<string>('');

  const formFieldKey = ref<number>(0);
  function clearPassword() {
    formFieldKey.value += 1; // incrementing the key tells Vue to recreate the input fields, clearing the browser's autofill state
    passwordValue.value = '';
  }

  async function handleSubmit(event: Event) {
    event.preventDefault();
    submitting.value = true;

    const passwordInput = (event.target as HTMLFormElement).password;

    // get the username and password from the form
    const _username = usernameValue.value;
    const password = passwordInput.value;
    let domain = _username.includes('\\') ? _username.split('\\')[0] : ''; // extract domain if present, otherwise empty
    const username = _username.includes('\\') ? _username.split('\\')[1] : _username; // extract username, or use the whole input if no domain

    // remove the password from the form for security
    clearPassword();

    // if the domain is .\, set it to the machine name
    if (domain === '.') {
      domain = envMachineName;

      // set the domain in the form
      usernameValue.value = domain + '\\' + username;
    }

    // if there is no domain, get the domain from the server
    if (!domain) {
      domain = await fetch(iisBase + 'api/domain')
        .then((res) => res.json())
        .then((data) => data.domain)
        .catch(() => '');

      // set the domain in the form
      usernameValue.value = domain + '\\' + username;
    }

    authenticateUser(domain + '\\' + username, password, returnUrl.value || undefined);
  }

  function submit() {
    const form = document.querySelector('form');
    if (form && !submitting.value) {
      submitting.value = true;
      form.dispatchEvent(new Event('submit', { bubbles: false, cancelable: true }));
    }
  }

  function proceedAsAnonymous() {
    authenticateUser('RAWEB\\anonymous', '', returnUrl.value || undefined);
  }

  const hidePasswordChange = policies.passwordChangeEnabled === false;
</script>

<template>
  <Titlebar :title="$t('login.title')" forceVisible hideProfileMenu withBorder />
  <div class="dialog-wrapper">
    <div class="dialog">
      <InfoBar
        v-if="sslError"
        severity="caution"
        :title="$t('login.securityError503.title')"
        style="
          border-radius: var(--wui-overlay-corner-radius) var(--wui-overlay-corner-radius) 0 0;
          flex-grow: 0;
          flex-shrink: 0;
        "
      >
        <span
          style="font-size: 13px; font-weight: 400; opacity: 0.7; position: absolute; top: -12px; right: 6px"
        >
          {{ $t('login.securityError503.code') }}
        </span>
        <TextBlock>{{ $t('login.securityError503.message') }}</TextBlock>
        <br />
        <Button
          variant="hyperlink"
          href="https://github.com/kimmknight/raweb/wiki/Trusting-the-RAWeb-server-(Fix-security-error-5003)"
          class="unindent"
          style="margin-bottom: -6px"
        >
          {{ $t('login.securityError503.action') }}
        </Button>
      </InfoBar>

      <div class="dialog-body">
        <form @submit="handleSubmit">
          <div>
            <h1>{{ $t('login.title') }}</h1>
            <p>
              {{ $t('login.captionContinue') }}
              <strong>{{ $t('longAppName') }}{{ ' ' }}</strong>
              <span style="white-space: nowrap">
                {{ $t('login.captionOn') }}
                <strong> {{ machineName }} </strong>
              </span>
            </p>

            <InfoBar severity="critical" v-if="errorMessage" style="margin-bottom: 16px">
              <TextBlock
                v-if="errorMessage.includes('{password_change_button}')"
                v-for="(part, index) in errorMessage.split('{password_change_button}')"
              >
                {{ part }}
                <Button
                  :href="`password?username=${usernameValue}`"
                  variant="hyperlink"
                  class="inline-button"
                  v-if="
                    !hidePasswordChange && index < errorMessage.split('{password_change_button}').length - 1
                  "
                >
                  {{ $t('login.changePasswordButton') }}
                </Button>
              </TextBlock>
              <TextBlock v-else>{{ errorMessage }}</TextBlock>
            </InfoBar>

            <label class="input">
              <TextBlock>{{ $t('username') }}</TextBlock>
              <TextBox
                :key="formFieldKey"
                type="text"
                id="username"
                name="username"
                v-model:value="usernameValue"
                :disabled="submitting"
                required
                autocomplete="username"
                @keyup.enter="submit"
              />
            </label>

            <label class="input">
              <TextBlock>{{ $t('password') }}</TextBlock>
              <TextBox
                :key="formFieldKey"
                type="password"
                id="password"
                name="password"
                v-model:value="passwordValue"
                :disabled="submitting"
                autocomplete="current-password"
                @keyup.enter="submit"
              />
            </label>

            <p class="access">
              {{ $t('poweredBy') }}
              <br />
              <Button href="https://github.com/kimmknight/raweb" variant="hyperlink" class="unindent">
                {{ $t('poweredByLearnMore') }}
              </Button>
            </p>
          </div>

          <div class="button-row">
            <Button type="submit" variant="accent" :disabled="submitting">
              <ProgressRing
                v-if="submitting"
                :size="16"
                style="--wui-accent-default: var(--wui-text-on-accent-primary)"
              />
              <span :style="`visibility: ${submitting ? 'hidden' : 'visible'};`">{{
                $t('login.continue')
              }}</span>
            </Button>
            <Button
              type="button"
              variant="hyperlink"
              :disabled="submitting"
              @click="proceedAsAnonymous"
              v-if="!submitting && policies.anonymousAuthentication === 'allow'"
            >
              {{ $t('login.continueAnonymous') }}
            </Button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<style>
  @media (prefers-color-scheme: dark) {
    .app-header.with-border {
      /* hide the bottom border in dark mode */
      --wui-solid-background-tertiary: transparent;
    }
  }
</style>

<style scoped>
  .dialog-wrapper {
    display: flex;
    flex-direction: column;
    inline-size: 100%;
    block-size: var(--content-height);
    top: var(--header-height);
    justify-content: center;
    position: fixed;
    align-items: center;
    background-color: color-mix(in srgb, var(--wui-layer-default) 50%, transparent);
  }

  .dialog {
    inline-size: 448px;
  }

  .dialog {
    background-clip: padding-box;
    background-color: var(--wui-solid-background-base);
    border-radius: var(--wui-overlay-corner-radius);
    box-shadow: var(--wui-dialog-shadow);
    box-sizing: border-box;
    max-inline-size: calc(100% - 24px);
    overflow: hidden;
    position: fixed;
  }
  @media (prefers-color-scheme: dark) {
    .dialog {
      border: 1px solid var(--wui-surface-stroke-default);
    }
  }

  .dialog-body {
    background-color: var(--wui-layer-default);
    color: var(--wui-text-primary);
    font-family: var(--wui-font-family-text);
    font-size: var(--wui-font-size-body);
    font-weight: 400;
    line-height: 20px;
    -webkit-user-select: none;
    -moz-user-select: none;
    -ms-user-select: none;
    user-select: none;
    padding: 24px;
  }

  h1 {
    color: var(--wui-text-primary);
    display: block;
    margin-bottom: 12px;
    font-size: var(--wui-font-size-subtitle);
    line-height: 28px;
    font-family: var(--wui-font-family-display);
    font-weight: 600;
    white-space: pre-wrap;
    margin: 0;
    padding: 0;
  }

  form {
    margin: 0;
  }

  label {
    display: flex;
    flex-direction: column;
    gap: 3px;
    margin-bottom: 10px;
  }

  strong {
    font-weight: 600;
  }

  .unindent {
    margin-left: -11px;
  }

  .inline-button {
    margin: -4px -11px -6px;
  }

  .button-row {
    display: flex;
    flex-direction: row-reverse;
    gap: 0.5rem;
  }

  .access {
    margin-top: 1.25rem;
    opacity: 0.9;
  }

  .button-row button[type='submit'] {
    position: relative;
  }
  .button-row button[type='submit'] .progress-ring {
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
  }

  @media (max-width: 600px) or (max-height: 600px) {
    .dialog {
      box-sizing: border-box !important;
      inline-size: 100% !important;
      max-inline-size: unset !important;
      border-radius: 0 !important;
      border: none !important;
      box-shadow: none !important;
      inset: calc(var(--header-height) + env(safe-area-inset-top, 0px)) env(safe-area-inset-right, 0px)
        env(safe-area-inset-bottom, 0px) env(safe-area-inset-left, 0px);
    }

    .dialog-body {
      height: 100%;
      box-sizing: border-box;
      display: flex;
      flex-direction: column;
      overflow: auto;
    }

    .dialog-body > * {
      max-inline-size: 600px;
      inline-size: 100%;
      margin: 0 auto;
    }

    .content-wrapper {
      flex-grow: 1;
      display: flex;
      flex-direction: column;
      justify-content: space-between;
    }
  }
</style>
