<script setup lang="ts">
  import { Button, InfoBar, ProgressRing, TextBlock, TextBox, Titlebar } from '$components';
  import { parseXmlResponse, registerServiceWorker, removeSplashScreen } from '$utils';
  import { useTranslation } from 'i18next-vue';
  import { computed, onMounted, ref, watchEffect } from 'vue';
  import { i18nextPromise } from './i18n';

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
      returnUrl.value = window.__base;
    }
    window.history.replaceState({}, '', window.location.pathname); // remove the query string from the URL

    // redirect to the anonymous login if anonymous authentication is enabled
    skipIfAnonymousAuthenticationEnabled();

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

  /**
   * Checks if anonymous authentication is enabled and automatically
   * signs in as the anonymous user if it is.
   */
  async function skipIfAnonymousAuthenticationEnabled() {
    // Check if anonymous authentication is enabled
    const loginPath = window.__iisBase + 'auth/login.aspx';
    const defaultReturnHref = window.__base;
    const returnPathOrHref = decodeURIComponent(
      new URLSearchParams(window.location.search).get('ReturnUrl') || ''
    );
    const returnUrl = new URL(returnPathOrHref || defaultReturnHref, window.location.origin);
    const loginOrigin = returnUrl.origin;
    const loginUrl = new URL(loginPath, loginOrigin);
    if (returnUrl) {
      loginUrl.searchParams.set('ReturnUrl', returnUrl.toString());
    }

    const isAnonymousAuthEnabled = await fetch(
      window.__iisBase + 'auth.asmx/CheckLoginPageForAnonymousAuthentication?loginPageUrl=' + loginUrl
    )
      .then(parseXmlResponse)
      .then((xmlDoc) => xmlDoc.textContent === 'true')
      .catch(() => false);

    if (isAnonymousAuthEnabled) {
      authenticateUser('', '', returnUrl.href);
    }
  }

  async function authenticateUser(username: string, password: string, returnUrl?: string) {
    // Base64 encode the credentials
    const credentials = btoa(username + ':' + password);
    console.log('Authenticating user:', username);
    console.log('password:', password);

    // clear the HTML form values
    // document.querySelector('form').reset();

    // authenticate
    return fetch(window.__iisBase + 'auth/login.aspx', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
        Authorization: 'Basic ' + credentials,
        'x-requested-with': 'XMLHttpRequest',
      },
      credentials: 'include',
    })
      .then((response) => {
        if (response.ok) {
          // Redirect to the main application page on successful login
          const redirectUrl = returnUrl ? decodeURIComponent(returnUrl) : window.__base;
          window.location.href = redirectUrl;
          return true;
        } else {
          // Handle login failure (e.g., show an error message)
          alert('Login failed. Please check your credentials.');
          return false;
        }
      })
      .catch((error) => {
        console.error('Error during login:', error);
        alert('An error occurred. Please try again.');
        return false;
      });
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

  async function handleSubmit(event: Event) {
    event.preventDefault();
    submitting.value = true;

    // get the username and password from the form
    const _username = (event.target as HTMLFormElement).username.value;
    const password = (event.target as HTMLFormElement).password.value;
    let domain = _username.includes('\\') ? _username.split('\\')[0] : ''; // extract domain if present, otherwise empty
    const username = _username.includes('\\') ? _username.split('\\')[1] : _username; // extract username, or use the whole input if no domain

    // if there is no domain, get the domain from the server
    domain = await fetch(window.__iisBase + 'auth.asmx/GetDomainName')
      .then(parseXmlResponse)
      .then((xmlDoc) => xmlDoc.textContent || '')
      .catch(() => '');

    // set the domain in the form
    usernameValue.value = domain + '\\' + username;

    // verify that the username and password are correct
    const response = await fetch(
      window.__iisBase +
        'auth.asmx/ValidateCredentials?username=' +
        encodeURIComponent(username) +
        '&password=' +
        encodeURIComponent(password)
    )
      .then(parseXmlResponse)
      .then((xmlDoc) => JSON.parse(xmlDoc.textContent || '{ success: false }') as CredentialsResponse)
      .catch(() => ({ success: false } as InvalidCredentialsResponse));

    // show the correct domain and username in the form
    usernameValue.value =
      (response.domain || domain) + '\\' + (response.success ? response.username : username);

    // if the response indicates invalid credentials, show an error message
    if (!response.success) {
      errorMessage.value = response.error || t('login.incorrectUsernameOrPassword');
      submitting.value = false;
      return;
    }

    // once we know the credentials are valid, authenticate the user
    authenticateUser(response.domain + '\\' + response.username, password, returnUrl.value || undefined);
  }

  function submit() {
    const form = document.querySelector('form');
    if (form) {
      form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    }
  }

  const machineName = window.__machineName;
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
              <TextBlock>{{ errorMessage }}</TextBlock>
            </InfoBar>

            <label class="input">
              <TextBlock>{{ $t('username') }}</TextBlock>
              <TextBox
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
                type="password"
                id="password"
                name="password"
                :disabled="submitting"
                required
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
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

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

  .button-row {
    display: flex;
    flex-direction: row-reverse;
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
