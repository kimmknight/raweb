export class PreventableEvent<T = unknown> {
  defaultPrevented = false;
  detail: T;

  constructor(detail: T = undefined as unknown as T) {
    this.detail = detail;
  }

  preventDefault() {
    this.defaultPrevented = true;
  }
}
