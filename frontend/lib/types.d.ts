declare global {
  interface Window {
    __pinia: import('pinia').Pinia;
  }
}

export {};
