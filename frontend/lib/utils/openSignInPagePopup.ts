import { useCoreDataStore } from '$stores';

export function openSignInPagePopup(target: string, onSuccess?: () => void) {
  const { appBase, __refetchCoreData } = useCoreDataStore();

  const popup = window.open(`${appBase}login`, target, 'width=472,height=502,menubar=0,status=0');
  if (popup) {
    popup.focus();

    // close the popup and re-check authentication once the popup signals that authentication was successful
    window.addEventListener('message', (event) => {
      if (event.origin !== location.origin) return;
      if (popup && event.source !== popup) return;

      if (event.data.type === 'authentication-success') {
        popup.close();
        console.log('re');
        __refetchCoreData();
        onSuccess?.();
      }
    });
  } else {
    alert('Please allow popups for this application');
  }
}
