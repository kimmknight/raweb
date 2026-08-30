using System.IO;
using RAWeb.Server.Installer.Setup;

namespace RAWeb.Server.Installer.Wizard.Pages;

/// <summary>
/// Shown for any installer type that requires us to handoff to an external script (e.g. setup.ps1)
/// to complete the installation. The wizard closes once the script exits.
/// </summary>
public partial class LegacySetupPage : WizardPage {
  public LegacySetupPage() => InitializeComponent();

  public override string Title => "Continue in PowerShell";

  public override string Description => State.Strategy == InstallStrategy.RemotePreview
    ? "This version installs through the RAWeb Developer Preview installer."
    : "This release installs itself through setup.ps1.";

  public override string NextText => "Continue in PowerShell";

  public override bool ShouldSkip() => !State.Strategy.RequiresExternalHandoff();

  public override void OnEnter(WizardNavigationDirection direction) {
    if (State.Strategy == InstallStrategy.RemotePreview) {
      ExplanationBar.Title = "This version uses the developer preview installer";
      ExplanationBar.Message =
        "Unreleased branches are installed through install.raweb.app, which finds the newest build for this branch "
        + "or builds it from source if none is available yet.";
      InstructionsText.Text =
        "Choosing Continue opens a PowerShell window and runs the preview installer. Answer its prompts there. "
        + "This wizard closes once it exits.";
      ScriptLabelText.Text = "Branch";
      ScriptPathText.Text = $"{State.PreviewOwner}/{State.PreviewBranch}";
    }
    else {
      ExplanationBar.Title = "This release uses the PowerShell installer";
      ExplanationBar.Message = "Releases from before setup.json was introduced are installed by their own setup.ps1 script.";
      InstructionsText.Text =
        "Choosing Continue opens a PowerShell window and runs the release's own setup.ps1. Answer its prompts there. "
        + "This wizard closes once the script exits.";
      ScriptLabelText.Text = "Script";
      ScriptPathText.Text = Path.Combine(State.PayloadRoot, "setup.ps1");
    }

    CanGoNext = true;
    ShowBack = false;
  }

  public override async Task<bool> OnNextAsync() {
    // The software to install lives in a scratch directory that is deleted when this window
    // closes, so the installer script has to finish before the wizard exits.
    if (State.Strategy == InstallStrategy.RemotePreview) {
      await Task.Run(() => ReleaseInspector.LaunchPreviewScript(State.PreviewOwner!, State.PreviewBranch!));
    }
    else {
      await Task.Run(() => ReleaseInspector.LaunchLegacySetup(State.PayloadRoot));
    }

    return true;
  }
}
