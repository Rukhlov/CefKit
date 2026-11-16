using System.Runtime.InteropServices;
using CefKit.Interop;
using CefKit.Native;

namespace CefKit;

/// <summary>
/// Browser life span (cef_life_span_handler_t): creation, popups, closing. Called on the UI thread.
/// </summary>
public class CefLifeSpanHandler : CefHandler<cef_life_span_handler_t>
{
    /// <summary>
    /// A page wants a popup. Return true to cancel it; false lets CEF create it.
    /// </summary>
    protected virtual bool OnBeforePopup(CefBrowser browser, CefFrame frame, string targetUrl, string targetFrameName, bool userGesture) => false;

    /// <summary>
    /// The browser exists; the first time a browser can be used.
    /// </summary>
    protected virtual void OnAfterCreated(CefBrowser browser)
    {
    }

    /// <summary>
    /// Alloy style: closing reached the top-level window. Return true to close the window yourself, false to let
    /// CEF send it the platform close message.
    /// </summary>
    protected virtual bool DoClose(CefBrowser browser) => false;

    /// <summary>
    /// The browser is about to be destroyed: release every reference to it here.
    /// </summary>
    protected virtual void OnBeforeClose(CefBrowser browser)
    {
    }

    private protected override unsafe void Fill(cef_life_span_handler_t* native)
    {
        native->on_before_popup = &BeforePopup;
        native->on_after_created = &AfterCreated;
        native->do_close = &Close;
        native->on_before_close = &BeforeClose;
    }

