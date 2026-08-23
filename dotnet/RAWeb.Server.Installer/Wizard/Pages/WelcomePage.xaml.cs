using System.Windows;
using System.Windows.Controls;
using RAWeb.Server.Installer.Setup;

namespace RAWeb.Server.Installer.Wizard.Pages;

public partial class WelcomePage : WizardPage {
  private static readonly string[] s_steps =
  [
    "Enable Internet Information Services (IIS) and related features",
    "Install the ASP.NET Core Hosting Bundle",
    "Create a folder on this device for RAWeb to store its files",
    "Configure an application and application pool in IIS to host RAWeb",
    "Install a Windows service for actions that require elevated privileges",
  ];

  public WelcomePage() {
    InitializeComponent();

    foreach (var step in s_steps) {
      StepList.Children.Add(new TextBlock {
        Text = "•  " + step,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 0, 0, 6),
        Opacity = 0.85,
      });
    }
  }

  public override string Title => "Install or Update RAWeb";

  public override string Description => "RemoteApps and Devices, published from your own server.";

  public override bool ShowBack => false;

  public override string NextText {
    get {
      if (ElevationHelper.IsElevated) {
        return "Continue";
      }

      return "Restart as administrator";
    }
  }

  public override void OnEnter(WizardNavigationDirection direction) {
    AdminBar.IsOpen = !ElevationHelper.IsElevated;

    // warn installers early that their OS is not supported so they don't waste time downloading a release that won't work.
    var os = SystemChecks.DescribeOperatingSystem();
    OsBar.IsOpen = !os.IsSupported;
    OsBar.Message = os.IsHome
      ? $"{os.Caption} cannot host Remote Desktop connections, so RAWeb will not work on it."
      : $"RAWeb is intended for Windows 10, Windows 11, and Windows Server. This machine reports {os.Caption}.";

    EnvironmentText.Text = $"{os.Caption}  ·  Installer v{InstallerVersion.Current}";

    CanGoNext = !os.IsHome;
  }

  public override Task<bool> OnNextAsync() {
    if (ElevationHelper.IsElevated) {
      return Task.FromResult(true);
    }

    // the installer is running unelevated, so the next step is to relaunch it elevated.
    var relaunched = ElevationHelper.RelaunchElevated(State.CaptureHandoff());
    if (relaunched) {
      Application.Current.Shutdown();
    }
    return Task.FromResult(false);
  }

  public void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
    e.Handled = true;
  }
}
