interface RegistryIconPath {
  iconPath?: string;
  iconIndex?: number | string;
  isManagedFileResource: false;
  isRemoteApp: boolean;
}

interface FileIconPath {
  identifier: string;
  isRemoteApp: boolean;
  isManagedFileResource: true;
}

export function buildManagedIconPath(
  data: RegistryIconPath | FileIconPath,
  cacheBust?: string | number | null,
  theme?: 'light' | 'dark',
  canFrame: boolean = false
): string {
  let iconPath = '';
  let iconIndex: string | number = -1;
  if (data.isManagedFileResource) {
    iconPath = 'managed-resources/' + (data.identifier || '');
  } else {
    iconPath = data.iconPath || '';
    iconIndex = data.iconIndex ?? -1;
  }

  let fallbackIconPath = '';
  if (data.isManagedFileResource) {
    if (data.isRemoteApp) {
      fallbackIconPath = '../lib/assets/remoteicon.png';
    } else {
      if (theme === 'light') {
        fallbackIconPath = '../lib/assets/wallpaper.png';
      } else {
        fallbackIconPath = '../lib/assets/wallpaper-dark.png';
      }
    }
  }

  let frame = false;
  if (canFrame && !data.isRemoteApp) {
    frame = true;
  }

  const prefersDarkScheme = window.matchMedia('(prefers-color-scheme: dark)');
  if (!theme) {
    theme = prefersDarkScheme.matches ? 'dark' : 'light';
  }

  return `api/management/resources/icon?path=${encodeURIComponent(
    iconPath
  )}&index=${iconIndex}&fallback=${fallbackIconPath}&theme=${theme}&frame=${
    frame ? 'pc' : ''
  }&__cacheBust=${cacheBust}`;
}
