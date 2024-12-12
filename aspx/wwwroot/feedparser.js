function parseFeed(xmlString) {
    const parser = new DOMParser();
    const xmlDoc = parser.parseFromString(xmlString, "application/xml");

    function getAttribute(node, attr) {
        return node.getAttribute(attr) || null;
    }

    function getTextContent(node) {
        return node ? node.textContent.trim() : null;
    }

    // Parse main structure
    const resourceCollection = xmlDoc.querySelector("ResourceCollection");
    const pubDate = getAttribute(resourceCollection, "PubDate");
    const schemaVersion = getAttribute(resourceCollection, "SchemaVersion");

    // Parse publisher details
    const publisher = resourceCollection.querySelector("Publisher");
    const publisherData = {
        lastUpdated: getAttribute(publisher, "LastUpdated"),
        name: getAttribute(publisher, "Name"),
        id: getAttribute(publisher, "ID"),
        description: getAttribute(publisher, "Description"),
        supportsReconnect: getAttribute(publisher, "SupportsReconnect") === "true",
        displayFolder: getAttribute(publisher, "DisplayFolder"),
    };

    // Extract SubFolders at the same level as resources
    const subFolders = Array.from(publisher.querySelectorAll("SubFolders > Folder")).map(folder => ({
        name: getAttribute(folder, "Name"),
    }));

    subFolders.unshift({name: null})

    // Parse resources
    const resources = Array.from(publisher.querySelectorAll("Resources > Resource")).map(resource => {
        const icons = Array.from(resource.querySelectorAll("Icons > *")).map(icon => ({
            type: icon.tagName,
            fileType: getAttribute(icon, "FileType"),
            fileURL: getAttribute(icon, "FileURL"),
            dimensions: getAttribute(icon, "Dimensions") || null
        }));

        const hostingTerminalServers = Array.from(resource.querySelectorAll("HostingTerminalServers > HostingTerminalServer")).map(server => ({
            resourceFile: {
                fileExtension: getAttribute(server.querySelector("ResourceFile"), "FileExtension"),
                url: getAttribute(server.querySelector("ResourceFile"), "URL"),
            },
            terminalServerRef: getAttribute(server.querySelector("TerminalServerRef"), "Ref")
        }));

        return {
            id: getAttribute(resource, "ID"),
            alias: getAttribute(resource, "Alias"),
            title: getAttribute(resource, "Title"),
            lastUpdated: getAttribute(resource, "LastUpdated"),
            type: getAttribute(resource, "Type"),
            icons: icons,
            folders: Array.from(resource.querySelectorAll("Folders > Folder")).map(folder => ({
                name: getAttribute(folder, "Name")
            })),
            hostingTerminalServers: hostingTerminalServers
        };
    });

    // Parse terminal servers
    const terminalServers = Array.from(publisher.querySelectorAll("TerminalServers > TerminalServer")).map(server => ({
        id: getAttribute(server, "ID"),
        name: getAttribute(server, "Name"),
        lastUpdated: getAttribute(server, "LastUpdated")
    }));

    return {
        pubDate,
        schemaVersion,
        publisher: publisherData,
        subFolders: subFolders, // Now at the same level as resources
        resources: resources,
        terminalServers: terminalServers
    };
}
