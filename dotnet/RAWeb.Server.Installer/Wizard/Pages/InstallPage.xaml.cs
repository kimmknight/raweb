using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using iNKORE.UI.WPF.Modern.Controls;
using RAWeb.Server.Installer.Setup;

namespace RAWeb.Server.Installer.Wizard.Pages;

public partial class InstallPage : WizardPage {
  private CancellationTokenSource? _cancellation;
  private bool _isRunning;
  private bool _finished;

  public InstallPage() {
    InitializeComponent();
    CanGoNext = false;
  }

  public override string Title {
    get {
      if (!_finished) {
        return "Installing RAWeb";
      }

      if (State.Result?.Succeeded == true) {
        return "RAWeb Installed";
      }

      return _cancellation?.IsCancellationRequested == true ? "Installation Cancelled" : "Installation Failed";
    }
  }

  public override string Description => _finished ? null! : "This usually takes a minute or two.";

  public override string NextText => _finished ? "Finish" : "Install";

  public override bool ShowBack => false;

  public override bool ShowCancel => !_finished;

  public override bool IsBusy => _isRunning;

  public override bool ShowSecondaryAction => _finished && State.Result?.Succeeded == true;

  public override string SecondaryActionText => "Open RemoteApps & Devices";

  public override void OnSecondaryAction() => Process.Start(new ProcessStartInfo(State.Result?.WebInterfaceUrl) { UseShellExecute = true });

  public override async Task<bool> OnCancelRequested() {
    if (!_isRunning) {
      return false;
    }

    var confirmed = await DialogHelpers.ShowConfirmAsync(
      Window.GetWindow(this),
      "This will stop the installation and roll back any changes already made. Continue?"
    );

    if (confirmed) {
      _cancellation?.Cancel();
    }

    // prevents the installation wizard from closing during the actual installation process
    return true;
  }

  // when a setup.ps1 is used, this page is unnecessary.
  public override bool ShouldSkip() => State.Strategy.RequiresExternalHandoff();

  public override void OnEnter(WizardNavigationDirection direction) {
    if (_isRunning || _finished) {
      return;
    }

    State.Log.Logged += OnLogged;
    State.Log.ProgressChanged += OnProgressChanged;

    _ = RunAsync();
  }

  public override Task<bool> OnNextAsync() {
    // once finished, Next reads "Finish" and just closes the wizard.
    if (_finished) {
      return Task.FromResult(true);
    }

    ResultBar.IsOpen = false;
    RaiseNavigationStateChanged();

    _ = RunAsync();

    // disable the next button until the installation is complete
    return Task.FromResult(false);
  }

  private async Task RunAsync() {
    _isRunning = true;
    CanGoNext = false;
    RaiseNavigationStateChanged();

    _cancellation = new CancellationTokenSource();

    try {
      State.Plan = InstallPlanner.Create(State.Manifest!, State.System!, State.Request, State.Iis);

      foreach (var warning in State.Plan.Warnings) {
        State.Log.Warning($"{warning.Title}: {warning.Detail}");
      }

      if (!await ConfirmBlockingWarningsAsync(State.Plan)) {
        State.Log.Warning("Installation cancelled before any changes were made.");
        ShowResult(false, "Cancelled", "No changes were made to this computer.", rolledBack: false);
        return;
      }

      var engine = new InstallEngine(State.Log);
      var result = await Task.Run(() => engine.Run(State.Plan, _cancellation.Token));
      State.Result = result;

      if (result.Succeeded) {
        ShowSuccess(result);
      }
      else {
        ShowResult(false, result.RestartRequired ? "Restart required" : "Installation failed",
          result.FailureMessage ?? "", result.RolledBack);
      }
    }
    catch (Exception exception) {
      State.Log.Error(exception.Message);
      ShowResult(false, "Installation failed", exception.Message, rolledBack: false);
    }
    finally {
      _isRunning = false;
      _finished = true;
      CanGoNext = true;
      RaiseNavigationStateChanged();
    }
  }

