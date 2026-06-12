using System;
using System.IO;
using System.Text.Json.Nodes;
using Microsoft.UI.Reactor;
using Microsoft.Web.WebView2.Core;
using Windows.Storage.Pickers;
using static RAWeb.DesktopApp.Events.WebView2PickerHelpers;

namespace RAWeb.DesktopApp.Events;

internal static class UseDownloadFilePickerExtensions {
  /// <summary>
  /// Replaces the default download popover with a custom implementation that
  /// uses Windows.Storage.Pickers to let the user choose the download location
  /// and sends a message back to the web app.
  /// </summary>
  internal static void UseDownloadFilePicker(this CoreWebView2 core, Func<ReactorWindow?>? getWindow) {
    core.DownloadStarting += (s, e) => {
      var deferral = e.GetDeferral();
      var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
      dispatcherQueue.TryEnqueue(async () => {
        try {
          try {
            var suggestedFileName = Path.GetFileName(e.ResultFilePath);

            var savePicker = new FileSavePicker {
              SuggestedFileName = Path.GetFileNameWithoutExtension(suggestedFileName),
              SuggestedStartLocation = PickerLocationId.Downloads
            };

            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, GetWindowHandle(getWindow));

            var extension = Path.GetExtension(suggestedFileName);
            savePicker.FileTypeChoices.Add(
              string.IsNullOrEmpty(extension) ? "File" : $"{extension.TrimStart('.').ToUpperInvariant()} File",
              [string.IsNullOrEmpty(extension) ? "." : extension]
            );

            var file = await savePicker.PickSaveFileAsync();
            if (string.IsNullOrEmpty(file?.Path)) {
              e.Cancel = true;
              return;
            }

            e.ResultFilePath = file.Path;
            e.Handled = true;

            // report download progress to the web app
            e.DownloadOperation.StateChanged += (downloadOperation, _) => {
              var stateChangedMessage = new JsonObject {
                ["type"] = "raweb-download-state-changed",
                ["state"] = downloadOperation.State.ToString(),
                ["resultFilePath"] = downloadOperation.ResultFilePath
              };
              core.PostWebMessageAsJson(stateChangedMessage.ToJsonString());
            };
          }
          catch (Exception ex) {
            Console.WriteLine($"Error showing download save picker: {ex}");
            LogErrorToPage(core, "Error showing download save picker", ex);
            e.Cancel = true;
          }
        }
        finally {
          deferral.Complete();
        }
      });
    };
  }
}
