using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using RAWeb.Server.Installer.Setup;

namespace RAWeb.Server.Installer.Wizard.Pages;

public partial class VersionPage : WizardPage {
  private bool _releasesLoaded;
  private bool _pinnedResolved;
  private bool _pinnedFailed;
  private IReadOnlyList<ReleaseInfo> _releases = [];
  private IReadOnlyList<ReleaseInfo> _unreleasedBranches = [];

  public VersionPage() => InitializeComponent();

  /// <summary>
  /// False once resolving the pin has failed, so Title/Description fall back to describing the
  /// manual picker that PinnedErrorBar tells the user to use instead.
  /// </summary>
  private bool IsPinned => !_pinnedFailed && (InstallPin.ReleaseTag is { Length: > 0 } || InstallPin.SourcePath is { Length: > 0 });

  private static string PinnedLabel => InstallPin.ReleaseTag ?? Path.GetFileName(InstallPin.SourcePath!.TrimEnd(Path.DirectorySeparatorChar));

  public override string Title => IsPinned ? $"Preparing RAWeb {PinnedLabel}" : "Choose a version";

  public override string Description => IsPinned
    ? "This installer was built for a specific version of RAWeb. Please wait while we retrieve the required files."
    : "Install the latest release, an earlier one, or a build you already have on disk.";

  public override void OnEnter(WizardNavigationDirection direction) {
    if (IsPinned) {
      _ = OnEnterPinnedAsync(direction);
      return;
    }

    UpdateCanGoNext();

    if (!_releasesLoaded) {
      _ = LoadReleasesAsync();
    }
  }

  /// <summary>
  /// A pinned build has nothing for the user to choose, so this page just shows a brief loading
  /// state while it resolves what raweb-install-pin.json (see InstallPin) points to and then
  /// advances on its own.
  /// 
  /// If the pin cannot be resolved, an error message is shown and then the user is given
  /// the option to pick a version manually instead.
  /// </summary>
  private async Task OnEnterPinnedAsync(WizardNavigationDirection direction) {
    PickerPanel.Visibility = Visibility.Collapsed;
    PinnedErrorBar.IsOpen = false;

    if (_pinnedResolved) {
      if (direction == WizardNavigationDirection.Forward) {
        RaiseRequestNext();
      }
      else {
        CanGoNext = true;
      }
      return;
    }

    CanGoNext = false;
    ShowBack = false;
    RaiseNavigationStateChanged();

    // if the pin points to a local folder, resolve it directly
    if (InstallPin.SourcePath is { Length: > 0 } sourcePath) {
      ResolvePinnedSource(sourcePath);
      return;
    }

    // otherwise, check for a release tag on GitHub

    var tag = InstallPin.ReleaseTag!;
    LoadingRing.IsActive = true;

    try {
      var release = await Task.Run(() => ReleaseSource.GetReleaseByTag(tag));

      if (release.PrimaryArchive is not { } asset) {
        throw new InvalidOperationException($"Release {tag} does not have an installable archive attached.");
      }

      State.SelectedAsset = asset;
      State.LocalSourcePath = null;
      State.PreviewOwner = null;
      State.PreviewBranch = null;
      State.DisplayVersion = release.TagName;

      CompletePinned();
    }
    catch (Exception exception) {
      FailPinned(exception);
    }
  }

