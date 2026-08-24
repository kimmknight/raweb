using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using RAWeb.Server.Installer.Setup;
using RAWeb.Server.Installer.Wizard;

namespace RAWeb.Server.Installer.Wizard.Controls;

/// <summary>
/// Full-page log view a source build is drilled into, matching <see cref="Pages.InstallPage"/>'s log
/// (ANSI-colored, wrapped, selectable/copyable entries, plus a "Copy Log" button).
/// </summary>
public partial class BuildLogView : UserControl {
  public BuildLogView() => InitializeComponent();

  public void Clear() => LogBox.Document.Blocks.Clear();

  public void AddEntry(LogEntry entry) {
    var paragraph = AnsiTextRenderer.RenderParagraph(entry.Message, AnsiTextRenderer.BrushFor(entry.Severity), AnsiTextRenderer.WeightFor(entry.Severity));
    LogBox.Document.Blocks.Add(paragraph);
    LogBox.ScrollToEnd();
  }

  private void OnCopyLog(object sender, RoutedEventArgs e) {
    var text = new TextRange(LogBox.Document.ContentStart, LogBox.Document.ContentEnd).Text;
    if (string.IsNullOrWhiteSpace(text)) {
      return;
    }

    try {
      Clipboard.SetText(text);
    }
    catch (Exception) { }
  }
}
