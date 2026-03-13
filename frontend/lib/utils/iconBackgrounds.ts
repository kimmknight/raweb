import { createWritableBooleanSetting } from './createBooleanWritableSetting';

export const iconBackgroundsEnabled = createWritableBooleanSetting(
  'icon-backgrounds:enabled',
  'iconBackgroundsEnabled',
  true
);