  private void ResolvePinnedSource(string sourcePath) {
    if (!Directory.Exists(sourcePath) && !File.Exists(sourcePath)) {
      FailPinned(new InvalidOperationException($"'{sourcePath}' does not exist."));
      return;
    }

    State.SelectedAsset = null;
    State.LocalSourcePath = sourcePath;
    State.PreviewOwner = null;
    State.PreviewBranch = null;
    State.DisplayVersion = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar));

    CompletePinned();
  }

  private void CompletePinned() {
    _pinnedResolved = true;
    LoadingRing.IsActive = false;
    RaiseNavigationStateChanged();
    RaiseRequestNext();
  }

  private void FailPinned(Exception exception) {
    _pinnedFailed = true;
    LoadingRing.IsActive = false;
    ShowBack = true;
    PinnedErrorBar.Message = $"{exception.Message} Choose a version manually below instead.";
    PinnedErrorBar.IsOpen = true;
    PickerPanel.Visibility = Visibility.Visible;

    if (!_releasesLoaded) {
      _ = LoadReleasesAsync();
    }
    UpdateCanGoNext();
    RaiseNavigationStateChanged();
  }

  private async Task LoadReleasesAsync() {
    _releasesLoaded = true;
    LoadingRing.IsActive = true;
    ReleaseList.Visibility = Visibility.Collapsed;

    try {
      _releases = await Task.Run(() => ReleaseSource.ListGitHubReleases());

      // branches without an official release yet are extra options, not a requirement, so a
      // failure here (rate limiting, no network) must not block the real release list from showing.
      _unreleasedBranches = await Task.Run(SafeListUnreleasedBranches);

      ReleaseList.Visibility = Visibility.Visible;
      RenderReleaseList();
    }
    catch (Exception exception) {
      LoadErrorBar.Message = $"{exception.Message} You can still install from a local folder or .zip file below.";
      LoadErrorBar.IsOpen = true;

      // since there are no releases to select, the user must choose a local source to continue.
      FromLocalRadio.IsChecked = true;
    }
    finally {
      LoadingRing.IsActive = false;
      UpdateCanGoNext();
    }
  }

  private static IReadOnlyList<ReleaseInfo> SafeListUnreleasedBranches() {
    try {
      return ReleaseSource.ListUnreleasedBranches();
    }
    catch (Exception) {
      return [];
    }
  }

  private void OnShowUnreleasedToggled(object sender, RoutedEventArgs e) {
    if (!IsInitialized) {
      return;
    }

    RenderReleaseList();
  }

  /// <summary>
  /// Rebuilds the visible list from the two sources already fetched, so toggling "Show unreleased
  /// versions" does not re-hit the network. Everything is kept in one chronological order rather
  /// than releases first and branches appended after.
  /// </summary>
  private void RenderReleaseList() {
    var releases = _releases.AsEnumerable();
    if (ShowUnreleasedToggle.IsOn) {
      releases = releases.Concat(_unreleasedBranches);
    }

    ReleaseList.ItemsSource = releases
      .Where(release => release.IsUnreleased || release.PrimaryArchive is not null)
      .OrderByDescending(release => release.PublishedAt)
      .Select(release => new ReleaseRow(release))
      .ToList();

    ReleaseListTransition.ReloadTransition();

    if (ReleaseList.Items.Count > 0) {
      ReleaseList.SelectedIndex = 0;
    }

    UpdateCanGoNext();
  }

  private void OnSourceChanged(object sender, RoutedEventArgs e) {
    if (!IsInitialized) {
      return;
    }

    var useGitHub = FromGitHubRadio.IsChecked == true;

    ReleaseList.IsEnabled = useGitHub;
    ReleaseList.Opacity = useGitHub ? 1 : 0.4;
    LocalPathBox.IsEnabled = !useGitHub;
    BrowseFolderButton.IsEnabled = !useGitHub;
    BrowseZipButton.IsEnabled = !useGitHub;

    UpdateCanGoNext();
  }

  private void OnReleaseSelected(object sender, SelectionChangedEventArgs e) => UpdateCanGoNext();

  private void OnReleaseDoubleClick(object sender, MouseButtonEventArgs e) {
    if (ReleaseList.SelectedItem is ReleaseRow) {
      RaiseRequestNext();
    }
  }

  private void OnLocalPathChanged(object sender, TextChangedEventArgs e) => UpdateCanGoNext();

  private void OnBrowseFolder(object sender, RoutedEventArgs e) {
    using var dialog = new System.Windows.Forms.FolderBrowserDialog {
      Description = "Select the folder containing setup.json",
      ShowNewFolderButton = false,
    };

    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
      LocalPathBox.Text = dialog.SelectedPath;
    }
  }

  private void OnBrowseZip(object sender, RoutedEventArgs e) {
    var dialog = new OpenFileDialog {
      Title = "Select a RAWeb release archive",
      Filter = "RAWeb release (*.zip)|*.zip",
    };

    if (dialog.ShowDialog(Window.GetWindow(this)) == true) {
      LocalPathBox.Text = dialog.FileName;
    }
  }

  private void UpdateCanGoNext() {
    CanGoNext = FromGitHubRadio.IsChecked == true
      ? ReleaseList.SelectedItem is ReleaseRow
      : LocalPathBox.Text.Trim().Length > 0;
  }

  public override async Task<bool> OnNextAsync() {
    if (_pinnedResolved) {
      // the version was resolved automatically based on the pin
      return true;
    }

    if (FromGitHubRadio.IsChecked == true) {
      if (ReleaseList.SelectedItem is not ReleaseRow row) {
        return false;
      }

      if (row.Release.IsUnreleased) {
        State.SelectedAsset = null;
        State.LocalSourcePath = null;
        State.PreviewOwner = row.Release.PreviewOwner;
        State.PreviewBranch = row.Release.PreviewBranch;
      }
      else {
        State.SelectedAsset = row.Release.PrimaryArchive;
        State.LocalSourcePath = null;
        State.PreviewOwner = null;
        State.PreviewBranch = null;
      }

      State.DisplayVersion = row.Release.TagName;
      return true;
    }

    var path = LocalPathBox.Text.Trim().Trim('"');
    if (!Directory.Exists(path) && !File.Exists(path)) {
      await DialogHelpers.ShowWarningAsync(Window.GetWindow(this), $"'{path}' does not exist.");
      return false;
    }

    State.SelectedAsset = null;
    State.LocalSourcePath = path;
    State.PreviewOwner = null;
    State.PreviewBranch = null;
    State.DisplayVersion = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar));
    return true;
  }

  private sealed class ReleaseRow(ReleaseInfo release) {
    public ReleaseInfo Release { get; } = release;

    public string Title => Release.Title;

    public string PublishedText => Release.PublishedAt.ToLocalTime().ToString("d MMM yyyy");

    public Visibility PrereleaseVisibility => Release.IsPrerelease ? Visibility.Visible : Visibility.Collapsed;

    public Visibility UnreleasedVisibility => Release.IsUnreleased ? Visibility.Visible : Visibility.Collapsed;

    public string AssetSummary => Release.IsUnreleased
      ? $"{Release.TagName}  ·  Uses the latest build or installs from source"
      : Release.PrimaryArchive is { } asset
        ? $"{Release.TagName}  ·  {asset.Name}  ·  {asset.SizeInBytes / 1024d / 1024d:0.0} MiB"
        : "No archive available";
  }
}
