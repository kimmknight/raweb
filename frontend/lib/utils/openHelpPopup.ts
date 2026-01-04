export function openHelpPopup(docsUrl: string) {
  const popup = window.open(docsUrl, 'help', 'width=1000,height=600,menubar=0,status=0');
  if (popup) {
    popup.focus();
  } else {
    alert('Please allow popups for this application');
  }
}
