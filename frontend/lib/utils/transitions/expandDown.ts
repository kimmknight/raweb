/**
 * Fades in and expands the height of the given element.
 */
export async function expandDown(
  contentElem?: Element | null,
  { duration = 130, fill = 'both' as FillMode, startHeight = 0, endHeight = 0 } = {}
) {
  if (!contentElem) {
    return;
  }

  // do not animate if the user prefers reduced motion
  const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  if (prefersReducedMotion) {
    return;
  }

  const fadeInAnimation = contentElem.animate([{ opacity: 0 }, { opacity: 1 }], {
    duration,
    easing: 'linear',
    fill,
  });

  const expandDownAnimation = contentElem.animate(
    [{ height: `${startHeight}px`, overflow: 'hidden' }, { height: `${endHeight}px` }],
    {
      duration,
      easing: 'cubic-bezier(0.16, 1, 0.3, 1)', // expo out
      fill,
    }
  );

  await Promise.all([expandDownAnimation.finished, fadeInAnimation.finished]);
}
