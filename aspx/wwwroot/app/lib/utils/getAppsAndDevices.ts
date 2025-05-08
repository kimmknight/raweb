/**
 * Fetches and parses the MS-TWSP webfeed provided by RAWeb. Returns a list of apps and devices that are available to the current user.
 * @param base The base/prefix for the url. It should be the path to the IIS application root and always end in forward slash, e.g. '/RAWeb/'
 */
export async function getAppsAndDevices(base = '/', { mergeTerminalServers = true } = {}) {
  const [origin, feed] = await getFeed(base, 2.0, mergeTerminalServers);
  if (!feed || !origin) {
    throw new Error('Failed to fetch the feed.');
  }

  const { resouceCollection, pubDate, schemaVersion } = getResourceCollection(feed);
  const { publisher, name: publisherName, id: publisherId, lastUpdated } = getPublisher(resouceCollection);
  const terminalServers = getTerminalServers(publisher);
  const resources = await getResources(publisher, terminalServers, origin);
  const folders = getFolders(resources);

  return {
    publishedDate: pubDate,
    schemaVersion,
    publisher: {
      name: publisherName,
      id: publisherId,
      lastUpdated,
    },
    terminalServers,
    resources,
    folders,
  };
}

/**
 * @see [MS-TSWP 2.2.2.2.1](https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-tswp/ffd8839c-17c4-4172-9017-d75a45ce50fe)
 */
function getResourceCollection(xmlDoc: Document) {
  const resouceCollection = xmlDoc.querySelector('ResourceCollection');
  if (!resouceCollection) {
    throw new Error('ResourceCollection not found in the feed.');
  }

  const pubDate = resouceCollection.getAttribute('PublishedDate');

  const schemaVersion = resouceCollection.getAttribute('SchemaVersion');
  if (!schemaVersion) {
    throw new Error('SchemaVersion not found in the feed.');
  }

  return {
    resouceCollection,
    pubDate: pubDate ? new Date(pubDate) : new Date(),
    schemaVersion: parseFloat(schemaVersion),
  };
}

/**
 * @see [MS-TSWP 2.2.2.1.2](https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-tswp/ebbd4794-113a-4674-944f-2e9acccf0926)
 */
function getPublisher(resourceCollection: Element) {
  const publisher = resourceCollection.querySelector('Publisher');
  if (!publisher) {
    throw new Error('Publisher not found in the ResourceCollection.');
  }

  const lastUpdated = publisher.getAttribute('LastUpdated');
  const lastUpdatedDate = lastUpdated ? new Date(lastUpdated) : new Date();

  const name = publisher.getAttribute('Name');
  if (!name) {
    throw new Error('Publisher Name not found in the ResourceCollection.');
  }

  const id = publisher.getAttribute('ID');
  if (!id) {
    throw new Error('Publisher ID not found in the ResourceCollection.');
  }

  // validate the ID to ensure it is either a GUID or a FQDN
  const isGUID = id.replaceAll('-', '').length === 32;
  if (!isGUID) {
    try {
      new URL('https://' + id);
    } catch {
      throw new Error('Publisher ID is neither a valid GUID nor a FQDN.');
    }
  }

  return {
    publisher,
    /** When the publisher or any of its resources were last updated */
    lastUpdated: lastUpdatedDate,
    /** The name of the publisher */
    name,
    /** A GUID or FQDN of the publisher */
    id,
    description: publisher.getAttribute('Description') ?? undefined,
    supportsReconnect: publisher.getAttribute('SupportsReconnect') === 'true',
  };
}

/**
 * Returns a map of terminal server IDs to their names from the Publisher element.
 * @see [MS-TSWP 2.2.2.1.12](https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-tswp/50c99bfb-6c5b-49b2-85f5-57b37f215601)
 */
function getTerminalServers(publisher: Element) {
  const terminalServers = publisher.querySelectorAll('TerminalServers > TerminalServer');
  if (!terminalServers) {
    throw new Error('TerminalServers not found in the Publisher.');
  }
  if (terminalServers.length === 0) {
    throw new Error('No TerminalServers found in the Publisher.');
  }

  const terminalServerNames = new Map<string, string>();
  terminalServers.forEach((terminalServer) => {
    const name = terminalServer.getAttribute('Name');
    const id = terminalServer.getAttribute('ID');
    if (!id) {
      return;
    }
    terminalServerNames.set(id, name || id);
  });

  return terminalServerNames;
}

