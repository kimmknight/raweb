using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using RAWeb.Server.Installer.Setup;

namespace RAWeb.Server.Installer.Wizard;

/// <summary>
/// Turns a log line that may carry ANSI/VT100 SGR color codes (emitted by pnpm, vite, dotnet, etc.
/// during a source build) into a WPF TextBlock with matching colored Runs, since the log is a plain
/// ListBox rather than a real terminal.
/// </summary>
public static class AnsiTextRenderer {
  // Any CSI escape sequence (ESC '[' params final-byte). Only "m" (SGR) sequences carry meaning here;
  // everything else (cursor moves, line clears) is dropped since a static list view has no cursor.
  private static readonly Regex s_escapeSequence = new(@"\x1B\[([0-9;]*)([A-Za-z])", RegexOptions.Compiled);

  /// <summary>
  /// Builds the log list item for one entry: a colored TextBlock as the content (ANSI codes rendered,
  /// falling back to the severity color for the rest of the line) and the raw message stashed in the
  /// item's Tag so "Copy Log" can recover plain text.
  /// </summary>
  public static ListBoxItem CreateLogItem(LogEntry entry) {
    var weight = entry.Severity == LogSeverity.Step ? FontWeights.SemiBold : FontWeights.Normal;

    return new ListBoxItem {
      Content = Render(entry.Message, BrushFor(entry.Severity), weight),
      Tag = entry.Message,
      Padding = new Thickness(10, 1, 10, 1),
      IsHitTestVisible = false,
    };
  }

  public static Brush BrushFor(LogSeverity severity) => severity switch {
    LogSeverity.Step => Brushes.DodgerBlue,
    LogSeverity.Success => Brushes.SeaGreen,
    LogSeverity.Warning => Brushes.Goldenrod,
    LogSeverity.Error => Brushes.IndianRed,
    _ => (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"],
  };

  public static TextBlock Render(string text, Brush defaultBrush, FontWeight defaultWeight) {
    var block = new TextBlock { TextWrapping = TextWrapping.NoWrap };

    var brush = defaultBrush;
    var weight = defaultWeight;
    var italic = false;
    var lastIndex = 0;

    foreach (Match match in s_escapeSequence.Matches(text)) {
      if (match.Index > lastIndex) {
        AddRun(block.Inlines, text[lastIndex..match.Index], brush, weight, italic);
      }

      if (match.Groups[2].Value == "m") {
        ApplySgr(match.Groups[1].Value, ref brush, ref weight, ref italic, defaultBrush, defaultWeight);
      }

      lastIndex = match.Index + match.Length;
    }

    if (lastIndex < text.Length) {
      AddRun(block.Inlines, text[lastIndex..], brush, weight, italic);
    }

    return block;
  }

  /// <summary>
  /// Removes ANSI escape sequences entirely, for plain-text uses like the "Copy Log" button.
  /// </summary>
  public static string StripAnsi(string text) => s_escapeSequence.Replace(text, "");

  private static void AddRun(InlineCollection inlines, string text, Brush brush, FontWeight weight, bool italic) {
    if (text.Length == 0) {
      return;
    }

    inlines.Add(new Run(text) {
      Foreground = brush,
      FontWeight = weight,
      FontStyle = italic ? FontStyles.Italic : FontStyles.Normal,
    });
  }

  private static void ApplySgr(string codes, ref Brush brush, ref FontWeight weight, ref bool italic, Brush defaultBrush, FontWeight defaultWeight) {
    var parts = codes.Length == 0 ? ["0"] : codes.Split(';');

    foreach (var part in parts) {
      if (!int.TryParse(part, out var code)) {
        continue;
      }

      switch (code) {
        case 0:
          brush = defaultBrush;
          weight = defaultWeight;
          italic = false;
          break;
        case 1: weight = FontWeights.Bold; break;
        case 3: italic = true; break;
        case 22: weight = defaultWeight; break;
        case 23: italic = false; break;
        case 39: brush = defaultBrush; break;
        case 30: brush = Brushes.Black; break;
        case 31 or 91: brush = Brushes.IndianRed; break;
        case 32 or 92: brush = Brushes.SeaGreen; break;
        case 33 or 93: brush = Brushes.Goldenrod; break;
        case 34 or 94: brush = Brushes.DodgerBlue; break;
        case 35 or 95: brush = Brushes.MediumOrchid; break;
        case 36 or 96: brush = Brushes.CadetBlue; break;
        case 37 or 97: brush = Brushes.Gainsboro; break;
        case 90: brush = Brushes.Gray; break;
      }
    }
  }
}
