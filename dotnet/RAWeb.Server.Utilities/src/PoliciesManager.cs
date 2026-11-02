using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace RAWeb.Server.Utilities;

/// <summary>
/// Manages application policies stored in the appSettings.config file.
/// <br /><br />
/// The appSettings.config file is located in the App_Data folder by default.
/// Specify a different path via the constructor parameter if needed.
/// </summary>
/// <param name="appSettingsPath"></param>
public enum RdpSignatureManagementMode {
  DoNothing = 1,
  StripAll = 2,
  SignUnsigned = 3,
  SignUnsignedAndResignSigned = 4,
}

public sealed class PoliciesManager {
  private static string s_appSettingsPath = Path.Combine(Constants.AppDataFolderPath, "appSettings.config");

  /// <summary>
  /// Constructor for PoliciesManager.
  /// Unless you need to specify a different path for appSettings.config,
  /// you do not need to instantiate this class. Instead, use the static
  /// methods.
  /// </summary>
  /// <param name="appSettingsPath"></param>
  public PoliciesManager(string? appSettingsPath = null) {
    if (appSettingsPath != null) {
      s_appSettingsPath = appSettingsPath;
    }
  }

  /// <summary>
  /// Exposes the key-value pairs from the appSettings.config file as a dictionary.
  /// </summary>
  public static PoliciesDictionary RawPolicies {
    get {
      // read the XML content of appSettings
      if (!File.Exists(s_appSettingsPath)) {
        return new PoliciesDictionary();
      }

      var appSettingsXml = File.ReadAllText(s_appSettingsPath);

      // parse the XML to extract key-value pairs
      var appSettings = XDocument.Parse(appSettingsXml)
        .Descendants("appSettings")
        .Descendants("add")
        .ToDictionary(
          x => x.Attribute("key")?.Value ?? "",
          x => x.Attribute("value")?.Value ?? ""
        );

      return appSettings;
    }
  }

  /// <summary>
  /// Sets or updates the specified key-value pair in the appSettings.config file.
  /// </summary>
  /// <param name="key"></param>
  /// <param name="value"></param>
  public static void Set(string key, string value) {
    if (string.IsNullOrEmpty(value)) {
      Remove(key);
      return;
    }

    var policies = RawPolicies.Value;
    policies[key] = value;
    WriteFile(policies);
  }

  /// <summary>
  /// Removes the specified key from the appSettings.config file.
  /// </summary>
  /// <param name="key"></param>
  public static void Remove(string key) {
    var policies = RawPolicies.Value;
    if (policies.ContainsKey(key)) {
      policies.Remove(key);
      WriteFile(policies);
    }
  }

  /// <summary>
  /// The policies that cannot be changed through the web API. They can only be changed by
  /// editing appSettings.config on the server.
  /// </summary>
  private static readonly string[] s_fileOnlyPolicies = [
    "App.Auth.Anonymous",
    "App.Auth.TestLoginForm.Enabled"
  ];

