using System.Windows;
using iNKORE.UI.WPF.Modern.Controls;

namespace RAWeb.Server.Installer.Wizard;

/// <summary>
/// Provides a common method for showing  fluent-styled <see cref="ContentDialog"/>
/// prompts so every wizard page and the shell present dialogs consistently.
/// </summary>
public static class DialogHelpers {
  public static async Task ShowWarningAsync(Window? owner, string message, string title = "RAWeb Installer") {
    var dialog = new ContentDialog {
      Title = title,
      Content = message,
      CloseButtonText = "OK",
      DefaultButton = ContentDialogButton.Close,
      Owner = owner,
    };

    await dialog.ShowAsync();
  }

  public static async Task<bool> ShowConfirmAsync(
    Window? owner,
    string message,
    string primaryText = "Yes",
    string closeText = "No",
    string title = "RAWeb Installer"
  ) {
    var dialog = new ContentDialog {
      Title = title,
      Content = message,
      PrimaryButtonText = primaryText,
      CloseButtonText = closeText,
      DefaultButton = ContentDialogButton.Close,
      Owner = owner,
    };

    return await dialog.ShowAsync() == ContentDialogResult.Primary;
  }
}
