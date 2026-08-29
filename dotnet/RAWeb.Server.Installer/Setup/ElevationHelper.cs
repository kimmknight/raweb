using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace RAWeb.Server.Installer.Setup;

/// <summary>
/// When to close the installer window on its own after InstallPage finishes
/// </summary>
public enum AutoCloseMode {
  /// <summary>
  /// The user must manually close the installer.
  /// </summary>
  Never,
  /// <summary>
  /// The installer will close automatically after a successful installation,
  /// but it will remain open if the installation fails.
  /// </summary>
  Success,
  /// <summary>
  /// The isntaller will automatically close after installation even if it failed.
  /// </summary>
  Always,
}

public sealed class StartupOptions {
  public const string WebSiteSwitch = "--website";
  public const string VirtualPathSwitch = "--virtual-path";
  public const string InstallDirSwitch = "--install-dir";
  public const string OptionSwitch = "--option";
  public const string ExpressSwitch = "--express";
  public const string OverwriteSwitch = "--overwrite";
  public const string AutoCloseSwitch = "--autoclose";
  public const string NoWelcomeSwitch = "--no-welcome";
  public const string NoGuiSwitch = "--no-gui";
  public const string SourceSwitch = "--source";
  public const string ReleaseLabelSwitch = "--release-label";

  /// <summary>
  /// These legacy setup.ps1 switches are aliases for one of the switches above. "-SkipHealthCheck" and
  /// "-AnonymousAuthMode" are not here because they alias a specific "--option" value.
  /// </summary>
  private static readonly Dictionary<string, string> s_aliases = new(StringComparer.OrdinalIgnoreCase) {
    ["-WebSite"] = WebSiteSwitch,
    ["-VirtualPath"] = VirtualPathSwitch,
    ["-InstallDir"] = InstallDirSwitch,
    ["-Express"] = ExpressSwitch,
    ["-AcceptAll"] = ExpressSwitch,
    ["-Overwrite"] = OverwriteSwitch,
  };

  private readonly Dictionary<string, string> _options = new(StringComparer.OrdinalIgnoreCase);

  public string? WebSite { get; private set; }
  public string? VirtualPath { get; private set; }
  public string? InstallDirectory { get; private set; }

  /// <summary>
  /// Values for options declared in setup.json, keyed by option id, from repeated
  /// "--option &lt;id&gt;=&lt;value&gt;" switches.
  /// </summary>
  public IReadOnlyDictionary<string, string> Options => _options;

  /// <summary>
  /// See <see cref="Wizard.WizardState.Express"/>
  /// </summary>
  public bool Express { get; private set; }

  /// <summary>
  /// See <see cref="Wizard.WizardState.Overwrite"/>
  /// </summary>
  public bool Overwrite { get; private set; }

  /// <summary>
  /// See <see cref="Wizard.WizardState.AutoClose"/>
  /// </summary>
  public AutoCloseMode AutoClose { get; private set; } = AutoCloseMode.Never;

  /// <summary>
  /// See <see cref="Wizard.WizardState.NoWelcome"/>
  /// </summary>
  public bool NoWelcome { get => field || Express; private set; }

  /// <summary>
  /// Run entirely in the console instead of showing the wizard window. See
  /// <see cref="ConsoleInstaller"/>.
  /// </summary>
  public bool NoGui { get; private set; }

  /// <summary>
  /// A local folder, a local .zip file, or a GitHub release tag (optionally "&lt;owner&gt;::&lt;tag&gt;"
  /// to pull from a trusted fork) to install without showing the version picker. See <see cref="SourceSelection"/>.
  /// </summary>
  public string? Source { get; private set; }

  /// <summary>
  /// Overrides the version label shown in the wizard/console (e.g. a PR number or branch name),
  /// instead of whatever <see cref="Source"/> would otherwise display.
  /// </summary>
  public string? ReleaseLabel { get; private set; }