  /// <summary>
  /// Whether the policy can only be changed by editing appSettings.config on the server.
  /// </summary>
  public static bool IsFileOnlyPolicy(string key) {
    return s_fileOnlyPolicies.Contains(key, System.StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>
  /// The policies whose values contain secrets. Each value is one or more connections in the
  /// format <c>clientId:clientSecret@hostname@domainsCSV</c>, separated by semicolons.
  /// </summary>
  private static readonly string[] s_policiesWithSecrets = ["App.Auth.MFA.Duo", "App.Auth.MFA.LoginTC"];

  /// <summary>
  /// Replaces a secret when policies are read through the web API so that the secret cannot be
  /// read back (e.g., by read-only administrators). It must not contain ':', '@', ';', or ','.
  /// </summary>
  public const string RedactedSecret = "********";

  /// <summary>
  /// Returns the policy value with each connection's secret replaced by <see cref="RedactedSecret"/>.
  /// Values of policies that do not contain secrets are returned unchanged.
  /// </summary>
  public static string? RedactSecrets(string key, string? value) {
    if (value is null || !s_policiesWithSecrets.Contains(key)) {
      return value;
    }

    return string.Join(';', value.Split(';').Select(connection => {
      var (clientId, _, rest) = SplitConnectionSecret(connection);
      return clientId is null ? connection : $"{clientId}:{RedactedSecret}{rest}";
    }));
  }

  /// <summary>
  /// Returns a copy of the policies with the secrets replaced by <see cref="RedactedSecret"/>.
  /// </summary>
  public static Dictionary<string, string> RedactSecrets(Dictionary<string, string> policies) {
    return policies.ToDictionary(kv => kv.Key, kv => RedactSecrets(kv.Key, kv.Value) ?? "");
  }

  /// <summary>
  /// Replaces each <see cref="RedactedSecret"/> in a new policy value with the secret that is
  /// currently stored for the same client ID. This allows administrators to change other parts
  /// of a connection (e.g., its domains) without entering the secret again.
  /// </summary>
  /// <exception cref="ArgumentException">When a connection has the redacted secret but no stored
  /// secret exists for its client ID (e.g., a new connection).</exception>
  public static string RestoreRedactedSecrets(string key, string value) {
    if (!s_policiesWithSecrets.Contains(key) || !value.Contains(RedactedSecret)) {
      return value;
    }

    // find the stored secret for each client ID
    var storedSecrets = new Dictionary<string, string>();
    foreach (var connection in (RawPolicies[key] ?? "").Split(';')) {
      var (clientId, secret, _) = SplitConnectionSecret(connection);
      if (clientId is not null) {
        storedSecrets[clientId.Trim()] = secret!;
      }
    }

    return string.Join(';', value.Split(';').Select(connection => {
      var (clientId, secret, rest) = SplitConnectionSecret(connection);
      if (clientId is null || secret != RedactedSecret) {
        return connection;
      }
      if (!storedSecrets.TryGetValue(clientId.Trim(), out var storedSecret)) {
        throw new System.ArgumentException($"The secret for '{clientId.Trim()}' must be entered.");
      }
      return $"{clientId}:{storedSecret}{rest}";
    }));
  }

  /// <summary>
  /// Splits a connection (<c>clientId:clientSecret@hostname@domainsCSV</c>) into its client ID,
  /// its secret, and the rest of the connection (starting at the first '@'). The client ID is
  /// null if the connection does not contain a secret.
  /// </summary>
  private static (string? ClientId, string? Secret, string Remainder) SplitConnectionSecret(string connection) {
    var atIndex = connection.IndexOf('@');
    var credentials = atIndex >= 0 ? connection[..atIndex] : connection;
    var rest = atIndex >= 0 ? connection[atIndex..] : "";

    var colonIndex = credentials.IndexOf(':');
    if (colonIndex < 0) {
      return (null, null, rest);
    }
    return (credentials[..colonIndex], credentials[(colonIndex + 1)..], rest);
  }

  /// <summary>
  /// Writes the provided policies dictionary back to the appSettings.config file.
  /// </summary>
  /// <param name="policies"></param>
  private static void WriteFile(Dictionary<string, string> policies) {
    var appSettingsElement = new XElement("appSettings",
      policies
        // order keys alphabetically
        .OrderBy(kv => kv.Key)
        // convert to XML elements
        .Select(kv => new XElement("add",
          new XAttribute("key", kv.Key),
          new XAttribute("value", kv.Value)
        ))
    );

    var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), appSettingsElement);
    doc.Save(s_appSettingsPath);
  }

