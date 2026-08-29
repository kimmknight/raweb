using RAWeb.Server.Installer.Setup;

namespace RAWeb.Server.Installer.Wizard.Pages;

public partial class ModePage : WizardPage {
  private bool _initialized;

  public ModePage() => InitializeComponent();

  public override string Title => "Choose an installation mode";

  public override string Description =>
    "Express installation installs RAWeb with recommended defaults. Custom installation lets you choose the web site, path, directory, and other options.";

  public override bool ShouldSkip() =>
    // if setup.ps1 is used
    State.Strategy.RequiresExternalHandoff()
    // express mode already selected via --express
    || State.Express;

  public override void OnEnter(WizardNavigationDirection direction) {
    if (!_initialized) {
      _initialized = true;
      ExpressRadio.IsChecked = true;
    }

    var defaults = State.Manifest!.Defaults;

    var webSite = State.Request.WebSite is { Length: > 0 } requestedSite ? requestedSite : defaults.WebSite;
    var virtualPath = State.Request.VirtualPath is { Length: > 0 } requestedPath
      ? requestedPath.Trim('/', '\\')
      : defaults.VirtualPath;

    var existingPhysicalPath = State.System!.IsIisInstalled
      ? State.Iis.GetApplicationPhysicalPath(webSite, virtualPath)
      : null;

    var installDirectory = State.Request.InstallDirectory is { Length: > 0 } requestedDirectory
      ? requestedDirectory
      : InstallPlanner.ResolveInstallDirectory(State.Manifest!, webSite, virtualPath, existingPhysicalPath);

    // covers "--website", "--virtual-path", "--install-dir", and any "--option"
    OverridesBar.IsOpen =
      State.Request.WebSite.Length > 0
      || State.Request.VirtualPath.Length > 0
      || State.Request.InstallDirectory.Length > 0
      || State.Request.Options.Count > 0;

    WebSiteText.Text = webSite;
    VirtualPathText.Text = $"/{virtualPath}";
    InstallDirectoryText.Text = installDirectory;

    CanGoNext = true;
  }

  public override Task<bool> OnNextAsync() {
    State.Express = ExpressRadio.IsChecked == true;
    return Task.FromResult(true);
  }
}
