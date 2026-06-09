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
/// synthetic-input handoff to DWM for smooth compositor drag with real Aero Snap.
/// </summary>
internal sealed class WebView2TitleBarHook {
  // ── P/Invoke ─────────────────────────────────────────────────────────────

  private delegate nint WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);
  private delegate bool EnumChildProc(nint hWnd, nint lParam);

  [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }
  [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X, Y; }

  // INPUT / MOUSEINPUT layout matches Win32 on 64-bit (sizeof = 40).
  [StructLayout(LayoutKind.Sequential)]
  private struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public nint dwExtraInfo; }
  [StructLayout(LayoutKind.Sequential)]
  private struct INPUT { public uint type; public MOUSEINPUT mi; }

  [DllImport("user32.dll")] private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);
  [DllImport("user32.dll")] private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint newLong);
  [DllImport("user32.dll")] private static extern nint CallWindowProc(nint prev, nint hWnd, uint msg, nint wParam, nint lParam);
  [DllImport("user32.dll")] private static extern bool EnumChildWindows(nint parent, EnumChildProc cb, nint lParam);
  [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern int GetClassName(nint hWnd, System.Text.StringBuilder sb, int max);
  [DllImport("user32.dll")] private static extern nint GetParent(nint hWnd);
  [DllImport("user32.dll")] private static extern bool GetWindowRect(nint hWnd, ref RECT rect);
  [DllImport("user32.dll")] private static extern bool SetWindowPos(nint hWnd, nint after, int x, int y, int cx, int cy, uint flags);
  [DllImport("user32.dll")] private static extern int GetSystemMetrics(int nIndex);
  [DllImport("user32.dll")] private static extern nint SendMessage(nint hWnd, uint msg, nint wParam, nint lParam);
  [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT pt);
  [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vKey);
  [DllImport("user32.dll")] private static extern uint SendInput(uint n, INPUT[] inputs, int cbSize);

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
  private const uint WM_EXITSIZEMOVE = 0x0232;

  // ── Per-instance fields ──────────────────────────────────────────────────
  //
  // WndProcDelegate fields must be kept alive for as long as the hook is
  // installed — native code holds raw function pointers. The instance itself
  // is kept alive by TransparentWebView2._hook, which satisfies this.

  private WndProcDelegate? _parentWndProc;
  private WndProcDelegate? _wv2WndProc;
  private nint _parentOldWndProc;
  private nint _wv2OldWndProc;
  private nint _parentHwnd;
  private nint _wv2Hwnd;
  private int _captionRightInsetPx;
  private int _captionLeftInsetPx;
  private int _iconAreaLeftOffset;
  private int _titleBarHeightPx;
  private int _resizeBorderPx;
  private Windows.Graphics.RectInt32[] _dragRects = [];
  private bool _suppressNextWv2Click;
  private Action? _onDragEnd;

  private readonly InputNonClientPointerSource _inputSource;
  private readonly Func<double> _getScale;

  private WebView2TitleBarHook(InputNonClientPointerSource inputSource, Func<double> getScale) {
    _inputSource = inputSource;
    _getScale = getScale;
  }

  /// <summary>
  /// Installs all hooks. Must be called after <c>CreateCoreWebView2ControllerAsync</c>
  /// completes, because WebView2 subclasses the parent HWND during controller creation.
  /// Returns null if Chrome_WidgetWin_ could not be found as a direct child.
  /// </summary>
  public static WebView2TitleBarHook? Install(nint parentHwnd, AppWindow appWindow, Func<double> getScale) {
    var hook = new WebView2TitleBarHook(
        InputNonClientPointerSource.GetForWindowId(appWindow.Id),
        getScale);

    hook._parentHwnd = parentHwnd;

    // ── Parent WndProc ────────────────────────────────────────────────────
    hook._parentWndProc = (hWnd, msg, wParam, lParam) => {
      var result = CallWindowProc(hook._parentOldWndProc, hWnd, msg, wParam, lParam);

      if (msg == WM_EXITSIZEMOVE && hook._onDragEnd is { } onEnd) {
        hook._onDragEnd = null;
        onEnd();
      }

      if (msg == WM_NCHITTEST) {
        var screenX = (short)(lParam & 0xFFFF);
        var screenY = (short)((lParam >> 16) & 0xFFFF);
        var wr = default(RECT);
        GetWindowRect(hWnd, ref wr);
        var relX = screenX - wr.Left;
        var relY = screenY - wr.Top;
        var width = wr.Right - wr.Left;

        // Icon area → Windows natively handles left-click (show menu) and double-click (close).
        if (hook._captionLeftInsetPx > 0 && hook._titleBarHeightPx > 0 &&
            relX >= hook._iconAreaLeftOffset && relX < (hook._captionLeftInsetPx + hook._iconAreaLeftOffset) &&
            relY >= hook._resizeBorderPx && relY < hook._titleBarHeightPx) {
          return HTSYSMENU;
        }

        if (result == HTCAPTION) {
          // Keep HTCAPTION for the native caption buttons.
          if (hook._captionRightInsetPx > 0 && relX >= width - hook._captionRightInsetPx)
            return result;

          // Keep HTCAPTION for registered drag rects so WM_MOUSE dragging works.
          foreach (var r in hook._dragRects) {
            if (relX >= r.X && relX < r.X + r.Width && relY >= r.Y && relY < r.Y + r.Height)
              return result;
          }

          // When no drag rects are registered (e.g. the page still loading),
          // fall back to the default HTCAPTION result so the window remains
          // draggable.
          if (hook._dragRects.Length == 0) return result;

          // Everything else: let WebView2 receive the event.
          return HTCLIENT;
        }
      }
      return result;
    };
    hook._parentOldWndProc = SetWindowLongPtr(parentHwnd, GWLP_WNDPROC,
        Marshal.GetFunctionPointerForDelegate(hook._parentWndProc));
    Console.WriteLine($"[Hook] Parent WndProc installed  hwnd=0x{parentHwnd:X}");

    // ── InputNonClientPointerSource passthrough ───────────────────────────
    // Clear the Caption region so the input system stops intercepting WM_POINTER
    // events for the titlebar strip before WM_NCHITTEST can fire.
    hook._inputSource.SetRegionRects(NonClientRegionKind.Caption, []);

    // ── WebView2 child WndProc ────────────────────────────────────────────

    // get the handle to window (HWND) for Chrome_WidgetWin_0, which is the
    // parent of Chrome_WidgetWin_1, which is the WebView2 content host
    nint chromeWidgetWin0Hwnd = 0;
    EnumChildWindows(parentHwnd, (hwnd, _) => {
      var classNameFillableString = new System.Text.StringBuilder(128);
      _ = GetClassName(hwnd, classNameFillableString, classNameFillableString.Capacity);
      var className = classNameFillableString.ToString();

      var isDirectChild = GetParent(hwnd) == parentHwnd;
      Console.WriteLine($"[Hook]   hwnd=0x{hwnd:X}  class={className}  isChild={isDirectChild}");

      if (!isDirectChild) {
        return true;
      }

      if (className == "Chrome_WidgetWin_0") {
        chromeWidgetWin0Hwnd = hwnd;
        return false;
      }

      return true;
    }, 0);

    if (chromeWidgetWin0Hwnd == 0) {
      Console.WriteLine("[Hook] Chrome_WidgetWin_0 not found. Skipping WebView2 hook.");
      return null;
    }


    // now, get the HWND for Chrome_WidgetWin_1, which is the WebView2 content host
    nint chromeWidgetWin1Hwnd = 0;
    EnumChildWindows(parentHwnd, (hwnd, _) => {
      var classNameFillableString = new System.Text.StringBuilder(128);
      _ = GetClassName(hwnd, classNameFillableString, classNameFillableString.Capacity);
      var className = classNameFillableString.ToString();

      var isDirectGrandchild = GetParent(hwnd) == chromeWidgetWin0Hwnd;
      Console.WriteLine($"[Hook]   hwnd=0x{hwnd:X}  class={className}  isGrandChild={isDirectGrandchild}");

      if (!isDirectGrandchild) {
        return true;
      }

      if (className == "Chrome_WidgetWin_1") {
        chromeWidgetWin1Hwnd = hwnd;
        return false;
      }

      return true;
    }, 0);


    if (chromeWidgetWin1Hwnd == 0) {
      Console.WriteLine("[Hook] Chrome_WidgetWin_1 not found. Skipping WebView2 hook.");
      return null;
    }

    hook._wv2Hwnd = chromeWidgetWin1Hwnd;
    Console.WriteLine($"[Hook] Chrome_WidgetWin_ found=0x{hook._wv2Hwnd:X}");

    // Elevate above WinUI's XAML input HWND so it receives pointer events
    // in the titlebar area.
    SetWindowPos(hook._wv2Hwnd, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

    var resizePx = GetSystemMetrics(SM_CYSIZEFRAME);
    hook._resizeBorderPx = resizePx;

    hook._wv2WndProc = (hWnd, msg, wParam, lParam) => {
      if (msg == WM_NCHITTEST) {
        var screenY = (short)((lParam >> 16) & 0xFFFF);
        var screenX = (short)(lParam & 0xFFFF);
        var rect = default(RECT);
        GetWindowRect(hWnd, ref rect);
        var relY = screenY - rect.Top;
        var relX = screenX - rect.Left;
        var width = rect.Right - rect.Left;

        // One-shot: route the synthetic re-press to the parent so
        // InputNonClientPointerSource can start the DWM caption drag.
        if (hook._suppressNextWv2Click) {
          hook._suppressNextWv2Click = false;
          return HTTRANSPARENT;
        }

        // Top resize border → let parent return HTTOP.
        if (relY < resizePx) return HTTRANSPARENT;

        // Caption button strip → let parent return HTCLOSE etc.
        if (hook._captionRightInsetPx > 0 && relY < 60 && relX >= width - hook._captionRightInsetPx)
          return HTTRANSPARENT;

        // Icon area → let parent return HTSYSMENU.
        if (hook._captionLeftInsetPx > 0 && hook._titleBarHeightPx > 0 &&
            relX >= hook._iconAreaLeftOffset && relX < hook._captionLeftInsetPx && relY < hook._titleBarHeightPx) {
          return HTTRANSPARENT;
        }
      }
      return CallWindowProc(hook._wv2OldWndProc, hWnd, msg, wParam, lParam);
    };
    hook._wv2OldWndProc = SetWindowLongPtr(hook._wv2Hwnd, GWLP_WNDPROC,
        Marshal.GetFunctionPointerForDelegate(hook._wv2WndProc));
    Console.WriteLine($"[Hook] WV2 WndProc installed  hwnd=0x{hook._wv2Hwnd:X}");

    // On Windows 11, "Intermediate D3D Window" sits above Chrome_RenderWidgetHostHWND
    // in z-order (the reverse of Windows 10) and is marked WS_EX_TRANSPARENT, For
    // some reason, clearing the flag is required for clicks and other interactions to work,
    var d3dPollAttempts = 0;
    System.Threading.Timer? d3dPollTimer = null;
    d3dPollTimer = new System.Threading.Timer(_ => {
      var found = false;
      EnumChildWindows(hook._wv2Hwnd, (hwnd, _) => {
        var sb = new System.Text.StringBuilder(128);
        _ = GetClassName(hwnd, sb, sb.Capacity);
        if (sb.ToString() == "Intermediate D3D Window") {
          var exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
          SetWindowLongPtr(hwnd, GWL_EXSTYLE, exStyle & ~WS_EX_TRANSPARENT);
          Console.WriteLine($"[Hook] Cleared WS_EX_TRANSPARENT on Intermediate D3D Window  hwnd=0x{hwnd:X}");
          found = true;
          return false;
        }
        return true;
      }, 0);

      if (found || ++d3dPollAttempts >= 40) {
        if (!found) Console.WriteLine("[Hook] Intermediate D3D Window not found after polling; WS_EX_TRANSPARENT not cleared.");
        d3dPollTimer?.Dispose();
      }
    }, null, 0, 250);

    return hook;
  }

  public void Uninstall() {
    if (_wv2Hwnd != 0 && _wv2OldWndProc != 0) {
      SetWindowLongPtr(_wv2Hwnd, GWLP_WNDPROC, _wv2OldWndProc);
      _wv2OldWndProc = 0;
    }
    if (_parentHwnd != 0 && _parentOldWndProc != 0) {
      SetWindowLongPtr(_parentHwnd, GWLP_WNDPROC, _parentOldWndProc);
      _parentOldWndProc = 0;
    }
  }

  /// <summary>Re-elevates Chrome_WidgetWin_ to HWND_TOP after a resize.</summary>
  public void EnsureWv2OnTop() {
    if (_wv2Hwnd != 0)
      SetWindowPos(_wv2Hwnd, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
  }

  public void UpdateCaptionRightInset(int pixels) => _captionRightInsetPx = pixels;
  public void UpdateCaptionLeftInset(int pixels) => _captionLeftInsetPx = pixels;
  public void UpdateIconAreaLeftOffset(int pixels) => _iconAreaLeftOffset = pixels;
  public void UpdateTitleBarHeight(int pixels) => _titleBarHeightPx = pixels;

  /// <summary>
  /// Registers caption drag regions with both the WndProc hit-test and
  /// InputNonClientPointerSource.
  /// </summary>
  public void SetDragRects(Windows.Graphics.RectInt32[] rects) {
    _inputSource.SetRegionRects(NonClientRegionKind.Caption, rects);
    _dragRects = rects;
  }

  /// <summary>
  /// If the left button is currently held and the cursor is over one of the
  /// registered drag rects, fires a synthetic UP+DOWN to hand the drag off to DWM
  /// (giving smooth compositor movement and real Aero Snap). Returns true if the
  /// handoff was initiated; <paramref name="onDragEnd"/> is called on WM_EXITSIZEMOVE.
  /// </summary>
  public bool TryBeginDrag(Action onDragEnd) {
    if ((GetAsyncKeyState(0x01) & unchecked((short)0x8000)) == 0) return false;
    if (!GetCursorPos(out var pt)) return false;

    var wr = default(RECT);
    GetWindowRect(_parentHwnd, ref wr);
    var relX = pt.X - wr.Left;
    var relY = pt.Y - wr.Top;

    foreach (var r in _dragRects) {
      if (relX < r.X || relX >= r.X + r.Width || relY < r.Y || relY >= r.Y + r.Height)
        continue;

      Console.WriteLine($"[Hook] TryBeginDrag: cursor at ({relX},{relY}), handing off to DWM");
      _onDragEnd = onDragEnd;
      _suppressNextWv2Click = true;
      SendInput(2, [
          new INPUT { type = 0, mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTUP } },
                new INPUT { type = 0, mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTDOWN } },
            ], Marshal.SizeOf<INPUT>());
      return true;
    }
    return false;
  }

  // Message formats:
  //   {"type":"setDragRects","rects":[{"x":0,"y":0,"w":500,"h":32,"singleUse":false},...]}
  //   {"type":"setIconAreaLeft","left":32}
  // Coordinates are CSS pixels; scaled to physical pixels before registering.
  public void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs e) {
    var raw = e.TryGetWebMessageAsString();
    try {
      using var doc = JsonDocument.Parse(raw);
      var root = doc.RootElement;
      var type = root.GetProperty("type").GetString();

      if (type == "setIconAreaLeftOffset") {
        var offset = (int)(root.GetProperty("offset").GetDouble() * _getScale());
        UpdateIconAreaLeftOffset(offset);
        Console.WriteLine($"[IconArea] Left offset set to {offset}px");
        return;
      }

      if (type != "setDragRects") return;

      var scale = _getScale();
      var rects = new System.Collections.Generic.List<Windows.Graphics.RectInt32>();
      foreach (var r in root.GetProperty("rects").EnumerateArray()) {
        rects.Add(new Windows.Graphics.RectInt32(
            (int)(r.GetProperty("x").GetDouble() * scale),
            (int)(r.GetProperty("y").GetDouble() * scale),
            (int)(r.GetProperty("w").GetDouble() * scale),
            (int)(r.GetProperty("h").GetDouble() * scale)));
      }

      SetDragRects(rects.ToArray());
      Console.WriteLine($"[DragRects] {rects.Count} Caption region(s) registered");

      // Releases rects marked singleUse=true once the current drag ends.
      var releaseSingleUseRects = () => {
        using var doc2 = JsonDocument.Parse(raw);
        var root2 = doc2.RootElement;
        var remaining = new System.Collections.Generic.List<Windows.Graphics.RectInt32>();
        foreach (var r in root2.GetProperty("rects").EnumerateArray()) {
          if (!r.TryGetProperty("singleUse", out var su) || !su.GetBoolean()) {
            remaining.Add(new Windows.Graphics.RectInt32(
                (int)(r.GetProperty("x").GetDouble() * scale),
                (int)(r.GetProperty("y").GetDouble() * scale),
                (int)(r.GetProperty("w").GetDouble() * scale),
                (int)(r.GetProperty("h").GetDouble() * scale)));
          }
        }
        SetDragRects(remaining.ToArray());
        sender.PostWebMessageAsJson($"{{\"type\":\"dragRectsCleaned\",\"remaining\":{remaining.Count}}}");
        Console.WriteLine($"[DragRects] Released single-use rects, {remaining.Count} remain");
      };

      if (!TryBeginDrag(releaseSingleUseRects))
        releaseSingleUseRects();
    }
    catch (Exception ex) {
      Console.WriteLine($"[DragRects] Error: {ex.Message}  raw='{raw}'");
    }
  }
}
