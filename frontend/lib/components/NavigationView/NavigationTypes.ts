export interface TreeItem {
  name: string;
  href?: string;
  onClick?: () => void;
  icon?: string;
  /** @default 'navigation' */
  type?: 'category' | 'expander' | 'navigation';
  children?: TreeItem[];
  selected?: boolean;
  disabled?: boolean;
}
