import { useCoreDataStore } from '$stores';

/**
 * Redirects the user to the fully-qualified domain name (FQDN) if the current hostname is localhost or an IP address,
 * and if the server capabilities indicate that FQDN redirection is supported.
 */
export function redirectToFqdn() {
  const coreData = useCoreDataStore();

  if (coreData.capabilities.supportsFqdnRedirect && coreData.envFQDN) {
    // check if we are on localhost or an IP address
    const hostname = window.location.hostname;
    const isLocalhost = hostname === 'localhost';
    const isIpAddress = /^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$/.test(hostname);
    const port = window.location.port;

    if (isLocalhost || isIpAddress) {
      // check if envFQDN is reachable
      fetch(`https://${coreData.envFQDN}${port ? ':' + port : ''}`, { method: 'HEAD', mode: 'no-cors' })
        .then(() => {
          // redirect to envFQDN
          const newUrl = window.location.href.replace(hostname, coreData.envFQDN!);
          window.location.href = newUrl;
        })
        .catch(() => {
          // envFQDN is not reachable, do nothing
        });
    }
  }
}