interface Resource {
  id: string;
  alias: string;
  title: string;
  type: 'Desktop' | 'RemoteApp';
  lastUpdated?: Date;
  /** IDs of terminal servers that provide this resource */
  hosts: Host[];
  /** Extensions that the app claims to be able to open */
  fileExtensions: string[];
  /** Folders in which this resource belongs */
  folders: string[];
  /** Icons for this resource */
  icons: Icon[];
}

interface Host {
  /** The ID of the terminal server that hosts the resource */
  id: string;
  /** The name of the terminal server that hosts the resource */
  name: string;
  /** The URL to download the resource from the terminal server */
  url: URL;
  rdp?: AppOrDesktopProperties;
}

type AppOrDesktopProperties = Record<string, string | number> & { rdpFileText: string };

interface Icon {
  /** The type of the icon */
  type: 'ico' | 'png';
  /** The URL to the icon file */
  url: URL;
  /** The dimensions of the icon, if available */
  dimensions?: string;
}

/**
 * Returns a map of resource IDs to their details from the Publisher element.
 * @see [MS-TSWP 2.2.2.3.1](https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-tswp/8a1aa260-646b-4361-80e9-cdf6eb050999)
 */
async function getResources(
  publisher: Element,
  terminalServers: ReturnType<typeof getTerminalServers>,
  origin: string
) {
  const resources = publisher.querySelectorAll('Resources > Resource');
  if (!resources) {
    throw new Error('Resources not found in the Publisher.');
  }
  if (resources.length === 0) {
    throw new Error('No Resources found in the Publisher.');
  }

  // track resources by ID to avoid duplicates
  // (ID is supposed to be unique per the MS-TWSP spec)
  const validResources = new Map<string, Resource>();
  const existingAliases = new Set<string>();

  // add resources to the map if they are valid
  for await (const resource of resources) {
    const id = resource.getAttribute('ID') ?? '';
    if (!id) {
      console.debug('Resource is missing an ID attribute. Skipping...', resource);
      continue;
    }

    const alias = resource.getAttribute('Alias');
    if (!alias) {
      console.debug(`Resource ${id} is missing the Alias attribute. Skipping...`, resource);
      continue;
    }
    if (existingAliases.has(alias)) {
      console.warn(`Resource ${id} has a duplicate Alias attribute. Alias should be unique.`, resource);
    }

    let title = resource.getAttribute('Title');
    if (!title) {
      title = alias;
    }

    const type = (() => {
      const foundType = resource.getAttribute('Type');
      if (foundType === 'Desktop' || foundType === 'RemoteApp') {
        return foundType;
      }
    })();
    if (!type) {
      console.debug(`Resource ${id} has an invalid or missing Type attribute. Skipping...`, resource);
      continue;
    }

    let lastUpdated = resource.getAttribute('LastUpdated');
    let lastUpdatedDate: Date | undefined = undefined;
    if (lastUpdated) {
      lastUpdatedDate = new Date(lastUpdated);
    }

    const hosts = new Map<string, Host>();
    for await (const element of resource.querySelectorAll('HostingTerminalServer')) {
      const terminalServerId = element.querySelector('TerminalServerRef')?.getAttribute('Ref');
      if (!terminalServerId) {
        continue;
      }
      const terminalServerName = terminalServers.get(terminalServerId);

      const resourceFile = element.querySelector('ResourceFile');
      if (!resourceFile) {
        continue;
      }

      const urlAttr = resourceFile.getAttribute('URL');
      if (!urlAttr) {
        continue;
      }
      let url: URL;
      try {
        url = new URL(urlAttr, origin);
      } catch {
        continue;
      }

      const isRDP = resourceFile.getAttribute('FileExtension')?.toLowerCase() === '.rdp';
      let rdp: AppOrDesktopProperties | undefined = undefined;
      if (isRDP) {
        const found = await fetch(url, { method: 'GET', cache: 'no-cache' })
          .then((response) => {
            if (!response.ok) {
              throw new Error(`Failed to fetch RDP file: ${response.statusText}`);
            }
            return response.text();
          })
          .then((text) => {
            const properties = Object.fromEntries(
              text
                .split('\r\n')
                .filter((line) => line.trim() !== '')
                .map((line) => {
                  const parts = line.split(':');
                  const key = parts.slice(0, 1).join(':').trim();
                  const type = parts.slice(1, 2).join(':').trim() as 's' | 'i';
                  const value = parts.slice(2).join(':').trim();
                  return [key, type === 'i' ? parseInt(value) : value];
                })
            );

            rdp = {
              rdpFileText: text,
              ...properties,
            };
            return true;
          })
          .catch((error) => {
            console.debug(`Error fetching RDP file from ${url}:`, error);
            return false;
          });

        if (!found) {
          // if the RDP file could not be fetched, skip this terminal server
          continue;
        }
      }

      hosts.set(terminalServerId, {
        id: terminalServerId,
        name: terminalServerName ?? '',
        url,
        rdp,
      });
    }

    if (hosts.size === 0) {
      console.debug(`Resource ${id} has no valid hosting terminal servers. Skipping...`, resource);
      continue;
    }

    const fileExtensions = new Set<string>();
    for (const element of resource.querySelectorAll('FileExtension')) {
      const fileExtension = element.getAttribute('Name')?.toLowerCase();
      if (fileExtension) {
        fileExtensions.add(fileExtension);
      }
    }

    const icons = new Map<string, { type: 'ico' | 'png'; url: URL; dimensions?: string }>();
    for (const element of resource.querySelectorAll('Icons > *')) {
      const type = element.getAttribute('FileType')?.toLowerCase();
      let parsedType: 'ico' | 'png' | undefined = undefined;
      if (type && (type === 'ico' || type === 'png')) {
        parsedType = type;
      }
      if (!parsedType) {
        continue;
      }

      const urlAttr = element.getAttribute('FileURL');
      if (!urlAttr) {
        continue;
      }
      let url: URL;
      try {
        url = new URL(urlAttr, origin);
      } catch {
        continue;
      }
      const dimensions = element.getAttribute('Dimensions') ?? undefined;
      icons.set(url.href, { type: parsedType, url, dimensions });
    }

    const folders = new Set<string>();
    for (const element of resource.querySelectorAll('Folders > Folder')) {
      const folder = element.getAttribute('Name');
      if (folder && folder.startsWith('/')) {
        folders.add(folder);
      }
    }

    const validResource: Resource = {
      id,
      alias,
      title,
      lastUpdated: lastUpdatedDate,
      type,
      hosts: Array.from(hosts.values()).sort((a, b) => a.name.localeCompare(b.name)),
      fileExtensions: Array.from(fileExtensions),
      icons: Array.from(icons.values()),
      folders: Array.from(folders),
    };

    validResources.set(id, validResource);
    existingAliases.add(alias);
  }

  return Array.from(validResources.values());
}

