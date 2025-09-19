import { useCoreDataStore } from '$stores';
import { ref } from 'vue';

const update = ref({
  loading: false,
  status: null as number | null,
  details: null as {
    tag: string;
    version: string;
    notes: string;
    name: string;
    html_url: string;
  } | null,
});

function populateUpdateDetails(force = false) {
  // do not fetch if details are already populated
  if (update.value.details && !force) {
    return;
  }

  // do not fetch if already in progress
  if (update.value.loading) {
    return;
  }

  const { authUser, coreVersion } = useCoreDataStore();
  if (authUser.isLocalAdministrator) {
    update.value.loading = true;
    fetch('https://api.github.com/repos/kimmknight/raweb/releases', {})
      .then((res) => {
        update.value.status = res.status;
        if (!res.ok) {
          throw new Error(`HTTP error: ${res.status}`);
        }
        return res.json();
      })
      .then(async (data) => {
        // find the first release that is not a draft or prerelease
        const latestRelease = data.find(
          (release: { draft: boolean; prerelease: boolean }) => !release.draft && !release.prerelease
        );

        if (!latestRelease) {
          update.value.details = null;
          return;
        }

        const tagName = latestRelease.tag_name as string;
        const version = tagName.replace(/^v/, ''); // Remove leading 'v' if present

        // stop if the new version is not newer than the current version
        const newVersionParts = version.split('.');
        const currentVersionParts = coreVersion.split('.');
        const currentVersionYear = parseInt(currentVersionParts[0], 10);
        const currentVersionMonth = parseInt(currentVersionParts[1], 10);
        const currentVersionDay = parseInt(currentVersionParts[2], 10);
        const currentVersionPatch = parseInt(currentVersionParts[3] || '0', 10);
        const newVersionYear = parseInt(newVersionParts[0], 10);
        const newVersionMonth = parseInt(newVersionParts[1], 10);
        const newVersionDay = parseInt(newVersionParts[2], 10);
        const newVersionPatch = parseInt(newVersionParts[3] || '0', 10);
        if (
          newVersionYear < currentVersionYear ||
          (newVersionYear === currentVersionYear && newVersionMonth < currentVersionMonth) ||
          (newVersionYear === currentVersionYear &&
            newVersionMonth === currentVersionMonth &&
            newVersionDay < currentVersionDay) ||
          (newVersionYear === currentVersionYear &&
            newVersionMonth === currentVersionMonth &&
            newVersionDay === currentVersionDay &&
            newVersionPatch <= currentVersionPatch)
        ) {
          return;
        }

        // remove leading zeros from version
        const cleanedVersion = newVersionParts
          .map((part) => (part.startsWith('0') ? part.replace(/^0+/, '') : part))
          .map((part) => part || '0')
          .join('.');

        // convert notes markdown to html
        let markdown = await fetch('https://api.github.com/markdown', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            text: latestRelease.body || 'No release notes available',
            mode: 'gfm',
          }),
        }).then((res) => res.text());

        // make all links in the markdown open in a new tab
        const linkRegex = /<a([^>]*?)\s+href="([^"]+)"([^>]*)>(.*?)<\/a>/g;
        markdown = markdown.replace(linkRegex, (match, beforeHref, href, afterHref, text) => {
          // Remove any existing target or rel attributes
          let attrs = (beforeHref + afterHref).replace(/\s*(target|rel)=["'][^"']*["']/gi, '').trim();
          return `<a ${
            attrs ? attrs : ''
          } href="${href}" target="_blank" rel="noopener noreferrer">${text}</a>`;
        });

        const releaseDetails = {
          tag: tagName,
          version: cleanedVersion,
          notes: markdown || 'No release notes available',
          name: latestRelease.name,
          html_url: latestRelease.html_url,
        };

        update.value.details = releaseDetails;
      })
      .catch((error) => {
        console.error('Error fetching update details:', error);
        update.value.details = null;
      })
      .finally(() => {
        update.value.loading = false;
      });
  }
}

export function useUpdateDetails() {
  return {
    updateDetails: update,
    populateUpdateDetails,
  };
}
