declare module '@vue/runtime-core' {
  interface ComponentCustomProperties {
    /** The base path of the IIS application. It will always start and end with a forward slash. @example `/RAWeb/` */
    base: string;
    /** The doman and username of the current authenticated user, from the .ASPXAUTH cookie */
    authUser: {
      username: string;
      domain: string;
    };
    /** An object that can be used to convert terminal server names to their aliases */
    terminalServerAliases: Record<string, string>;
  }
}

declare global {
  interface Window {
    __namespace: string;
  }
}

export {};
