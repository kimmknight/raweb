using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.Web.WebView2.Core;

namespace RAWeb.DesktopApp;

/// <summary>
/// Manages the Win32 window-hook layer that makes a WebView2-hosted titlebar behave
/// like a native one: correct NCHITTEST routing, caption-button passthrough, and
/// synthetic-input handoff to DWM for smooth window dragging and functional window
/// snapping.
/// </summary>
internal sealed class WebView2TitleBarHook {
  private delegate nint WindowProcedureDelegate(nint windowHandle, uint message, nint wordParameter, nint longParameter);
  private delegate bool EnumerateChildWindowsCallback(nint windowHandle, nint longParameter);

  // WindowProcedureDelegate fields must be kept alive for as long as the hook is
  // installed, because native code holds raw function pointers to them. The
  // instance itself is kept alive by TransparentWebView2._hook, which satisfies this.

  /// <summary>
  /// Our replacement window procedure for the parent window.
  /// </summary>
  private WindowProcedureDelegate? _parentWindowProcedure;

  /// <summary>
  /// Our replacement window procedure for the WebView2 child window.
  /// </summary>
  private WindowProcedureDelegate? _webView2WindowProcedure;

  /// <summary>
  /// The parent window's original window procedure, so it can be restored on <see cref="Uninstall"/>.
  /// </summary>
  private nint _originalParentWindowProcedure;

  /// <summary>
  /// The WebView2 window's original window procedure, so it can be restored on <see cref="Uninstall"/>.
  /// </summary>
  private nint _originalWebView2WindowProcedure;

  /// <summary>
  /// The handle of the top-level application window.
  /// </summary>
  private nint _parentWindowHandle;

  /// <summary>
  /// The handle of the WebView2 content host window (Chrome_WidgetWin_1).
  /// </summary>
  private nint _webView2WindowHandle;

  /// <summary>
  /// Width, in pixels, of the native caption buttons (minimize/maximize/close) on the right side of the titlebar.
  /// </summary>
  private int _captionRightInsetPx;

  /// <summary>
  /// Width, in pixels, of the reserved area on the left side of the titlebar that contains the app icon.
  /// </summary>
  private int _captionLeftInsetPx;

  /// <summary>
  /// Distance, in pixels, from the left edge of the window to where the app icon area begins.
  /// </summary>
  private int _iconAreaLeftOffsetPx;

  /// <summary>
  /// Height, in pixels, of the titlebar.
  /// </summary>
  private int _titleBarHeightPx;

  /// <summary>
  /// Height, in pixels, of the resizable border along the top of the window.
  /// </summary>
  private int _resizeBorderPx;

  /// <summary>
  /// The areas of the titlebar, registered by the web page, that should behave like the native caption (drag to move the window).
  /// </summary>
  private Windows.Graphics.RectInt32[] _dragRectangles = [];

  /// <summary>
  /// When true, the next WM_NCHITTEST on the WebView2 window is reported as transparent so the parent window can begin a DWM drag.
  /// </summary>
  private bool _suppressNextWebView2Click;

  /// <summary>
  /// To prevent a synthetic click used for DWM handoff from being misinterpreted
  /// as a double-click on the caption (which would maximize/restore the window),
  /// we set a short time window during which we will suppress one WM_NCLBUTTONDBLCLK
  /// on HTCAPTION. This field holds the tick count until which we should suppress
  /// the double click.
  /// </summary>
  private int _suppressCaptionDoubleClickUntilTick;

  /// <summary>
  /// Callback to run when the active drag, handed off to DWM, finishes (on WM_EXITSIZEMOVE).
  /// </summary>
  private Action? _dragEndCallback;

  /// <summary>
  /// WinUI's input source, used to tell the system which parts of the window count as the non-client caption area.
  /// </summary>
  private readonly InputNonClientPointerSource _inputSource;

