export type MenuFlyoutPlacement = 'top' | 'right' | 'bottom' | 'left';
export type MenuFlyoutAnchor = 'start' | 'center' | 'end';

/**
 * Gets the CSS `position-area` value that opens a menu on the given side of its anchor,
 * aligned to the given edge of that side.
 */
export function getPositionArea(placement: MenuFlyoutPlacement, anchor: MenuFlyoutAnchor) {
  if (placement === 'top') {
    if (anchor === 'start') return 'top span-right';
    if (anchor === 'center') return 'top center';
    if (anchor === 'end') return 'top span-left';
  }
  if (placement === 'right') {
    if (anchor === 'start') return 'right span-bottom';
    if (anchor === 'center') return 'right center';
    if (anchor === 'end') return 'right span-top';
  }
  if (placement === 'bottom') {
    if (anchor === 'start') return 'bottom span-right';
    if (anchor === 'center') return 'bottom center';
    if (anchor === 'end') return 'bottom span-left';
  }
  if (placement === 'left') {
    if (anchor === 'start') return 'left span-bottom';
    if (anchor === 'center') return 'left center';
    if (anchor === 'end') return 'left span-top';
  }
}
