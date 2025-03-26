declare module '@vue/runtime-core' {
  interface ComponentCustomProperties {
    /** The base path of the IIS application. It will always start and end with a forward slash. @example `/RAWeb/` */
    base: string;
    /** The doman and username of the current authenticated user, from the .ASPXAUTH cookie */
    authUser: {
      username: string;
      domain: string;
    };
  }
}

export {};
