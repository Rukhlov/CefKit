using System.Runtime.InteropServices;
using CefKit.Interop;

namespace CefKit;

/// <summary>
/// Handlers of a browser (cef_client_t). CEF asks for a handler on every event, so handlers can be set or
/// replaced at any time; a null handler means CEF's default behavior.
/// </summary>
public class CefClient : CefHandler<cef_client_t>
{
    /// <summary>
    /// Browser creation, popups and closing.
    /// </summary>
    public CefLifeSpanHandler? LifeSpanHandler { get; set; }

    /// <summary>
    /// Loading state and errors.
    /// </summary>
    public CefLoadHandler? LoadHandler { get; set; }

    /// <summary>
    /// Title, address and console messages.
    /// </summary>
    public CefDisplayHandler? DisplayHandler { get; set; }

    /// <summary>
    /// Off-screen rendering; required for windowless browsers.
    /// </summary>
    public CefRenderHandler? RenderHandler { get; set; }

    /// <summary>
    /// Navigation and render process events.
    /// </summary>
    public CefRequestHandler? RequestHandler { get; set; }

    /// <summary>
    /// JavaScript dialogs.
    /// </summary>
    public CefJsDialogHandler? JsDialogHandler { get; set; }

    /// <summary>
    /// Focus requests.
    /// </summary>
    public CefFocusHandler? FocusHandler { get; set; }

    private protected override unsafe void Fill(cef_client_t* native)
    {
        native->get_life_span_handler = &GetLifeSpanHandler;
        native->get_load_handler = &GetLoadHandler;
        native->get_display_handler = &GetDisplayHandler;
        native->get_render_handler = &GetRenderHandler;
        native->get_request_handler = &GetRequestHandler;
        native->get_jsdialog_handler = &GetJsDialogHandler;
        native->get_focus_handler = &GetFocusHandler;
    }

    /// <summary>
    /// A handler with a new reference for CEF, or null.
    /// </summary>
    private static unsafe void* Get(cef_client_t* self, Func<CefClient, CefHandler?> select)
    {
        try
        {
            var handler = select(FromNative<CefClient>(self));
            return handler is null ? null : handler.AddRefNative();
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
            return null;
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe cef_life_span_handler_t* GetLifeSpanHandler(cef_client_t* self) =>
        (cef_life_span_handler_t*)Get(self, static c => c.LifeSpanHandler);

    [UnmanagedCallersOnly]
    private static unsafe cef_load_handler_t* GetLoadHandler(cef_client_t* self) =>
        (cef_load_handler_t*)Get(self, static c => c.LoadHandler);

    [UnmanagedCallersOnly]
    private static unsafe cef_display_handler_t* GetDisplayHandler(cef_client_t* self) =>
        (cef_display_handler_t*)Get(self, static c => c.DisplayHandler);

    [UnmanagedCallersOnly]
    private static unsafe cef_render_handler_t* GetRenderHandler(cef_client_t* self) =>
        (cef_render_handler_t*)Get(self, static c => c.RenderHandler);

    [UnmanagedCallersOnly]
    private static unsafe cef_request_handler_t* GetRequestHandler(cef_client_t* self) =>
        (cef_request_handler_t*)Get(self, static c => c.RequestHandler);

    [UnmanagedCallersOnly]
    private static unsafe cef_jsdialog_handler_t* GetJsDialogHandler(cef_client_t* self) =>
        (cef_jsdialog_handler_t*)Get(self, static c => c.JsDialogHandler);

    [UnmanagedCallersOnly]
    private static unsafe cef_focus_handler_t* GetFocusHandler(cef_client_t* self) =>
        (cef_focus_handler_t*)Get(self, static c => c.FocusHandler);
}
