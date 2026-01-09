import { defineStore } from 'pinia';

export const usePopupWindow = defineStore('windows', {
  state: () => ({
    openedWindows: new Map<string, Window | null>(),
  }),
  actions: {
    openWindow(url: string, id: string, features?: string) {
      if (this.openedWindows.has(id)) {
        const existingWindow = this.openedWindows.get(id);
        if (existingWindow && !existingWindow.closed) {
          existingWindow.focus();
          return;
        } else {
          this.openedWindows.delete(id);
        }
      }

      const newWindow = window.open(url, id, features);
      this.openedWindows.set(id, newWindow);
    },
    closeWindow(id: string) {
      const win = this.openedWindows.get(id);
      if (win) {
        win.close();
        this.openedWindows.delete(id);
      }
    },
  },
});
