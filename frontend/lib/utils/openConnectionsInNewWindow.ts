import { createWritableBooleanSetting } from './createBooleanWritableSetting';

export const openConnectionsInNewWindowEnabled = createWritableBooleanSetting(
  'open-connections-in-new-window:enabled',
  'openConnectionsInNewWindowEnabled',
  false
);
