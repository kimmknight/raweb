using System.IO;
using RAWeb.Server.Installer.Setup;
using RAWeb.Server.Installer.Wizard;

namespace RAWeb.Server.Installer;

/// <summary>
/// Performs same install as the wizard GUI but entirely through the console instead
/// of a window, for "--no-gui". In general, options must be provided as
/// command-line arguments instead of through prpts, but warnings will still be displayed
/// and may require action.
/// </summary>
public static class ConsoleInstaller {
  public static int Run(StartupOptions options, string[] rawArgs, bool allocatedNewConsole) {
    if (!ElevationHelper.IsElevated) {
      Console.WriteLine("Administrator privileges are required. Requesting elevation...");

      if (!ElevationHelper.RelaunchElevated(ElevationHelper.QuoteArguments(rawArgs))) {
        Console.Error.WriteLine("Elevation was declined. Installation cancelled.");
        return 1;
      }

      // the installer successfully launched an elevated copy of itself
      return 0;
    }

    var (exitCode, succeeded) = RunElevated(options);
    if (allocatedNewConsole) {

      MaybeWaitToClose(options.AutoClose, succeeded);
    }
    return exitCode;
  }

  /// <summary>
  /// Decides whether to automatically exit the terminal based on
  /// the --autoclose option.
  /// </summary>
  private static void MaybeWaitToClose(AutoCloseMode autoClose, bool succeeded) {
    var shouldClose = autoClose switch {
      AutoCloseMode.Always => true,
      AutoCloseMode.Success => succeeded,
      _ => false,
    };

    if (shouldClose) {
      return;
    }

    Console.Write("\nPress Enter to close... ");
    Console.ReadLine();
  }

  private static (int ExitCode, bool Succeeded) RunElevated(StartupOptions options) {
    var state = new WizardState();

    try {
      ResolveRelease(state, options);

      Console.WriteLine();
      Console.Write("┌─");
      Console.ForegroundColor = ConsoleColor.Green;
      Console.Write(" RAWeb Installer ");
      Console.ResetColor();
      Console.WriteLine("─────────────────────────────┐");
      Console.WriteLine("│                                               │");
      Console.WriteLine("│  Please wait while we prepare the installer.  │");
      Console.WriteLine("│                                               │");
      Console.WriteLine("└───────────────────────────────────────────────┘");
      Console.ForegroundColor = ConsoleColor.DarkGray;
      Console.WriteLine($"{state.DisplayVersion}");
      Console.ResetColor();
      Console.WriteLine();

      var distributablesRoot = ReleasePreparer.Prepare(state, ReportProgress, CancellationToken.None);
      EndProgressLine();

      if (distributablesRoot is null) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine("This release only installs through setup.ps1 or install.raweb.app, which --no-gui does not support. Use the graphical user interface installer instead.");
        Console.ResetColor();
        return (1, false);
      }

      state.PayloadRoot = distributablesRoot;
      state.Strategy = ReleaseInspector.DetermineStrategy(distributablesRoot);

      if (state.Strategy != InstallStrategy.Native) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine("This release predates setup.json and cannot be installed through --no-gui. Use setup.ps1 directly or the graphical user interface installer.");
        Console.ResetColor();
        return (1, false);
      }

      ReleasePreparer.FinishPreparation(state, distributablesRoot, ReportProgress, WriteLogEntry, CancellationToken.None);
      EndProgressLine();

      ApplyCommandLineOptions(state, options);

      state.Plan = InstallPlanner.Create(state.Manifest!, state.System!, state.Request, state.Iis);
      if (state.Request.WebSite.Length == 0) {
        state.Request.WebSite = state.Plan.WebSite;
      }
      if (state.Request.VirtualPath.Length == 0) {
        state.Request.VirtualPath = state.Plan.VirtualPath;
      }
      if (state.Request.InstallDirectory.Length == 0) {
        state.Request.InstallDirectory = state.Plan.InstallDirectory;
      }

