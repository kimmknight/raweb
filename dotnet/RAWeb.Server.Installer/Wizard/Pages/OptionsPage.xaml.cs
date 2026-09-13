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
  private readonly Dictionary<string, OptionBinding> _bindingsById = new(StringComparer.OrdinalIgnoreCase);
  private readonly Dictionary<string, SettingsCard> _cardsById = new(StringComparer.OrdinalIgnoreCase);
  private readonly Dictionary<string, List<Action>> _changeListenersByOptionId = new(StringComparer.OrdinalIgnoreCase);
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

    ApplyCertificateInterlocks();
    ApplyOptionDependencies();

    EmptyText.Visibility = options.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    CanGoNext = true;

    // In express mode, we take all current values. These may be values set from the command
    // line arguments or the default values.
    if (State.Express) {
      RaiseRequestNext();
    }
  }

  /// <summary>
  /// Disables "Create a self-signed certificate" when the web site's HTTPS binding already has a
  /// certificate.
  /// </summary>
  private void ApplyCertificateInterlocks() {
    if (!_cardsById.TryGetValue("createCertificate", out var createCertificateCard)
      || !_bindingsById.TryGetValue("createCertificate", out var createCertificateBinding)) {
      return;
    }

    if (!SiteAlreadyHasCertificate()) {
      return;
    }

    createCertificateCard.IsEnabled = false;
    createCertificateBinding.ForceOff?.Invoke();
  }

  private bool SiteAlreadyHasCertificate() {
    if (!State.System!.IsIisInstalled) {
      return false;
    }

    return State.Iis
      .GetBindings(State.Request.WebSite)
      .Any(binding => binding.Protocol == "https" && binding.CertificateHash is { Length: > 0 });
  }

  /// <summary>
  /// Configures each option to be enabled only when its declared dependsOn
  /// option's current value matches. As values are changed, the enabled/disabled
  /// state is re-evaluated.
  /// </summary>
  private void ApplyOptionDependencies() {
    var siteAlreadyHasCertificate = SiteAlreadyHasCertificate();

    foreach (var option in State.Manifest!.Options) {
      if (option.DependsOn is not { } dependency) {
        continue;
      }
      if (!_cardsById.TryGetValue(option.Id, out var card) || !_bindingsById.TryGetValue(option.Id, out var binding)) {
        continue;
      }

      void Evaluate() {
        var dependedOnValue = _bindingsById.TryGetValue(dependency.Option, out var dependedOn) ? dependedOn.Read() : null;
        var satisfied = string.Equals(dependedOnValue, dependency.Value, StringComparison.OrdinalIgnoreCase);

        // we also need to allow this option if a certificate already exists, not just when a new one will be created
        if (string.Equals(option.Id, "signRdpFiles", StringComparison.OrdinalIgnoreCase)) {
          satisfied |= siteAlreadyHasCertificate;
        }

        card.IsEnabled = satisfied;
        if (!satisfied) {
          binding.ForceOff?.Invoke();
        }
      }

      Evaluate();
      RegisterChangeListener(dependency.Option, Evaluate);
    }
  }

  private void RegisterChangeListener(string optionId, Action listener) {
    if (!_changeListenersByOptionId.TryGetValue(optionId, out var listeners)) {
      listeners = [];
      _changeListenersByOptionId[optionId] = listeners;
    }
    listeners.Add(listener);
  }

  private void NotifyOptionChanged(string optionId) {
    if (!_changeListenersByOptionId.TryGetValue(optionId, out var listeners)) {
      return;
    }
    foreach (var listener in listeners.ToArray()) {
      listener();
    }
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
    _cardsById[option.Id] = card;

    var current = State.Request.GetOption(option.Id) ?? option.DefaultValue;

    if (option.IsBoolean) {
      var toggle = new ToggleSwitch {
        IsOn = string.Equals(current, "true", StringComparison.OrdinalIgnoreCase),
      };
      toggle.Toggled += (_, _) => NotifyOptionChanged(option.Id);

      RegisterBinding(new OptionBinding(option, () => toggle.IsOn ? "true" : "false", () => toggle.IsOn = false));
      card.Content = toggle;
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
      combo.SelectionChanged += (_, _) => NotifyOptionChanged(option.Id);

      RegisterBinding(new OptionBinding(option,
        () => (combo.SelectedItem as SetupOptionComboboxChoice)?.Choice.Value ?? option.DefaultValue));
      card.Content = combo;
      return card;
    }

    var textBox = new TextBox { Text = current, MinWidth = 260 };
    textBox.TextChanged += (_, _) => NotifyOptionChanged(option.Id);
    RegisterBinding(new OptionBinding(option, () => textBox.Text));
    card.Content = textBox;
    return card;
  }

  private void RegisterBinding(OptionBinding binding) {
    _bindings.Add(binding);
    _bindingsById[binding.Option.Id] = binding;
  }

  public override async Task<bool> OnNextAsync() {
    foreach (var binding in _bindings) {
      State.Request.Options[binding.Option.Id] = binding.Read();
    }

    var errors = State.Request.Validate(State.Manifest!);
    if (errors.Count > 0) {
      await DialogHelpers.ShowWarningAsync(Window.GetWindow(this), string.Join(Environment.NewLine, errors));
      return false;
    }

    return true;
  }

  /// <summary>
  /// <paramref name="ForceOff"/> is set only for boolean options and lets a dependency
  /// (declarative or environmental) force the control off when it becomes unsatisfied.
  /// </summary>
  private sealed record OptionBinding(SetupManifest.SetupOption Option, Func<string> Read, Action? ForceOff = null);

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
