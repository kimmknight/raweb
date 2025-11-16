/**
 * Animates an element in (fade in + fly in).
 */
export async function entranceIn(
  contentElem?: Element | null,
  flyDistance = 40,
  fadeDuration = 210,
  flyDuration = 270
) {
  if (!contentElem) {
    return;
  }

  /**
   * Function for fading in an element.
   * Set the duration to 0 to skip the animation.
   */
  function fadeIn(elem: Element, duration = fadeDuration) {
    return elem.animate([{ opacity: 0 }, { opacity: 1 }], {
      duration: duration,
      easing: 'cubic-bezier(0.16, 1, 0.3, 1)',
      fill: 'both',
    });
  }

  const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  if (prefersReducedMotion) {
    fadeIn(contentElem, 0);
    return;
  }

  // animate in the new content
  const inFade = fadeIn(contentElem);
  const inFly = contentElem.animate(
    [
      { transform: `translateY(${flyDistance}px)`, opacity: 0 },
      { transform: 'translateY(0)', opacity: 1 },
    ],
    {
      duration: flyDuration,
      easing: 'cubic-bezier(0.16, 1, 0.3, 1)', // expo out
      fill: 'none',
    }
  );
  await Promise.all([inFade.finished, inFly.finished]);
}
