/**
 * Converts a MAC address into the canonical form that RAWeb stores: twelve
 * lowercase hexadecimal characters separated into six pairs by colons
 * (e.g. `00:1a:2b:3c:4d:5e`).
 *
 * Any of the common separators are accepted on input, including colons,
 * hyphens, periods, and spaces. A value that is empty or only whitespace
 * normalizes to an empty string, which clears the stored address.
 *
 * @returns the normalized address, or null if the value is not a valid MAC address
 */
export function normalizeMacAddress(macAddress: string | null | undefined): string | null {
  if (!macAddress || !macAddress.trim()) {
    return '';
  }

  const digits = macAddress.replace(/[:\-. ]/g, '');
  if (!/^[0-9a-f]{12}$/i.test(digits)) {
    return null;
  }

  return (digits.toLowerCase().match(/.{2}/g) ?? []).join(':');
}

/**
 * Whether the value can be stored as a MAC address. An empty value is
 * considered valid because the MAC address is optional.
 */
export function isValidMacAddress(macAddress: string | null | undefined): boolean {
  return normalizeMacAddress(macAddress) !== null;
}
