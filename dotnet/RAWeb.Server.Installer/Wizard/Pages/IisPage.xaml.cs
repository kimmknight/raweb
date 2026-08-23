using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RAWeb.Server.Installer.Setup;

namespace RAWeb.Server.Installer.Wizard.Pages;

public partial class IisPage : WizardPage {
  private bool _initialized;

  public IisPage() => InitializeComponent();

  public override string Title => "Configure location in IIS";

  public override string Description => "RAWeb is published as an application inside an Internet Information Services web site.";

  // when a setup.ps1 is used, this page is unnecessary.
  public override bool ShouldSkip() => State.Strategy.RequiresExternalHandoff();

  public override void OnEnter(WizardNavigationDirection direction) {
    if (!_initialized) {
      _initialized = true;
      ShowBack = false;

      var defaults = State.Manifest!.Defaults;
      var sites = State.System!.IsIisInstalled ? State.Iis.GetSiteNames() : [];

      // when IIS is not yet installed, show a notice that the Default Web Site will be used
      IisMissingBar.IsOpen = sites.Count == 0;

      foreach (var site in sites.Count > 0 ? sites : [defaults.WebSite]) {
        SiteBox.Items.Add(site);
      }

      SiteBox.SelectedItem = SiteBox.Items.Cast<string>()
        .FirstOrDefault(name => string.Equals(name, defaults.WebSite, StringComparison.OrdinalIgnoreCase))
        ?? SiteBox.Items.Cast<string>().FirstOrDefault();

      VirtualPathBox.Text = defaults.VirtualPath;
    }

    UpdatePreview();
  }

  private void OnSiteChanged(object sender, SelectionChangedEventArgs e) => UpdatePreview();

  private void OnVirtualPathChanged(object sender, TextChangedEventArgs e) => UpdatePreview();

  private void UpdatePreview() {
    if (!IsInitialized) {
      return;
    }

    var site = SiteBox.SelectedItem as string ?? "";
    var virtualPath = VirtualPathBox.Text.Trim().Trim('/', '\\');

    CanGoNext = site.Length > 0 && virtualPath.Length > 0;

    UrlPreview.Text = $"https://{Environment.MachineName}/{virtualPath}";
    NamesPreview.Text =
      $"Application pool: {Naming.AppPoolName(site, virtualPath)}\n"
      + $"Management service: {Naming.ServiceName(site, virtualPath)}";

    ShowUpgradeStateFor(site, virtualPath);
  }

  /// <summary>
  /// Warns when an installation already exists at this site and path, which turns the run into an
  /// in-place upgrade rather than a fresh install.
  /// </summary>
  private void ShowUpgradeStateFor(string site, string virtualPath) {
    UpgradeBar.IsOpen = false;
    VPathBar.IsOpen = false;
    UpgradeBar.Margin = new Thickness(0, 0, 0, 0);
    VPathBar.Margin = new Thickness(0, 0, 0, 0);

    if (virtualPath.Length == 0) {
      VPathBar.IsOpen = true;
      VPathBar.Margin = new Thickness(0, 16, 0, 0);
      return;
    }

    if (site.Length == 0 || !InstallPlanner.IsUpgrade(State.Manifest!, State.System!, State.Iis, site, virtualPath)) {
      return;
    }

    var existing = State.Iis.GetApplicationPhysicalPath(site, virtualPath);
    UpgradeBar.Message =
      $"Installing here upgrades the existing application at {existing}. "
      + "Resources, policies, and other app data are preserved.";
    UpgradeBar.IsOpen = true;
    UpgradeBar.Margin = new Thickness(0, 16, 0, 0);
  }

  public override Task<bool> OnNextAsync() {
    State.Request.WebSite = SiteBox.SelectedItem as string ?? "";
    State.Request.VirtualPath = VirtualPathBox.Text;
    return Task.FromResult(true);
  }
}
