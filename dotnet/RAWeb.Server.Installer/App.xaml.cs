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
    MainWindow = new MainWindow(options);
    MainWindow.Show();
  }
}
