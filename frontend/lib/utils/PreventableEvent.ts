export class PreventableEvent {
  defaultPrevented = false;

  preventDefault() {
    this.defaultPrevented = true;
  }
}
