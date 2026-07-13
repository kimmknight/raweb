/**
 * Removes the splash screen element from the DOM and updates the theme color.
 *
 * The splash screen is expected to be a div with the class 'root-splash-wrapper'.
 * `~/lib/controls/AppRoot.ascx` provides the splash screen element.
 */
export function removeSplashScreen() {
  return new Promise<void>((resolve) => {
    const durationMs = 300;

    const splashWrapperElem: HTMLDivElement | null = document.querySelector('.root-splash-wrapper');
    const rootAppElem: HTMLDivElement | null = document.querySelector('#app');
    if (splashWrapperElem && rootAppElem) {
      splashWrapperElem.style.transition = `opacity ${durationMs}ms cubic-bezier(0.16, 1, 0.3, 1)`;
      splashWrapperElem.style.opacity = '0';
      setTimeout(() => {
        // splashWrapperElem.remove();
        splashWrapperElem.style.display = 'none';
        resolve();
      }, durationMs); // wait for the transition to finish before removing the element

      // notify the native host exactly when the browser actually starts
      // animating #app's opacity so that the splash screen can be hidden
      // at the exact same time
      const onAppTransitionRun = (e: TransitionEvent) => {
        if (e.target !== rootAppElem || e.propertyName !== 'opacity') {
          return;
        }
        rootAppElem.removeEventListener('transitionrun', onAppTransitionRun);
        if ('chrome' in window) {
          (window.chrome as { webview?: { postMessage?: Function } } | undefined)?.webview?.postMessage?.(
            '{ "type": "hide-splash" }'
          );
        }
      };
      rootAppElem.addEventListener('transitionrun', onAppTransitionRun);

      requestAnimationFrame(() => {
        rootAppElem.style.transition = `opacity ${durationMs}ms cubic-bezier(0.16, 1, 0.3, 1)`;
        rootAppElem.style.opacity = '1';
      });
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
    const rootAppElem: HTMLDivElement | null = document.querySelector('#app');
    if (splashWrapperElem && rootAppElem) {
      splashWrapperElem.style.display = 'flex';
      splashWrapperElem.style.opacity = '1';
      setTimeout(() => {
        resolve();
      }, 300); // wait for the transition to finish before resolving the promise

      const onAppTransitionRun = (e: TransitionEvent) => {
        if (e.target !== rootAppElem || e.propertyName !== 'opacity') {
          return;
        }
        rootAppElem.removeEventListener('transitionrun', onAppTransitionRun);
        if ('chrome' in window) {
          (window.chrome as { webview?: { postMessage?: Function } } | undefined)?.webview?.postMessage?.(
            '{ "type": "show-splash" }'
          );
        }
      };
      rootAppElem.addEventListener('transitionrun', onAppTransitionRun);

      rootAppElem.style.opacity = '0';

      // and update the theme color to match the splash screen color
      const themeColorMetaTags = document.querySelectorAll('meta[name="theme-color"]');
      const color = getComputedStyle(splashWrapperElem).getPropertyValue('background-color').trim();
      themeColorMetaTags.forEach((metaTag) => {
        metaTag.setAttribute('content', color);
      });
    }
  });
}
