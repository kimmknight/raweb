<script setup lang="ts">
  import { requestCredentials as _requestCredentials, showConfirm } from '$dialogs';
  import { useCoreDataStore } from '$stores';
  import { debounce, getAppsAndDevices, useWebfeedData } from '$utils';
  import Guacamole from 'guacamole-common-js';
  import { useTranslation } from 'i18next-vue';
  import { computed, onMounted, onUnmounted, ref } from 'vue';
  import { useRouter } from 'vue-router';
  import NotFound from '../404.vue';

  const props = defineProps<{
    workspace: Awaited<ReturnType<typeof getAppsAndDevices>>;
    refreshWorkspace: () => ReturnType<typeof useWebfeedData>['refresh'];
  }>();

  const { t } = useTranslation();
  const router = useRouter();
  const { capabilities } = useCoreDataStore();

  if (!capabilities.supportsGuacdWebClient) {
    router.replace('/404');
  }

  // determine the host to connect to based on the route params
  const resourceId = computed(() => router.currentRoute.value.params.resourceId as string);
  const hostId = computed(() => router.currentRoute.value.params.hostId as string);

  // extract the identifiers used by the server to configure the connection
  const resourceConnectionIds = computed(() => {
    const resource = props.workspace?.resources.find(
      (resource) => resource.id === resourceId.value && resource.type === 'Desktop'
    );
    const host = resource?.hosts.find((host) => host.id === hostId.value);
    const resourceHostUrl = host?.url;
    if (!resourceHostUrl) {
      return null;
    }

    const resourcePath = resourceHostUrl.pathname.replace('/api/resources/', '');
    const resourceFrom = resourceHostUrl.searchParams.get('from');
    if (!resourcePath || !resourceFrom) return null;
    return { resourcePath, resourceFrom };
  });

  const state = ref<Guacamole.Client.State | null>(Guacamole.Client.State.DISCONNECTED);
  const errorMessage = ref<string | null>(null);
  const statusMessage = ref<string | null>('client.connecting');
  const reconnectInterval = ref<number | undefined>(undefined);
  const reconnectFunction = ref<() => void>();

  /**
   * Registers the event listeners for mouse, touch, and keyboard input on the given display element,
   * forwarding the events to the provided Guacamole client.
   *
   * Returns a function that can be called to unregister the event listeners.
   */
  function registerEventListeners(displayElement: HTMLElement, client: Guacamole.Client) {
    // forward all mouse interaction over Guacamole connection
    const mouse = new Guacamole.Mouse(displayElement);
    const handleMouseEvent = (evt: Guacamole.Mouse.Event) => {
      client.sendMouseState(evt.state, true);
    };
    // @ts-expect-error
    mouse.onEach(['mousedown', 'mousemove', 'mouseup'], handleMouseEvent);

    // forward all touch interaction over Guacamole connection
    const touch = new Guacamole.Touch(displayElement);
    const handleTouchEvent = (evt: Guacamole.Touch.Event) => {
      client.sendTouchState(evt.state, true);
    };
    // @ts-expect-error
    touch.onEach(['touchstart', 'touchmove', 'touchend'], handleTouchEvent);

    // forward all keyboard interaction over Guacamole connection
    const keyboard = new Guacamole.Keyboard(document);
    const handleKeyDown = (keysym: number) => {
      client.sendKeyEvent(1, keysym);
    };
    const handleKeyUp = (keysym: number) => {
      client.sendKeyEvent(0, keysym);
    };
    keyboard.onkeydown = handleKeyDown;
    keyboard.onkeyup = handleKeyUp;

    return () => {
      // @ts-expect-error
      mouse.offEach(['mousedown', 'mousemove', 'mouseup'], handleMouseEvent);
      // @ts-expect-error
      touch.offEach(['touchstart', 'touchmove', 'touchend'], handleTouchEvent);
      keyboard.onkeydown = null;
      keyboard.onkeyup = null;
    };
  }

  const currentClient = ref<Guacamole.Client | null>(null);
  const unregisterEventListeners = ref<ReturnType<typeof registerEventListeners> | null>(null);

  /**
   * Connects to a remove device using the provided options.
   */
  async function connect(options: {
    ignoreCertificateError?: boolean;
    domain?: string;
    username?: string;
    password?: string;
    rPath: string;
    rFrom: string;
  }) {
    const { ignoreCertificateError = false, rPath, rFrom } = options;
    let { domain, username, password } = options;

    if (resourceConnectionIds.value === null) {
      currentClient.value = null;
      return;
    }

    // ensure the display element is cleared before attempting to connect
    resetDisplay();

    // if we don't have credentials yet, request them from the user
    let exitEarly = false;
    if (!username || !password || !domain) {
      await requestCredentials()
        .then(({ credentials, done }) => {
          domain = credentials.domain;
          options.domain = domain;
          username = credentials.username;
          options.username = username;
          password = credentials.password;
          options.password = password;
          done();
        })
        .catch((err) => {
          exitEarly = true;
          const fromNavigateAway = typeof err === 'string' && err === 'NAVIGATE_AWAY';
          if (!fromNavigateAway) {
            router.back();
          }
        });
    }
    if (exitEarly) {
      currentClient.value = null;
      return;
    }

    state.value = Guacamole.Client.State.CONNECTING;

    // configure the connection to guacd
    const tunnel = new Guacamole.WebSocketTunnel('/guacd-tunnel/');
    const client = new Guacamole.Client(tunnel);
    currentClient.value = client;

    // attach the display to the DOM
    const displayElement = client.getDisplay().getElement();
    const container = document.getElementById('display');
    if (container) {
      container.appendChild(displayElement);
      unregisterEventListeners.value = registerEventListeners(displayElement, client);
    }

    // adjust display size when client size changes
    const displayAreaElem = container?.parentElement ?? undefined;
    if (displayAreaElem) {
      const sendResized = debounce(() => {
        client.sendSize(displayAreaElem.clientWidth, displayAreaElem.clientHeight), 200;
      });
      new ResizeObserver(sendResized).observe(displayAreaElem);
    }

    // connect using the provided connection parameters
    let connectionString = '';
    connectionString += `rPath=${rPath}`;
    connectionString += `&rFrom=${rFrom}`;
    connectionString += `&ignoreCertErrors=${ignoreCertificateError ? 'true' : 'false'}`;
    client.connect(connectionString);

    // listen for custom instructions that demand additional configuration
    const originalOnInstruction = tunnel.oninstruction;
    let lastInstructionTime = Date.now();
    tunnel.oninstruction = (opcode, parameters) => {
      lastInstructionTime = Date.now();

      if (opcode === 'raweb-demand-credentials') {
        tunnel.sendMessage('domain', domain);
        tunnel.sendMessage('username', username);
        tunnel.sendMessage('password', password);
      }

      if (opcode === 'raweb-demand-display-info') {
        tunnel.sendMessage('displayWidth', (displayAreaElem?.clientWidth ?? 800) * 1);
        tunnel.sendMessage('displayHeight', (displayAreaElem?.clientHeight ?? 600) * 1);
        tunnel.sendMessage('displayDPI', 96);
      }

      if (opcode === 'raweb-demand-timezone') {
        const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
        tunnel.sendMessage('timezone', timezone);
      }

      originalOnInstruction?.(opcode, parameters);
    };

    // handle errors
    client.onerror = (error) => {
      const errorCode = error.code as Guacamole.Status.Code | number;

      // transform error message for specific known errors
      let parsedErrorMessage = error.message || 'An unknown error occurred.';
      if (error.message === 'Desktop service unavailable' && isNaN(errorCode)) {
        parsedErrorMessage = t('client.serviceOffline');
      }
      if (errorCode === 10010) {
        parsedErrorMessage = t('client.hostNotFoundError', { hostId: hostId.value });
      }
      errorMessage.value = parsedErrorMessage;

      // clear the display when the connection fails
      resetDisplay();

      // unregister event listeners so that they do not interfere
      // will the dialogs that may be shown to the user
      unregisterEventListeners.value?.();

      // show a dialog asking the user to ignore certificate errors
      // if the certificate for the host is invalid
      if (errorCode === 10003) {
        return showConfirm('Security Error', parsedErrorMessage, 'Ignore', 'Cancel', { size: 'max' })
          .then((done) => {
            // retry connection
            done();
            connect({ ...options, ignoreCertificateError: true });
          })
          .catch((err) => {
            const fromNavigateAway = typeof err === 'string' && err === 'NAVIGATE_AWAY';
            if (!fromNavigateAway) {
              router.back();
            }
          })
          .finally(() => {
            errorMessage.value = null;
          });
      }

      // incorrect credentials: request new credentials from the user
      if (
        errorCode === 769 ||
        (errorCode === 519 && parsedErrorMessage.includes('Server refused connection (wrong security type?)'))
      ) {
        return requestCredentials(
          t('client.creds.failtitle'),
          t('client.creds.failmessage', { hostId: hostId.value }),
          t('client.creds.failerror')
        )
          .then(({ credentials, done }) => {
            // retry connection with new credentials
            options.domain = credentials.domain;
            options.username = credentials.username;
            options.password = credentials.password;
            done();
            connect(options);
          })
          .catch((err) => {
            const fromNavigateAway = typeof err === 'string' && err === 'NAVIGATE_AWAY';
            if (!fromNavigateAway) {
              window.location.reload();
            }
          })
          .finally(() => {
            errorMessage.value = null;
          });
      }

      // show a message for all other errors
      console.error('Guacamole error:', error);
      showConfirm(
        t('client.connectionError.title'),
        parsedErrorMessage,
        t('client.connectionError.retry'),
        t('client.connectionError.cancel')
      )
        .then((done) => {
          // retry connection
          done();
          connect(options);
        })
        .catch((err) => {
          const fromNavigateAway = typeof err === 'string' && err === 'NAVIGATE_AWAY';
          if (!fromNavigateAway) {
            router.back();
          }
        })
        .finally(() => {
          errorMessage.value = null;
        });
    };

    client.onstatechange = (newState) => {
      state.value = newState;

      if (!isMounted.value) {
        return;
      }

      if (newState === Guacamole.Client.State.CONNECTED) {
        // if no instructions have been received for 10 seconds,
        // assume thr connection is dead and needs to be re-established
        reconnectFunction.value = () => {
          errorMessage.value = t('client.reconnectingErrorMessage');
          client.disconnect();
          clearInterval(reconnectInterval.value);
          statusMessage.value = 'client.reconnecting';
          resetDisplay();
          connect(options);
        };

        reconnectInterval.value = setInterval(() => {
          if (Date.now() - lastInstructionTime > 10000) {
            reconnectFunction.value?.();
          }
        }, 1000) as unknown as number;
      } else {
        clearInterval(reconnectInterval.value);
      }

      // show a message when the connection has been closed without any errors
      if (newState === Guacamole.Client.State.DISCONNECTED && !errorMessage.value) {
        resetDisplay();
        setTimeout(() => {
          if (isMounted.value) {
            showConfirm(
              t('client.disconnectedTitle'),
              t('client.disconnectedMessage', { hostId: hostId.value }),
              t('client.disconnectedRetry'),
              t('client.disconnectedCancel')
            )
              .then((done) => {
                // retry connection
                done();
                connect(options);
              })
              .catch(() => {
                router.back();
              });
          }
        }, 300);
      }
    };

    // ensure the connection is closed when the user leaves the page
    window.addEventListener('beforeunload', () => client.disconnect());
  }

  // reset the idle reconnection process when the user navigates away
  onUnmounted(() => {
    clearInterval(reconnectInterval.value);
  });

  // disconnect the client when the component is unmounted
  const isMounted = ref(true);
  onUnmounted(() => {
    currentClient.value?.disconnect();
    currentClient.value = null;
    isMounted.value = false;
    unregisterEventListeners.value?.();
  });

  // start the connection when the component is mounted
  onMounted(() => {
    isMounted.value = true;
    clearInterval(reconnectInterval.value);
    setTimeout(() => {
      connect({
        rPath: resourceConnectionIds.value?.resourcePath ?? '',
        rFrom: resourceConnectionIds.value?.resourceFrom ?? '',
      });
    }, 300);
  });

  // reset connection state on hot module replacement (dev mode)
  if (import.meta.hot) {
    import.meta.hot.dispose(() => {
      clearInterval(reconnectInterval.value);
      state.value = Guacamole.Client.State.CONNECTING;
      reconnectFunction.value?.();
    });
  }

  /**
   * A helper function that resets the display area by removing any existing canvas elements.
   */
  function resetDisplay() {
    const container = document.getElementById('display');
    if (container) {
      container.innerHTML = ''; // removes the canvas entirely
    }
  }

  // this message to show when the resource could not be found
  const message404 = computed(() => {
    return t('client.resourceNotFoundMessage', { resourceId: resourceId.value, hostId: hostId.value });
  });

  /**
   * Requests credentials from the user before connecting to the remote device.
   */
  function requestCredentials(
    title = t('client.creds.title'),
    message = t('client.creds.message', { hostId: hostId.value }),
    errorMessage = ''
  ) {
    return _requestCredentials(title, message, 'OK', 'Cancel', errorMessage);
  }
