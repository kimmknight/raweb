declare global {
  interface Window {
    __namespace: string;

    /** The base path of the IIS application. It will always start and end with a forward slash. @example `/RAWeb/` */
    __iisBase: string;

    /** The base path of the vue application. It will always start and end with a forward slash. @example `/RAWeb/app/` */
    __base: string;

    /** The doman and username of the current authenticated user, from the .ASPXAUTH cookie */
    __authUser: {
      username: string;
      domain: string;
    };

    /** An object that can be used to convert terminal server names to their aliases */
    __terminalServerAliases: Record<string, string>;

    /** Policies that affect app settings for all users. They can be configured in Web.config. */
    __policies: {
      combineTerminalServersModeEnabled: string;
      favoritesEnabled: string;
      flatModeEnabled: string;
      iconBackgroundsEnabled: string;
      simpleModeEnabled: string;
    };
  }
}

export {};
