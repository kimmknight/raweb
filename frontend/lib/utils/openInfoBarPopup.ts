export function openInfoBarPopup(href: string, target: string) {
  const popup = window.open(href, target, 'width=1000,height=600,menubar=0,status=0');
  if (popup) {
    popup.focus();
  } else {
    alert('Please allow popups for this application');
  }
}
