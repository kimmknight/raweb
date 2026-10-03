<script setup lang="ts">
  import { Button, TextBlock } from '$components';
  import { requestCredentials as _requestCredentials, showConfirm } from '$dialogs';
  import { useCoreDataStore } from '$stores';
  import { debounce, openHelpPopup, openSignInPagePopup } from '$utils';
  import Guacamole from 'guacamole-common-js';
  import { useTranslation } from 'i18next-vue';
  import { storeToRefs } from 'pinia';
  import { computed, onMounted, onUnmounted, ref } from 'vue';
  import { useRoute, useRouter } from 'vue-router';
  import NotFound from '../404.vue';

  // the server will send a packet at least every 10 seconds,
  // so consider the connection closed if no packets are received
  // for slightly more than that
  const TUNNEL_RECEIVE_TIMEOUT_MS = 10100;

  const props = defineProps<import('./types.d.ts').PageProps>();

  const { t } = useTranslation();
  const route = useRoute();
  const router = useRouter();
  const { iisBase, appBase, docsUrl } = useCoreDataStore();
  const { needsSignInAgain } = storeToRefs(useCoreDataStore());

  function goBackOrClose() {
    route.meta.isDeviceCancelButton = true;
    if (window.opener && window.opener !== window) {
      window.close(); // fails unless the window was opened by RAWeb's javascript
    } else {
      router.back();
    }
  }

  function openHelp(errorCode: number | string) {
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

  const resourceGatewayHostname = computed(() => {
    const resource = props.workspace?.resources.find((resource) => resource.id === resourceId.value);
    const host = resource?.hosts.find((host) => host.id === hostId.value);
    if (host?.rdp?.gatewayusagemethod === 1 || host?.rdp?.gatewayusagemethod === 2) {
      return host?.rdp?.gatewayhostname as string | undefined;
    }
  });

  const resourceAuthenticationLevel = computed(() => {
    const resource = props.workspace?.resources.find((resource) => resource.id === resourceId.value);
    const host = resource?.hosts.find((host) => host.id === hostId.value);
    const authLevel = host?.rdp?.['authentication level'];

    if (authLevel === 0) {
      return 'ignore-certificate-errors';
    }
    if (authLevel === 1) {
      return 'require-valid-certificate';
    }
    if (authLevel === 2) {
      return 'show-warning-on-certificate-errors';
    }

    // default to level 2
    return 'show-warning-on-certificate-errors';
  });

  const state = ref<Guacamole.Client.State | null>(Guacamole.Client.State.DISCONNECTED);
  const destroyCurrentClient = ref<Promise<(() => void) | undefined>>(Promise.resolve(undefined));
  const errorMessage = ref<string | null>(null);
  const statusMessage = ref<string | null>('client.connecting');
  const reconnectOptions = ref<Parameters<typeof connect>[0] | null>(null);

  const renderedDimensions = ref<{ width: number; height: number } | null>(null);
  const browserDimensions = ref<{ width: number; height: number } | null>(null);
  const showDimensionsOverlay = ref(false);
  let dimensionsOverlayTimeoutId: number | null = null;
  function flashDimensionsOverlay() {
    showDimensionsOverlay.value = state.value === Guacamole.Client.State.CONNECTED;
    if (dimensionsOverlayTimeoutId !== null) {
      clearTimeout(dimensionsOverlayTimeoutId);
    }
    dimensionsOverlayTimeoutId = setTimeout(() => {
      showDimensionsOverlay.value = false;
    }, 2000) as unknown as number;
  }

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
    const displayWrapperElem = displayElement?.parentElement?.parentElement ?? undefined;

    /**
     * Computes how far the remote display's native resolution needs to be stretched
     * to fill displayWrapperElem. This is needed whenever a dimension is locked by the RDP
     * file's desktopwidth/desktopheight.
     *
     * This used used both to apply the CSS stretch and to translate pointer coordinates
     * back into the remote display's native coordinate space before sending them.
     */
    function getDisplayScale() {
      const display = client.getDisplay();
      const nativeWidth = display.getWidth();
      const nativeHeight = display.getHeight();
      return {
        x: displayWrapperElem && nativeWidth ? displayWrapperElem.clientWidth / nativeWidth : 1,
        y: displayWrapperElem && nativeHeight ? displayWrapperElem.clientHeight / nativeHeight : 1,
      };
    }

    function updateDisplayStretch() {
      const scale = getDisplayScale();
      displayElement.style.transformOrigin = '0 0';
      displayElement.style.transform = `scale(${scale.x}, ${scale.y})`;
    }

    // forward all mouse interaction over Guacamole connection
    const mouse = new Guacamole.Mouse(displayElement);
    const handleMouseEvent = (evt: Guacamole.Mouse.Event) => {
      const scale = getDisplayScale();
      const state = new Guacamole.Mouse.State(
        evt.state.x / scale.x,
        evt.state.y / scale.y,
        evt.state.left,
        evt.state.middle,
        evt.state.right,
        evt.state.up,
        evt.state.down
      );
      client.sendMouseState(state, false);
    };
    // @ts-expect-error
    mouse.onEach(['mousedown', 'mousemove', 'mouseup'], handleMouseEvent);

    // forward all touch interaction over Guacamole connection
    const touch = new Guacamole.Touch(displayElement);
    const handleTouchEvent = (evt: Guacamole.Touch.Event) => {
      const scale = getDisplayScale();
      const state = new Guacamole.Touch.State({
        id: evt.state.id,
        x: evt.state.x / scale.x,
        y: evt.state.y / scale.y,
        radiusX: evt.state.radiusX / scale.x,
        radiusY: evt.state.radiusY / scale.y,
        angle: evt.state.angle,
        force: evt.state.force,
      });
      client.sendTouchState(state, false);
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

    /**
     * Refreshes the rendered/browser dimensions shown in dimensionsOverlay and shows it.
     */
    function refreshDimensionsOverlay() {
      const display = client.getDisplay();
      renderedDimensions.value = { width: display.getWidth(), height: display.getHeight() };
      if (displayWrapperElem) {
        browserDimensions.value = {
          width: displayWrapperElem.clientWidth,
          height: displayWrapperElem.clientHeight,
        };
      }
      flashDimensionsOverlay();
    }

    // re-stretch the display whenever the remote resolution itself changes (e.g. once the
    // initial resolution is negotiated, or whenever an unlocked dimension is live-resized),
    // and show the current dimensions briefly
    client.getDisplay().onresize = () => {
      updateDisplayStretch();
      refreshDimensionsOverlay();
    };
    updateDisplayStretch();

    // adjust display size when client size changes
    let resizeObserver: ResizeObserver | null = null;
    let resizeDebouncerClear: (() => void) | null = null;
    if (displayWrapperElem) {
      const [sendResized, clear] = debounce(() => {
        if (state.value === Guacamole.Client.State.CONNECTED && displayWrapperElem) {
          client.sendSize(displayWrapperElem.clientWidth, displayWrapperElem.clientHeight);
        }
      }, 500);
      resizeDebouncerClear = clear;
      resizeObserver = new ResizeObserver(() => {
        updateDisplayStretch();
        refreshDimensionsOverlay();
        sendResized();
      });
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
    navigator.clipboard?.addEventListener('clipboardchange', handleClipboardChange);

    // when focus returns to the window, send the current clipboard data
    // since some browsers block reading the clipboard if the window is not focused
    const handleWindowFocus = () => {
      sendClientClipboard();
    };
    window.addEventListener('focus', handleWindowFocus);

    return () => {
      navigator.clipboard?.removeEventListener('clipboardchange', handleClipboardChange);
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
    ignoreGatewayCertificateError?: boolean;
    domain?: string;
    username?: string;
    password?: string;
    rPath: string;
    rFrom: string;
    isReconnect?: boolean;
    gatewayDomain?: string;
    gatewayUsername?: string;
    gatewayPassword?: string;
  }) {
    if (state.value !== Guacamole.Client.State.DISCONNECTED || currentClient.value !== null) {
      console.warn('Attempted to connect while a connection is already active.');
      await showConfirm(
        t('client.alreadyConnected.title'),
        t('client.alreadyConnected.message', { hostId: hostId.value }),
        t('client.alreadyConnected.retry'),
        t('client.alreadyConnected.cancel')
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
    const {
      ignoreCertificateError = false,
      ignoreGatewayCertificateError = false,
      rPath,
      rFrom,
      isReconnect,
    } = options;

    if (resourceConnectionIds.value === null) {
      return;
    }

    // ensure the display element is cleared before attempting to connect
    resetDisplay();

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
    const tunnel = new Guacamole.WebSocketTunnel(`${iisBase}guacd-tunnel`);
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
    connectionString += `&ignoreCertErrors=${ignoreCertificateError ? 'true' : 'false'}&ignoreGatewayCertErrors=${
      ignoreGatewayCertificateError ? 'true' : 'false'
    }`;
    client.connect(connectionString);

    // listen for custom instructions that demand additional configuration
    const originalOnInstruction = tunnel.oninstruction;
    tunnel.oninstruction = (opcode, parameters) => {
      if (opcode === 'raweb-demand-credentials') {
        // if we don't have credentials yet, request them from the user
        if (!options.username || !options.password || !options.domain) {
          // we need to temporarily unregister the event listeners so that they do
          // not interfere with the dialog that will be shown to the user
          unregisterEventListeners?.();

          requestCredentials()
            .then(({ credentials, done }) => {
              if (!isMounted.value) return done();

              options.domain = credentials.domain;
              options.username = credentials.username;
              options.password = credentials.password;

              tunnel.sendMessage('domain', options.domain);
              tunnel.sendMessage('username', options.username);
              tunnel.sendMessage('password', options.password);

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
              // re-register the event listeners after the dialog is closed
              unregisterEventListeners = registerEventListeners(displayElement, client);
            });
        } else {
          tunnel.sendMessage('domain', options.domain);
          tunnel.sendMessage('username', options.username);
          tunnel.sendMessage('password', options.password);
        }
      }

      if (opcode === 'raweb-demand-display-info') {
        tunnel.sendMessage('displayWidth', (displayWrapperElem?.clientWidth ?? 800) * 1);
        tunnel.sendMessage('displayHeight', (displayWrapperElem?.clientHeight ?? 600) * 1);
        tunnel.sendMessage('displayDPI', 96);
      }

      if (opcode === 'raweb-demand-gateway-credentials') {
        const hostname = parameters[0] as string;

        // if we do not have gateway credentials yet, request them from the user
        if (!options.gatewayUsername || !options.gatewayPassword || !options.gatewayDomain) {
          // we need to temporarily unregister the event listeners so that they do
          // not interfere with the dialog that will be shown to the user
          unregisterEventListeners?.();

          requestCredentials(
            t('client.creds.gatewayTitle'),
            t('client.creds.gatewayMessage', { hostId: hostname })
          )
            .then(({ credentials, done }) => {
              if (!isMounted.value) return done();

              options.gatewayUsername = credentials.username;
              options.gatewayPassword = credentials.password;
              options.gatewayDomain = credentials.domain;

              tunnel.sendMessage('gateway-domain', credentials.domain);
              tunnel.sendMessage('gateway-username', credentials.username);
              tunnel.sendMessage('gateway-password', credentials.password);

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
              // re-register the event listeners after the dialog is closed
              unregisterEventListeners = registerEventListeners(displayElement, client);
            });
        } else {
          tunnel.sendMessage('gateway-domain', options.gatewayDomain);
          tunnel.sendMessage('gateway-username', options.gatewayUsername);
          tunnel.sendMessage('gateway-password', options.gatewayPassword);
        }
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
        const serverErrorCode = parameters[0] as string;
        const serverErrorMessage = parameters[1] as string;
        console.error(`Guacd error ${serverErrorCode}: ${serverErrorMessage}`);
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

      const securityDialogOptions = {
        // size: 'max',
        titlebar: t('security.dialogTitle'),
        severity: 'caution',
        emphasizeCancelButton: true,
        titlebarIcon: {
          light: `${appBase}lib/assets/security-icon.svg`,
          dark: `${appBase}lib/assets/security-icon-dark.svg`,
        },
      } satisfies Parameters<typeof showConfirm>[4];

      // show a dialog asking the user to ignore certificate errors
      // if the certificate for the host is invalid
      if (
        // failed manual check using TCP connection to the host
        errorCode === 10003 ||
        // failure is from guacd when ignore-cert is false
        (errorCode === 519 &&
          parsedErrorMessage.includes('SSL/TLS connection failed (untrusted/self-signed certificate?)'))
      ) {
        // automatically retry with ignoring certificate errors if the authentication
        // level allows it without showing a dialog to the user
        if (resourceAuthenticationLevel.value === 'ignore-certificate-errors') {
          return retryWithOptions({ ...options, ignoreCertificateError: true });
        }

        const isStrictError = resourceAuthenticationLevel.value === 'require-valid-certificate';

        // the first line of the error message is a generic message about the certificate being invalid,
        // but we only need to show it when the error is allowed to be ignored
        const messageAfterSecondLineBreak = parsedErrorMessage.split('\n').slice(2).join('\n');
        const message =
          isStrictError && messageAfterSecondLineBreak ? messageAfterSecondLineBreak : parsedErrorMessage;

        return showConfirm(
          isStrictError ? t('client.certError.strictErrorTitle') : t('client.certError.tsTitle'),
          message,
          isStrictError ? '' : t('client.certError.yes'),
          isStrictError ? t('client.certError.cancel') : t('client.certError.no'),
          {
            ...securityDialogOptions,
            severity: isStrictError ? 'critical' : 'caution',
            subtitle: isStrictError ? t('client.certError.tsStrictErrorMessage') : undefined,
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

      // show a dialog asking the user to ignore gateway certificate errors
      // if the certificate for the gateway is invalid
      if (errorCode === 10037 || errorCode === 10044) {
        const isStrictError = errorCode === 10044;

        // the first line of the error message is a generic message about the certificate being invalid,
        // but we only need to show it when the error is allowed to be ignored
        const messageAfterSecondLineBreak = parsedErrorMessage.split('\n').slice(2).join('\n');
        const message =
          isStrictError && messageAfterSecondLineBreak ? messageAfterSecondLineBreak : parsedErrorMessage;

        return showConfirm(
          isStrictError ? t('client.certError.strictErrorTitle') : t('client.certError.gatewayTitle'),
          message,
          isStrictError ? '' : t('client.certError.yes'),
          isStrictError ? t('client.certError.cancel') : t('client.certError.no'),
          {
            ...securityDialogOptions,
            severity: isStrictError ? 'critical' : 'caution',
            subtitle: isStrictError ? t('client.certError.gatewayStrictErrorMessage') : undefined,
          }
        )
          .then(async (done) => {
            if (!isMounted.value) return done();

            // retry connection
            done();
            retryWithOptions({ ...options, ignoreGatewayCertificateError: true });
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
        // if the host uses a gateway, we cannot determine whether the credentials for the host or the gateway are incorrect,
        // so we must ask the user to re-enter both sets of credentials after the connection process starts again
        if (resourceGatewayHostname.value) {
          return showConfirm(
            t('client.creds.allFail.title'),
            t('client.creds.allFail.message', {
              hostId: hostId.value,
              gatewayHostId: resourceGatewayHostname.value,
            }),
            t('client.creds.allFail.retry'),
            t('client.creds.allFail.cancel'),
            { size: 'max', helpAction: () => openHelp(errorCode === 519 ? '519-1' : errorCode) }
          )
            .then(async (done) => {
              if (!isMounted.value) return done();

              // retry connection
              done();
              retryWithOptions({
                ...options,
                // clear any previously saved credentials since they may be invalid
                domain: undefined,
                username: undefined,
                password: undefined,
                gatewayDomain: undefined,
                gatewayUsername: undefined,
                gatewayPassword: undefined,
              });
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

              // Also clear any previously saved gateway credentials since they may be invalid.
              // RAWeb's server will demand new gateway credentials if they are needed.
              gatewayDomain: undefined,
              gatewayUsername: undefined,
              gatewayPassword: undefined,
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

      // malformed or incorrect gateway credentials: request new credentials from the user
      const isInvalidGatewayCredentialsError =
        !!resourceGatewayHostname.value &&
        errorCode === 771 &&
        parsedErrorMessage.includes('Access denied by server (account locked/disabled?)');
      if (errorCode === 10007 || errorCode === 10008 || isInvalidGatewayCredentialsError) {
        return requestCredentials(
          t('client.creds.gatewayfailtitle'),
          t('client.creds.gatewayfailmessage', { hostId: resourceGatewayHostname.value }),
          t('client.creds.failerror')
        )
          .then(async ({ credentials, done }) => {
            if (!isMounted.value) return done();

            // retry connection with new credentials
            done();
            retryWithOptions({
              ...options,
              gatewayDomain: credentials.domain,
              gatewayUsername: credentials.username,
              gatewayPassword: credentials.password,
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
  const connectTimeoutId = ref<number | null>(null);
  onUnmounted(() => {
    if (connectTimeoutId.value !== null) {
      clearTimeout(connectTimeoutId.value);
    }
    isMounted.value = false;
    destroyCurrentClient.value.then((destroy) => destroy?.());
  });

  // start the connection when the component is mounted
  onMounted(() => {
    isMounted.value = true;
    connectTimeoutId.value = setTimeout(() => {
      destroyCurrentClient.value = connect({
        rPath: resourceConnectionIds.value?.resourcePath ?? '',
        rFrom: resourceConnectionIds.value?.resourceFrom ?? '',
      });
    }, 300) as unknown as number;
  });

  // reset connection state on hot module replacement (dev mode)
  if (import.meta.hot) {
    import.meta.hot.dispose(() => {
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
        return result.state !== 'denied';
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
    renderedDimensions.value = null;
    browserDimensions.value = null;
    showDimensionsOverlay.value = false;
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

  <div v-else-if="needsSignInAgain" class="full-page-notice">
    <TextBlock variant="subtitle">{{ t('needsSignInAgain.title') }}</TextBlock>
    <TextBlock block>{{ t('needsSignInAgain.message') }}</TextBlock>
    <div class="button-row">
      <Button
        variant="accent"
        @click.prevent="
          openSignInPagePopup('sign-in-again', () => {
            refreshWorkspace();
            reconnect();
          })
        "
        >{{ t('needsSignInAgain.action') }}</Button
      >
    </div>
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

    <Transition name="dimensions-overlay-fade">
      <div
        class="dimensions-overlay acrylic"
        :style="`--acrylic-noise: url(${appBase}lib/assets/acrylic-noise.png);`"
        v-if="showDimensionsOverlay && renderedDimensions && browserDimensions"
      >
        <svg
          class="dimensions-overlay-icon"
          width="24"
          height="24"
          viewBox="0 0 24 24"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
        >
          <path
            d="M6.75 22.0004C6.33579 22.0004 6 21.6647 6 21.2504C6 20.8707 6.28215 20.557 6.64823 20.5073L6.75 20.5004L8.499 20.5V18.002L4.25 18.0023C3.05914 18.0023 2.08436 17.0771 2.00519 15.9063L2 15.7523V5.25C2 4.05914 2.92516 3.08436 4.09595 3.00519L4.25 3H19.7488C20.9397 3 21.9145 3.92516 21.9936 5.09595L21.9988 5.25V15.7523C21.9988 16.9431 21.0737 17.9179 19.9029 17.9971L19.7488 18.0023L15.499 18.002V20.5L17.25 20.5004C17.6642 20.5004 18 20.8362 18 21.2504C18 21.6301 17.7178 21.9439 17.3518 21.9936L17.25 22.0004H6.75ZM13.998 18.002H9.998L9.999 20.5004H13.999L13.998 18.002ZM19.7488 4.5H4.25C3.8703 4.5 3.55651 4.78215 3.50685 5.14823L3.5 5.25V15.7523C3.5 16.132 3.78215 16.4458 4.14823 16.4954L4.25 16.5023H19.7488C20.1285 16.5023 20.4423 16.2201 20.492 15.854L20.4988 15.7523V5.25C20.4988 4.8703 20.2167 4.55651 19.8506 4.50685L19.7488 4.5Z"
            fill="currentColor"
          />
        </svg>
        <div class="dimensions-overlay-text">
          <TextBlock variant="body" block>
            {{
              t('client.dimensionsOverlay.connection', {
                width: renderedDimensions.width,
                height: renderedDimensions.height,
              })
            }}
          </TextBlock>
          <TextBlock variant="caption" block class="dimensions-overlay-browser">
            {{
              t('client.dimensionsOverlay.browser', {
                width: browserDimensions.width,
                height: browserDimensions.height,
              })
            }}
          </TextBlock>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
  #display-wrapper {
    height: 100%;
    width: 100%;
    overflow: hidden;
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

  .dimensions-overlay {
    position: absolute;
    top: 64px;
    left: 50%;
    transform: translateX(-50%);
    z-index: 10;

    display: flex;
    flex-direction: row;
    align-items: center;
    gap: 1rem;

    padding: 0.75rem 1.25rem;
    border-radius: var(--wui-overlay-corner-radius);
    background-color: var(--wui-acrylic-backdrop-background-color);
    backdrop-filter: blur(24px) saturate(150%);
    -webkit-backdrop-filter: blur(24px) saturate(150%);
    box-shadow: var(--wui-flyout-shadow);
    color: var(--wui-text-primary);
    border: 1px solid var(--wui-surface-stroke-default);

    pointer-events: none;
  }
  .dimensions-overlay.acrylic {
    /* noise texture + luminosity blend (saturation part) */
    backdrop-filter: blur(24px) saturate(4);
    background:
      /* luminosity blend (exclusion part) */
      linear-gradient(
        oklch(from var(--wui-acrylic-backdrop-background-color) l c h / 10%),
        oklch(from var(--wui-acrylic-backdrop-background-color) l c h / 10%)
      ),
      /* tint/color blend */
      linear-gradient(
          oklch(from var(--wui-acrylic-backdrop-background-color) l c h / 80%),
          oklch(from var(--wui-acrylic-backdrop-background-color) l c h / 80%)
        ),
      /* noise texture */ var(--acrylic-noise);
    background-blend-mode: exclusion, normal, normal;
  }

  .dimensions-overlay-icon {
    flex-shrink: 0;
    width: 24px;
    height: 24px;
  }

  .dimensions-overlay-text {
    display: flex;
    flex-direction: column;
  }

  .dimensions-overlay-browser {
    color: var(--wui-text-secondary);
  }

  .dimensions-overlay-fade-enter-active,
  .dimensions-overlay-fade-leave-active {
    transition:
      opacity 0.15s ease,
      transform 0.15s ease;
  }

  .dimensions-overlay-fade-enter-from,
  .dimensions-overlay-fade-leave-to {
    opacity: 0;
    transform: translateX(-50%) translateY(-8px);
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

  .full-page-notice {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    block-size: calc(100% - 52px);
    gap: 8px;
    padding: 24px 16px;
    background-color: var(--wui-subtle-transparent);
    border-radius: var(--wui-control-corner-radius);
    box-sizing: border-box;
    text-align: center;
  }
  .full-page-notice .button-row {
    display: flex;
    flex-direction: row;
    gap: 8px;
    margin-top: 12px;
  }
</style>
