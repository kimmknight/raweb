export interface DraggableArea {
  x: number;
  y: number;
  w: number;
  h: number;
  singleUse?: boolean;
}

export class DraggableWindowAreas {
  areas: Map<string, DraggableArea>;
  shouldDrawOverlay = false;

  static isWebview2Available(window: Window): window is typeof window & {
    chrome: { webview: { postMessage: Function; addEventListener: Function; removeEventListener: Function } };
  } {
    return (
      'chrome' in window &&
      typeof window.chrome === 'object' &&
      window.chrome !== null &&
      'webview' in window.chrome &&
      typeof window.chrome.webview === 'object' &&
      window.chrome.webview !== null &&
      'postMessage' in window.chrome.webview &&
      typeof window.chrome.webview.postMessage === 'function' &&
      'addEventListener' in window.chrome.webview &&
      typeof window.chrome.webview.addEventListener === 'function' &&
      'removeEventListener' in window.chrome.webview &&
      typeof window.chrome.webview.removeEventListener === 'function'
    );
  }

  constructor(shouldDrawOverlay = false) {
    this.listenForMessages();
    this.shouldDrawOverlay = shouldDrawOverlay;
    this.areas = new Map();
  }

  private dispatch() {
    if (!DraggableWindowAreas.isWebview2Available(window)) {
      return;
    }

    window.chrome.webview.postMessage(
      JSON.stringify({
        type: 'setDragRects',
        rects: Array.from(this.areas.values()),
      })
    );

    if (this.shouldDrawOverlay) {
      this.drawOverlay();
    }
  }

  private drawOverlay() {
    // draw an overlay to visualize the draggable area for testing purposes
    this.clearOverlays();
    this.areas.forEach((rect) => {
      const overlay = document.createElement('div');
      overlay.classList.add('drag-overlay');
      overlay.style.position = 'absolute';
      overlay.style.left = `${rect.x}px`;
      overlay.style.top = `${rect.y}px`;
      overlay.style.width = `${rect.w}px`;
      overlay.style.height = `${rect.h}px`;
      overlay.style.backgroundColor = 'rgba(255, 0, 0, 0.3)';
      overlay.style.pointerEvents = 'none';
      document.body.appendChild(overlay);
    });
  }

  private clearOverlays() {
    const existingOverlays = document.querySelectorAll('.drag-overlay');
    existingOverlays.forEach((overlay) => overlay.remove());
  }

  private get handleIncomingMessage() {
    const self = this;

    return (event: MessageEvent) => {
      try {
        if (event.data?.type === 'dragRectsCleaned') {
          // remove any non single use drag rects locally
          // (this event means the host already cleaned on its side)
          for (const [id, rect] of self.areas.entries()) {
            if (rect.singleUse) {
              self.areas.delete(id);
              if (self.shouldDrawOverlay) {
                self.drawOverlay();
              }
            }
          }
        }
      } catch (error) {
        console.error('Failed to parse message from host:', error);
      }
    };
  }

  private listenForMessages() {
    if (!DraggableWindowAreas.isWebview2Available(window)) {
      return;
    }

    window.chrome.webview.addEventListener('message', this.handleIncomingMessage);
  }

  set(id: string, area: DraggableArea) {
    this.areas.set(id, { ...area });
    this.dispatch();
  }

  get(id: string) {
    return this.areas.get(id);
  }

  has(id: string) {
    return this.areas.has(id);
  }

  delete(id: string) {
    this.areas.delete(id);
    this.dispatch();
  }

  /**
   * Offsets the position of the system icon and menu area by
   * the specified amount. When this is zero, it starts at the very
   * left of the window. The height is always the same as the titlebar.
   *
   * This should match the position of the titlebar icon.
   *
   * Use this option when the titlebar icon is offset to
   * accomodate the back button.
   */
  offsetSysMenuPosition(offsetX: number) {
    if (!DraggableWindowAreas.isWebview2Available(window)) {
      return;
    }

    window.chrome.webview.postMessage(
      JSON.stringify({
        type: 'setIconAreaLeftOffset',
        offset: offsetX,
      })
    );
  }

  /**
   * Intercepts a pointer event if it turns into a drag operation
   * and creates a temporary draggable area on the window chrome
   * to allow the draw action to drag the entire window.
   * @param pointerEvent
   * @returns Whether the drag operation was initiated.
   */
  get setAroundJsDrag() {
    const self = this;
    return (pointerEvent: PointerEvent) => {
      if (pointerEvent.button !== 0) {
        return;
      }

      if (!DraggableWindowAreas.isWebview2Available(window)) {
        return;
      }

      const startX = pointerEvent.clientX;
      const startY = pointerEvent.clientY;

      // require startX and startY to be within the bounds of the
      // element where the event was registered
      const registeredElement = pointerEvent.currentTarget as HTMLElement;
      const rect = registeredElement.getBoundingClientRect();
      if (startX > rect.right || startY > rect.bottom) {
        return;
      }

      const onMove = (ev: PointerEvent) => {
        const dx = ev.clientX - startX;
        const dy = ev.clientY - startY;
        if (Math.abs(dx) > 4 || Math.abs(dy) > 4) {
          cleanup();

          // build a drag rect that is a large square
          // around the current pointer position
          const dragRect = {
            singleUse: true,
            x: 0,
            y: 0,
            w: window.innerWidth,
            h: window.innerHeight,
          };
          self?.set('temp-drag-area', dragRect);

          // since we converted to a window drag operation, we need
          // to stop any events that might have reacted to the mouse
          // being released
          const supress = (evt: Event) => {
            evt.preventDefault();
            evt.stopImmediatePropagation();
          };
          window.addEventListener('mouseup', supress, { capture: true, once: true });
          window.addEventListener('pointerup', supress, { capture: true, once: true });
          window.addEventListener('touchup', supress, { capture: true, once: true });
          window.addEventListener('click', supress, { capture: true, once: true });
        }
      };

      const onUp = () => cleanup();

      const cleanup = () => {
        window.removeEventListener('pointermove', onMove);
        window.removeEventListener('pointerup', onUp);
      };

      window.addEventListener('pointermove', onMove);
      window.addEventListener('pointerup', onUp);
    };
  }

  dispose() {
    if (DraggableWindowAreas.isWebview2Available(window)) {
      window.chrome.webview.removeEventListener('message', this.handleIncomingMessage);
    }
    this.areas.clear();
    this.dispatch();
    this.clearOverlays();
  }

  [Symbol.dispose]() {
    this.dispose();
  }
}
