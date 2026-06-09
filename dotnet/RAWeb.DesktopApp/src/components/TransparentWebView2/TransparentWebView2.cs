using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Animation;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Windowing;
using Microsoft.Web.WebView2.Core;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Storage;
using static Microsoft.UI.Reactor.Factories;

namespace RAWeb.DesktopApp;

record TransparentWebView2Props(
    string? Url = null,
    Action<CoreWebView2>? OnCoreReady = null
);

static partial class Components {
  public static ComponentElement<TransparentWebView2Props> TransparentWebView2(string? url = null, Action<CoreWebView2>? onCoreReady = null) {
    return Component<TransparentWebView2, TransparentWebView2Props>(new(url, onCoreReady));
  }
}

/// <summary>
/// A Reactor component that hosts a full-window WebView2 with automatic bounds
/// management, titlebar hook installation, and caption right-inset CSS variable sync.
/// </summary>
partial class TransparentWebView2 : Component<TransparentWebView2Props> {
  // props and their fluent setters
  private string? _url;
  private Action<CoreWebView2>? _onCoreReady;
  public TransparentWebView2 Url(string url) { _url = url; return this; }
  public TransparentWebView2 OnCoreReady(Action<CoreWebView2> cb) { _onCoreReady = cb; return this; }

  /// <summary>
  /// The window where the WebView2 is being rendered.
  /// This should be set from within the <see cref="Render"/> method using the
  /// UseWindow() hook.
  /// </summary>
  private ReactorWindow? _window;

  // there are all set by InitAsync
  private WebView2TitleBarHook? _hook;
  private CoreWebView2Controller? _controller;
  private CoreWebView2? _core;

  public override Element Render() {
    _window = UseWindow();

    var (captionButtonsWidth, setCaptionButtonsWidth) = UseState(0.0);
    var (captionButtonsHeight, setCaptionButtonsHeight) = UseState(32.0);
    var (iconAreaWidth, setIconAreaWidth) = UseState(0.0);
    var (coreReady, setCoreReady) = UseState(false);
    var (pageLoadCounter, tickPageLoad) = UseReducer(0);

    // initialize WebView2
    UseEffect(() => {
      _ = InitAsync(() => setCoreReady(true));
      return () => { _hook?.Uninstall(); _controller?.Close(); };
    });

    UseEffect(() => {
      var appWindow = _window!.AppWindow;
      var titleBar = appWindow.TitleBar;

      // by default, make the entire standard titlebar height draggable
      titleBar.SetDragRectangles([new RectInt32(0, 0, appWindow.ClientSize.Width, titleBar.Height)]);

      void UpdateInsets(SizeInt32 clientSize) {
        if (_controller is not null) {
          SetBounds(clientSize);
        }

        // WebView2TitleBarHook needs to know the right inset to avoid disrupting
        // the functionality of the caption buttons
        var leftInset = titleBar.LeftInset == 0 ? (int)(48 * DisplayScale) : titleBar.LeftInset;
        _hook?.UpdateCaptionRightInset(titleBar.RightInset);
        _hook?.UpdateCaptionLeftInset(leftInset);
        _hook?.UpdateTitleBarHeight(titleBar.Height);

        // track the scaled version of the caption buttons area size so that
        // we can expose these to the web content via CSS custom properties
        setCaptionButtonsWidth(titleBar.RightInset / DisplayScale);
        setCaptionButtonsHeight(titleBar.Height / DisplayScale);
        setIconAreaWidth(leftInset / DisplayScale);
      }

      void OnChanged(AppWindow sender, AppWindowChangedEventArgs args) {
        if (!args.DidSizeChange) {
          return;
        }

        UpdateInsets(sender.ClientSize);
      }

      appWindow.Changed += OnChanged;

      if (titleBar.Height > 0) {
        UpdateInsets(appWindow.ClientSize);
      }
      else {
        // titlebar metrics aren't available yet; force OnChanged to run by
        // quickly resizing the window by 1px
        var originalSize = appWindow.Size;
        var newSize = new SizeInt32(appWindow.Size.Width + 1, appWindow.Size.Height + 1);
        appWindow.Resize(newSize);
        appWindow.Resize(originalSize);
      }

      return () => appWindow.Changed -= OnChanged;
    }, coreReady);

    // track every time the page loads so that we can re-inject CSS custom properties
    UseEffect(() => {
      if (_core is null) {
        return () => { };
      }

      void OnNavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args) {
        // make sure the web view is visible after first load
        if (_controller is { } controller) {
          controller.IsVisible = true;
        }

        // count page loads
        tickPageLoad(c => c + 1);
      }

      _core.NavigationCompleted += OnNavigationCompleted;
      return () => _core.NavigationCompleted -= OnNavigationCompleted;
    }, coreReady);

