using System.Windows;
using System.Windows.Controls;
using RAWeb.Server.Installer.Setup;

namespace RAWeb.Server.Installer.Wizard.Controls;

/// <summary>
/// Full-page log view a source build matching <see cref="Pages.InstallPage"/>'s log list
/// </summary>
public partial class BuildLogView : UserControl {
  public BuildLogView() => InitializeComponent();

  public void Clear() => LogList.Items.Clear();

  public void AddEntry(LogEntry entry) {
    var item = AnsiTextRenderer.CreateLogItem(entry);
    LogList.Items.Add(item);
    LogList.ScrollIntoView(item);
  }

  private void OnCopyLog(object sender, RoutedEventArgs e) {
    var text = string.Join(
      Environment.NewLine,
      LogList.Items.Cast<ListBoxItem>().Select(item => (item.Tag as string) ?? item.Content?.ToString() ?? ""));

    if (text.Length == 0) {
      return;
    }

    try {
      Clipboard.SetText(text);
    }
    catch (Exception) { }
  }
}
