import { createWritableBooleanSetting } from './createBooleanWritableSetting';

export const combineTerminalServersModeEnabled = createWritableBooleanSetting(
  'combine-terminal-servers-mode:enabled',
  'combineTerminalServersModeEnabled',
  true
);