  /// <summary>
  /// Returns the current display scale factor, used to convert CSS pixels from the web page into physical pixels.
  /// </summary>
  private readonly Func<double> _getDisplayScale;

  private WebView2TitleBarHook(InputNonClientPointerSource inputSource, Func<double> getDisplayScale) {
    _inputSource = inputSource;
    _getDisplayScale = getDisplayScale;
  }

  /// <summary>
  /// Installs all hooks. Must be called after <c>CreateCoreWebView2ControllerAsync</c>
  /// completes, because WebView2 subclasses the parent window handle during controller creation.
  /// Returns null if Chrome_WidgetWin_ could not be found as a direct child.
  /// </summary>
  public static WebView2TitleBarHook? Install(nint parentWindowHandle, AppWindow appWindow, Func<double> getDisplayScale) {
    var hook = new WebView2TitleBarHook(
        InputNonClientPointerSource.GetForWindowId(appWindow.Id),
        getDisplayScale) {
      _parentWindowHandle = parentWindowHandle
    };

    // implement a custom procedure for the parent window that
    // implements our custom logic for the titlebar area
    hook._parentWindowProcedure = (windowHandle, message, wordParameter, longParameter) => {
      var captionDoubleClickShouldBeSupressed = Environment.TickCount <= hook._suppressCaptionDoubleClickUntilTick;
      var isDoubleClickOnCaption = message == WM_NCLBUTTONDBLCLK && wordParameter == HTCAPTION;

      // ignore the any double clicks on the caption that happen in the
      // short exclusion window
      if (isDoubleClickOnCaption && captionDoubleClickShouldBeSupressed) {
        hook._suppressCaptionDoubleClickUntilTick = 0;
        return 0;
      }

      var result = CallWindowProc(hook._originalParentWindowProcedure, windowHandle, message, wordParameter, longParameter);
      var isResizeOrMoveFinish = message == WM_EXITSIZEMOVE;

      if (isResizeOrMoveFinish && hook._dragEndCallback is { } onDragEnd) {
        hook._dragEndCallback = null;
        onDragEnd();
      }

      // manually evaluate how we want DWM to treat clicks at the current mouse coordinate
      var isWindowPartAtCoordinateQuery = message == WM_NCHITTEST;
      if (isWindowPartAtCoordinateQuery) {
        var cursorPositionOnScreenX = (short)(longParameter & 0xFFFF);
        var cursorPositionOnScreenY = (short)((longParameter >> 16) & 0xFFFF);

        // calculate the cursor position relative to the top-left of the window
        var windowRectangle = default(WindowRectangle);
        GetWindowRect(windowHandle, ref windowRectangle);
        var cursorPosX = cursorPositionOnScreenX - windowRectangle.Left;
        var cursorPosY = cursorPositionOnScreenY - windowRectangle.Top;
        var windowWidth = windowRectangle.Right - windowRectangle.Left;

        // Icon area: Windows natively handles left-click (show menu) and double-click (close).
        if (hook._captionLeftInsetPx > 0 && hook._titleBarHeightPx > 0 &&
            cursorPosX >= hook._iconAreaLeftOffsetPx && cursorPosX < (hook._captionLeftInsetPx + hook._iconAreaLeftOffsetPx) &&
            cursorPosY >= hook._resizeBorderPx && cursorPosY < hook._titleBarHeightPx) {
          // inform DWM that the cursor is over the app icon area
          return HTSYSMENU;
        }

        var isCursorInCaptionArea = result == HTCAPTION;
        if (isCursorInCaptionArea) {
          // Keep HTCAPTION for the native caption buttons.
          var isCursorInCaptionButtonsArea = hook._captionRightInsetPx > 0 && cursorPosX >= windowWidth - hook._captionRightInsetPx;
          if (isCursorInCaptionButtonsArea) {
            return result;
          }

          // Keep HTCAPTION for registered drag rectangles so WM_MOUSE dragging works.
          foreach (var dragRectangle in hook._dragRectangles) {
            var isCursorInDragRectangle = cursorPosX >= dragRectangle.X &&
              cursorPosX < dragRectangle.X + dragRectangle.Width &&
              cursorPosY >= dragRectangle.Y &&
              cursorPosY < dragRectangle.Y + dragRectangle.Height;
            if (isCursorInDragRectangle) {
              return result;
            }
          }

          // When no drag rectangles are registered (e.g. the page is still loading),
          // fall back to the default HTCAPTION result so the window remains draggable.
          if (hook._dragRectangles.Length == 0) {
            return result;
          }

          // Everything else: let WebView2 receive the event.
          return HTCLIENT;
        }
      }

      return result;
    };

    // preserve the original window procedure so that we can
    // uninstall the hook later
    hook._originalParentWindowProcedure = SetWindowLongPtr(
      parentWindowHandle,
      GWLP_WNDPROC,
      Marshal.GetFunctionPointerForDelegate(hook._parentWindowProcedure)
    );
    Console.WriteLine($"[Hook] Parent window procedure installed  windowHandle=0x{parentWindowHandle:X}");

    // start with an empty caption region
    hook._inputSource.SetRegionRects(NonClientRegionKind.Caption, []);

    // Find the handle for Chrome_WidgetWin_0, which is the parent of
    // Chrome_WidgetWin_1, the WebView2 content host.
    nint chromeWidgetWindow0Handle = 0;
    EnumChildWindows(parentWindowHandle, (windowHandle, _) => {
      var classNameBuilder = new System.Text.StringBuilder(128);
      _ = GetClassName(windowHandle, classNameBuilder, classNameBuilder.Capacity);
      var className = classNameBuilder.ToString();

      var isDirectChild = GetParent(windowHandle) == parentWindowHandle;
      Console.WriteLine($"[Hook]   windowHandle=0x{windowHandle:X}  class={className}  isChild={isDirectChild}");

      if (!isDirectChild) {
        return true;
      }

      if (className == "Chrome_WidgetWin_0") {
        chromeWidgetWindow0Handle = windowHandle;
        return false;
      }

      return true;
    }, 0);

    if (chromeWidgetWindow0Handle == 0) {
      Console.WriteLine("[Hook] Chrome_WidgetWin_0 not found. Skipping WebView2 hook.");
      return null;
    }

    // Now find the handle for Chrome_WidgetWin_1, which is the WebView2 content host.
    nint chromeWidgetWindow1Handle = 0;
    EnumChildWindows(parentWindowHandle, (windowHandle, _) => {
      var classNameBuilder = new System.Text.StringBuilder(128);
      _ = GetClassName(windowHandle, classNameBuilder, classNameBuilder.Capacity);
      var className = classNameBuilder.ToString();

      var isDirectGrandchild = GetParent(windowHandle) == chromeWidgetWindow0Handle;
      Console.WriteLine($"[Hook]   windowHandle=0x{windowHandle:X}  class={className}  isGrandChild={isDirectGrandchild}");

      if (!isDirectGrandchild) {
        return true;
      }

      if (className == "Chrome_WidgetWin_1") {
        chromeWidgetWindow1Handle = windowHandle;
        return false;
      }

      return true;
    }, 0);

    if (chromeWidgetWindow1Handle == 0) {
      Console.WriteLine("[Hook] Chrome_WidgetWin_1 not found. Skipping WebView2 hook.");
      return null;
    }

    hook._webView2WindowHandle = chromeWidgetWindow1Handle;
    Console.WriteLine($"[Hook] Chrome_WidgetWin_ found=0x{hook._webView2WindowHandle:X}");

    // Elevate above WinUI's XAML input window so it receives pointer events
    // in the titlebar area.
    // TODO: figure out a way still be able to display WinUI content above the webview while keeping our procedure working and the webview otherwise interactive
    SetWindowPos(hook._webView2WindowHandle, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

    var resizeBorderPixels = GetSystemMetrics(SM_CYSIZEFRAME);
    hook._resizeBorderPx = resizeBorderPixels;

    // implement a custom procedure for the WebView2 window that
    // helps direct certain clicks to our window with the custom
    // titlebar logic
    hook._webView2WindowProcedure = (windowHandle, message, wordParameter, longParameter) => {
      var isWindowPartAtCoordinateQuery = message == WM_NCHITTEST;
      if (isWindowPartAtCoordinateQuery) {
        var cursorPositionOnScreenX = (short)(longParameter & 0xFFFF);
        var cursorPositionOnScreenY = (short)((longParameter >> 16) & 0xFFFF);

        // calculate the cursor position relative to the top-left of the window
        var windowRectangle = default(WindowRectangle);
        GetWindowRect(windowHandle, ref windowRectangle);
        var cursorPosX = cursorPositionOnScreenX - windowRectangle.Left;
        var cursorPosY = cursorPositionOnScreenY - windowRectangle.Top;
        var windowWidth = windowRectangle.Right - windowRectangle.Left;

        // Sometimes, the webview captures a click that needs to be
        // sednt to DWM or the parent window instead. In that case, we
        // synthetically create a click and record that is dhould not
        // be processed by WebView2. In that case, we must pass the
        // click to the parent window. Since the parent window is
        // always behind the WebView2 window, we can say that the
        // WebView2 is transparent for this click.
        if (hook._suppressNextWebView2Click) {
          hook._suppressNextWebView2Click = false;
          return HTTRANSPARENT;
        }

        // let the parent window handle resize borders,
        // caption buttons, and the icon area
        var isCursorInResizeBorder = cursorPosY < resizeBorderPixels;
        var isCursorInCaptionButtonsArea = hook._captionRightInsetPx > 0 &&
          cursorPosX >= windowWidth - hook._captionRightInsetPx &&
          cursorPosY < 60;
        var isCursorInIconArea = hook._captionLeftInsetPx > 0 &&
          hook._titleBarHeightPx > 0 &&
          cursorPosY >= hook._iconAreaLeftOffsetPx &&
          cursorPosX < hook._captionLeftInsetPx &&
          cursorPosY < hook._titleBarHeightPx;
        if (isCursorInResizeBorder || isCursorInCaptionButtonsArea || isCursorInIconArea) {
          return HTTRANSPARENT;
        }
      }

      return CallWindowProc(hook._originalWebView2WindowProcedure, windowHandle, message, wordParameter, longParameter);
    };

    // preserve the original WebView2 window procedure so that we can
    // uninstall the hook later
    hook._originalWebView2WindowProcedure = SetWindowLongPtr(
      hook._webView2WindowHandle,
      GWLP_WNDPROC,
      Marshal.GetFunctionPointerForDelegate(hook._webView2WindowProcedure)
    );
    Console.WriteLine($"[Hook] WebView2 window procedure installed  windowHandle=0x{hook._webView2WindowHandle:X}");

    // On Windows 11, "Intermediate D3D Window" sits above Chrome_RenderWidgetHostHWND
    // in z-order (the reverse of Windows 10) and is marked WS_EX_TRANSPARENT. For
    // some reason, clearing the flag is required for clicks and other interactions to work.
    var d3dWindowPollAttempts = 0;
    System.Threading.Timer? d3dWindowPollTimer = null;
    d3dWindowPollTimer = new System.Threading.Timer(_ => {
      var foundD3dWindow = false;
      EnumChildWindows(hook._webView2WindowHandle, (windowHandle, _) => {
        var classNameBuilder = new System.Text.StringBuilder(128);
        _ = GetClassName(windowHandle, classNameBuilder, classNameBuilder.Capacity);
        if (classNameBuilder.ToString() == "Intermediate D3D Window") {
          var extendedStyle = GetWindowLongPtr(windowHandle, GWL_EXSTYLE);
          SetWindowLongPtr(windowHandle, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
          Console.WriteLine($"[Hook] Cleared WS_EX_TRANSPARENT on Intermediate D3D Window  windowHandle=0x{windowHandle:X}");
          foundD3dWindow = true;
          return false;
        }
        return true;
      }, 0);

      if (foundD3dWindow || ++d3dWindowPollAttempts >= 40) {
        if (!foundD3dWindow) {
          Console.WriteLine("[Hook] Intermediate D3D Window not found after polling; WS_EX_TRANSPARENT not cleared.");
        }
        d3dWindowPollTimer?.Dispose();
      }
    }, null, 0, 250);

    return hook;
  }

  /// <summary>
  /// Restores the original window procedures for the parent and WebView2 windows,
  /// effectively uninstalling the hook.
  /// <br /><br />
  /// This should be called when the WebView2 is being destroyed.
  /// </summary>
  public void Uninstall() {
    if (_webView2WindowHandle != 0 && _originalWebView2WindowProcedure != 0) {
      SetWindowLongPtr(_webView2WindowHandle, GWLP_WNDPROC, _originalWebView2WindowProcedure);
      _originalWebView2WindowProcedure = 0;
    }
    if (_parentWindowHandle != 0 && _originalParentWindowProcedure != 0) {
      SetWindowLongPtr(_parentWindowHandle, GWLP_WNDPROC, _originalParentWindowProcedure);
      _originalParentWindowProcedure = 0;
    }
  }

  /// <summary>
  /// Re-elevates rendered content layer from the WebView2 window
  /// to be on top of all content.
  /// </summary>
  public void EnsureWebView2OnTop() {
    if (_webView2WindowHandle != 0) {
      SetWindowPos(_webView2WindowHandle, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }
  }

  public void UpdateCaptionRightInset(int pixels) => _captionRightInsetPx = pixels;
  public void UpdateCaptionLeftInset(int pixels) => _captionLeftInsetPx = pixels;
  public void UpdateIconAreaLeftOffset(int pixels) => _iconAreaLeftOffsetPx = pixels;
  public void UpdateTitleBarHeight(int pixels) => _titleBarHeightPx = pixels;

  /// <summary>
  /// Registers caption drag regions with both the window procedure hit-test and
  /// InputNonClientPointerSource.
  /// </summary>
  public void SetDragRectangles(Windows.Graphics.RectInt32[] rectangles) {
    _inputSource.SetRegionRects(NonClientRegionKind.Caption, rectangles);
    _dragRectangles = rectangles;
  }

  /// <summary>
  /// If the left mouse button is currently held and the cursor is over one of the
  /// registered drag rectangles, fires a synthetic UP+DOWN to hand the drag off to DWM
  /// (required for window drag and snapping). Returns true if the handoff was started,
  /// and <paramref name="onDragEnd"/> is called on WM_EXITSIZEMOVE.
  /// </summary>
  public bool TryBeginDrag(Action onDragEnd) {
    if ((GetAsyncKeyState(0x01) & unchecked((short)0x8000)) == 0) {
      return false;
    }
    if (!GetCursorPos(out var cursorPosition)) {
      return false;
    }

    var windowRectangle = default(WindowRectangle);
    GetWindowRect(_parentWindowHandle, ref windowRectangle);
    var relativeX = cursorPosition.X - windowRectangle.Left;
    var relativeY = cursorPosition.Y - windowRectangle.Top;

    foreach (var dragRectangle in _dragRectangles) {
      if (relativeX < dragRectangle.X || relativeX >= dragRectangle.X + dragRectangle.Width ||
          relativeY < dragRectangle.Y || relativeY >= dragRectangle.Y + dragRectangle.Height) {
        continue;
      }

      Console.WriteLine($"[Hook] TryBeginDrag: cursor at ({relativeX},{relativeY}), handing off to DWM");
      _dragEndCallback = onDragEnd;
      _suppressNextWebView2Click = true;
      _suppressCaptionDoubleClickUntilTick = Environment.TickCount + 200;

      SendInput(2, [
          new INPUT { type = 0, mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTUP } },
          new INPUT { type = 0, mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTDOWN } },
        ], Marshal.SizeOf<INPUT>());
      return true;
    }

    return false;
  }

  /// <summary>
  /// Handles messages posted from the page's JavaScript. Message formats:
  /// {"type":"setDragRects","rects":[{"x":0,"y":0,"w":500,"h":32,"singleUse":false},...]}
  /// {"type":"setIconAreaLeft","left":32}
  /// Coordinates are CSS pixels and are scaled to physical pixels before registering.
  /// </summary>
  public void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs e) {
    var rawMessage = e.TryGetWebMessageAsString();
    try {
      using var messageDocument = JsonDocument.Parse(rawMessage);
      var messageRoot = messageDocument.RootElement;
      var messageType = messageRoot.GetProperty("type").GetString();

      if (messageType == "setIconAreaLeftOffset") {
        var offsetPixels = (int)(messageRoot.GetProperty("offset").GetDouble() * _getDisplayScale());
        UpdateIconAreaLeftOffset(offsetPixels);
        Console.WriteLine($"[IconArea] Left offset set to {offsetPixels}px");
        return;
      }

      if (messageType != "setDragRects") {
        return;
      }

      var displayScale = _getDisplayScale();
      var dragRectangles = new System.Collections.Generic.List<Windows.Graphics.RectInt32>();
      foreach (var rectangleJson in messageRoot.GetProperty("rects").EnumerateArray()) {
        dragRectangles.Add(new Windows.Graphics.RectInt32(
            (int)(rectangleJson.GetProperty("x").GetDouble() * displayScale),
            (int)(rectangleJson.GetProperty("y").GetDouble() * displayScale),
            (int)(rectangleJson.GetProperty("w").GetDouble() * displayScale),
            (int)(rectangleJson.GetProperty("h").GetDouble() * displayScale)));
      }

      SetDragRectangles(dragRectangles.ToArray());
      Console.WriteLine($"[DragRects] {dragRectangles.Count} Caption region(s) registered");

      // releases rectangles marked singleUse=true once the current drag ends.
      var releaseSingleUseRectangles = () => {
        using var releaseDocument = JsonDocument.Parse(rawMessage);
        var releaseRoot = releaseDocument.RootElement;
        var remainingRectangles = new System.Collections.Generic.List<Windows.Graphics.RectInt32>();
        foreach (var rectangleJson in releaseRoot.GetProperty("rects").EnumerateArray()) {
          if (!rectangleJson.TryGetProperty("singleUse", out var singleUse) || !singleUse.GetBoolean()) {
            remainingRectangles.Add(new Windows.Graphics.RectInt32(
                (int)(rectangleJson.GetProperty("x").GetDouble() * displayScale),
                (int)(rectangleJson.GetProperty("y").GetDouble() * displayScale),
                (int)(rectangleJson.GetProperty("w").GetDouble() * displayScale),
                (int)(rectangleJson.GetProperty("h").GetDouble() * displayScale)));
          }
        }
        SetDragRectangles(remainingRectangles.ToArray());
        sender.PostWebMessageAsJson($"{{\"type\":\"dragRectsCleaned\",\"remaining\":{remainingRectangles.Count}}}");
        Console.WriteLine($"[DragRects] Released single-use rectangles, {remainingRectangles.Count} remain");
      };

      if (!TryBeginDrag(releaseSingleUseRectangles)) {
        releaseSingleUseRectangles();
      }
    }
    catch (Exception ex) {
      Console.WriteLine($"[DragRects] Error: {ex.Message}  rawMessage='{rawMessage}'");
    }
  }

