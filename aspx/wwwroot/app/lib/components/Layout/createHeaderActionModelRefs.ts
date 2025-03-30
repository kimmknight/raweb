import { computed, ref } from 'vue';

interface CreateHeaderActionModelRefsProps {
  defaults?: {
    mode?: 'card' | 'list' | 'grid' | 'tile';
    sortName?: 'Name' | 'Terminal server' | 'Date modified';
    sortOrder?: 'asc' | 'desc';
    query?: string;
  };
  /** If provided, the state will be persisted to localStorage and made unique with this key. */
  persist?: string;
}

type GlobalDefaults = NonNullable<Required<CreateHeaderActionModelRefsProps['defaults']>>;

const globalDefaults = {
  mode: 'card',
  sortName: 'Name',
  sortOrder: 'asc',
  query: '',
} satisfies GlobalDefaults;

type ModeType = GlobalDefaults['mode'];
type SortNameType = GlobalDefaults['sortName'];
type SortOrderType = GlobalDefaults['sortOrder'];

type Resource = NonNullable<
  Awaited<ReturnType<typeof import('$utils/getAppsAndDevices').getAppsAndDevices>>
>['resources'][number];

function organize(resources: Resource[], sortName: SortNameType, sortOrder: SortOrderType, query?: string) {
  // sort the resources according to the sortName and sortOrder
  // but always sort by name within the other criteria
  const sortedResources = resources.sort((a, b) => {
    const getValue = (resource: (typeof resources)[number], key: string | undefined) => {
      if (key === 'Terminal server') return resource.hosts[0]?.name?.toString()?.toLowerCase();
      if (key === 'Date modified') return resource.lastUpdated?.toString()?.toLowerCase();
      return resource.title?.toLowerCase();
    };

    const aValue = getValue(a, sortName);
    const bValue = getValue(b, sortName);

    if (aValue == undefined || bValue == undefined) return 0;

    const comparison = aValue.localeCompare(bValue);
    if (comparison !== 0) {
      return sortOrder === 'asc' ? comparison : -comparison;
    }

    // Always sort by name as a secondary criterion
    const nameComparison = (a.title?.toLowerCase() || '').localeCompare(b.title?.toLowerCase() || '');
    return sortOrder === 'asc' ? nameComparison : -nameComparison;
  });

  // search by the query if it exists
  if (query) {
    const filteredResources = resources.filter((resource) => {
      const queryValue = query.toLowerCase() || '';
      const title = resource.title?.toLowerCase() || '';
      const hostname = resource.hosts[0]?.name?.toLowerCase() || '';
      return title.includes(queryValue) || hostname.includes(queryValue);
    });

    return filteredResources;
  }

  return sortedResources;
}

const persistPrefix = `${window.__namespace}::header-action-model:`;

export function createHeaderActionModelRefs({ defaults, persist }: CreateHeaderActionModelRefsProps) {
  const internalMode = ref<ModeType>(defaults?.mode ?? globalDefaults.mode);
  const mode = computed({
    get: () => {
      internalMode.value;
      if (persist && localStorage.getItem(persistPrefix + persist + ':mode')) {
        const value = localStorage.getItem(persistPrefix + persist + ':mode') as ModeType;
        if (internalMode.value !== value) internalMode.value = value;
        return value;
      }
      return internalMode.value;
    },
    set: (value: ModeType) => {
      if (persist && value) {
        localStorage.setItem(persistPrefix + persist + ':mode', value);
      } else {
        localStorage.removeItem(persistPrefix + persist + ':mode');
      }
      internalMode.value = value;
    },
  });

  const internalSortName = ref<SortNameType>(defaults?.sortName ?? globalDefaults.sortName);
  const sortName = computed({
    get: () => {
      internalSortName.value;
      if (persist && localStorage.getItem(persistPrefix + persist + ':sortName')) {
        return localStorage.getItem(persistPrefix + persist + ':sortName') as SortNameType;
      }
      return internalSortName.value;
    },
    set: (value: SortNameType) => {
      if (persist && value) {
        localStorage.setItem(persistPrefix + persist + ':sortName', value);
      } else {
        localStorage.removeItem(persistPrefix + persist + ':sortName');
      }
      internalSortName.value = value;
    },
  });

  const internalSortOrder = ref<SortOrderType>(defaults?.sortOrder ?? globalDefaults.sortOrder);
  const sortOrder = computed({
    get: () => {
      internalSortOrder.value;
      if (persist && localStorage.getItem(persistPrefix + persist + ':sortOrder')) {
        return localStorage.getItem(persistPrefix + persist + ':sortOrder') as SortOrderType;
      }
      return internalSortOrder.value;
    },
    set: (value: SortOrderType) => {
      if (persist && value) {
        localStorage.setItem(persistPrefix + persist + ':sortOrder', value);
      } else {
        localStorage.removeItem(persistPrefix + persist + ':sortOrder');
      }
      internalSortOrder.value = value;
    },
  });

  const query = ref<string>(defaults?.query ?? globalDefaults.query);

  return {
    mode,
    sortName,
    sortOrder,
    query,
    organize,
  };
}
