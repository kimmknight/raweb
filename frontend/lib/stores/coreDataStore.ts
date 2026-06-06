import { isBrowser } from '$utils/environment.ts';
import { offline } from '$utils/offline.ts';
import { parse, stringify } from 'devalue';
import { defineStore } from 'pinia';
import { toRaw } from 'vue';

interface State extends EmptyState {
  userNamespace: string;

  /**
   * When this is true, the app should show a warning that the user needs to sign in again.
   * It usually means that the server thinks the user is unauthenticated, but the web app
   * is continuing to show the last authenticate user's information until they manually
   * sign out.
   */
  needsSignInAgain: boolean;

  /** The base path of the IIS application. It will always start and end with a forward slash. @example `/RAWeb/` */
  iisBase: string;

  /** The base path of the vue application. It will always start and end with a forward slash. @example `/RAWeb/app/` */
  appBase: string;

  /** The doman and username of the current authenticated user, from the .ASPXAUTH cookie */
  authUser: {
    username: string;
    domain: string;
    fullName: string;
    isLocalAdministrator: boolean;
  };

  /** An object that can be used to convert terminal server names to their aliases */
  terminalServerAliases: Record<string, string>;

  /** Policies that affect app settings for all users. They can be configured in Web.config. */
  policies: {
    combineTerminalServersModeEnabled?: boolean | null;
    favoritesEnabled?: boolean | null;
    flatModeEnabled?: boolean | null;
    hidePortsEnabled?: boolean | null;
    iconBackgroundsEnabled?: boolean | null;
    simpleModeEnabled?: boolean | null;
    passwordChangeEnabled?: boolean | null;
    openConnectionsInNewWindowEnabled?: boolean | null;
    anonymousAuthentication?: 'never' | 'always' | 'allow';
    signedInUserGlobalAlerts?: string | null;
    workspaceAuthBlocked?: boolean | null;
    connectionMethods?: {
      rdpFile: boolean | null;
      rdpProtocolUri: boolean | null;
    } | null;
  };

  /** The machine name (`Environment.MachineName`). If it has an alias, it is used instead. */
  machineName: string;

  /** The machine name (`Environment.MachineName`) */
  envMachineName: string;

  /** The fully-qualified domain name for the machine name on the local network. */
  envFQDN?: string;

  /** The current version of RAWeb Server */
  coreVersion: string;

  /** The build timestamp for the web client */
  webVersion: string;

  /** Server capabilities that may be different for newer versions or diferent settings/configurations */
  capabilities: {
    /** Whether the registry RemoteApps feature checks for ShowinTSWA in TSAppAllowList or checks the CentralizedPublishing subkeys forShowInPortal */
    supportsCentralizedPublishing?: boolean;
    /** Whether the client should attempt to redirect from localhost or an IP address
     * to the `envFQDN` value. The client MUST check whether the envFQDN can be reached. */
    supportsFqdnRedirect?: boolean;
    /** Whether the Guacd Web Client feature is enabled and properly configured */
    supportsGuacdWebClient?: boolean;
    /** Whether the the host server support WSL2 and the guacd.wsl image is available. */
    supportsWsl2?: boolean;
    /** Whether the host server supports remote desktop connections. */
    supportsTerminalServerConnections?: boolean;
  };

  /** The URL to the documentation site, or the wiki-redirect page if docs are excluded */
  docsUrl: string;
}

interface EmptyState {
  initialized: boolean;
  initializing?: boolean;
}

const _srr_data = {
  initialized: true,
  needsSignInAgain: true,
  appBase: '/',
  iisBase: '/',
  userNamespace: 'SSR',
  authUser: {
    username: 'SSRUser',
    domain: 'SSRDomain',
    fullName: 'SSR User',
    isLocalAdministrator: false,
  },
  terminalServerAliases: {},
  policies: {
    combineTerminalServersModeEnabled: false,
    favoritesEnabled: true,
    flatModeEnabled: false,
    hidePortsEnabled: false,
    iconBackgroundsEnabled: false,
    simpleModeEnabled: false,
    passwordChangeEnabled: false,
    anonymousAuthentication: 'never',
    signedInUserGlobalAlerts: null,
    workspaceAuthBlocked: null,
    connectionMethods: null,
    openConnectionsInNewWindowEnabled: null,
  },
  machineName: 'SSR-Machine',
  envMachineName: 'SSR-Machine',
  coreVersion: 'SSR-Version',
  webVersion: 'SSR-Web-Version',
  capabilities: {},
  docsUrl: '',
} satisfies State;

