using System.Windows;
using iNKORE.UI.WPF.Modern.Media.Animation;
using RAWeb.Server.Installer.Setup;
using RAWeb.Server.Installer.Wizard;
using RAWeb.Server.Installer.Wizard.Pages;

namespace RAWeb.Server.Installer;

public partial class MainWindow : Window {
  private readonly WizardState _state = new();
  private readonly List<WizardPage> _pages;
  private int _currentIndex = -1;
  private bool _closeConfirmed;

  public MainWindow(StartupOptions options) {
    InitializeComponent();

    // if the installer was launched with a resume file, load it into the wizard\
    // state so that the pages can pick up where they left off.
    if (options.IsResumedAfterElevation) {
      _state.RestoreFrom(HandoffState.Load(options.ResumeFilePath!));
    }
    // otherwise, apply command line arguments to the state
    else {
      if (options.WebSite is { Length: > 0 } webSite) {
        _state.Request.WebSite = webSite;
      }
      if (options.VirtualPath is { Length: > 0 } virtualPath) {
        _state.Request.VirtualPath = virtualPath;
      }
      if (options.InstallDirectory is { Length: > 0 } installDirectory) {
        _state.Request.InstallDirectory = installDirectory;
      }
      foreach (var option in options.Options) {
        _state.Request.Options[option.Key] = option.Value;
      }

      _state.Express = options.Express;
      _state.Overwrite = options.Overwrite;
      _state.AutoClose = options.AutoClose;
      _state.NoWelcome = options.NoWelcome;
    }

    _pages =
    [
      new WelcomePage(),
      new VersionPage(),
      new PreparePage(),
      new LegacySetupPage(),
      new IisPage(),
      new LocationPage(),
      new OptionsPage(),
      new InstallPage(),
    ];

    foreach (var page in _pages) {
      page.Attach(_state);
      page.NavigationStateChanged += UpdateFooter;
      page.RequestNext += OnPageRequestedNext;
    }

    Loaded += (_, _) => GoTo(
      Math.Max(0, FindNext(-1)),
      WizardNavigationDirection.Forward
    );
    Closing += OnClosing;
  }

  private WizardPage Current => _pages[_currentIndex];

  private void GoTo(int index, WizardNavigationDirection direction) {
    _currentIndex = index;
    var page = Current;

    var transition = new SlideNavigationTransitionInfo {
      Effect = direction == WizardNavigationDirection.Forward
        ? SlideNavigationTransitionEffect.FromRight
        : SlideNavigationTransitionEffect.FromLeft,
    };
    PageHost.Navigate(page, transition);

    page.OnEnter(direction);
    UpdateFooter();
  }

  /// <summary>
  /// Finds the next page that applies, skipping any that opted out.
  /// </summary>
  private int FindNext(int from) {
    for (var i = from + 1; i < _pages.Count; i++) {
      if (!_pages[i].ShouldSkip()) {
        return i;
      }
    }
    return -1;
  }

  private int FindPrevious(int from) {
    for (var i = from - 1; i >= 0; i--) {
      if (!_pages[i].ShouldSkip()) {
        return i;
      }
    }
    return -1;
  }

  private async void OnNext(object sender, RoutedEventArgs e) {
    NextButton.IsEnabled = false;
    await AdvanceCurrentAsync();
  }

  // Pages that finish their own work (e.g. a completed download) raise this to act exactly as if
  // the user had pressed Next themselves, running the same validation and side effects.
  private void OnPageRequestedNext() => Dispatcher.BeginInvoke(new Action(() => _ = AdvanceCurrentAsync()));

  private async Task AdvanceCurrentAsync() {
    try {
      if (!await Current.OnNextAsync()) {
        return;
      }
    }
    finally {
      UpdateFooter();
    }

    Advance();
  }

  private void Advance() {
    var next = FindNext(_currentIndex);
    if (next < 0) {
      Close();
      return;
    }

    GoTo(next, WizardNavigationDirection.Forward);
  }

  private void OnBack(object sender, RoutedEventArgs e) {
    var previous = FindPrevious(_currentIndex);
    if (previous >= 0) {
      GoTo(previous, WizardNavigationDirection.Backward);
    }
  }

  private async void OnCancel(object sender, RoutedEventArgs e) {
    if (!await Current.OnCancelRequested()) {
      Close();
    }
  }

  private void OnSecondaryAction(object sender, RoutedEventArgs e) => Current.OnSecondaryAction();

  private void UpdateFooter() {
    var page = Current;

    TitleText.Text = page.Title;
    DescriptionText.Text = page.Description ?? "";
    DescriptionText.Visibility = string.IsNullOrEmpty(page.Description) ? Visibility.Collapsed : Visibility.Visible;

    NextButton.Content = page.NextText;
    NextButton.IsEnabled = page.CanGoNext;
    BackButton.Visibility = page.ShowBack && FindPrevious(_currentIndex) >= 0 ? Visibility.Visible : Visibility.Collapsed;
    BackButton.IsEnabled = !page.IsBusy;
    CancelButton.Visibility = page.ShowCancel ? Visibility.Visible : Visibility.Collapsed;
    SecondaryActionButton.Content = page.SecondaryActionText;
    SecondaryActionButton.Visibility = page.ShowSecondaryAction ? Visibility.Visible : Visibility.Collapsed;

    var applicable = _pages.Where(candidate => !candidate.ShouldSkip()).ToList();
    var position = applicable.IndexOf(page);
    StepText.Text = position >= 0 ? $"Step {position + 1} of {applicable.Count}" : "";
  }

  private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e) {
    if (Current.IsBusy && !_closeConfirmed) {
      e.Cancel = true;
      _ = ConfirmCloseAsync();
      return;
    }

    _state.CleanUpScratch();
  }

  private async Task ConfirmCloseAsync() {
    var confirmed = await DialogHelpers.ShowConfirmAsync(
      this,
      "Installation is still in progress. Closing now may leave RAWeb partially installed. Close anyway?"
    );

    if (confirmed) {
      _closeConfirmed = true;
      Close();
    }
  }
}
