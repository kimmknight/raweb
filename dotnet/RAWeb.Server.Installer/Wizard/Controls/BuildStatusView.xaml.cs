using System.Windows;
using System.Windows.Controls;

namespace RAWeb.Server.Installer.Wizard.Controls;

/// <summary>
/// The "downloading/extracting/checking-prerequisites" view of <see cref="Pages.PreparePage"/>
/// </summary>
public partial class BuildStatusView : UserControl {
  public BuildStatusView() => InitializeComponent();

  public void SetStatus(string status, string detail) {
    StatusText.Text = status;
    DetailText.Text = detail;
    DetailText.Visibility = detail.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
  }

  public void SetProgress(double? percent) {
    if (percent is { } value) {
      Progress.IsIndeterminate = false;
      Progress.Value = value;
    }
    else {
      Progress.IsIndeterminate = true;
    }
  }

  public void ShowError(string title, string message) {
    ErrorBar.Title = title;
    ErrorBar.Message = message;
    ErrorBar.IsOpen = true;
  }

  public void ClearError() => ErrorBar.IsOpen = false;
}
