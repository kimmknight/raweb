export interface TreeItem {
  name: string;
  href?: string;
  onClick?: () => void;
  icon?: string | URL;
  /** @default 'navigation' */
  type?: 'category' | 'expander' | 'navigation';
  children?: TreeItem[];
  selected?: boolean;
  disabled?: boolean;
}