  /// <summary>
  /// Gets the LoginTC MFA policy configuration that matches the specified domain.
  /// If multiple policies match, the first one found is returned.
  /// If no matching policy is found, null is returned.
  /// Exact domain matches are prioritized over wildcard matches.
  ///
  /// If a username is provided, this method will also check if
  /// the username is excluded from MFA via the "App.Auth.MFA.LoginTC.Excluded" policy.
  /// If the username is excluded, this method returns null.
  /// </summary>
  /// <param name="domain"></param>
  /// <param name="username"></param>
  /// <returns></returns>
  public static LoginTCMfaPolicyResult? GetLoginTCMfaPolicyForDomain(string domain, string? username = null) {
    var loginTcPolicies = RawPolicies.LoginTCMfa;
    if (loginTcPolicies == null) {
      return null;
    }

    // check if the username is excluded from MFA
    if (username != null) {
      var excludedUsernamesCsv = RawPolicies["App.Auth.MFA.LoginTC.Excluded"];
      if (!string.IsNullOrEmpty(excludedUsernamesCsv) && excludedUsernamesCsv is not null) {
        var excludedUsernames = excludedUsernamesCsv
          .Split(',')
          .Select(u => u.Trim())
          .Where(u => !string.IsNullOrEmpty(u))
          .Select(u => {
            var parts = u.Split('\\');
            return parts.Length == 2 ? (parts[0], parts[1]) : (null, null);
          })
          .Where(t => t.Item1 != null && t.Item2 != null)
          .Cast<(string Domain, string Username)>()
          .ToArray();

        // check if the full username (with domain) is excluded
        var usernameIsExcluded = excludedUsernames
          .Any(excluded =>
            string.Equals(excluded.Domain, domain, System.StringComparison.OrdinalIgnoreCase) &&
            string.Equals(excluded.Username, username, System.StringComparison.Ordinal)
          );
        if (usernameIsExcluded) {
          return null;
        }

        // if the domain is the machine name, also check for .\username exclusion
        usernameIsExcluded = excludedUsernames
          .Any(excluded =>
            string.Equals(excluded.Domain, ".") &&
            string.Equals(excluded.Username, username, System.StringComparison.OrdinalIgnoreCase)
          );
        if (usernameIsExcluded) {
          return null;
        }
      }
    }

    // first try to find an exact match
    var matchingPolicies = loginTcPolicies.Where(p => p.Domains.Contains(domain)).ToArray();
    if (matchingPolicies.Length > 0) {
      return matchingPolicies[0];
    }

    // next, look for a wildcard match
    var wildcardPolicies = loginTcPolicies.Where(p => p.Domains.Contains("*")).ToArray();
    if (wildcardPolicies.Length > 0) {
      return wildcardPolicies[0];
    }

    return null;
  }

  /// <summary>
  /// Gets the Duo MFA policy configuration that matches the specified domain.
  /// If multiple policies match, the first one found is returned.
  /// If no matching policy is found, null is returned.
  /// Exact domain matches are prioritized over wildcard matches.
  /// 
  /// If a username is provided, this method will also check if
  /// the username is excluded from MFA via the "App.Auth.MFA.Duo.Excluded" policy.
  /// If the username is excluded, this method returns null.
  /// </summary>
  /// <param name="domain"></param>
  /// <returns></returns>
  public static DuoMfaPolicyResult? GetDuoMfaPolicyForDomain(string domain, string? username = null) {
    var duoPolicies = RawPolicies.DuoMfa;
    if (duoPolicies == null) {
      return null;
    }

    // check if the username is excluded from MFA
    if (username != null) {
      var excludedUsernamesCsv = RawPolicies["App.Auth.MFA.Duo.Excluded"];
      if (!string.IsNullOrEmpty(excludedUsernamesCsv) && excludedUsernamesCsv is not null) {
        var excludedUsernames = excludedUsernamesCsv
          .Split(',')
          .Select(u => u.Trim())
          .Where(u => !string.IsNullOrEmpty(u))
          .Select(u => {
            var parts = u.Split('\\');
            return parts.Length == 2 ? (parts[0], parts[1]) : (null, null);
          })
          .Where(t => t.Item1 != null && t.Item2 != null)
          .Cast<(string Domain, string Username)>()
          .ToArray();

        // check if the full username (with domain) is excluded
        var usernameIsExcluded = excludedUsernames
          .Any(excluded =>
            string.Equals(excluded.Domain, domain, System.StringComparison.OrdinalIgnoreCase) &&
            string.Equals(excluded.Username, username, System.StringComparison.Ordinal)
          );
        if (usernameIsExcluded) {
          return null;
        }

        // if the domain is the machine name, also check for .\username exclusion
        var machineName = System.Environment.MachineName;
        usernameIsExcluded = excludedUsernames
          .Any(excluded =>
            string.Equals(excluded.Domain, ".") &&
            string.Equals(excluded.Username, username, System.StringComparison.OrdinalIgnoreCase)
          );
        if (usernameIsExcluded) {
          return null;
        }
      }
    }

    // first try to find an exact match
    var matchingDuoPolicies = duoPolicies.Where(p => p.Domains.Contains(domain)).ToArray();
    if (matchingDuoPolicies.Length > 0) {
      return matchingDuoPolicies[0];
    }

    // next, look for a wildcard match
    var wildcardPolicies = duoPolicies.Where(p => p.Domains.Contains("*")).ToArray();
    if (wildcardPolicies.Length > 0) {
      return wildcardPolicies[0];
    }

    return null;
  }

