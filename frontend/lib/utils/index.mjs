import { capitalize } from './capitalize.ts';
import { combineTerminalServersModeEnabled } from './combineTerminalServersMode.ts';
import { flatModeEnabled } from './flatMode.ts';
import { generateRdpFileContents } from './generateRdpFileContents.ts';
import { generateRdpUri } from './generateRdpUri.ts';
import { getAppsAndDevices } from './getAppsAndDevices.ts';
import { iconBackgroundsEnabled } from './iconBackgrounds.ts';
import { inferUtfEncoding } from './inferUtfEncoding.ts';
import { prefixUserNS } from './prefixUserNS.ts';
import { PreventableEvent } from './PreventableEvent.ts';
import { registerServiceWorker } from './registerServiceWorker.ts';
import { removeSplashScreen, restoreSplashScreen } from './removeSplashScreen.ts';
import { simpleModeEnabled } from './simpleMode.ts';
import { unproxify } from './unproxify.ts';
import {
  favoritesEnabled,
  useFavoriteResources,
  useFavoriteResourceTerminalServers,
} from './useFavoriteResources.ts';
import { useUpdateDetails } from './useUpdateDetails.ts';
import { useWebfeedData } from './useWebfeedData.ts';

export {
  capitalize,
  combineTerminalServersModeEnabled,
  favoritesEnabled,
  flatModeEnabled,
  generateRdpFileContents,
  generateRdpUri,
  getAppsAndDevices,
  iconBackgroundsEnabled,
  inferUtfEncoding,
  prefixUserNS,
  PreventableEvent,
  unproxify as raw,
  registerServiceWorker,
  removeSplashScreen,
  restoreSplashScreen,
  simpleModeEnabled,
  useFavoriteResources,
  useFavoriteResourceTerminalServers,
  useUpdateDetails,
  useWebfeedData,
};
