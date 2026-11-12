import type { InjectionKey, Ref } from 'vue';

/** A column of the details view after the name column. */
export interface DetailsColumn {
  key: string;
  /** The header of the column, which the caller translates. */
  label: string;
  /** The column order. Columns are shown from the lowest order to the highest. Without it, the position in the list is used. */
  order?: number;
  /** The width of the column as a CSS grid track size. */
  width?: string;
  /** Lets the column header sort by this option. Without it, the column cannot be sorted. */
  sortKey?: 'Terminal server' | 'Date modified';
}

/** What a resource grid shares with the headings inside it. */
export interface ResourceGridContext {
  mode: Readonly<Ref<'card' | 'list' | 'grid' | 'tile' | 'details' | undefined>>;
  /** The columns of the details view in the order that they are shown. */
  columns: Readonly<Ref<DetailsColumn[]>>;
  /** The sections (groups of resources) that headings inside the grid can collapse and expand. */
  sections: {
    isCollapsed: (sectionKey: string) => boolean;
    setCollapsed: (sectionKey: string, collapsed: boolean) => void;
    expandAll: () => void;
    collapseAll: () => void;
    /**
     * Registers a section so that `collapseAll` and `expandAll` works with the section.
     * Returns a function that removes the section again.
     */
    register: (sectionKey: string) => () => void;
  };
}

export const resourceGridContextKey: InjectionKey<ResourceGridContext> = Symbol('resourceGridContext');