/**
 * Fetches the app-init-data from the server.
 *
 * If running in a non-browser environment (SSR), it returns mock data.
 */
async function fetchInitialData(): Promise<State> {
  if (isBrowser) {
    return fetch(__APP_INIT_DETAILS_API_PATH__)
      .then((res) => res.json())
      .then((json) => {
        // inject the docs url into the data
        const base = document.querySelector('base')?.getAttribute('href') || '/';
        const docsUrl =
          (__DOCS_EXCLUDED__
            ? `https://kimmknight.github.io/raweb/deploy-preview/v${json.coreVersion}/`
            : base) + 'docs';
        json.docsUrl = docsUrl;

        return json;
      });
  }

  return Promise.resolve(_srr_data);
}

export const useCoreDataStore = defineStore('coreData', {
  state: (): State => {
    // If there is state stored in localStorage, restore it.
    // We make sure that initialized and initializing are always false
    // so that the app knows to try to get the latest data from the API.
    const storedData = isBrowser ? localStorage.getItem(STORAGE_KEY) : null;
    if (storedData && typeof storedData === 'string') {
      try {
        const deserialized = parse(storedData);
        if (
          Array.isArray(deserialized) &&
          deserialized.length > 0 &&
          typeof deserialized[0] === 'object' &&
          deserialized[0] !== null
        ) {
          return {
            ...deserialized[0],
            initialized: false,
            initializing: false,
            needsSignInAgain: true,
          } as State;
        }
      } catch {
        // if there is an error parsing the stored data, clear it because it is invalid
        if (isBrowser) {
          localStorage.removeItem(STORAGE_KEY);
        }
      }
    }

    return { ..._srr_data, userNamespace: '', initialized: false, initializing: false }; // default data values
  },
  actions: {
    /**
     * Returns the current state of the store.
     */
    getCurrentState() {
      if (!this.initialized && !this.initializing) {
        throw new Error('coreDataStore state is not initialized');
      }

      // converthing this.$state to a plain object only preserves the properties in
      // the state that are objects, so we need to manually copy the others
      const snapshot = structuredClone(toRaw(this.$state));
      for (const key of Object.keys(this)) {
        if (key.startsWith('$') || key.startsWith('_') || key in snapshot) {
          continue;
        }

        const value = (this as any)[key];
        if (typeof value === 'function') {
          continue;
        }

        // @ts-ignore
        snapshot[key] = value;
      }

      return snapshot as State;
    },
    storeCurrentState() {
      if (!isBrowser || !this.userNamespace || this.userNamespace === 'RAWEB:UNAUTHENTICATED') {
        return;
      }

      const currentStorage = localStorage.getItem(STORAGE_KEY);
      let currentData: State[] | null | boolean | string | number = currentStorage
        ? parse(currentStorage)
        : null;
      if (!Array.isArray(currentData)) {
        currentData = [] as State[];
      }

      localStorage.setItem(
        STORAGE_KEY,
        stringify([
          ...currentData.filter((item) => item.userNamespace !== this.userNamespace),
          this.getCurrentState(),
        ])
      );
    },
    async fetchData() {
      // only fetch once
      if (this.initialized || this.initializing) {
        return;
      }

      this.initializing = true;
      await fetchInitialData()
        .then((data) => {
          if (typeof data !== 'object' || data === null) {
            throw new Error('Invalid data');
          }

          // In the case that the user is unauthenticated, we only want to use that
          // user information if there is no existing userNamespace in the store.
          const isUnauthenticated = data.userNamespace === 'RAWEB:UNAUTHENTICATED';
          const storedUserInfo = {
            userNamespace: this.userNamespace,
            authUser: this.authUser,
          };
          Object.assign(this, data);
          if (isUnauthenticated && storedUserInfo.userNamespace !== 'RAWEB:UNAUTHENTICATED') {
            this.userNamespace = storedUserInfo.userNamespace;
            this.authUser = storedUserInfo.authUser;
          }

          this.needsSignInAgain = isUnauthenticated;
          this.initialized = true;
        })
        .catch((error) => {
          console.log(navigator.onLine, 'Error fetching initial data:', error);
          if (!offline.value) {
            alert(
              'Catastrophic Error: \n    An error occurred while initializing the application data. \n\nPlease reload the page and try again. \n\nError details: \n' +
                error.message
            );
            window.location.reload();
          }
        })
        .finally(() => {
          this.initializing = false;

          // store in localStorage
          this.storeCurrentState();
        });
    },
    async __refetchCoreData() {
      this.initialized = false;
      this.fetchData();
    },
  },
});

const STORAGE_KEY = 'coreDataStore:data';
