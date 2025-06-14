/**
 * Removes the splash screen element from the DOM and updates the theme color.
 *
 * The splash screen is expected to be a div with the class 'root-splash-wrapper'.
 * `~/lib/controls/AppRoot.ascx` provides the splash screen element.
 */
export function removeSplashScreen() {
  return new Promise<void>((resolve) => {
    const splashWrapperElem: HTMLDivElement | null = document.querySelector('.root-splash-wrapper');
    if (splashWrapperElem) {
      splashWrapperElem.style.transition = 'opacity 300ms cubic-bezier(0.16, 1, 0.3, 1)';
      splashWrapperElem.style.opacity = '0';
      setTimeout(() => {
        // splashWrapperElem.remove();
        splashWrapperElem.style.display = 'none';
        resolve();
      }, 300); // wait for the transition to finish before removing the element
    }

    // and update the theme color to match the app's background color instead of the splash screen color
    const themeColorMetaTags = document.querySelectorAll('meta[name="theme-color"]');
    const color = getComputedStyle(document.documentElement)
      .getPropertyValue('--wui-solid-background-base')
      .trim();
    setTimeout(() => {
      themeColorMetaTags.forEach((metaTag) => {
        metaTag.setAttribute('content', color);
      });
    }, 10);
  });
}

/**
 * Restores the hidden splash screen element by making it visible again.
 * This will also update the theme color to match the splash screen color.
 */
export function restoreSplashScreen() {
  return new Promise<void>((resolve) => {
    const splashWrapperElem: HTMLDivElement | null = document.querySelector('.root-splash-wrapper');
    if (splashWrapperElem) {
      splashWrapperElem.style.display = 'flex';
      splashWrapperElem.style.opacity = '1';
      setTimeout(() => {
        resolve();
      }, 300); // wait for the transition to finish before resolving the promise

      // and update the theme color to match the splash screen color
      const themeColorMetaTags = document.querySelectorAll('meta[name="theme-color"]');
      const color = getComputedStyle(splashWrapperElem).getPropertyValue('background-color').trim();
      themeColorMetaTags.forEach((metaTag) => {
        metaTag.setAttribute('content', color);
      });
    }
  });
}
