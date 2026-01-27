<script setup lang="ts">
  import { requestCredentials as _requestCredentials, showConfirm } from '$dialogs';
  import { useCoreDataStore } from '$stores';
  import { debounce, getAppsAndDevices, openHelpPopup, useWebfeedData } from '$utils';
  import Guacamole from 'guacamole-common-js';
  import { useTranslation } from 'i18next-vue';
  import { computed, onMounted, onUnmounted, ref } from 'vue';
  import { useRoute, useRouter } from 'vue-router';
  import NotFound from '../404.vue';

  // the server will send a packet at least every 10 seconds,
  // so consider the connection closed if no packets are received
  // for slightly more than that
  const TUNNEL_RECEIVE_TIMEOUT_MS = 10100;

  const props = defineProps<{
    workspace: Awaited<ReturnType<typeof getAppsAndDevices>>;
    refreshWorkspace: () => ReturnType<typeof useWebfeedData>['refresh'];
  }>();

  const { t } = useTranslation();
  const route = useRoute();
  const router = useRouter();
  const { capabilities, iisBase, appBase, docsUrl } = useCoreDataStore();

  if (!capabilities.supportsGuacdWebClient) {
    router.replace('/404');
  }

  function goBackOrClose() {
    route.meta.isDeviceCancelButton = true;
    if (window.opener && window.opener !== window) {
      window.close(); // fails unless the window was opened by RAWeb's javascript
    } else {
      router.back();
    }
  }

  function openHelp(errorCode: number) {
    openHelpPopup(`${docsUrl}/web-client/errors/#code${errorCode}`);
  }

  // determine the host to connect to based on the route params
  const resourceId = computed(() => router.currentRoute.value.params.resourceId as string);
  const hostId = computed(() => (router.currentRoute.value.params.hostId as string).replace('‾', ':'));

  // extract the identifiers used by the server to configure the connection
  const resourceConnectionIds = computed(() => {
    const resource = props.workspace?.resources.find((resource) => resource.id === resourceId.value);
    const host = resource?.hosts.find((host) => host.id === hostId.value);
    const resourceHostUrl = host?.url;
    if (!resourceHostUrl) {
      return null;
    }

    const resourcePath = resourceHostUrl.pathname.replace(`${iisBase}api/resources/`, '');
    const resourceFrom = resourceHostUrl.searchParams.get('from') ?? 'rdp';
    if (!resourcePath) return null;
    return { resourcePath, resourceFrom };
  });

  const state = ref<Guacamole.Client.State | null>(Guacamole.Client.State.DISCONNECTED);
  const destroyCurrentClient = ref<Promise<(() => void) | undefined>>(Promise.resolve(undefined));
  const errorMessage = ref<string | null>(null);
  const statusMessage = ref<string | null>('client.connecting');
  const reconnectOptions = ref<Parameters<typeof connect>[0] | null>(null);

  /**
   * Starts a new connection using the previously saved options.
   * Messaging uses the word 'reconnecting' instead of 'connecting',
   * but it is actually an entirely new connection.
   *
   * Before calling this function, you may need to terminate the
   * old connection.
   *
   * This function will reset the display area and set the status message.
   */
  async function reconnect() {
    if (!reconnectOptions.value) {
      return;
    }
    resetDisplay();
    statusMessage.value = 'client.reconnecting';
    errorMessage.value = null;
    (await destroyCurrentClient.value)?.();
    destroyCurrentClient.value = connect({
      ...reconnectOptions.value,
      isReconnect: true,
    });
  }

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

    // adjust display size when client size changes
    const displayWrapperElem = displayElement?.parentElement?.parentElement ?? undefined;
    let resizeObserver: ResizeObserver | null = null;
    let resizeDebouncerClear: (() => void) | null = null;
    if (displayWrapperElem) {
      const [sendResized, clear] = debounce(() => {
        if (state.value === Guacamole.Client.State.CONNECTED && displayWrapperElem) {
          client.sendSize(displayWrapperElem.clientWidth, displayWrapperElem.clientHeight);
        }
      }, 200);
      resizeDebouncerClear = clear;
      resizeObserver = new ResizeObserver(sendResized);
      resizeObserver.observe(displayWrapperElem);
    }

    return () => {
      // @ts-expect-error
      mouse.offEach(['mousedown', 'mousemove', 'mouseup'], handleMouseEvent);
      // @ts-expect-error
      touch.offEach(['touchstart', 'touchmove', 'touchend'], handleTouchEvent);
      keyboard.onkeydown = null;
      keyboard.onkeyup = null;
      resizeObserver?.disconnect();
      resizeDebouncerClear?.();
    };
  }

  /**
   * Sends the current clipboard data from the user's clipboard to the remote device,
   * and then sets up event listeners to keep the clipboard in sync between the client
   * device and the remote device.
   *
   * Do not call this function until the Guacamole client is connected.
   * If the client gets disconnected, unregister the event listeners registered by this
   * function and call it again after reconnecting to ensure the clipboard stays in sync.
   */
  function registerClipboard(client: Guacamole.Client) {
    function sendClientClipboard() {
      if (!navigator.clipboard) {
        return;
      }

      // many browsers block clipboard access if the window is not focused
      if (!document.hasFocus()) {
        return;
      }

      navigator.clipboard
        .readText()
        .then((text) => {
          const stream = client.createClipboardStream('text/plain');
          const writer = new Guacamole.StringWriter(stream);
          writer.sendText(text);
          writer.sendEnd();
        })
        .catch((err) => {
          console.error('Failed to read from clipboard:', err);
        });
    }

    // send client clipboard on initial connection
    sendClientClipboard();

    // handle incoming clipboard data from the remote device and write it to the user's clipboard
    client.onclipboard = (stream: Guacamole.InputStream, mimetype: string) => {
      // guacd only sends plain text clipboard data
      if (mimetype !== 'text/plain') {
        console.warn(`Unsupported clipboard MIME type: ${mimetype}`);
        return;
      }

      // read the clipboard data from the input stream
      // and write it to the user's clipboard when the stream ends
      const reader = new Guacamole.StringReader(stream);
      let clipboardData = '';
      reader.ontext = (text) => {
        clipboardData += text;
      };
      reader.onend = () => {
        navigator.clipboard.writeText(clipboardData).catch((err) => {
          console.error('Failed to write to clipboard:', err);
        });
      };
    };

    // when the user's clipboard changes, send the new clipboard data to the remote device
    const handleClipboardChange = (event: Event) => {
      sendClientClipboard();
    };
    navigator.clipboard.addEventListener('clipboardchange', handleClipboardChange);

    // when focus returns to the window, send the current clipboard data
    // since some browsers block reading the clipboard if the window is not focused
    const handleWindowFocus = () => {
      sendClientClipboard();
    };
    window.addEventListener('focus', handleWindowFocus);

    return () => {
      navigator.clipboard.removeEventListener('clipboardchange', handleClipboardChange);
      window.removeEventListener('focus', handleWindowFocus);
      client.onclipboard = null;
    };
  }

  const currentClient = ref<Guacamole.Client | null>(null);
  const hasShownClipboardWarning = ref(false);

  /**
   * Connects to a remote device using the provided options.
   */
  async function connect(options: {
    ignoreCertificateError?: boolean;
    domain?: string;
    username?: string;
    password?: string;
    rPath: string;
    rFrom: string;
    isReconnect?: boolean;
  }) {
    if (state.value !== Guacamole.Client.State.DISCONNECTED || currentClient.value !== null) {
      console.warn('Attempted to connect while a connection is already active.');
      await showConfirm(
        t('client.alreadyConnected.title'),
        t('client.alreadyConnected.message', { hostId: hostId.value }),
        t('client.alreadyConnected.retry'),
        t('dialog.alreadyConnected.cancel')
      )
        .then((done) => {
          if (!isMounted.value) return done();

          // retry connection
          window.location.reload();
          done();
        })
        .catch((err) => {
          if (!isMounted.value) return;
          const fromNavigateAway = typeof err === 'string' && err === 'NAVIGATE_AWAY';
          if (!fromNavigateAway) {
            goBackOrClose();
          }
        })
        .finally(() => {
          errorMessage.value = null;
        });
      return;
    }

    reconnectOptions.value = options;
    const { ignoreCertificateError = false, rPath, rFrom, isReconnect } = options;
    let { domain, username, password } = options;

    if (resourceConnectionIds.value === null) {
      return;
    }

    // ensure the display element is cleared before attempting to connect
    resetDisplay();

    // if we don't have credentials yet, request them from the user
    let exitEarly = false;
    if (!username || !password || !domain) {
      await requestCredentials()
        .then(({ credentials, done }) => {
          if (!isMounted.value) return done();
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

          if (!isMounted.value) return;

          const fromNavigateAway = typeof err === 'string' && err === 'NAVIGATE_AWAY';
          if (!fromNavigateAway) {
            goBackOrClose();
          }
        });
    }
    if (exitEarly) {
      return;
    }

    // confirm clipboard API support and access, and show a warning if unavailable
    if (hasShownClipboardWarning.value === false) {
      await checkClipboardAccess().catch(async (error) => {
        if (!(error instanceof ClipboardAccessError)) {
          console.error('Unexpected error while checking clipboard access:', error);
          return;
        }

        await showConfirm(
          t(`client.clipboardError.${error.type}.title`),
          t(`client.clipboardError.${error.type}.message`),
          '',
          t('dialog.ok')
        ).catch(() => null);
      });
      hasShownClipboardWarning.value = true;
    }

    state.value = Guacamole.Client.State.CONNECTING;

    // configure the connection to guacd
    const tunnel = new Guacamole.WebSocketTunnel(`${iisBase}guacd-tunnel/`);
    tunnel.receiveTimeout = TUNNEL_RECEIVE_TIMEOUT_MS;
    tunnel.unstableThreshold = 5;
    const client = new Guacamole.Client(tunnel);
    currentClient.value = client;

    tunnel.onerror = (error) => {
      if (error.code === Guacamole.Status.Code.UPSTREAM_TIMEOUT) {
        console.warn(
          `Guacamole tunnel did not receive data for ${parseInt(String(tunnel.receiveTimeout / 1000))} seconds. The connection is now considered to be closed.`
        );
        client.disconnect();
        return;
      }

      if (error.code === Guacamole.Status.Code.UPSTREAM_NOT_FOUND) {
        console.warn(`The Guacamole tunnel was closed.`);
        errorMessage.value = t('client.tunnelClosed');
        client.disconnect();
        reconnect();
        return;
      }

      const errorCode = error.code as Guacamole.Status.Code | number;
      const errorCodeId =
        Object.entries(Guacamole.Status.Code).find(([_, code]) => code === errorCode)?.[0] ?? 'UNKNOWN';
      console.error('Guacamole tunnel error:', errorCodeId, error);
      client.disconnect();
    };

    // attach the display to the DOM
    const displayElement = client.getDisplay().getElement();
    const container = document.getElementById('display');
    const displayWrapperElem = container?.parentElement ?? undefined;
    let unregisterEventListeners: ReturnType<typeof registerEventListeners> | null = null;
    if (container) {
      container.appendChild(displayElement);
      unregisterEventListeners = registerEventListeners(displayElement, client);
    }

    // set the cursor displayed by the browser to match the cursor provided by the remote device
    client.getDisplay().oncursor = (cursorCanvas: HTMLCanvasElement, hotspotX: number, hotspotY: number) => {
      // extract the cursor image from the canvas
      const cursorUrl = cursorCanvas.toDataURL();

      if (container) {
        // set the css cursor for the display element to the cursor image, with the correct hotspot
        container.style.cursor = `url(${cursorUrl}) ${hotspotX} ${hotspotY}, auto`;

        // hide the default cursor layer provided by Guacamole since we are using the browser's cursor rendering
        client.getDisplay().getCursorLayer().dispose();
      }
    };

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
        tunnel.sendMessage('displayWidth', (displayWrapperElem?.clientWidth ?? 800) * 1);
        tunnel.sendMessage('displayHeight', (displayWrapperElem?.clientHeight ?? 600) * 1);
        tunnel.sendMessage('displayDPI', 96);
      }

      if (opcode === 'raweb-demand-timezone') {
        const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
        tunnel.sendMessage('timezone', timezone);
      }

      if (opcode === 'raweb-msg-starting-service') {
        statusMessage.value = 'client.startingService';
      }

      if (opcode === 'raweb-msg-service-started') {
        statusMessage.value = isReconnect ? 'client.reconnecting' : 'client.connecting';
      }

      if (opcode === 'raweb-msg-installing-service') {
        statusMessage.value = 'client.installingService';
      }

      if (opcode === 'raweb-console-error') {
        const errorCode = parameters[0] as string;
        const errorMessage = parameters[1] as string;
        console.error(`Guacd error ${errorCode}: ${errorMessage}`);
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
      unregisterEventListeners?.();

      const retryWithOptions = async (newOptions: typeof options) => {
        if (!isMounted.value) return;

        // retry connection
        (await destroyCurrentClient.value)?.();
        destroyCurrentClient.value = connect(newOptions);
      };

      // show a dialog asking the user to ignore certificate errors
      // if the certificate for the host is invalid
      if (errorCode === 10003) {
        // TODO: translations
        return showConfirm(
          'The identity of the remote computer could not be verified. Do you want to connect anyway?',
          parsedErrorMessage,
          'Yes',
          'No',
          {
            // size: 'max',
            titlebar: 'RAWeb Security',
            severity: 'caution',
            emphasizeCancelButton: true,
            titlebarIcon: {
              light: `${appBase}lib/assets/security-icon.svg`,
              dark: `${appBase}lib/assets/security-icon-dark.svg`,
            },
          }
        )
          .then(async (done) => {
            if (!isMounted.value) return done();

            // retry connection
            done();
            retryWithOptions({ ...options, ignoreCertificateError: true });
          })
          .catch((err) => {
            if (!isMounted.value) return;

            const fromNavigateAway = typeof err === 'string' && err === 'NAVIGATE_AWAY';
            if (!fromNavigateAway) {
              goBackOrClose();
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
          .then(async ({ credentials, done }) => {
            if (!isMounted.value) return done();

            // retry connection with new credentials
            done();
            retryWithOptions({
              ...options,
              domain: credentials.domain,
              username: credentials.username,
              password: credentials.password,
            });
          })
          .catch((err) => {
            const fromNavigateAway = typeof err === 'string' && err === 'NAVIGATE_AWAY';
            if (!fromNavigateAway) {
              goBackOrClose();
            }
          })
          .finally(() => {
            errorMessage.value = null;
          });
      }

      // the terminal server could not be reached
      if (errorCode === 10026 || errorCode === 10010 || errorCode === 10027) {
        return showConfirm(
          t('client.unreachable.title'),
          t('client.unreachable.message', { hostId: hostId.value }),
          t('client.unreachable.retry'),
          t('client.unreachable.cancel'),
          { size: 'max', helpAction: () => openHelp(errorCode) }
        )
          .then(async (done) => {
            if (!isMounted.value) return done();

            // retry connection
            done();
            retryWithOptions(options);
          })
          .catch((err) => {
            if (!isMounted.value) return;
            const fromNavigateAway = typeof err === 'string' && err === 'NAVIGATE_AWAY';
            if (!fromNavigateAway) {
              goBackOrClose();
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
        t('client.connectionError.cancel'),
        { helpAction: () => openHelp(errorCode) }
      )
        .then(async (done) => {
          if (!isMounted.value) return done();

          // retry connection
          done();
          retryWithOptions(options);
        })
        .catch((err) => {
          if (!isMounted.value) return;
          const fromNavigateAway = typeof err === 'string' && err === 'NAVIGATE_AWAY';
          if (!fromNavigateAway) {
            goBackOrClose();
          }
        })
        .finally(() => {
          errorMessage.value = null;
        });
    };

    /**
     * Show a message when the connection has been closed without any errors
     */
    function handleDisconnect(newState: Guacamole.Client.State) {
      if (newState !== Guacamole.Client.State.DISCONNECTED) {
        return;
      }

      if (!errorMessage.value) {
        resetDisplay();
        setTimeout(() => {
          if (isMounted.value) {
            showConfirm(
              t('client.disconnected.title'),
              t('client.disconnected.message', { hostId: hostId.value }),
              t('client.disconnected.reconnect'),
              t('client.disconnected.leave')
            )
              .then((done) => {
                if (!isMounted.value) return done();

                // retry connection
                done();
                reconnect();
              })
              .catch(() => {
                if (!isMounted.value) return;
                goBackOrClose();
              });
          }
        }, 300);
      }
    }

    let cleanupClipboardEvents: (() => void) | null = null;
    function handleClipboard(newState: Guacamole.Client.State) {
      if (newState === Guacamole.Client.State.CONNECTED) {
        cleanupClipboardEvents = registerClipboard(client);
      } else if (cleanupClipboardEvents) {
        cleanupClipboardEvents();
      }
    }

    client.onstatechange = (newState) => {
      state.value = newState;

      if (!isMounted.value) {
        return;
      }

      // ensure that the display size is correct once the connection is established
      if (newState === Guacamole.Client.State.CONNECTED && displayWrapperElem) {
        client.sendSize(displayWrapperElem.clientWidth, displayWrapperElem.clientHeight);
      }

      handleDisconnect(newState);
      handleClipboard(newState);
    };

    /**
     * Resets state, clears event listeners, and disconnects the Guacamole client.
     */
    const destroy = () => {
      client.disconnect();
      cleanupClipboardEvents?.();
      currentClient.value = null;
      state.value = Guacamole.Client.State.DISCONNECTED;
      tunnel.oninstruction = originalOnInstruction;
      tunnel.onerror = null;
      client.onerror = null;
      unregisterEventListeners?.();

      window.removeEventListener('beforeunload', destroy);
    };

    // ensure the connection is closed when the user leaves the page
    window.addEventListener('beforeunload', destroy);

    return destroy;
  }

  // block navigation away from the page because Guacamole does not capture
  // the back and forward buttons from the mouse
  const removeGuard = ref<() => void>();
  onMounted(() => {
    removeGuard.value = router.beforeEach((to, from) => {
      if (from.meta.isTitlebarBackButton || from.meta.isDeviceCancelButton) {
        return true;
      }
      return false; // block navigation
    });
  });
  onUnmounted(() => {
    removeGuard.value?.();
  });

  // disconnect the client when the component is unmounted
  const isMounted = ref(true);
  onUnmounted(() => {
    isMounted.value = false;
    destroyCurrentClient.value.then((destroy) => destroy?.());
  });

  // start the connection when the component is mounted
  onMounted(() => {
    isMounted.value = true;
    setTimeout(() => {
      destroyCurrentClient.value = connect({
        rPath: resourceConnectionIds.value?.resourcePath ?? '',
        rFrom: resourceConnectionIds.value?.resourceFrom ?? '',
      });
    }, 300);
  });

  // reset connection state on hot module replacement (dev mode)
  if (import.meta.hot) {
    import.meta.hot.dispose(() => {
      state.value = Guacamole.Client.State.CONNECTING;
      reconnect();
    });
  }

  type ClipboardAccessErrorType =
    | 'NO_CLIPBOARD_API'
    | 'NO_PERMISSIONS_API'
    | 'NO_PERMISSION_TO_READ'
    | 'NO_PERMISSION_TO_WRITE';
  class ClipboardAccessError extends Error {
    type: ClipboardAccessErrorType;
    constructor(type: ClipboardAccessErrorType, message: string) {
      super(message);
      this.name = 'ClipboardAccessError';
      this.type = type;
    }
  }

  /**
   * Checks if the clipboard can be accessed (read and write). If needed, it prompts
   * the user to grant permissions to access the clipboard.
   *
   * This function returns a promise that resolves if clipboard access is granted and
   * rejects if access is denied.
   *
   * This check should be performed before attempting to connect to the remote device,
   * and if it fails, an alert should be shown to the user explaining that clipboard
   * functionality will be unavailable unless they grant clipboard permissions in
   * a web browser with the clipboard API.
   */
  async function checkClipboardAccess() {
    // check if the clipboard API is available
    if (!navigator.clipboard) {
      return Promise.reject(new ClipboardAccessError('NO_CLIPBOARD_API', 'Clipboard API not available'));
    }

    if (!navigator.permissions) {
      return Promise.reject(new ClipboardAccessError('NO_PERMISSIONS_API', 'Permissions API not available'));
    }

    // check if we have permission to read to the clipboard
    const canReadClipboard = await navigator.permissions
      .query({ name: 'clipboard-read' as PermissionName })
      .then((result) => {
        if (!isMounted.value) return false;

        if (result.state === 'granted') {
          return true;
        } else if (result.state === 'denied') {
          return false;
        } else {
          // if permission is prompt, try to read from the clipboard to trigger the permission prompt
          return navigator.clipboard
            .readText()
            .then(() => true)
            .catch(() => false);
        }
      });
    if (!canReadClipboard) {
      return Promise.reject(
        new ClipboardAccessError('NO_PERMISSION_TO_READ', 'No permission to read from clipboard')
      );
    }

    // check if we have permission to write to the clipboard
    const canWriteClipboard = await navigator.permissions
      .query({ name: 'clipboard-write' as PermissionName })
      .then((result) => {
        if (!isMounted.value) return false;

        if (result.state === 'granted') {
          return true;
        } else if (result.state === 'denied') {
          return false;
        } else {
          // if permission is prompt, try to write to the clipboard to trigger the permission prompt
          return navigator.clipboard
            .writeText('clipboard access test')
            .then(() => true)
            .catch(() => false);
        }
      });
    if (!canWriteClipboard) {
      return Promise.reject(
        new ClipboardAccessError('NO_PERMISSION_TO_WRITE', 'No permission to write to clipboard')
      );
    }

    return Promise.resolve<true>(true);
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
        width="48"
        height="48"
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
    color: white;
  }

  #display {
    z-index: 0;
    position: relative;
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
    gap: 24px;
    margin-top: 28px;

    position: absolute;
    inset: 0;
    z-index: -1;

    font-size: 24px;
    font-weight: 600;
    line-height: 32px;
    font-family: var(--wui-font-family-display);
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

  main:has(#display-wrapper) {
    border-top-left-radius: 0;
  }
</style>
