using System.Runtime.InteropServices;
using CefKit.Interop;
using CefKit.Native;

namespace CefKit;

/// <summary>
/// How a browser is shown (cef_window_info_t): a child window of a native window or windowless (off-screen).
/// </summary>
public sealed class CefWindowInfo
{
    private const uint WsChild = 0x40000000;
    private const uint WsVisible = 0x10000000;
    private const uint WsClipSiblings = 0x04000000;
    private const uint WsClipChildren = 0x02000000;
    private const uint WsTabStop = 0x00010000;

    /// <summary>
    /// Parent window: HWND on Windows, X11 window on Linux.
    /// </summary>
    public nint ParentWindow { get; set; }

    /// <summary>
    /// Bounds of a windowed browser in its parent.
    /// </summary>
    public CefRect Bounds { get; set; }

    /// <summary>
    /// Off-screen rendering: frames go to <see cref="CefRenderHandler"/>.
    /// </summary>
    public bool Windowless { get; set; }

    /// <summary>
    /// Off-screen frames as GPU textures (<see cref="CefRenderHandler"/> accelerated paint) instead of CPU buffers.
    /// They need GPU compositing: without it (GPU disabled or blocklisted) the frames come as CPU buffers. On Linux
    /// they are dma-buf planes and need a GBM device as well: without one (e.g. WSL) no frames come at all while GPU
    /// compositing is on, so recreate the browser without shared textures when no accelerated paint arrives.
    /// </summary>
    public bool SharedTextureEnabled { get; set; }

    /// <summary>
    /// Off-screen: frames are produced on request (<c>send_external_begin_frame</c>) instead of a timer.
    /// </summary>
    public bool ExternalBeginFrameEnabled { get; set; }

    /// <summary>
    /// Alloy or Chrome style; windowless browsers are always Alloy.
    /// </summary>
    public CefRuntimeStyle RuntimeStyle { get; set; }

    /// <summary>
    /// Title of a top-level browser window.
    /// </summary>
    public string? WindowName { get; set; }

    /// <summary>
    /// Windows only: window style of the browser window (WS_*).
    /// </summary>
    public uint WindowsStyle { get; set; }

    /// <summary>
    /// Windows only: extended window style (WS_EX_*), e.g. WS_EX_NOACTIVATE.
    /// </summary>
    public uint WindowsExStyle { get; set; }

    /// <summary>
    /// A child window of the parent with the given bounds.
    /// </summary>
    public static CefWindowInfo Child(nint parent, CefRect bounds) => new()
    {
        ParentWindow = parent,
        Bounds = bounds,
        WindowsStyle = WsChild | WsClipChildren | WsClipSiblings | WsTabStop | WsVisible,
    };

    /// <summary>
    /// A windowless browser; the parent, if any, is used for screen information and dialogs.
    /// </summary>
    public static CefWindowInfo Offscreen(nint parent = 0, bool sharedTexture = false) => new()
    {
        ParentWindow = parent,
        Windowless = true,
        SharedTextureEnabled = sharedTexture,
        RuntimeStyle = CefRuntimeStyle.Alloy,
    };

    /// <summary>
    /// The platform cef_window_info_t in native memory; free it with <see cref="FreeNative"/>.
    /// </summary>
    internal unsafe void* ToNative()
    {
        if (OperatingSystem.IsWindows())
        {
            var info = (Interop.Windows.cef_window_info_t*)NativeMemory.AllocZeroed((nuint)sizeof(Interop.Windows.cef_window_info_t));
            info->size = (nuint)sizeof(Interop.Windows.cef_window_info_t);
            info->ex_style = WindowsExStyle;
            CefStrings.Set(&info->window_name, WindowName);
            info->style = WindowsStyle;
            info->bounds = Bounds.ToNative();
            info->parent_window = ParentWindow;
            info->windowless_rendering_enabled = Windowless ? 1 : 0;
            info->shared_texture_enabled = SharedTextureEnabled ? 1 : 0;
            info->external_begin_frame_enabled = ExternalBeginFrameEnabled ? 1 : 0;
            info->runtime_style = (cef_runtime_style_t)RuntimeStyle;
            return info;
        }

        if (OperatingSystem.IsLinux())
        {
            var info = (Interop.Linux.cef_window_info_t*)NativeMemory.AllocZeroed((nuint)sizeof(Interop.Linux.cef_window_info_t));
            info->size = (nuint)sizeof(Interop.Linux.cef_window_info_t);
            CefStrings.Set(&info->window_name, WindowName);
            info->bounds = Bounds.ToNative();
            info->parent_window = ParentWindow;
            info->windowless_rendering_enabled = Windowless ? 1 : 0;
            info->shared_texture_enabled = SharedTextureEnabled ? 1 : 0;
            info->external_begin_frame_enabled = ExternalBeginFrameEnabled ? 1 : 0;
            info->runtime_style = (cef_runtime_style_t)RuntimeStyle;
            return info;
        }

        throw new PlatformNotSupportedException("CefKit supports Windows and Linux");
    }

    /// <summary>
    /// Frees a struct made by <see cref="ToNative"/>.
    /// </summary>
    internal static unsafe void FreeNative(void* info)
    {
        if (info == null)
        {
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            CefStrings.Clear(&((Interop.Windows.cef_window_info_t*)info)->window_name);
        }
        else
        {
            CefStrings.Clear(&((Interop.Linux.cef_window_info_t*)info)->window_name);
        }

        NativeMemory.Free(info);
    }
}

/// <summary>
/// Settings of a browser (cef_browser_settings_t). Unset values keep CEF's defaults.
/// </summary>
public sealed class CefBrowserSettings
{
    /// <summary>
    /// Off-screen frame rate, 1-60; 0 keeps the default of 30.
    /// </summary>
    public int WindowlessFrameRate { get; set; }

    /// <summary>
    /// Background color (ARGB) behind the page; 0 keeps the default.
    /// </summary>
    public uint BackgroundColor { get; set; }

    internal unsafe void CopyTo(cef_browser_settings_t* settings)
    {
        settings->size = (nuint)sizeof(cef_browser_settings_t);
        settings->windowless_frame_rate = WindowlessFrameRate;
        settings->background_color = BackgroundColor;
    }
}
