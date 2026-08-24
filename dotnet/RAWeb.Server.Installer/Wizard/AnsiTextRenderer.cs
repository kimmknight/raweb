using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using RAWeb.Server.Installer.Setup;

namespace RAWeb.Server.Installer.Wizard;

/// <summary>
/// Turns a log line that may carry ANSI/VT100 SGR color codes (emitted by pnpm, vite, dotnet, etc.
/// during a source build) into WPF <see cref="Run"/>s with matching colors.
/// </summary>
public static class AnsiTextRenderer {
  // matches any escape sequence. Only "m" (SGR) sequences carry meaning here;
  // everything else (cursor moves, line clears) end up being dropped since there is no cursor.
  private static readonly Regex s_escapeSequence = new(@"\x1B\[([0-9;]*)([A-Za-z])", RegexOptions.Compiled);

  public static Brush BrushFor(LogSeverity severity) => severity switch {
    LogSeverity.Step => Brushes.DodgerBlue,
    LogSeverity.Success => Brushes.SeaGreen,
    LogSeverity.Warning => Brushes.Goldenrod,
    LogSeverity.Error => Brushes.IndianRed,
    _ => (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"],
  };

  public static FontWeight WeightFor(LogSeverity severity) =>
    severity == LogSeverity.Step ? FontWeights.SemiBold : FontWeights.Normal;

  /// <summary>
  /// Builds a wrapped, margin-free paragraph for one log line, for use as a block in a RichTextBox's
  /// FlowDocument. ANSI codes are rendered as colored Runs, falling back to <paramref name="defaultBrush"/>.
  /// </summary>
  public static Paragraph RenderParagraph(string text, Brush defaultBrush, FontWeight defaultWeight) {
    var paragraph = new Paragraph {
      Margin = new Thickness(0),
      TextAlignment = TextAlignment.Left,
    };

    AppendRuns(paragraph.Inlines, text, defaultBrush, defaultWeight);
    return paragraph;
  }

  /// <summary>
  /// Removes ANSI escape sequences entirely, for plain-text uses like a chunk of process output that
  /// is echoed elsewhere without going through <see cref="RenderParagraph"/>.
  /// </summary>
  public static string StripAnsi(string text) => s_escapeSequence.Replace(text, "");

  private static void AppendRuns(InlineCollection inlines, string text, Brush defaultBrush, FontWeight defaultWeight) {
    var brush = defaultBrush;
    var weight = defaultWeight;
    var italic = false;
    var lastIndex = 0;

    foreach (Match match in s_escapeSequence.Matches(text)) {
      if (match.Index > lastIndex) {
        AddRun(inlines, text[lastIndex..match.Index], brush, weight, italic);
      }

      if (match.Groups[2].Value == "m") {
        ApplySgr(match.Groups[1].Value, ref brush, ref weight, ref italic, defaultBrush, defaultWeight);
      }

      lastIndex = match.Index + match.Length;
    }

    if (lastIndex < text.Length) {
      AddRun(inlines, text[lastIndex..], brush, weight, italic);
    }
  }

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

  /// <summary>
  /// Applies SGR codes to the current brush/weight/italic state. The default brush/weight are used for reset codes.
  /// <br/><br/>
  /// SGR codes are documented here: https://en.wikipedia.org/wiki/ANSI_escape_code#SGR
  /// </summary>
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