  [StructLayout(LayoutKind.Sequential)]
  private struct WindowRectangle { public int Left, Top, Right, Bottom; }

  [StructLayout(LayoutKind.Sequential)]
  private struct CursorPoint { public int X, Y; }

  [StructLayout(LayoutKind.Sequential)]
  private struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public nint dwExtraInfo; }
  [StructLayout(LayoutKind.Sequential)]
  private struct INPUT { public uint type; public MOUSEINPUT mi; }

  /// <summary>
  /// Retrieves information about the specified window.
  /// </summary>
  [DllImport("user32.dll")] private static extern nint GetWindowLongPtr(nint windowHandle, int index);

  /// <summary>
  /// Changes an attribute of the specified window.
  /// </summary>
  [DllImport("user32.dll")] private static extern nint SetWindowLongPtr(nint windowHandle, int index, nint newValue);

  /// <summary>
  /// Passes message information to the specified window procedure.
  /// </summary>
  [DllImport("user32.dll")] private static extern nint CallWindowProc(nint previousProcedure, nint windowHandle, uint message, nint wordParameter, nint longParameter);

  /// <summary>
  /// Calls the given callback once for every child window of the given window.
  /// </summary>
  [DllImport("user32.dll")] private static extern bool EnumChildWindows(nint parentWindowHandle, EnumerateChildWindowsCallback callback, nint longParameter);

  /// <summary>
  /// Gets the window class name (e.g. "Chrome_WidgetWin_0") for the given window.
  /// </summary>
  [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern int GetClassName(nint windowHandle, System.Text.StringBuilder buffer, int bufferSize);

  /// <summary>
  /// Gets the handle of the given window's parent window.
  /// </summary>
  [DllImport("user32.dll")] private static extern nint GetParent(nint windowHandle);

  /// <summary>
  /// Gets the bounding rectangle of a window in screen coordinates.
  /// </summary>
  [DllImport("user32.dll")] private static extern bool GetWindowRect(nint windowHandle, ref WindowRectangle rectangle);

  /// <summary>
  /// Changes a window's size, position, and z-order.
  /// </summary>
  [DllImport("user32.dll")] private static extern bool SetWindowPos(nint windowHandle, nint insertAfter, int x, int y, int width, int height, uint flags);

  /// <summary>
  /// Gets a system metric or configuration setting, such as the resize border thickness.
  /// </summary>
  /// <param name="index">The index of the metric to get.</param>
  /// <returns></returns>
  [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);

  /// <summary>
  /// Gets the current position of the mouse cursor relative to the top-left corner of the screen.
  /// </summary>
  [DllImport("user32.dll")] private static extern bool GetCursorPos(out CursorPoint point);

  /// <summary>
  /// Gets whether a key or mouse button was pressed since the last call.
  /// </summary>
  [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int virtualKeyCode);

  /// <summary>
  /// Sends mouse or keyboard input event. This is useful for synthetically re-creating
  /// input events that were captured and need to be directed to a different window (e.g. for DWM drag handoff).
  /// </summary>
  [DllImport("user32.dll")] private static extern uint SendInput(uint inputCount, INPUT[] inputs, int structSizeBytes);

  private const int GWLP_WNDPROC = -4;
  private const int GWL_EXSTYLE = -20;
  private const nint WS_EX_TRANSPARENT = 0x00000020;
  private const int SM_CYSIZEFRAME = 33;
  private const uint WM_NCHITTEST = 0x0084;
  private const nint HTTRANSPARENT = -1;
  private const nint HTCLIENT = 1;
  private const nint HTCAPTION = 2;
  private const nint HTSYSMENU = 3;
  private const nint HWND_TOP = 0;
  private const uint SWP_NOMOVE = 0x0002;
  private const uint SWP_NOSIZE = 0x0001;
  private const uint SWP_NOACTIVATE = 0x0010;
  private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
  private const uint MOUSEEVENTF_LEFTUP = 0x0004;
  private const uint WM_NCLBUTTONDBLCLK = 0x00A3;
  private const uint WM_EXITSIZEMOVE = 0x0232;
}
