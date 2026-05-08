---
title: Links in documentation
---

When referencing other documentation pages, use relative links to the other page's index.md file. For example, to link to the connection methods page from this page, use a relative link to the connection methods page's index.md file like this:

```
[Connection methods for accessing remote resources](/docs/connection-methods/)
```

Always start and end links to other documentation pages with a slash (/) to ensure the link works correctly in both development and production environments.

## Anchors

When possible, link to specific sections of other documentation pages using anchors. For example, to link to the section about downloading an RDP file on the connection methods page, use the following link:

```
[Download an RDP file](/docs/connection-methods/#download-an-rdp-file)
```

Anchors may be added to any heading or paragraph by adding an HTML anchor tag with a unique ID in curly braces. For example:

```
### Download an RDP file {#download-an-rdp-file}
```