    [UnmanagedCallersOnly]
    private static unsafe int BeforePopup(cef_life_span_handler_t* self, cef_browser_t* browser, cef_frame_t* frame, int popupId,
        cef_string_utf16_t* targetUrl, cef_string_utf16_t* targetFrameName, cef_window_open_disposition_t disposition, int userGesture,
        cef_popup_features_t* features, void* windowInfo, cef_client_t** client, cef_browser_settings_t* settings,
        cef_dictionary_value_t** extraInfo, int* noJavascriptAccess)
    {
        try
        {
            using var b = CefBrowser.From(browser);
            using var f = CefFrame.From(frame);
            return b is not null && f is not null && FromNative<CefLifeSpanHandler>(self).OnBeforePopup(b, f,
                CefStrings.Read(targetUrl) ?? string.Empty, CefStrings.Read(targetFrameName) ?? string.Empty, userGesture != 0) ? 1 : 0;
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
            return 1;
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe void AfterCreated(cef_life_span_handler_t* self, cef_browser_t* browser)
    {
        try
        {
            using var b = CefBrowser.From(browser);
            if (b is not null)
            {
                FromNative<CefLifeSpanHandler>(self).OnAfterCreated(b);
            }
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe int Close(cef_life_span_handler_t* self, cef_browser_t* browser)
    {
        try
        {
            using var b = CefBrowser.From(browser);
            return b is not null && FromNative<CefLifeSpanHandler>(self).DoClose(b) ? 1 : 0;
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
            return 0;
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe void BeforeClose(cef_life_span_handler_t* self, cef_browser_t* browser)
    {
        try
        {
            using var b = CefBrowser.From(browser);
            if (b is not null)
            {
                FromNative<CefLifeSpanHandler>(self).OnBeforeClose(b);
            }
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
        }
    }
}

/// <summary>
/// Loading (cef_load_handler_t). Called on the UI thread.
/// </summary>
public class CefLoadHandler : CefHandler<cef_load_handler_t>
{
    /// <summary>
    /// Loading started or stopped: the whole page with all frames.
    /// </summary>
    protected virtual void OnLoadingStateChange(CefBrowser browser, bool isLoading, bool canGoBack, bool canGoForward)
    {
    }

    /// <summary>
    /// A frame committed a navigation and starts loading the document.
    /// </summary>
    protected virtual void OnLoadStart(CefBrowser browser, CefFrame frame, int transitionType)
    {
    }

    /// <summary>
    /// A frame finished loading; the HTTP status of the document, 0 for non-HTTP loads.
    /// </summary>
    protected virtual void OnLoadEnd(CefBrowser browser, CefFrame frame, int httpStatusCode)
    {
    }

    /// <summary>
    /// A navigation failed or was canceled; errorCode is a Chromium net error (cef_errorcode_t).
    /// </summary>
    protected virtual void OnLoadError(CefBrowser browser, CefFrame frame, int errorCode, string errorText, string failedUrl)
    {
    }

    private protected override unsafe void Fill(cef_load_handler_t* native)
    {
        native->on_loading_state_change = &LoadingStateChange;
        native->on_load_start = &LoadStart;
        native->on_load_end = &LoadEnd;
        native->on_load_error = &LoadError;
    }

    [UnmanagedCallersOnly]
    private static unsafe void LoadingStateChange(cef_load_handler_t* self, cef_browser_t* browser, int isLoading, int canGoBack, int canGoForward)
    {
        try
        {
            using var b = CefBrowser.From(browser);
            if (b is not null)
            {
                FromNative<CefLoadHandler>(self).OnLoadingStateChange(b, isLoading != 0, canGoBack != 0, canGoForward != 0);
            }
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe void LoadStart(cef_load_handler_t* self, cef_browser_t* browser, cef_frame_t* frame, cef_transition_type_t transitionType)
    {
        try
        {
            using var b = CefBrowser.From(browser);
            using var f = CefFrame.From(frame);
            if (b is not null && f is not null)
            {
                FromNative<CefLoadHandler>(self).OnLoadStart(b, f, (int)transitionType);
            }
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe void LoadEnd(cef_load_handler_t* self, cef_browser_t* browser, cef_frame_t* frame, int httpStatusCode)
    {
        try
        {
            using var b = CefBrowser.From(browser);
            using var f = CefFrame.From(frame);
            if (b is not null && f is not null)
            {
                FromNative<CefLoadHandler>(self).OnLoadEnd(b, f, httpStatusCode);
            }
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe void LoadError(cef_load_handler_t* self, cef_browser_t* browser, cef_frame_t* frame, cef_errorcode_t errorCode,
        cef_string_utf16_t* errorText, cef_string_utf16_t* failedUrl)
    {
        try
        {
            using var b = CefBrowser.From(browser);
            using var f = CefFrame.From(frame);
            if (b is not null && f is not null)
            {
                FromNative<CefLoadHandler>(self).OnLoadError(b, f, (int)errorCode, CefStrings.Read(errorText) ?? string.Empty,
                    CefStrings.Read(failedUrl) ?? string.Empty);
            }
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
        }
    }
}

/// <summary>
/// Page display state (cef_display_handler_t). Called on the UI thread.
/// </summary>
public class CefDisplayHandler : CefHandler<cef_display_handler_t>
{
    /// <summary>
    /// The URL of a frame changed.
    /// </summary>
    protected virtual void OnAddressChange(CefBrowser browser, CefFrame frame, string url)
    {
    }

    /// <summary>
    /// The page title changed.
    /// </summary>
    protected virtual void OnTitleChange(CefBrowser browser, string title)
    {
    }

    /// <summary>
    /// A console message. Return true to stop CEF from logging it.
    /// </summary>
    protected virtual bool OnConsoleMessage(CefBrowser browser, CefLogSeverity level, string message, string source, int line) => false;

    private protected override unsafe void Fill(cef_display_handler_t* native)
    {
        native->on_address_change = &AddressChange;
        native->on_title_change = &TitleChange;
        native->on_console_message = &ConsoleMessage;
    }

    [UnmanagedCallersOnly]
    private static unsafe void AddressChange(cef_display_handler_t* self, cef_browser_t* browser, cef_frame_t* frame, cef_string_utf16_t* url)
    {
        try
        {
            using var b = CefBrowser.From(browser);
            using var f = CefFrame.From(frame);
            if (b is not null && f is not null)
            {
                FromNative<CefDisplayHandler>(self).OnAddressChange(b, f, CefStrings.Read(url) ?? string.Empty);
            }
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe void TitleChange(cef_display_handler_t* self, cef_browser_t* browser, cef_string_utf16_t* title)
    {
        try
        {
            using var b = CefBrowser.From(browser);
            if (b is not null)
            {
                FromNative<CefDisplayHandler>(self).OnTitleChange(b, CefStrings.Read(title) ?? string.Empty);
            }
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe int ConsoleMessage(cef_display_handler_t* self, cef_browser_t* browser, cef_log_severity_t level,
        cef_string_utf16_t* message, cef_string_utf16_t* source, int line)
    {
        try
        {
            using var b = CefBrowser.From(browser);
            return b is not null && FromNative<CefDisplayHandler>(self).OnConsoleMessage(b, (CefLogSeverity)level,
                CefStrings.Read(message) ?? string.Empty, CefStrings.Read(source) ?? string.Empty, line) ? 1 : 0;
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
            return 0;
        }
    }
}

/// <summary>
/// Navigation and render process events (cef_request_handler_t).
/// </summary>
public class CefRequestHandler : CefHandler<cef_request_handler_t>
{
    /// <summary>
    /// The render process of the browser ended: crashed, killed or out of memory. Called on the UI thread.
    /// </summary>
    protected virtual void OnRenderProcessTerminated(CefBrowser browser, CefTerminationStatus status, int errorCode, string errorString)
    {
    }

    private protected override unsafe void Fill(cef_request_handler_t* native) =>
        native->on_render_process_terminated = &RenderProcessTerminated;

    [UnmanagedCallersOnly]
    private static unsafe void RenderProcessTerminated(cef_request_handler_t* self, cef_browser_t* browser, cef_termination_status_t status,
        int errorCode, cef_string_utf16_t* errorString)
    {
        try
        {
            using var b = CefBrowser.From(browser);
            if (b is not null)
            {
                FromNative<CefRequestHandler>(self).OnRenderProcessTerminated(b, (CefTerminationStatus)status, errorCode,
                    CefStrings.Read(errorString) ?? string.Empty);
            }
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
        }
    }
}

/// <summary>
/// JavaScript dialogs (cef_jsdialog_handler_t). Called on the UI thread.
/// </summary>
public class CefJsDialogHandler : CefHandler<cef_jsdialog_handler_t>
{
    /// <summary>
    /// alert(), confirm() or prompt(). Return true when the callback is (or will be) answered; false with
    /// suppressMessage = true answers "cancel" silently; false otherwise shows CEF's default dialog.
    /// </summary>
    protected virtual bool OnJsDialog(CefBrowser browser, string originUrl, CefJsDialogType type, string messageText,
        string defaultPromptText, CefJsDialogCallback callback, ref bool suppressMessage) => false;

    /// <summary>
    /// A page asks to confirm leaving it. Return true when the callback is (or will be) answered.
    /// </summary>
    protected virtual bool OnBeforeUnloadDialog(CefBrowser browser, string messageText, bool isReload, CefJsDialogCallback callback) => false;

    private protected override unsafe void Fill(cef_jsdialog_handler_t* native)
    {
        native->on_jsdialog = &JsDialog;
        native->on_before_unload_dialog = &BeforeUnloadDialog;
    }

    [UnmanagedCallersOnly]
    private static unsafe int JsDialog(cef_jsdialog_handler_t* self, cef_browser_t* browser, cef_string_utf16_t* originUrl, cef_jsdialog_type_t type,
        cef_string_utf16_t* messageText, cef_string_utf16_t* defaultPromptText, cef_jsdialog_callback_t* callback, int* suppressMessage)
    {
        try
        {
            using var b = CefBrowser.From(browser);
            using var c = CefJsDialogCallback.From(callback);
            if (b is null || c is null)
            {
                return 0;
            }

            var suppress = *suppressMessage != 0;
            var handled = FromNative<CefJsDialogHandler>(self).OnJsDialog(b, CefStrings.Read(originUrl) ?? string.Empty, (CefJsDialogType)type,
                CefStrings.Read(messageText) ?? string.Empty, CefStrings.Read(defaultPromptText) ?? string.Empty, c, ref suppress);
            *suppressMessage = suppress ? 1 : 0;
            return handled ? 1 : 0;
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
            return 0;
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe int BeforeUnloadDialog(cef_jsdialog_handler_t* self, cef_browser_t* browser, cef_string_utf16_t* messageText, int isReload,
        cef_jsdialog_callback_t* callback)
    {
        try
        {
            using var b = CefBrowser.From(browser);
            using var c = CefJsDialogCallback.From(callback);
            return b is not null && c is not null &&
                FromNative<CefJsDialogHandler>(self).OnBeforeUnloadDialog(b, CefStrings.Read(messageText) ?? string.Empty, isReload != 0, c) ? 1 : 0;
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
            return 0;
        }
    }
}

/// <summary>
/// Focus (cef_focus_handler_t). Called on the UI thread.
/// </summary>
public class CefFocusHandler : CefHandler<cef_focus_handler_t>
{
    /// <summary>
    /// The browser wants focus. Return true to refuse it.
    /// </summary>
    protected virtual bool OnSetFocus(CefBrowser browser, CefFocusSource source) => false;

    private protected override unsafe void Fill(cef_focus_handler_t* native) => native->on_set_focus = &SetFocus;

    [UnmanagedCallersOnly]
    private static unsafe int SetFocus(cef_focus_handler_t* self, cef_browser_t* browser, cef_focus_source_t source)
    {
        try
        {
            using var b = CefBrowser.From(browser);
            return b is not null && FromNative<CefFocusHandler>(self).OnSetFocus(b, (CefFocusSource)source) ? 1 : 0;
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
            return 0;
        }
    }
}
