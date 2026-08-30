import { defineStore } from 'pinia';

interface DialogStackEntry {
  id: string;
  acrylicBackdrop: boolean;
}

/**
 * Tracks the open ContentDialog instances in the order they were opened so that
 * dialogs can tell whether a dialog with an acrylic backdrop is open above them.
 **/
export const useDialogStackStore = defineStore('dialogStack', {
  state: () => ({
    stack: [] as DialogStackEntry[],
  }),
  actions: {
    pushDialog(id: string, acrylicBackdrop: boolean) {
      if (this.stack.some((entry) => entry.id === id)) {
        return;
      }
      this.stack.push({ id, acrylicBackdrop });
    },
    removeDialog(id: string) {
      const index = this.stack.findIndex((entry) => entry.id === id);
      if (index !== -1) {
        this.stack.splice(index, 1);
      }
    },
    updateDialog(id: string, acrylicBackdrop: boolean) {
      const index = this.stack.findIndex((entry) => entry.id === id);
      if (index !== -1) {
        this.stack[index].acrylicBackdrop = acrylicBackdrop;
      }
    },
    hasAcrylicBackdropAbove(id: string) {
      const index = this.stack.findIndex((entry) => entry.id === id);
      if (index === -1) {
        return false;
      }
      return this.stack.slice(index + 1).some((entry) => entry.acrylicBackdrop);
    },
  },
});