    // keep the css custom properties injected and updated on every page
    UseEffect(() => {
      if (_core is null) {
        return;
      }

      var captionWidthPx = captionButtonsWidth.ToString(CultureInfo.InvariantCulture);
      var captionHeightPx = captionButtonsHeight.ToString(CultureInfo.InvariantCulture);
      var iconAreaWidthPx = iconAreaWidth.ToString(CultureInfo.InvariantCulture);
      _ = _core.ExecuteScriptAsync(
          $"""
          document.documentElement.style.setProperty('--titlebar-caption-buttons-width', '{captionWidthPx}px');
          document.documentElement.style.setProperty('--titlebar-area-height', '{captionHeightPx}px');
          document.documentElement.style.setProperty('--titlebar-icon-area-width', '{iconAreaWidthPx}px');
          """
      );
    }, captionButtonsWidth, coreReady, pageLoadCounter, iconAreaWidth);

    return FlexColumn(); // spacer where WebView2 will be placed
  }

  /// <summary>
  /// Initializes the WebView2 environment and controller, sets up the titlebar hook, and navigates to the initial URL.
  /// </summary>
  /// <param name="onReady"></param>
  /// <param name="userDataFolder"></param>
  /// <returns></returns>
  private async Task InitAsync(Action onReady, StorageFolder? userDataFolder = null) {
    var appWindow = _window!.AppWindow;
    var hwnd = Win32Interop.GetWindowFromWindowId(appWindow.Id);
    var windowRef = CoreWebView2ControllerWindowReference.CreateFromWindowHandle((ulong)hwnd);

    var env = await CreateEnvironmentAsync(userDataFolder ?? AppStorage.StorageFolder);
    _controller = await env.CreateCoreWebView2ControllerAsync(windowRef);

    // start hidden to avoid flicker during initialization and first navigation
    _controller.IsVisible = false;

    // make the WebView2 background transparent
    _controller.DefaultBackgroundColor = new Windows.UI.Color { A = 0, R = 0, G = 0, B = 0 };

    _hook = WebView2TitleBarHook.Install(hwnd, appWindow, () => DisplayScale);

    // always render the web view to fill the entire window
    SetBounds(appWindow.ClientSize);

    _core = _controller.CoreWebView2;
    (Props?.OnCoreReady ?? _onCoreReady)?.Invoke(_core);

    if (_hook is not null) {
      _core.WebMessageReceived += _hook.OnWebMessageReceived;
    }

    var url = Props?.Url ?? _url;
    if (url is not null) {
      _core.Navigate(url);
    }

    onReady();
  }

  /// <summary>
  /// Creates a WebView2 environment with the specified user data folder and options, ensuring
  /// that mandatory features for our use case are always enabled.
  /// </summary>
  /// <param name="userDataFolder"></param>
  /// <param name="options"></param>
  /// <returns></returns>
  private async Task<CoreWebView2Environment> CreateEnvironmentAsync(StorageFolder userDataFolder, CoreWebView2EnvironmentOptions? options = null) {
    var userWebViewDataFolder = Path.Combine(userDataFolder.Path, "WebView2");
    Directory.CreateDirectory(userWebViewDataFolder);

    // merge out list of always-enabled features with any features specified in options.AdditionalBrowserArguments
    List<string> mandatoryFeatures = [
      "msEdgeFluentOverlayScrollbar",
      "msOverlayScrollbarWinStyle",
      "msWebView2CodeCache",
      "allow-insecure-localhost"
    ];
    if (options is not null && !string.IsNullOrEmpty(options.AdditionalBrowserArguments)) {
      var enableFeaturesFlagContent = options.AdditionalBrowserArguments
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .FirstOrDefault(arg => arg.StartsWith("--enable-features="));
      if (enableFeaturesFlagContent is not null) {
        var existingFeatureFlags = enableFeaturesFlagContent.Substring("--enable-features=".Length).Split(',', StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim());
        mandatoryFeatures.AddRange(existingFeatureFlags);
      }
    }
    var enableFeaturesFlag = "--enable-features=" + string.Join(",", mandatoryFeatures.Distinct());

    // re-build the AdditionalBrowserArguments string to ensure our required features are included
    var existingAdditionalBrowserArgumentsWithoutEnableFeatures = options?.AdditionalBrowserArguments?
      .Split(' ', StringSplitOptions.RemoveEmptyEntries)
      .Where(arg => !arg.StartsWith("--enable-features="))
      .ToArray() ?? [];
    var mergedAdditionalBrowserArguments = string.Join(" ", existingAdditionalBrowserArgumentsWithoutEnableFeatures.Prepend(enableFeaturesFlag));

    var mergedOptions = new CoreWebView2EnvironmentOptions {
      AdditionalBrowserArguments = mergedAdditionalBrowserArguments
    };

    return await CoreWebView2Environment.CreateWithOptionsAsync(null, userWebViewDataFolder, mergedOptions);
  }

  /// <summary>
  /// Sets the bounds of where the WebView2 controller is rendered
  /// within the window. This method always renders the web view
  /// content on top of other content. The titlebar caption buttons
  /// are still always rendered on top of the web view content.
  /// </summary>
  /// <param name="clientSize"></param>
  private void SetBounds(SizeInt32 clientSize) {
    _controller!.Bounds = new Rect(0, 0, clientSize.Width, clientSize.Height);
    _hook?.EnsureWv2OnTop();
  }

  private double DisplayScale => _window?.NativeWindow.Content?.XamlRoot?.RasterizationScale ?? 1.0;
}
