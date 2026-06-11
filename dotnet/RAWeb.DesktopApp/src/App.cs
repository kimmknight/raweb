using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Animation;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using RAWeb.DesktopApp;
using static Microsoft.UI.Reactor.Factories;
using static RAWeb.DesktopApp.Components;

partial class App(
    string? url = null,
    Action<CoreWebView2>? onCoreReady = null,
    Func<ReactorWindow?>? getWindow = null
) : Component {
  readonly string? _explicitUrl = url;

  private static IHost? s_internalRawebHost;
  private static string? s_internalBaseUrl;
  private static string? s_internalAuthSecret;

  public static string s_colorStyleSheet = BuildColorStyleSheet();

  public static async Task<(IHost Host, string BaseUrl, string AuthSecret)> GetInternalRawebServer() {
    if (s_internalRawebHost is not null && s_internalBaseUrl is not null && s_internalAuthSecret is not null) {
      return (s_internalRawebHost, s_internalBaseUrl, s_internalAuthSecret);
    }

    var (apiHost, baseUrl, authSecret) = await RAWeb.DesktopApp.InternalServer.ServerUtils.StartServer(
      getStyleTagContent: () => (s_colorStyleSheet, "dynamic-color-stylesheet")
    );
    s_internalRawebHost = apiHost;
    s_internalBaseUrl = baseUrl;
    s_internalAuthSecret = authSecret;
    return (apiHost, baseUrl, authSecret);
  }

  public override Element Render() {
    var (_, baseUrl, authSecret) = GetInternalRawebServer().GetAwaiter().GetResult();
    var navigateUrl = _explicitUrl ?? baseUrl;

    var (windowWidth, windowHeight) = UseWindowSize();
    var dpi = UseDpi();
    var dipScale = UseMemo(() => dpi / 96.0, dpi);
    var isDarkMode = UseIsDarkTheme();
    var prefersReducedMotion = UseReducedMotion();

    var (showSplash, setShowSplash) = UseState(true);

    var (webview, setWebview) = UseState<CoreWebView2?>(null);

    // update the color stylesheet whenever the theme changes
    var (colorTick, bumpColorTick) = UseReducer(0);
    UseEffect(() => {
      var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
      var uiSettings = new Windows.UI.ViewManagement.UISettings();

      void OnColorValuesChanged(Windows.UI.ViewManagement.UISettings sender, object args) {
        dispatcherQueue.TryEnqueue(() => bumpColorTick(c => c + 1));
      }

      uiSettings.ColorValuesChanged += OnColorValuesChanged;
      return () => uiSettings.ColorValuesChanged -= OnColorValuesChanged;
    });
    UseEffect(() => {
      s_colorStyleSheet = BuildColorStyleSheet();

      // also immediately inject the new stylesheet into the head of the document so the colors update without a full page refresh
      var script = $@"
        (function() {{
          const styleTags = document.querySelectorAll('style#dynamic-color-stylesheet');
          styleTags.forEach(tag => tag.remove());

          styleTag = document.createElement('style');
          styleTag.id = 'dynamic-color-stylesheet';
          styleTag.innerHTML = `{s_colorStyleSheet}`;
          document.head.appendChild(styleTag);
        }})();
      ";
      webview?.ExecuteScriptAsync(script);
    }, webview, isDarkMode, colorTick);

    var windowsBuild = Environment.OSVersion.Version.Build;
    var shouldUseAccentSplashScreen = windowsBuild < 19045; // Windows 10 21H2 or earlier

    var webview2 = TransparentWebView2(
      url: navigateUrl,
      onCoreReady: core => {
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.AreDevToolsEnabled = true;
        core.Settings.IsSwipeNavigationEnabled = false;
        core.Settings.IsZoomControlEnabled = false;
        core.Settings.IsPasswordAutosaveEnabled = false;
        core.Settings.IsGeneralAutofillEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsPinchZoomEnabled = false;

        // set auth cookie so the internal server accepts requests from this WebView2 instance
        var authCookie = core.CookieManager.CreateCookie(
          RAWeb.DesktopApp.InternalServer.ServerUtils.AuthSecretCookieName,
          authSecret,
          "localhost",
          "/"
        );
        authCookie.IsHttpOnly = true;
        authCookie.IsSecure = false;
        core.CookieManager.AddOrUpdateCookie(authCookie);

        onCoreReady?.Invoke(core);
        setWebview(core);

        // prefer a new internal window instead of the webview generic popup window
        core.NewWindowRequested += async (s, e) => {
          e.Handled = true;
          var deferral = e.GetDeferral();

          var newCore = await PopupWindow.OpenAsync(e.Uri, e.Name, e.WindowFeatures, navigateUrl);
          if (newCore is not null) {
            e.NewWindow = newCore;
          }
          deferral.Complete();
        };

        core.WindowCloseRequested += (s, e) => {
          getWindow?.Invoke()?.Close();
        };

        // keep the window title in sync with the web page title
        core.DocumentTitleChanged += (s, e) => {
          var title = core.DocumentTitle;
          if (!string.IsNullOrEmpty(title)) {
            getWindow?.Invoke()?.AppWindow.Title = title;
          }
        };

        // allow the web app to toggle the splash screen
        core.WebMessageReceived += (s, e) => {
          var message = e.TryGetWebMessageAsString();
          if (message == "{ \"type\": \"show-splash\" }") {
            setShowSplash(true);
          }
          else if (message == "{ \"type\": \"hide-splash\" }") {
            setShowSplash(false);
          }
        };
      })
      .Opacity(showSplash ? 0 : 1); // hide until the splash screen is hidden

    if (!prefersReducedMotion) {
      webview2 = webview2.Animate(Curve.Ease(300, new Easing(0.16f, 1f, 0.3f, 1f)), AnimateProperty.Opacity);
    }

    var appLogoPath = Path.Combine(AppContext.BaseDirectory, "Assets", shouldUseAccentSplashScreen ? "SplashLogo288x288.altform-heavyshadow.png" : "SplashLogo288x288.png");
    var appLogoUri = new Uri(appLogoPath);

    var splashProgressRing = ProgressRing().IsActive().Width(32).Height(32)
      .HAlign(HorizontalAlignment.Center)
      .VAlign(VerticalAlignment.Bottom)
      .Grid(row: 0, column: 0)
      .Margin(bottom: 150)
      .Foreground(Theme.Accent)
      .WithKey("progress-ring");

    var splashStatusText = TextBlock("Powered by RAWeb")
      .HAlign(HorizontalAlignment.Center)
      .VAlign(VerticalAlignment.Bottom)
      .Grid(row: 0, column: 0)
      .Foreground(Theme.TertiaryText)
      .FontSize(16)
      .Margin(bottom: 100)
      .WithKey("status-text");

    if (shouldUseAccentSplashScreen) {
      // always use the light mode accent color, even when the app is in dark mode.
      var textOnHighightAccentColor = ThemeRef.Resolve("TextOnAccentFillColorSelectedTextBrush", isDark: false);
      if (textOnHighightAccentColor is Microsoft.UI.Xaml.Media.SolidColorBrush brush) {
        splashStatusText = splashStatusText.Foreground(brush);
        splashProgressRing = splashProgressRing.Foreground(brush);
      }
    }

    var splashScreen = Grid(
      columns: [GridSize.Star()],
      rows: [GridSize.Star()],

      // app logo centered in the window
      Image(appLogoUri.AbsoluteUri)
        .HAlign(HorizontalAlignment.Center)
        .VAlign(VerticalAlignment.Center)
        .Grid(row: 0, column: 0)
        .Width(100)
        .Height(100)
        .AccessibilityHidden()
        .WithKey("logo"),

      // progress ring horizontally centered 150 dip from the bottom of the window
      prefersReducedMotion ? null : splashProgressRing,

      // status text horizontally centered 100 dip from the bottom of the window
      splashStatusText
    )
    .Opacity(showSplash ? 1 : 0)
    .Width(windowWidth)
    .Height(windowHeight);

    if (shouldUseAccentSplashScreen) {
      // always use the light mode accent color, even when the app is in dark mode.
      var lightAccentBrush = ThemeRef.Resolve("SystemControlHighlightAccentBrush", isDark: false);
      splashScreen = lightAccentBrush is not null
        ? splashScreen.Background(lightAccentBrush)
        : splashScreen.Background(Theme.Accent);
    }

    if (!prefersReducedMotion) {
      splashScreen = splashScreen.Animate(Curve.Ease(300, new Easing(0.16f, 1f, 0.3f, 1f)), AnimateProperty.Opacity);
    }

    // The Windows caption buttons (min/max/close) are system chrome —
    // they don't adapt to RequestedTheme on the Reactor tree. Push the
    // colors directly onto AppWindow.TitleBar so they track the toggle.
    UseEffect(() => {
      if (ReactorApp.PrimaryWindow?.AppWindow is { } appWindow) {
        // button icon color when window is active
        if (showSplash && shouldUseAccentSplashScreen) {
          if (ThemeRef.Resolve("TextOnAccentFillColorSelectedTextBrush", isDark: false) is Microsoft.UI.Xaml.Media.SolidColorBrush onAccentBrush) {
            appWindow.TitleBar.ButtonForegroundColor = onAccentBrush.Color;
          }
        }
        else if (Application.Current.Resources.TryGetValue("TextFillColorPrimaryBrush", out var resource) && resource is Microsoft.UI.Xaml.Media.SolidColorBrush textPrimaryBrush) {
          appWindow.TitleBar.ButtonForegroundColor = textPrimaryBrush.Color;
        }
      }
      return () => { };
    }, isDarkMode, colorTick, showSplash, shouldUseAccentSplashScreen);

    var supportsMica = Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported();
    var supportsAcrylic = Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported();
    return Grid(
      columns: [GridSize.Star()],
      rows: [GridSize.Star()],

      Border(
        // render content in a canvas so the splash screen and web view
        // are absolutely positioned starting from the top-left corner
        // of the window (they stack on each other)
        Canvas(
          splashScreen,
          webview2
        )
      )
      .Background(
        isDarkMode
          // we put a significant tint on the acrylic backdrop because
          // acrylic is much more transparent than mica
          ? (supportsAcrylic && !supportsMica ? "#50080808" : "#00000000") // ARGB
          : (supportsAcrylic && !supportsMica ? "#80ebebeb" : "#00000000")
      )
    )
    .Backdrop(
        supportsMica ? BackdropKind.Mica :
        supportsAcrylic ? BackdropKind.AcrylicThin :
        BackdropKind.None
    );
  }

  public static class PopupWindow {
    private static readonly Dictionary<string, ReactorWindow> s_openWindows = [];

    public static Task<CoreWebView2?> OpenAsync(string url, string windowName, CoreWebView2WindowFeatures windowFeatures, string openerUrl, ReactorWindow? owner = null) {
      if (s_openWindows.TryGetValue(windowName, out var existingWindow)) {
        existingWindow.Activate();
        return Task.FromResult<CoreWebView2?>(null);
      }

      var tcs = new TaskCompletionSource<CoreWebView2?>();

      var popupOrigin = new Uri(url).GetLeftPart(UriPartial.Authority);
      var openerOrigin = new Uri(openerUrl).GetLeftPart(UriPartial.Authority);
      var sameOrigin = popupOrigin == openerOrigin;

      ReactorWindow? window = null;
      window = ReactorApp.OpenWindow(
        new WindowSpec {
          Title = windowName,
          Width = windowFeatures.Width > 0 ? (int)windowFeatures.Width : 600,
          Height = windowFeatures.Height > 0 ? (int)windowFeatures.Height : 400,
          ExtendsContentIntoTitleBar = sameOrigin,
          StartPosition = WindowStartPosition.CenterOnOwner,
          Owner = owner
        },
        () => {
          return new App(url, core => tcs.TrySetResult(core), () => window);
        }
      );

      s_openWindows[windowName] = window;
      window.Closed += (s, e) => s_openWindows.Remove(windowName);

      return tcs.Task;
    }
  }

  public static (double Hue, double Saturation, double Lightness, double Alpha)? ResourceColorToHsla(object key) {
    try {
      if (Application.Current.Resources.TryGetValue(key, out var resource) && resource is Microsoft.UI.Xaml.Media.SolidColorBrush accentBrush) {
        // 1. Normalize RGB values to a scale of 0 to 1
        var r = accentBrush.Color.R / 255.0;
        var g = accentBrush.Color.G / 255.0;
        var b = accentBrush.Color.B / 255.0;
        var a = accentBrush.Color.A / 255.0;

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;

        // 2. Calculate Lightness
        var lightness = (max + min) / 2.0;

        // 3. Calculate Saturation
        double saturation = 0;
        if (delta != 0) {
          saturation = lightness < 0.5
              ? delta / (max + min)
              : delta / (2.0 - max - min);
        }

        // 4. Calculate Hue
        double hue = 0;
        if (delta != 0) {
          if (max == r) {
            hue = (g - b) / delta + (g < b ? 6 : 0);
          }
          else if (max == g) {
            hue = (b - r) / delta + 2;
          }
          else if (max == b) {
            hue = (r - g) / delta + 4;
          }

          hue /= 6.0;
        }

        // Convert to standard units: Hue (0-360°), Saturation (0-100%), Lightness (0-100%), Alpha (0-1)
        return (Math.Round(hue * 360), Math.Round(saturation * 100), Math.Round(lightness * 100), Math.Round(a, 4));
      }
    }
    catch (Exception ex) {
      Console.WriteLine($"Error converting resource color to HSLA: {ex}");
    }
    return null;
  }

  public static string? ResourceColorToCssHsl(object key) {
    var hsla = ResourceColorToHsla(key);
    if (hsla.HasValue) {
      if (hsla.Value.Alpha < 1) {
        return $"hsla({hsla.Value.Hue}, {hsla.Value.Saturation}%, {hsla.Value.Lightness}%, {hsla.Value.Alpha})";
      }

      return $"hsl({hsla.Value.Hue}, {hsla.Value.Saturation}%, {hsla.Value.Lightness}%)";
    }
    return null;
  }

  /// <summary>
  /// Builds a stylesheet that overrides the colors in the app to
  /// match the current Windows theme.
  /// 
  /// This also changes the window background color based on whether the
  /// mica or acrylic backdrops are available.
  /// </summary>
  /// <returns></returns>
  internal static string BuildColorStyleSheet() {
    var builder = new System.Text.StringBuilder();

    var addProperty = (string name, string? value) => {
      if (value is not null) {
        builder.AppendLine($"--wui-{name}: {value};");
      }
    };

    builder.AppendLine(":root {");

    addProperty("accent-default", ResourceColorToCssHsl("AccentFillColorDefaultBrush"));
    addProperty("accent-secondary", ResourceColorToCssHsl("AccentFillColorSecondaryBrush"));
    addProperty("accent-tertiary", ResourceColorToCssHsl("AccentFillColorTertiaryBrush"));
    addProperty("accent-disabled", ResourceColorToCssHsl("AccentFillColorDisabledBrush"));
    addProperty("accent-selected-text-background", ResourceColorToCssHsl("AccentFillColorSelectedTextBackgroundBrush"));

    addProperty("accent-text-primary", ResourceColorToCssHsl("AccentTextFillColorPrimaryBrush"));
    addProperty("accent-text-secondary", ResourceColorToCssHsl("AccentTextFillColorSecondaryBrush"));
    addProperty("accent-text-tertiary", ResourceColorToCssHsl("AccentTextFillColorTertiaryBrush"));
    addProperty("accent-text-disabled", ResourceColorToCssHsl("AccentTextFillColorDisabledBrush"));

    addProperty("text-on-accent-primary", ResourceColorToCssHsl("TextOnAccentFillColorPrimaryBrush"));
    addProperty("text-on-accent-secondary", ResourceColorToCssHsl("TextOnAccentFillColorSecondaryBrush"));
    addProperty("text-on-accent-disabled", ResourceColorToCssHsl("TextOnAccentFillColorDisabledBrush"));
    addProperty("text-on-accent-selected-text", ResourceColorToCssHsl("TextOnAccentFillColorSelectedTextBrush"));

    builder.AppendLine("}");

    var supportsMica = Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported();
    var supportsAcrylic = Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported();

    if (supportsMica || supportsAcrylic) {
      builder.AppendLine(":root { --window-background-color: transparent; }");
    }

    // hide the web app splash screen because we have
    // a WinUI splash screen that renders before the web view
    // is even ready
    builder.AppendLine(".root-splash-wrapper { display: none !important; }");

    return builder.ToString();
  }
}