function getFolders(resouces: Resource[]) {
  const folders = new Map<string, Set<Resource>>();

  for (const resource of resouces) {
    for (const folder of resource.folders) {
      if (!folders.has(folder)) {
        folders.set(folder, new Set<Resource>());
      }
      folders.get(folder)?.add(resource);
    }
  }

  for (const [folder, resources] of folders.entries()) {
    // remove the folder if it has no resources
    if (resources.size === 0) {
      folders.delete(folder);
    }
  }

  // convert the sets to an arrays with resources within each folder sorted by title,
  // and return the result as an object with folder names as keys
  return Object.entries(Object.fromEntries(folders.entries())).reduce((acc, [folder, resources]) => {
    acc[folder] = Array.from(resources).sort((a, b) => a.title.localeCompare(b.title));
    return acc;
  }, {} as Record<string, Resource[]>);
}

/**
 * Gets the MS-TWSP webfeed document.
 * @param base The base/prefix for the url. It should be the path to the IIS application root and always end in forward slash, e.g. '/RAWeb/'
 */
async function getFeed(base = '/', version: 1.1 | 2.0 | 2.1 = 2.1, mergeTerminalServers = true) {
  const parser = new DOMParser();
  const path = `${base}webfeed.aspx?mergeTerminalServers=${mergeTerminalServers ? 1 : 0}`;

  return await fetch(path, {
    method: 'GET',
    headers: {
      Accept: `application/x-msts-radc+xml; radc_schema_version=${version.toFixed(1)}`,
    },
    cache: 'no-cache',
  })
    .then(async (response) => {
      const url = new URL(response.url);
      const xmlString = await response.text();
      const xmlDoc = parser.parseFromString(xmlString, 'application/xml');
      return [url.origin, xmlDoc] as const;
    })
    .catch((err) => {
      console.error('Error fetching or parsing XML:', err);
      return [null, null];
    });
}