      var validationErrors = state.Request.Validate(state.Manifest!);
      if (validationErrors.Count > 0) {
        foreach (var error in validationErrors) {
          Console.Error.WriteLine($"Error: {error}");
        }
        return (1, false);
      }

      if (!options.Express) {
        PrintOverview(state);
        Console.Write("\nPress Enter to continue or Ctrl+C to cancel... ");
        Console.ReadLine();
      }

      foreach (var warning in state.Plan.Warnings) {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Warning: {warning.Title}: {warning.Detail}");
        Console.ResetColor();
      }

      if (!ConfirmBlockingWarnings(state.Plan.Warnings, options.Overwrite)) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Installation cancelled before any changes were made.");
        Console.ResetColor();
        return (1, false);
      }

      state.Log.Logged += WriteLogEntry;
      var engine = new InstallEngine(state.Log);
      var result = engine.Run(state.Plan, CancellationToken.None);
      state.Result = result;

      if (result.Succeeded) {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"RAWeb {state.DisplayVersion} installed.");
        Console.ResetColor();
        Console.WriteLine($"Web interface: {result.WebInterfaceUrl}");
        Console.WriteLine($"Workspace feed: {result.WorkspaceUrl}");
        return (0, true);
      }

      Console.Error.WriteLine();
      Console.Error.WriteLine(result.RestartRequired
        ? $"A restart is required to continue: {result.FailureMessage}"
        : $"Installation failed: {result.FailureMessage}");
      return (1, false);
    }
    catch (Exception exception) {
      Console.Error.WriteLine($"Installation failed: {exception.Message}");
      return (1, false);
    }
    finally {
      state.CleanUpScratch();
    }
  }

  private static void ResolveRelease(WizardState state, StartupOptions options) {
    if (options.Source is { Length: > 0 } raw) {
      var source = SourceSelection.Parse(raw);

      if (source.LocalPath is { Length: > 0 } sourcePath) {
        state.LocalSourcePath = sourcePath;
        state.DisplayVersion = options.ReleaseLabel ?? Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar));
        return;
      }

      var release = ReleaseSource.GetReleaseByTag(source.ReleaseTag!, source.Repository);
      state.SelectedAsset = release.PrimaryArchive
        ?? throw new InstallFailedException($"Release {source.ReleaseTag} does not have an installable archive attached.");
      state.DisplayVersion = options.ReleaseLabel ?? release.TagName;
      return;
    }

    // Console.WriteLine("No --source was specified. Resolving the latest RAWeb release from GitHub...");
    var latest = ReleaseSource.ListGitHubReleases()
      .Where(candidate => candidate.PrimaryArchive is not null)
      .OrderByDescending(candidate => candidate.PublishedAt)
      .FirstOrDefault()
      ?? throw new InstallFailedException("Could not find an installable release on GitHub.");

    state.SelectedAsset = latest.PrimaryArchive;
    state.DisplayVersion = latest.TagName;
  }

  private static void ApplyCommandLineOptions(WizardState state, StartupOptions options) {
    if (options.WebSite is { Length: > 0 } webSite) {
      state.Request.WebSite = webSite;
    }
    if (options.VirtualPath is { Length: > 0 } virtualPath) {
      state.Request.VirtualPath = virtualPath;
    }
    if (options.InstallDirectory is { Length: > 0 } installDirectory) {
      state.Request.InstallDirectory = installDirectory;
    }
    foreach (var option in options.Options) {
      state.Request.Options[option.Key] = option.Value;
    }
  }

  /// <summary>
  /// Mirrors InstallPage.ConfirmBlockingWarningsAsync but as a yes/no console prompt instead of a
  /// ContentDialog.
  /// </summary>
  private static bool ConfirmBlockingWarnings(IReadOnlyList<PlanWarning> warnings, bool overwrite) {
    var blocking = warnings.Where(warning => !warning.DefaultsToProceed).ToArray();
    var unanswered = overwrite ? blocking.Where(warning => !warning.IsOverwriteWarning).ToArray() : blocking;

    if (unanswered.Length == 0) {
      return true;
    }

    Console.WriteLine("\nThe following require confirmation to continue:");
    foreach (var warning in unanswered) {
      Console.WriteLine($"  - {warning.Title}: {warning.Detail}");
    }

    Console.Write("Continue anyway? [y/N] ");
    var response = Console.ReadLine()?.Trim();
    return string.Equals(response, "y", StringComparison.OrdinalIgnoreCase)
      || string.Equals(response, "yes", StringComparison.OrdinalIgnoreCase);
  }

  private static void PrintOverview(WizardState state) {
    var plan = state.Plan!;

    Console.WriteLine("\nThe following will be used to install RAWeb:");
    Console.WriteLine($"  Version:            {state.DisplayVersion}");
    Console.WriteLine($"  Web site:           {plan.WebSite}");
    Console.WriteLine($"  Virtual path:       {plan.VirtualPath}");
    Console.WriteLine($"  Install directory:  {plan.InstallDirectory}");

    foreach (var option in state.Manifest!.Options) {
      var value = state.Request.GetOption(option.Id) ?? option.DefaultValue;
      Console.WriteLine($"  {option.Label}: {value}");
    }
  }

  private static string? s_lastProgressPrefix;
  private static int s_lastProgressLineLength;

  /// <summary>
  /// Writes the progress to the console, overwriting the previous line if the status is the same as the last report.
  /// </summary>
  private static void ReportProgress(string status, string detail, double? percent) {
    var prefix = detail.Length > 0 ? $"{status}: {detail}" : status;
    var line = percent is { } value ? $"{prefix} ({value:0}%)" : prefix;
    var sameGroup = percent is not null && prefix == s_lastProgressPrefix;

    Console.ForegroundColor = ConsoleColor.Cyan;
    if (sameGroup) {
      Console.Write('\r' + line.PadRight(s_lastProgressLineLength));
    }
    else {
      Console.ResetColor();
      if (s_lastProgressPrefix is not null) {
        Console.WriteLine();
      }
      Console.ForegroundColor = ConsoleColor.Cyan;
      Console.Write(line);
    }
    Console.ResetColor();

    s_lastProgressLineLength = Math.Max(sameGroup ? s_lastProgressLineLength : 0, line.Length);

    if (percent is null) {
      s_lastProgressPrefix = null;
      Console.WriteLine();
    }
    else {
      s_lastProgressPrefix = prefix;
    }
  }

  /// <summary>
  /// If a progress line is currently being displayed, ends it with a newline so the next output starts on a new line.
  /// </summary>
  private static void EndProgressLine() {
    if (s_lastProgressPrefix is not null) {
      s_lastProgressPrefix = null;
      Console.WriteLine();
    }
  }

  /// <summary>
  /// Writes a log entry, coloring "[n/total] Title" step entries cyan the same way setup.ps1 colors
  /// its own numbered steps.
  /// </summary>
  private static void WriteLogEntry(LogEntry entry) {
    var color = entry.Severity switch {
      LogSeverity.Step => ConsoleColor.Cyan,
      LogSeverity.Success => ConsoleColor.Green,
      LogSeverity.Warning => ConsoleColor.Yellow,
      LogSeverity.Error => ConsoleColor.Red,
      _ => (ConsoleColor?)null,
    };

    if (color is { } value) {
      Console.ForegroundColor = value;
    }
    Console.WriteLine(FormatLogEntry(entry));
    if (color is not null) {
      Console.ResetColor();
    }
  }

  private static string FormatLogEntry(LogEntry entry) => entry.Severity switch {
    LogSeverity.Error => $"ERROR: {entry.Message}",
    LogSeverity.Warning => $"WARNING: {entry.Message}",
    _ => entry.Message,
  };
}
