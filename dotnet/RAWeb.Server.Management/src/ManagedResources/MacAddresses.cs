using System;
using System.Text;

namespace RAWeb.Server.Management;

/// <summary>
/// Helpers for validating and normalizing MAC addresses.
/// <br /><br />
/// RAWeb always stores MAC addresses in the canonical form used by
/// <see cref="Normalize"/>: twelve lowercase hexadecimal characters
/// separated into six pairs by colons (e.g. <c>00:1a:2b:3c:4d:5e</c>).
/// </summary>
public static class MacAddresses {
  /// <summary>
  /// Converts a MAC address into RAWeb's canonical storage format
  /// (lowercase hexadecimal pairs separated by colons).
  /// <br /><br />
  /// Any separators are accepted on input, including colons, hyphens,
  /// periods, and spaces. Input that is null, empty, or
  /// entirely whitespace normalizes to null.
  /// </summary>
  /// <param name="macAddress">The MAC address to normalize.</param>
  /// <returns>The normalized MAC address, or null if no address was provided.</returns>
  /// <exception cref="ArgumentException">If the value is not a valid MAC address.</exception>
  public static string? Normalize(string? macAddress) {
    if (string.IsNullOrWhiteSpace(macAddress)) {
      return null;
    }

    // strip out every accepted separator so that only the hexadecimal digits remain
    var digits = new StringBuilder(12);
    foreach (var character in macAddress!) {
      if (character is ':' or '-' or '.' or ' ') {
        continue;
      }

      var isHexDigit = character is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';
      if (!isHexDigit || digits.Length == 12) {
        throw new ArgumentException($"'{macAddress}' is not a valid MAC address.", nameof(macAddress));
      }

      digits.Append(char.ToLowerInvariant(character));
    }

    if (digits.Length != 12) {
      throw new ArgumentException($"'{macAddress}' is not a valid MAC address.", nameof(macAddress));
    }

    // insert a colon between each pair of hexadecimal digits
    var normalized = new StringBuilder(17);
    for (var i = 0; i < 12; i += 2) {
      if (i > 0) {
        normalized.Append(':');
      }
      normalized.Append(digits[i]).Append(digits[i + 1]);
    }

    return normalized.ToString();
  }

  /// <summary>
  /// Attempts to convert a MAC address into RAWeb's canonical storage format.
  /// See <see cref="Normalize"/> for details.
  /// </summary>
  /// <returns>True if the value was a valid MAC address or was not provided at all.</returns>
  public static bool TryNormalize(string? macAddress, out string? normalized) {
    try {
      normalized = Normalize(macAddress);
      return true;
    }
    catch (ArgumentException) {
      normalized = null;
      return false;
    }
  }

  /// <summary>
  /// Converts a normalized MAC address into the six bytes that identify the
  /// network adapter.
  /// </summary>
  /// <exception cref="ArgumentException">If the value is not a valid MAC address.</exception>
  public static byte[] ToBytes(string? macAddress) {
    var normalized = Normalize(macAddress) ?? throw new ArgumentException("A MAC address is required.", nameof(macAddress));

    var bytes = new byte[6];
    for (var i = 0; i < 6; i++) {
      bytes[i] = Convert.ToByte(normalized.Substring(i * 3, 2), 16);
    }

    return bytes;
  }
}
