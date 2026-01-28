import { createWritableBooleanSetting } from './createBooleanWritableSetting';

export const simpleModeEnabled = createWritableBooleanSetting(
  'simple-mode:enabled',
  'simpleModeEnabled',
  false
);
