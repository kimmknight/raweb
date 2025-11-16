import { PreventableEvent } from '$utils';

export interface TreeItem {
  name: string;
  href?: string;
  onClick?: (evt: PreventableEvent<MouseEvent | KeyboardEvent>) => void;
  icon?: string | URL;
  /** @default 'navigation' */
  type?: 'category' | 'expander' | 'navigation';
  children?: TreeItem[];
  selected?: boolean;
  disabled?: boolean;
}
