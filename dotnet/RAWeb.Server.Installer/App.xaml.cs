using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using RAWeb.Server.Installer.Setup;

namespace RAWeb.Server.Installer;

public partial class App : Application {
  protected override void OnStartup(StartupEventArgs e) {
    base.OnStartup(e);

    var schemaSwitchIndex = Array.IndexOf(e.Args, SetupSchemaGenerator.Switch);
    if (schemaSwitchIndex >= 0 && schemaSwitchIndex + 1 < e.Args.Length) {
      SetupSchemaGenerator.GenerateAndWrite(e.Args[schemaSwitchIndex + 1]);
      Shutdown(0);
      return;
    }

    var options = StartupOptions.Parse(e.Args);

    if (options.NoGui) {
      var allocatedNewConsole = EnsureConsole();
      var exitCode = ConsoleInstaller.Run(options, e.Args, allocatedNewConsole);
      if (exitCode != 0) {
        Console.WriteLine($"Installation failed with exit code {exitCode}.");
      }
      Environment.Exit(exitCode);
      return;
    }

    MainWindow = new MainWindow(options, e.Args);
    MainWindow.Show();
  }

  [DllImport("kernel32.dll")]
  private static extern bool AttachConsole(int processId);

  [DllImport("kernel32.dll")]
  private static extern bool AllocConsole();

  private const int AttachParentProcess = -1;

  /// <summary>
  /// Since this is a WinExe app, it has no console of its own.
  /// When using --no-gui mode, we need to either attach to the console
  /// of process that launched it (usually powershell or cmd) or create
  /// a new console window.
  /// </summary>
  /// <returns>
  /// True if a new console window was created and false if an existing one was used.
  /// </returns>
  private static bool EnsureConsole() {
    var attachedToExisting = AttachConsole(AttachParentProcess);
    if (!attachedToExisting) {
      AllocConsole();
    }

    Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
    Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
    Console.SetIn(new StreamReader(Console.OpenStandardInput()));

    return !attachedToExisting;
  }
}
