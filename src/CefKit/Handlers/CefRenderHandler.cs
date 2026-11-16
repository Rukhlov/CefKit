using System.Runtime.InteropServices;
using CefKit.Interop;

namespace CefKit;

/// <summary>
/// Off-screen rendering (cef_render_handler_t) of a windowless browser. Called on the UI thread.
/// </summary>
public abstract class CefRenderHandler : CefHandler<cef_render_handler_t>
{
    /// <summary>
    /// The view rectangle in DIP relative to the screen; width and height must be positive.
    /// </summary>
    protected abstract CefRect GetViewRect(CefBrowser browser);

    /// <summary>
    /// Screen information, chiefly the device scale factor. Return false to keep CEF's defaults.
    /// </summary>
    protected virtual bool GetScreenInfo(CefBrowser browser, ref CefScreenInfo screenInfo) => false;

    /// <summary>
    /// A frame in CPU memory: BGRA, width × height pixels with an upper-left origin; valid during the call only.
    /// </summary>
    protected virtual void OnPaint(CefBrowser browser, CefPaintElementType type, ReadOnlySpan<CefRect> dirtyRects, nint buffer, int width, int height)
    {
    }

    /// <summary>
    /// A frame as a GPU texture (shared texture mode); the texture is valid during the call only and must be
    /// copied to a texture of the application.
    /// </summary>
    protected virtual void OnAcceleratedPaint(CefBrowser browser, CefPaintElementType type, ReadOnlySpan<CefRect> dirtyRects, in CefAcceleratedPaintInfo info)
    {
    }

    private protected override unsafe void Fill(cef_render_handler_t* native)
    {
        native->get_view_rect = &ViewRect;
        native->get_screen_info = &ScreenInfo;
        native->on_paint = &Paint;
        native->on_accelerated_paint = &AcceleratedPaint;
    }