</script>

<template>
  <div id="404" v-if="!resourceConnectionIds">
    <NotFound :title="t('client.resourceNotFound')" :message="message404" />
  </div>

  <div id="display-wrapper" v-else>
    <div id="display"></div>

    <div class="connecting" v-if="state !== Guacamole.Client.State.DISCONNECTED">
      <svg
        tabindex="-1"
        class="progress-ring indeterminate"
        width="32"
        height="32"
        viewBox="0 0 16 16"
        role="status"
      >
        <circle
          cx="50%"
          cy="50%"
          r="7"
          stroke-dasharray="3"
          stroke-dashoffset="NaN"
          class="svelte-32f9k0"
        ></circle>
      </svg>
      <span v-if="statusMessage">{{ t(statusMessage, { hostId }) }}</span>
    </div>
  </div>
</template>

<style scoped>
  #display-wrapper {
    height: 100%;
    width: 100%;
    overflow: auto;
    position: relative;
  }

  #display {
    z-index: 0;
    position: relative;
    cursor: none;
  }

  #display canvas {
    width: 100%;
    height: 100%;
    transform: none !important;
    image-rendering: pixelated;
    image-rendering: crisp-edges;
  }

  .connecting {
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    gap: 16px;

    position: absolute;
    inset: 0;
    z-index: -1;

    line-height: var(--wui-line-height-body-strong);
    font-weight: 600;
    font-size: var(--wui-font-size-body-strong);
    font-family: var(--wui-font-family-text);
  }

  .connecting circle {
    fill: none;
    stroke: currentColor;
    stroke-width: 1.5;
    stroke-linecap: round;
    stroke-dasharray: 43.97;
    transform: rotate(-90deg);
    transform-origin: 50% 50%;
    transition: all var(--wui-control-normal-duration) linear;
    animation: root-splash-progress-ring-indeterminate 2s linear infinite;
  }
</style>

<style>
  #page:has(#display-wrapper) {
    padding: 0 !important;
    overflow: hidden !important;
    background-color: black !important;
  }
</style>
