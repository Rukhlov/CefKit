using System.Runtime.InteropServices;
using CefKit.Interop;
using CefKit.Native;

namespace CefKit;

/// <summary>
/// DevTools protocol messages of a browser (cef_dev_tools_message_observer_t). Called on the UI thread.
/// </summary>
public abstract class CefDevToolsMessageObserver : CefHandler<cef_dev_tools_message_observer_t>
{
    /// <summary>
    /// A raw message (UTF-8 JSON). Return true when handled: then neither of the other methods is called for it.
    /// </summary>
    protected virtual bool OnDevToolsMessage(CefBrowser browser, ReadOnlySpan<byte> message) => false;

    /// <summary>
    /// Result of a method call: the "result" or the "error" dictionary as UTF-8 JSON.
    /// </summary>
    protected virtual void OnDevToolsMethodResult(CefBrowser browser, int messageId, bool success, ReadOnlySpan<byte> result)
    {
    }

    /// <summary>
    /// An event with its "params" dictionary as UTF-8 JSON.
    /// </summary>
    protected virtual void OnDevToolsEvent(CefBrowser browser, string method, ReadOnlySpan<byte> parameters)
    {
    }

    private protected override unsafe void Fill(cef_dev_tools_message_observer_t* native)
    {
        native->on_dev_tools_message = &Message;
        native->on_dev_tools_method_result = &MethodResult;
        native->on_dev_tools_event = &Event;
    }

    [UnmanagedCallersOnly]
    private static unsafe int Message(cef_dev_tools_message_observer_t* self, cef_browser_t* browser, void* message, nuint size)
    {
        try
        {
            using var b = CefBrowser.From(browser);
            return b is not null &&
                FromNative<CefDevToolsMessageObserver>(self).OnDevToolsMessage(b, new ReadOnlySpan<byte>(message, checked((int)size))) ? 1 : 0;
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
            return 0;
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe void MethodResult(cef_dev_tools_message_observer_t* self, cef_browser_t* browser, int messageId, int success,
        void* result, nuint size)
    {
        try
        {
            using var b = CefBrowser.From(browser);
            if (b is not null)
            {
                FromNative<CefDevToolsMessageObserver>(self).OnDevToolsMethodResult(b, messageId, success != 0,
                    new ReadOnlySpan<byte>(result, checked((int)size)));
            }
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe void Event(cef_dev_tools_message_observer_t* self, cef_browser_t* browser, cef_string_utf16_t* method, void* parameters,
        nuint size)
    {
        try
        {
            using var b = CefBrowser.From(browser);
            if (b is not null)
            {
                FromNative<CefDevToolsMessageObserver>(self).OnDevToolsEvent(b, CefStrings.Read(method) ?? string.Empty,
                    new ReadOnlySpan<byte>(parameters, checked((int)size)));
            }
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
        }
    }
}
