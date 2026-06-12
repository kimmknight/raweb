using System;
using System.Text.Json.Nodes;
using Microsoft.UI.Reactor;
using Microsoft.Web.WebView2.Core;

namespace RAWeb.DesktopApp.Events;

/// <summary>
/// Shared helpers used by the Windows.Storage.Pickers-based replacement for
/// WebView2's download dialog.
/// </summary>
internal static class WebView2PickerHelpers {
  /// <summary>
  /// Sends a copy of an exception to the web page's console as an error.
  /// </summary>
  internal static void LogErrorToPage(CoreWebView2 core, string message, Exception ex) {
    var text = JsonValue.Create($"{message}: {ex}")!.ToJsonString();
    _ = core.ExecuteScriptAsync($"console.error({text})");
  }

  /// <summary>
  /// Gets the HWND of the window returned by <paramref name="getWindow"/>, or
  /// IntPtr.Zero if it cannot be determined. This is needed by
  /// WinRT.Interop.InitializeWithWindow to associate Windows.Storage.Pickers
  /// dialogs with an owner window.
  /// </summary>
  internal static IntPtr GetWindowHandle(Func<ReactorWindow?>? getWindow) {
    var windowId = getWindow?.Invoke()?.AppWindow.Id ?? default;
    return Microsoft.UI.Win32Interop.GetWindowFromWindowId(windowId);
  }
}