  /// <summary>
  /// A custom dictionary-like class that returns null for missing keys.
  /// </summary>
  public sealed class PoliciesDictionary(Dictionary<string, string>? innerDictionary = null) {
    private readonly Dictionary<string, string> _innerDictionary = innerDictionary ?? [];

    public static implicit operator PoliciesDictionary(Dictionary<string, string> dictionary) {
      return new PoliciesDictionary(dictionary);
    }

    /// <summary>
    /// Gets the value associated with the specified key, or null if the key does not exist. [indexer]
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public string? this[string key] {
      get {
        _innerDictionary.TryGetValue(key, out var value);
        return value;
      }
    }

    /// <summary>
    /// Gets the LoginTC MFA policy configuration if it exists; otherwise, returns null.
    /// The policy value, if present, is expected to be in the format:
    /// clientId:clientSecret@hostname@domainsCSV. This method parses that string and returns
    /// a LoginTCMfaPolicyResult object containing the individual components. Multiple
    /// domains can be specified as a comma-separated list. Multiple LoginTC integrations
    /// can be specified by providing multiple values separated by semicolons.
    /// </summary>
    public LoginTCMfaPolicyResult[]? LoginTCMfa {
      get {
        var rawEnablementValue = this["App.Auth.MFA.LoginTC.Enabled"];
        var rawConfigValue = this["App.Auth.MFA.LoginTC"];

        // check if LoginTC MFA is enabled
        if (string.IsNullOrEmpty(rawEnablementValue) ||
            !bool.TryParse(rawEnablementValue, out var isEnabled) ||
            !isEnabled) {
          return null;
        }

        // get each connection string (multiple connections separated by semicolons)
        var connectionStrings = rawConfigValue?.Split(';').Select(str => str.Trim()) ?? [];

        // parse each connection string into a LoginTCMfaPolicyResult
        var results = connectionStrings
          .Select(connectionString => {
            // part 1: clientId:clientSecret; part 2: hostname; part 3: domainsCSV
            var parts = connectionString.Split('@');
            if (parts.Length < 2 || parts.Length > 3) {
              return null;
            }
            var credentialsPart = parts[0];
            var hostnamePart = parts[1];
            var domainsPart = parts.Length == 3 ? parts[2] : null;

            // part 1a: clientId; part 1b: clientSecret
            var credentialsParts = credentialsPart.Split(':');
            if (credentialsParts.Length != 2) {
              return null;
            }
            var clientId = credentialsParts[0];
            var clientSecret = credentialsParts[1];

            // parse domains
            var domains = domainsPart != null
              ? domainsPart.Split(',').Select(d => d.Trim()).ToArray()
              : [];

            return new LoginTCMfaPolicyResult(
              Hostname: hostnamePart,
              ClientId: clientId,
              SecretKey: clientSecret,
              Domains: domains,
              RedirectPath: "/api/auth/logintc/callback"
            );
          })
          .OfType<LoginTCMfaPolicyResult>()
          .ToArray();

        return results;
      }
    }

