export type ViewMode = 'card' | 'list' | 'grid' | 'tile' | 'details';

/** The digit key (used with Ctrl + Shift) that switches to each view. */
const viewShortcutDigits: Record<ViewMode, number> = {
  card: 1,
  grid: 2,
  list: 5,
  details: 6,
  tile: 7,
};

export function getViewShortcutHint(mode: ViewMode) {
  return `Ctrl + Shift + ${viewShortcutDigits[mode]}`;
}

/** Returns the view that the key event's shortcut selects, if any. */
export function getViewModeForShortcut(event: KeyboardEvent) {
  if (!event.ctrlKey || !event.shiftKey || event.altKey || event.metaKey) {
    return undefined;
  }

  return (Object.keys(viewShortcutDigits) as ViewMode[]).find(
    (mode) => event.code === `Digit${viewShortcutDigits[mode]}`
  );
}
