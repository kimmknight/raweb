/**
 * Animates the content area out (fade out).
 */
export async function fadeOut(contentElem?: Element | null, duration = 130) {
  if (!contentElem) {
    return;
  }

  // do not animate if the user prefers reduced motion
  const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  if (prefersReducedMotion) {
    return;
  }

  // fade out old content
  const outAnimation = contentElem.animate([{ opacity: 1 }, { opacity: 0 }], {
    duration,
    easing: 'linear',
    fill: 'both',
  });
  await outAnimation.finished;
}