    /// <summary>
    /// Gets the Duo MFA policy configuration if it exists; otherwise, returns null.
    /// The policy value, if present, is expected to be in the format:
    /// clientId:clientSecret@hostname@domainsCSV. This method parses that string and returns
    /// a DuoMfaPolicyResult object containing the individual components. Multiple
    /// domains can be specified as a comma-separated list. Multiple Duo integrations
    /// can be specified by providing multiple values separated by semicolons.
    /// </summary>
    public DuoMfaPolicyResult[]? DuoMfa {
      get {
        var rawEnablementValue = this["App.Auth.MFA.Duo.Enabled"];
        var rawConfigValue = this["App.Auth.MFA.Duo"];

        // check if Duo MFA is enabled
        if (string.IsNullOrEmpty(rawEnablementValue) ||
            !bool.TryParse(rawEnablementValue, out var isEnabled) ||
            !isEnabled) {
          return null;
        }

        // get each connectiong string (multiple connections separated by semicolons)
        var connectionStrings = rawConfigValue?.Split(';').Select(str => str.Trim()) ?? [];

        // parse each connection string into a DuoMfaPolicyResult
        var results = connectionStrings
          .Select(connectionString => {
            // part 1: clientId:clientSecret; part 2: hostname; part 3: domainsCSV
            var parts = connectionString.Split('@');
            if (parts.Length < 2 || parts.Length > 3) {
              return null;
            }
            var credentialsPart = parts[0];
            var hostnamePart = parts[1];
            var domainsPart = parts.Length == 3 ? parts[2] : null;

            // part 1a: clientId; part 1b: clientSecret
            var credentialsParts = credentialsPart.Split(':');
            if (credentialsParts.Length != 2) {
              return null;
            }
            var clientId = credentialsParts[0];
            var clientSecret = credentialsParts[1];

            // parse domains
            var domains = domainsPart != null
              ? domainsPart.Split(',').Select(d => d.Trim()).ToArray()
              : [];

            return new DuoMfaPolicyResult(
              Hostname: hostnamePart,
              ClientId: clientId,
              SecretKey: clientSecret,
              Domains: domains,
              RedirectPath: "/api/auth/duo/callback"
            );
          })
          .OfType<DuoMfaPolicyResult>()
          .ToArray();

        return results;
      }
    }

    /// <summary>
    /// Gets the RDP file signature management mode from the "RDP.ManageSignatures" policy.
    /// <br /><br />
    /// For backward compatibility, if "RDP.ManageSignatures" is not set but the legacy
    /// "RDP.StripSignatures" policy is "true", this returns <see cref="RdpSignatureManagementMode.StripAll"/>.
    /// If this policy is set and the legacy "RDP.StripSignatures" policy exists, the legacy policy is removed.
    /// </summary>
    public RdpSignatureManagementMode RdpManageSignaturesMode {
      get {
        var raw = this["RDP.ManageSignatures"];
        if (raw is not null && int.TryParse(raw, out var modeValue) && System.Enum.IsDefined(typeof(RdpSignatureManagementMode), modeValue)) {
          // when set, clean up the legacy "RDP.StripSignatures" policy if it exists
          if (this["RDP.StripSignatures"] is not null) {
            Remove("RDP.StripSignatures");
          }

          return (RdpSignatureManagementMode)modeValue;
        }

        if (string.Equals(this["RDP.StripSignatures"], "true", System.StringComparison.OrdinalIgnoreCase)) {
          return RdpSignatureManagementMode.StripAll;
        }

        return RdpSignatureManagementMode.DoNothing;
      }
    }

    /// <summary>
    /// Gets the thumbprint of the certificate (in the LocalMachine\My store) used to sign RDP
    /// files, from the "RDP.SigningThumbprint" policy.
    /// </summary>
    public string? RdpSigningThumbprint => this["RDP.SigningThumbprint"];

    /// <summary>
    /// Exposes the internal dictionary for direct serialization to a JSON object.
    /// </summary>
    public Dictionary<string, string> Value => _innerDictionary;
  }

  /// <summary>
  /// Returns the raw bytes and MIME type for an <c>App.Icon.&lt;fileName&gt;</c> policy
  /// override, or <c>null</c> if the policy is not set or the stored data URI is invalid.
  /// </summary>
  public static (byte[] Bytes, string MimeType)? GetIconPolicyOverride(string fileName) {
    var policyValue = RawPolicies[$"App.Icon.{fileName}"];
    if (string.IsNullOrEmpty(policyValue) || !policyValue.StartsWith("data:")) {
      return null;
    }

    // expected format: "data:<mime type>;base64,<base64 data>"
    var semicolonIdx = policyValue.IndexOf(';');
    var commaIdx = policyValue.IndexOf(',');
    if (semicolonIdx <= 5 || commaIdx <= semicolonIdx) {
      return null;
    }

    try {
      var mimeType = policyValue.Substring(5, semicolonIdx - 5);
      var data = System.Convert.FromBase64String(policyValue.Substring(commaIdx + 1));
      return (data, mimeType);
    }
    catch {
      return null;
    }
  }

  public record DuoMfaPolicyResult(
    string Hostname,
    string ClientId,
    string SecretKey,
    string[] Domains,
    string RedirectPath
  );

  public record LoginTCMfaPolicyResult(
    string Hostname,
    string ClientId,
    string SecretKey,
    string[] Domains,
    string RedirectPath
  );
}
