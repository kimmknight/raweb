import { useCoreDataStore } from '$stores';

/**
 * Redirects the user to the fully-qualified domain name (FQDN) if the current hostname is localhost or an IP address,
 * and if the server capabilities indicate that FQDN redirection is supported.
 */
export async function redirectToFqdn() {
  const coreData = useCoreDataStore();

  if (coreData.capabilities.supportsFqdnRedirect && coreData.envFQDN) {
    // check if we are on localhost or an IP address
    const hostname = window.location.hostname;
    const isLocalhost = hostname === 'localhost';
    const isIpAddress = /^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$/.test(hostname);
    const port = window.location.port;

    if (isLocalhost || isIpAddress) {
      // check if envFQDN is reachable
      const redirectUrl = await fetch(`https://${coreData.envFQDN}${port ? ':' + port : ''}`, {
        method: 'HEAD',
        mode: 'no-cors',
      })
        .then(() => {
          // redirect to envFQDN
          const newUrl = new URL(window.location.href);
          newUrl.hostname = coreData.envFQDN!;
          newUrl.protocol = 'https:';
          return newUrl;
        })
        .catch(() => {
          // envFQDN is not reachable; do nothing
        });

      if (redirectUrl) {
        window.location.href = redirectUrl.href;
        return;
      }

      // try again with just the machine name
      fetch(`https://${coreData.envMachineName}${port ? ':' + port : ''}`, { method: 'HEAD', mode: 'no-cors' })
        .then(() => {
          // redirect to envMachineName
          const newUrl = new URL(window.location.href);
          newUrl.hostname = coreData.envMachineName!;
          newUrl.protocol = 'https:';
          window.location.href = newUrl.href;
        })
        .catch(() => {
          // envMachineName is not reachable; do nothing
        });
    }
  }
}
