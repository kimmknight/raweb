using System.IO;
using System.Windows;
using System.Windows.Controls;
using iNKORE.UI.WPF.Modern.Controls;
using RAWeb.Server.Installer.Setup;

namespace RAWeb.Server.Installer.Wizard.Pages;

/// <summary>
/// Renders one control per option declared in setup.json.
/// This means that a release can change the options is supports
/// without the installer needing to be changed.
/// </summary>
public partial class OptionsPage : WizardPage {
  private readonly List<OptionBinding> _bindings = [];
  private bool _built;

  public OptionsPage() => InitializeComponent();

  public override string Title => "Configure additional options";

  public override string Description => "Additional options are available after installation, but these options can only be set during installation.";

  public override string NextText => "Install";

  // when a setup.ps1 is used, this page is unnecessary.
  public override bool ShouldSkip() => State.Strategy.RequiresExternalHandoff();

  public override void OnEnter(WizardNavigationDirection direction) {
    if (_built) {
      return;
    }
    _built = true;

    // fill each unchanged option with its default value
    var existingAppSettings = FindExistingAppSettingsPath();
    State.Request.ApplyDefaults(State.Manifest!, existingAppSettings);
    CarriedForwardBar.IsOpen = existingAppSettings is not null;

    var options = State.Manifest!.Options;

    foreach (var option in options.Where(candidate => !candidate.Advanced)) {
      OptionList.Children.Add(BuildCard(option));
    }

    var advanced = options.Where(candidate => candidate.Advanced).ToArray();
    if (advanced.Length > 0) {
      AdvancedExpander.Visibility = Visibility.Visible;
      foreach (var option in advanced) {
        AdvancedList.Children.Add(BuildCard(option));
      }
    }

    EmptyText.Visibility = options.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    CanGoNext = true;
  }

  private string? FindExistingAppSettingsPath() {
    if (!State.System!.IsIisInstalled) {
      return null;
    }

    var existing = State.Iis.GetApplicationPhysicalPath(State.Request.WebSite, State.Request.VirtualPath);
    if (existing is not { Length: > 0 }) {
      return null;
    }

    var path = Path.Combine(
      existing,
      State.Manifest!.Layout.AppSettingsFile.Replace('/', Path.DirectorySeparatorChar)
    );

    return File.Exists(path) ? path : null;
  }

  private SettingsCard BuildCard(SetupManifest.SetupOption option) {
    var card = new SettingsCard {
      Header = option.Label,
      Description = option.Description ?? "",
      Margin = new Thickness(0, 0, 0, 8),
    };

    var current = State.Request.GetOption(option.Id) ?? option.DefaultValue;

    if (option.IsBoolean) {
      var toggle = new ToggleSwitch {
        IsOn = string.Equals(current, "true", StringComparison.OrdinalIgnoreCase),
      };
      card.Content = toggle;
      _bindings.Add(new OptionBinding(option, () => toggle.IsOn ? "true" : "false"));
      return card;
    }

    if (option.Choices.Count > 0) {
      var combo = new ComboBox { MinWidth = 260 };
      foreach (var choice in option.Choices) {
        combo.Items.Add(choice.ToComboboxChoice());
      }

      combo.SelectedItem = combo.Items.Cast<SetupOptionComboboxChoice>()
        .FirstOrDefault(row => string.Equals(row.Choice.Value, current, StringComparison.OrdinalIgnoreCase))
        ?? combo.Items.Cast<SetupOptionComboboxChoice>().FirstOrDefault();

      card.Content = combo;
      _bindings.Add(new OptionBinding(option,
        () => (combo.SelectedItem as SetupOptionComboboxChoice)?.Choice.Value ?? option.DefaultValue));
      return card;
    }

    var textBox = new TextBox { Text = current, MinWidth = 260 };
    card.Content = textBox;
    _bindings.Add(new OptionBinding(option, () => textBox.Text));
    return card;
  }

  public override Task<bool> OnNextAsync() {
    foreach (var binding in _bindings) {
      State.Request.Options[binding.Option.Id] = binding.Read();
    }

    var errors = State.Request.Validate(State.Manifest!);
    if (errors.Count > 0) {
      System.Windows.MessageBox.Show(
        Window.GetWindow(this),
        string.Join(Environment.NewLine, errors),
        "RAWeb Installer",
        MessageBoxButton.OK,
        MessageBoxImage.Warning);
      return Task.FromResult(false);
    }

    return Task.FromResult(true);
  }

  private sealed record OptionBinding(SetupManifest.SetupOption Option, Func<string> Read);

  /// <summary>
  /// Wraps a SetupOptionChoice with a custom ToString method that provides
  /// a user-friendly label for a combobox.
  /// </summary>
  internal sealed class SetupOptionComboboxChoice(SetupManifest.SetupOptionChoice choice) {
    public SetupManifest.SetupOptionChoice Choice { get; } = choice;

    public override string ToString() =>
      Choice.Recommended ? $"{Choice.Label}  (recommended)" : Choice.Label;
  }
}

internal static class SetupOptionChoiceExtensions {
  public static OptionsPage.SetupOptionComboboxChoice ToComboboxChoice(this SetupManifest.SetupOptionChoice choice) =>
    new(choice);
}