  public static StartupOptions Parse(IReadOnlyList<string> args) {
    var options = new StartupOptions();

    var parsedArgs = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

    // loop through the args and parse them into the options object
    for (var i = 0; i < args.Count; i++) {
      var isDoubleDash = args[i].StartsWith("--", StringComparison.OrdinalIgnoreCase);
      var isSingleDash = !isDoubleDash && args[i].Length > 1 && args[i][0] == '-';

      // keep skipping until we find a switch that starts with "-" or "--"
      if (isDoubleDash || isSingleDash) {
        var key = args[i];
        string? value = null;

        // allow values to be specified as "--key=value"/"-Key=value" or "--key value"/"-Key value"
        var equalsIndex = key.IndexOf('=');
        if (equalsIndex > 0) {
          value = key[(equalsIndex + 1)..];
          key = key[..equalsIndex];
        }
        else if (i + 1 < args.Count && args[i + 1].Length > 0 && args[i + 1][0] != '-') {
          value = args[++i];
        }

        // these are legacy switches that were present in setup.ps1
        // that we need to map to an options switch
        if (isSingleDash) {
          if (string.Equals(key, "-SkipHealthCheck", StringComparison.OrdinalIgnoreCase)) {
            key = OptionSwitch;
            value = "skipHealthCheck=true";
          }
          else if (string.Equals(key, "-AnonymousAuthMode", StringComparison.OrdinalIgnoreCase)) {
            if (value is not { Length: > 0 } authMode) {
              continue;
            }
            key = OptionSwitch;
            value = $"anonymousAuth={authMode}";
          }
          else if (s_aliases.TryGetValue(key, out var canonicalSwitch)) {
            key = canonicalSwitch;
          }
          else {
            // ignore unrecognized single-dash switches
            continue;
          }
        }

        // store the key/value pair in the dictionary
        if (parsedArgs.TryGetValue(key, out var existingValues)) {
          parsedArgs[key] = [.. existingValues, value ?? string.Empty];
        }
        else {
          parsedArgs[key] = [value ?? string.Empty];
        }
      }
    }

    // look at the parsed arguments and set options based on argument values

    if (parsedArgs.TryGetValue(WebSiteSwitch, out var webSiteValues) && webSiteValues.Length > 0) {
      options.WebSite = webSiteValues[0];
    }

    if (parsedArgs.TryGetValue(VirtualPathSwitch, out var virtualPathValues) && virtualPathValues.Length > 0) {
      options.VirtualPath = virtualPathValues[0];
    }

    if (parsedArgs.TryGetValue(InstallDirSwitch, out var installDirValues) && installDirValues.Length > 0) {
      options.InstallDirectory = installDirValues[0];
    }

    if (parsedArgs.TryGetValue(OptionSwitch, out var optionValues)) {
      foreach (var pair in optionValues) {
        var separatorIndex = pair.IndexOf('=');
        if (separatorIndex > 0) {
          options._options[pair[..separatorIndex]] = pair[(separatorIndex + 1)..];
        }
      }
    }

    if (parsedArgs.TryGetValue(ExpressSwitch, out var expressValues) && expressValues.Length > 0) {
      options.Express = expressValues[0].Equals("true", StringComparison.OrdinalIgnoreCase) ||
                        expressValues[0].Equals("1", StringComparison.OrdinalIgnoreCase) ||
                        expressValues[0].Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                        expressValues[0].Equals("", StringComparison.OrdinalIgnoreCase);
    }

    if (parsedArgs.TryGetValue(OverwriteSwitch, out var overwriteValues) && overwriteValues.Length > 0) {
      options.Overwrite = overwriteValues[0].Equals("true", StringComparison.OrdinalIgnoreCase) ||
                          overwriteValues[0].Equals("1", StringComparison.OrdinalIgnoreCase) ||
                          overwriteValues[0].Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                          overwriteValues[0].Equals("", StringComparison.OrdinalIgnoreCase);
    }

    if (parsedArgs.TryGetValue(AutoCloseSwitch, out var autoCloseValues) && autoCloseValues.Length > 0) {
      var value = autoCloseValues[0];
      if (string.Equals(value, "always", StringComparison.OrdinalIgnoreCase)) {
        options.AutoClose = AutoCloseMode.Always;
      }
      else if (string.Equals(value, "success", StringComparison.OrdinalIgnoreCase)) {
        options.AutoClose = AutoCloseMode.Success;
      }
      else if (string.Equals(value, "never", StringComparison.OrdinalIgnoreCase)) {
        options.AutoClose = AutoCloseMode.Never;
      }
    }

    if (parsedArgs.TryGetValue(NoWelcomeSwitch, out var noWelcomeValues) && noWelcomeValues.Length > 0) {
      options.NoWelcome = noWelcomeValues[0].Equals("true", StringComparison.OrdinalIgnoreCase) ||
                          noWelcomeValues[0].Equals("1", StringComparison.OrdinalIgnoreCase) ||
                          noWelcomeValues[0].Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                          noWelcomeValues[0].Equals("", StringComparison.OrdinalIgnoreCase);
    }

    if (parsedArgs.TryGetValue(NoGuiSwitch, out var noGuiValues) && noGuiValues.Length > 0) {
      options.NoGui = noGuiValues[0].Equals("true", StringComparison.OrdinalIgnoreCase) ||
                      noGuiValues[0].Equals("1", StringComparison.OrdinalIgnoreCase) ||
                      noGuiValues[0].Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                      noGuiValues[0].Equals("", StringComparison.OrdinalIgnoreCase);
    }

    if (parsedArgs.TryGetValue(SourceSwitch, out var sourceValues) && sourceValues.Length > 0) {
      options.Source = sourceValues[0];
    }

    if (parsedArgs.TryGetValue(ReleaseLabelSwitch, out var releaseLabelValues) && releaseLabelValues.Length > 0) {
      options.ReleaseLabel = releaseLabelValues[0];
    }

    return options;
  }
}

public static class ElevationHelper {
  public static bool IsElevated => SystemChecks.IsRunningAsAdministrator();

  /// <summary>
  /// Starts a second copy of the installer with elevated privileges and the
  /// given command-line arguments.
  /// </summary>
  /// <returns>
  /// True when the elevated process started, meaning this process should exit. False when the user
  /// dismissed the UAC prompt, so the wizard can stay where it is and let them retry.
  /// </returns>
  public static bool RelaunchElevated(string arguments) {
    var executablePath = Assembly.GetEntryAssembly()?.Location ?? Process.GetCurrentProcess().MainModule?.FileName;

    if (executablePath is not { Length: > 0 }) {
      throw new InstallFailedException("Could not determine the installer's own path in order to elevate.");
    }

    var startInfo = new ProcessStartInfo(executablePath, arguments) {
      UseShellExecute = true,
      Verb = "runas",
    };

    try {
      return Process.Start(startInfo) is not null;
    }
    catch (Win32Exception exception) when (exception.NativeErrorCode == 1223) {
      // the user dismissed the UAC prompt.
      return false;
    }
  }

  /// <summary>
  /// Quotes each argument that needs it so they survive
  /// <see cref="ProcessStartInfo.Arguments"/>.
  /// </summary>
  public static string QuoteArguments(IEnumerable<string> args) =>
    string.Join(" ", args.Select(arg => arg.Contains(' ') ? $"\"{arg}\"" : arg));
}
