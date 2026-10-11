using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace RAWeb.Server.Management;

/// <summary>
/// The RDP keyboard layouts ("server-layout" values) supported by guacd.
/// <br /><br />
/// See: https://github.com/apache/guacamole-server/tree/master/src/protocols/rdp/keymaps
/// </summary>
public static class GuacdKeyboardLayouts {
  /// <summary>
  /// Maps a Windows keyboard layout identifier (KLID) to the
  /// closest matching guacd "server-layout" value.
  /// </summary>
  private static readonly Dictionary<string, string> s_klidToGuacdLayout = new(System.StringComparer.OrdinalIgnoreCase) {
    ["00000405"] = "cs-cz-qwertz",
    ["00000406"] = "da-dk-qwerty",
    ["00000807"] = "de-ch-qwertz",
    ["00000407"] = "de-de-qwertz",
    ["00000809"] = "en-gb-qwerty",
    ["00000409"] = "en-us-qwerty",
    ["0000040a"] = "es-es-qwerty",
    ["0000080a"] = "es-latam-qwerty",
    ["0000080c"] = "fr-be-azerty",
    ["00000c0c"] = "fr-ca-qwerty",
    ["0000100c"] = "fr-ch-qwertz",
    ["0000040c"] = "fr-fr-azerty",
    ["0000040e"] = "hu-hu-qwertz",
    ["00000410"] = "it-it-qwerty",
    ["00000411"] = "ja-jp-qwerty",
    ["00000414"] = "no-no-qwerty",
    ["00000415"] = "pl-pl-qwerty",
    ["00000416"] = "pt-br-qwerty",
    ["00000816"] = "pt-pt-qwerty",
    ["00000418"] = "ro-ro-qwerty",
    ["0000041d"] = "sv-se-qwerty",
    ["0000041f"] = "tr-tr-qwerty",
  };

  /// <summary>
  /// Every supported guacd "server-layout" value and the failsafe unicode fallback.
  /// </summary>
  public static readonly IReadOnlyList<string> All = [.. s_klidToGuacdLayout.Values.Order(System.StringComparer.Ordinal), "failsafe"];

  private static readonly HashSet<string> s_values = [.. All];

  /// <summary>
  /// True if the given value is one of guacd's supported "server-layout" values.
  /// </summary>
  public static bool IsValid(string? value) => !string.IsNullOrEmpty(value) && s_values.Contains(value);

  [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
  private static extern bool GetKeyboardLayoutNameW([Out] char[] pwszKLID);

  /// <summary>
  /// Detects this server's current default keyboard layout and maps it to the closest
  /// guacd "server-layout" value.
  /// <br /><br />
  /// If a keyboard layout ID falls outside the recognized set, the method falls back to "en-us-qwerty".
  /// </summary>
  /// <returns>The detected guacd layout value and the raw Windows KLID that was detected (if any).</returns>
  public static (string Layout, string? Klid) DetectServerLayout() {
    var buffer = new char[9]; // KL_NAMELENGTH is 9 (8 hex digits + a null terminator)
    if (GetKeyboardLayoutNameW(buffer)) {
      var klid = new string(buffer).TrimEnd('\0');
      if (s_klidToGuacdLayout.TryGetValue(klid, out var layout)) {
        return (layout, klid);
      }
      return ("en-us-qwerty", klid);
    }
    return ("en-us-qwerty", null);
  }
}
