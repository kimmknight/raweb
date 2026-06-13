interface Padding {
  top: number;
  right: number;
  bottom: number;
  left: number;
}

interface ExpandDownOptions {
  duration?: number;
  fill?: FillMode;
  startHeight?: number;
  endHeight?: number;
  startPadding?: Padding;
  endPadding?: Padding;
  startOpacity?: number;
  endOpacity?: number;
}

/**
 * Fades in and expands the height of the given element.
 */
export async function expandDown(
  contentElem?: Element | null,
  {
    duration = 130,
    fill = 'both' as FillMode,
    startHeight = 0,
    endHeight = 0,
    startOpacity = 0,
    endOpacity = 1,
    startPadding,
    endPadding,
  }: ExpandDownOptions = {}
) {
  if (!contentElem) {
    return;
  }

  // do not animate if the user prefers reduced motion
  const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  if (prefersReducedMotion) {
    return;
  }

  const fadeInAnimation = contentElem.animate([{ opacity: startOpacity }, { opacity: endOpacity }], {
    duration,
    easing: 'linear',
    fill,
  });

  const expandDownAnimation = contentElem.animate(
    [
      {
        height: `${startHeight}px`,
        overflow: 'hidden',
        padding: startPadding
          ? `${startPadding.top}px ${startPadding.right}px ${startPadding.bottom}px ${startPadding.left}px`
          : undefined,
      },
      {
        height: `${endHeight}px`,
        padding: endPadding
          ? `${endPadding.top}px ${endPadding.right}px ${endPadding.bottom}px ${endPadding.left}px`
          : undefined,
      },
    ],
    {
      duration,
      easing: 'cubic-bezier(0.16, 1, 0.3, 1)', // expo out
      fill,
    }
  );

  await Promise.all([expandDownAnimation.finished, fadeInAnimation.finished]);
}