  /// <summary>
  /// Require an explicit confirmation for any warning that does not default to proceeding.
  /// </summary>
  private async Task<bool> ConfirmBlockingWarningsAsync(InstallPlan plan) {
    var blocking = plan.Warnings.Where(warning => !warning.DefaultsToProceed).ToArray();
    if (blocking.Length == 0) {
      return true;
    }

    var body = new StackPanel();
    foreach (var warning in blocking) {
      body.Children.Add(new TextBlock {
        Text = warning.Title,
        FontWeight = FontWeights.SemiBold,
        TextWrapping = TextWrapping.Wrap,
      });
      body.Children.Add(new TextBlock {
        Text = warning.Detail,
        TextWrapping = TextWrapping.Wrap,
        Opacity = 0.75,
        Margin = new Thickness(0, 2, 0, 12),
      });
    }

    var dialog = new ContentDialog {
      Title = "Continue anyway?",
      Content = body,
      PrimaryButtonText = "Continue",
      CloseButtonText = "Cancel",
      DefaultButton = ContentDialogButton.Close,
      Owner = Window.GetWindow(this),
    };

    return await dialog.ShowAsync() == ContentDialogResult.Primary;
  }

  private void ShowSuccess(InstallResult result) {
    Progress.Value = 100;
    StepText.Text = "Installation complete";
    PercentText.Text = "";

    LogList.Items.Add(new ListBoxItem {
      Content = "Installation complete",
      Foreground = Brushes.SeaGreen,
      FontWeight = FontWeights.SemiBold,
      Padding = new Thickness(10, 1, 10, 1),
      IsHitTestVisible = false,
    });
    LogList.Items.Add(new ListBoxItem {
      Content = $"RAWeb {State.DisplayVersion} installed",
      Foreground = Brushes.SeaGreen,
      FontWeight = FontWeights.SemiBold,
      Padding = new Thickness(10, 1, 10, 1),
      IsHitTestVisible = false,
    });
    LogList.Items.Add(new ListBoxItem {
      Content = $"Web interface: {result.WebInterfaceUrl}",
      Foreground = Brushes.SeaGreen,
      FontWeight = FontWeights.SemiBold,
      Padding = new Thickness(10, 1, 10, 1),
      IsHitTestVisible = false,
    });
    LogList.Items.Add(new ListBoxItem {
      Content = $"Workspace feed: {result.WorkspaceUrl}",
      Foreground = Brushes.SeaGreen,
      FontWeight = FontWeights.SemiBold,
      Padding = new Thickness(10, 1, 10, 1),
      IsHitTestVisible = false,
    });
    LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);

    ResultBar.Severity = InfoBarSeverity.Success;
    ResultBar.Title = $"RAWeb {State.DisplayVersion} installed";
    ResultBar.IsOpen = true;
  }

  private void OnCopyLog(object sender, RoutedEventArgs e) {
    var text = string.Join(
      Environment.NewLine,
      LogList.Items.Cast<ListBoxItem>().Select(item => item.Content?.ToString() ?? ""));

    if (text.Length == 0) {
      return;
    }

    try {
      Clipboard.SetText(text);
    }
    catch (Exception) { }
  }

  private void ShowResult(bool success, string title, string message, bool rolledBack) {
    if (rolledBack) {
      Progress.IsIndeterminate = false;
      Progress.Value = 100;
      PercentText.Text = "";
      StepText.Text = "Rollback complete";

      LogList.Items.Add(new ListBoxItem {
        Content = "Rollback complete.",
        Foreground = Brushes.Goldenrod,
        Padding = new Thickness(10, 1, 10, 1),
        IsHitTestVisible = false,
      });
      LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
    }

    ResultBar.Severity = success ? InfoBarSeverity.Success : InfoBarSeverity.Error;
    ResultBar.Title = title;
    ResultBar.Message = message;
    ResultBar.IsOpen = true;
  }

  private void OnLogged(LogEntry entry) => Dispatcher.BeginInvoke(new Action(() => {
    LogList.Items.Add(new ListBoxItem {
      Content = entry.Message,
      Foreground = BrushFor(entry.Severity),
      FontWeight = entry.Severity == LogSeverity.Step ? FontWeights.SemiBold : FontWeights.Normal,
      Padding = new Thickness(10, 1, 10, 1),
      IsHitTestVisible = false,
    });

    LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
  }));

  private void OnProgressChanged(ProgressUpdate update) => Dispatcher.BeginInvoke(new Action(() => {
    StepText.Text = update.CurrentStep;

    if (update.Percent < 0) {
      Progress.IsIndeterminate = true;
      PercentText.Text = "";
      return;
    }

    Progress.IsIndeterminate = false;
    Progress.Value = update.Percent;
    PercentText.Text = $"{update.CompletedSteps} of {update.TotalSteps}";
  }));

  private static Brush BrushFor(LogSeverity severity) => severity switch {
    LogSeverity.Step => Brushes.DodgerBlue,
    LogSeverity.Success => Brushes.SeaGreen,
    LogSeverity.Warning => Brushes.Goldenrod,
    LogSeverity.Error => Brushes.IndianRed,
    _ => (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"],
  };
}
