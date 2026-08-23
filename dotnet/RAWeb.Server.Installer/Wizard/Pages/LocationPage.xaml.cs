using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RAWeb.Server.Installer.Setup;

namespace RAWeb.Server.Installer.Wizard.Pages;

public partial class LocationPage : WizardPage {
  private bool _isUpgrade;

  public LocationPage() => InitializeComponent();

  public override string Title => "Choose install directory";

  public override string Description => "Program files are kept separately from the IIS site.";

  // when a setup.ps1 is used, this page is unnecessary.
  public override bool ShouldSkip() => State.Strategy.RequiresExternalHandoff();

  public override void OnEnter(WizardNavigationDirection direction) {
    var site = State.Request.WebSite;
    var virtualPath = State.Request.VirtualPath.Trim('/', '\\');

    // the default usually follows the site and virtual path chosen on the previous page,
    // but if the user is upgrading, it is the current installation directory
    var defaultDirectory = DefaultDirectory(site, virtualPath);

    // when upgrading, we should an additional option to simply keep the existing install directory
    _isUpgrade = InstallPlanner.IsUpgrade(State.Manifest!, State.System!, State.Iis, site, virtualPath);
    UpgradePanel.Visibility = _isUpgrade ? Visibility.Visible : Visibility.Collapsed;
    if (_isUpgrade) {
      ExistingLocationText.Text = defaultDirectory;
      KeepLocationRadio.IsChecked = true;
    }

    InstallDirBox.Text = defaultDirectory;
    UpdateCustomLocationVisibility();
    Validate();
  }

  private void OnLocationChoiceChanged(object sender, RoutedEventArgs e) {
    if (!IsInitialized) {
      return;
    }

    UpdateCustomLocationVisibility();

    // switching back to "keep" mode discards anything typed while "choose a different location" was active.
    if (KeepLocationRadio.IsChecked == true) {
      InstallDirBox.Text = DefaultDirectory(State.Request.WebSite, State.Request.VirtualPath.Trim('/', '\\'));
    }

    Validate();
  }

  private void OnKeepLocationDoubleClick(object sender, MouseButtonEventArgs e) {
    if (KeepLocationRadio.IsChecked == true && CanGoNext) {
      RaiseRequestNext();
    }
  }

  private void UpdateCustomLocationVisibility() =>
    CustomLocationPanel.Visibility = !_isUpgrade || ChooseLocationRadio.IsChecked == true
      ? Visibility.Visible
      : Visibility.Collapsed;

  /// <summary>
  /// Returns the default installation directory for the given site and virtual path.
  /// For existing installations, the current path is used, even if that is not the
  /// standard default for the site and virtual path.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="virtualPath"></param>
  /// <returns></returns>
  private string DefaultDirectory(string site, string virtualPath) {
    var existing = State.System!.IsIisInstalled
      ? State.Iis.GetApplicationPhysicalPath(site, virtualPath)
      : null;

    return InstallPlanner.ResolveInstallDirectory(State.Manifest!, site, virtualPath, existing);
  }

  private void OnResetToDefault(object sender, RoutedEventArgs e) => InstallDirBox.Text = DefaultDirectory(State.Request.WebSite, State.Request.VirtualPath.Trim('/', '\\'));

  private void OnPathChanged(object sender, TextChangedEventArgs e) => Validate();

  private void OnBrowse(object sender, RoutedEventArgs e) {
    using var dialog = new System.Windows.Forms.FolderBrowserDialog {
      Description = "Select the installation directory",
      ShowNewFolderButton = true,
      SelectedPath = Directory.Exists(InstallDirBox.Text) ? InstallDirBox.Text : "",
    };

    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
      InstallDirBox.Text = dialog.SelectedPath;
    }
  }

  private void Validate() {
    if (!IsInitialized) {
      return;
    }

    var path = InstallDirBox.Text.Trim().Trim('"');
    WarningBar.IsOpen = false;
    CanGoNext = false;

    if (path.Length == 0) {
      VersionedPathPreview.Text = "";
      return;
    }

    if (path.Length == 0 || path.IndexOfAny(Path.GetInvalidPathChars()) >= 0) {
      ShowWarning(
        "Error",
        "That is not a valid path.",
        "Check for typos or invalid characters."
      );
      VersionedPathPreview.Text = "";
      return;
    }

    if (path == "C:\\" || path == "C:\\\\") {
      ShowWarning(
        "Error",
        "Installing to the root of a system drive is not allowed.",
        "Choose a subfolder, such as C:\\Program Files\\RAWeb."
      );
      VersionedPathPreview.Text = "";
      return;
    }

    if (FileOperations.IsUnderInetpub(path)) {
      ShowWarning(
        "Error",
        @"Installing inside C:\inetpub is not allowed.",
        @"Choose somewhere else, such as C:\Program Files\RAWeb."
      );
      VersionedPathPreview.Text = "";
      return;
    }

    if (!IsRooted(path)) {
      ShowWarning(
        "Error",
        "That is not an absolute path.",
        "Include the drive letter, for example C:\\."
      );
      VersionedPathPreview.Text = "";
      return;
    }

    CanGoNext = true;
    VersionedPathPreview.Text = Path.Combine(path, $"{State.DisplayVersion}__{DateTime.UtcNow:yyyyMMdd-HHmmss}");

    if (Directory.Exists(path) && Directory.EnumerateFileSystemEntries(path).Any()) {
      ShowWarning(
        "Warning",
        "This folder is not empty.",
        "Existing files may be overwritten during installation."
      );
    }
  }

  private void ShowWarning(string severity, string title, string message) {
    WarningBar.Severity = severity == "Error"
      ? iNKORE.UI.WPF.Modern.Controls.InfoBarSeverity.Error
      : iNKORE.UI.WPF.Modern.Controls.InfoBarSeverity.Warning;
    WarningBar.Title = title;
    WarningBar.Message = message;
    WarningBar.IsOpen = true;
  }

  private static bool IsRooted(string path) {
    try {
      return Path.IsPathRooted(path);
    }
    catch (ArgumentException) {
      return false;
    }
  }

  public override Task<bool> OnNextAsync() {
    State.Request.InstallDirectory = InstallDirBox.Text;
    return Task.FromResult(true);
  }
}