    [UnmanagedCallersOnly]
    private static unsafe void ViewRect(cef_render_handler_t* self, cef_browser_t* browser, cef_rect_t* rect)
    {
        try
        {
            using var b = CefBrowser.From(browser);
            var view = b is null ? new CefRect(0, 0, 1, 1) : FromNative<CefRenderHandler>(self).GetViewRect(b);
            *rect = view.ToNative();
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
            *rect = new cef_rect_t { width = 1, height = 1 };
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe int ScreenInfo(cef_render_handler_t* self, cef_browser_t* browser, cef_screen_info_t* info)
    {
        try
        {
            using var b = CefBrowser.From(browser);
            if (b is null)
            {
                return 0;
            }

            var screen = new CefScreenInfo
            {
                DeviceScaleFactor = info->device_scale_factor,
                Depth = info->depth,
                DepthPerComponent = info->depth_per_component,
                IsMonochrome = info->is_monochrome != 0,
                Rect = CefRect.FromNative(info->rect),
                AvailableRect = CefRect.FromNative(info->available_rect),
            };
            if (!FromNative<CefRenderHandler>(self).GetScreenInfo(b, ref screen))
            {
                return 0;
            }

            info->device_scale_factor = screen.DeviceScaleFactor;
            info->depth = screen.Depth;
            info->depth_per_component = screen.DepthPerComponent;
            info->is_monochrome = screen.IsMonochrome ? 1 : 0;
            info->rect = screen.Rect.ToNative();
            info->available_rect = screen.AvailableRect.ToNative();
            return 1;
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
            return 0;
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe void Paint(cef_render_handler_t* self, cef_browser_t* browser, cef_paint_element_type_t type, nuint dirtyRectsCount,
        cef_rect_t* dirtyRects, void* buffer, int width, int height)
    {
        try
        {
            using var b = CefBrowser.From(browser);
            if (b is not null)
            {
                var rects = MemoryMarshal.Cast<cef_rect_t, CefRect>(new ReadOnlySpan<cef_rect_t>(dirtyRects, (int)dirtyRectsCount));
                FromNative<CefRenderHandler>(self).OnPaint(b, (CefPaintElementType)type, rects, (nint)buffer, width, height);
            }
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe void AcceleratedPaint(cef_render_handler_t* self, cef_browser_t* browser, cef_paint_element_type_t type,
        nuint dirtyRectsCount, cef_rect_t* dirtyRects, void* info)
    {
        try
        {
            using var b = CefBrowser.From(browser);
            if (b is not null && info != null)
            {
                var rects = MemoryMarshal.Cast<cef_rect_t, CefRect>(new ReadOnlySpan<cef_rect_t>(dirtyRects, (int)dirtyRectsCount));
                var paint = new CefAcceleratedPaintInfo(info);
                FromNative<CefRenderHandler>(self).OnAcceleratedPaint(b, (CefPaintElementType)type, rects, in paint);
            }
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
        }
    }
}

/// <summary>
/// A frame as a GPU resource (cef_accelerated_paint_info_t): a D3D11 texture handle on Windows, dma-buf planes
/// on Linux. Valid during the paint call only.
/// </summary>
public readonly unsafe ref struct CefAcceleratedPaintInfo
{
    private readonly void* _native;

    internal CefAcceleratedPaintInfo(void* native) => _native = native;

    /// <summary>
    /// Windows: NT handle of a D3D11 texture (ID3D11Device1::OpenSharedResource1); 0 on other platforms.
    /// </summary>
    public nint SharedTextureHandle =>
        OperatingSystem.IsWindows() ? ((Interop.Windows.cef_accelerated_paint_info_t*)_native)->shared_texture_handle : 0;

    /// <summary>
    /// Pixel format of the texture.
    /// </summary>
    public CefColorType Format => (CefColorType)(OperatingSystem.IsWindows()
        ? ((Interop.Windows.cef_accelerated_paint_info_t*)_native)->format
        : ((Interop.Linux.cef_accelerated_paint_info_t*)_native)->format);

    /// <summary>
    /// Linux: planes of the dma-buf (file descriptor, stride, offset, size); empty on other platforms.
    /// </summary>
    public ReadOnlySpan<CefNativePixmapPlane> Planes
    {
        get
        {
            if (!OperatingSystem.IsLinux())
            {
                return default;
            }

            var info = (Interop.Linux.cef_accelerated_paint_info_t*)_native;
            var count = Math.Clamp(info->plane_count, 0, 4);
            return MemoryMarshal.Cast<Interop.Linux.cef_accelerated_paint_native_pixmap_plane_t, CefNativePixmapPlane>(
                new ReadOnlySpan<Interop.Linux.cef_accelerated_paint_native_pixmap_plane_t>(&info->planes, count));
        }
    }

    /// <summary>
    /// Linux: DRM format modifier of the dma-buf; 0 on other platforms.
    /// </summary>
    public ulong Modifier => OperatingSystem.IsLinux() ? ((Interop.Linux.cef_accelerated_paint_info_t*)_native)->modifier : 0;

    /// <summary>
    /// Presentation timestamp of the frame in microseconds.
    /// </summary>
    public ulong Timestamp => Extra->timestamp;

    /// <summary>
    /// Full size of the texture in pixels.
    /// </summary>
    public (int Width, int Height) CodedSize => (Extra->coded_size.width, Extra->coded_size.height);

    /// <summary>
    /// The part of the texture with the frame in pixels.
    /// </summary>
    public CefRect VisibleRect => CefRect.FromNative(Extra->visible_rect);

    private cef_accelerated_paint_info_common_t* Extra => OperatingSystem.IsWindows()
        ? &((Interop.Windows.cef_accelerated_paint_info_t*)_native)->extra
        : &((Interop.Linux.cef_accelerated_paint_info_t*)_native)->extra;
}

/// <summary>
/// A plane of a Linux dma-buf; same layout as cef_accelerated_paint_native_pixmap_plane_t.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct CefNativePixmapPlane
{
    /// <summary>
    /// Bytes per row.
    /// </summary>
    public readonly uint Stride;

    /// <summary>
    /// Offset of the plane in the buffer.
    /// </summary>
    public readonly ulong Offset;

    /// <summary>
    /// Size of the plane in bytes.
    /// </summary>
    public readonly ulong Size;

    /// <summary>
    /// File descriptor of the buffer, owned by CEF.
    /// </summary>
    public readonly int Fd;
}
