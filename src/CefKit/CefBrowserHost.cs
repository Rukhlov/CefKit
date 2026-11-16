using CefKit.Interop;
using CefKit.Native;

namespace CefKit;

/// <summary>
/// The browser-process side of a browser (cef_browser_host_t).
/// </summary>
public sealed unsafe class CefBrowserHost : CefObject
{
    internal CefBrowserHost(cef_browser_host_t* ptr)
        : base(ptr)
    {
    }

    /// <summary>
    /// The browser window: the child window of a windowed browser (HWND on Windows, X11 window on Linux), the
    /// parent given at creation for a windowless one.
    /// </summary>
    public nint WindowHandle
    {
        get
        {
            var self = Self;
            return self->get_window_handle(self);
        }
    }

    /// <summary>
    /// Zoom level: 0 is 100 %, each step is a factor of 1.2. Call on the UI thread.
    /// </summary>
    public double ZoomLevel
    {
        get
        {
            var self = Self;
            return self->get_zoom_level(self);
        }

        set
        {
            var self = Self;
            self->set_zoom_level(self, value);
        }
    }

    /// <summary>
    /// Audio of the browser is muted.
    /// </summary>
    public bool IsAudioMuted
    {
        get
        {
            var self = Self;
            return self->is_audio_muted(self) != 0;
        }
    }

    private cef_browser_host_t* Self => (cef_browser_host_t*)NativePtr;

    /// <summary>
    /// Creates a browser asynchronously: <see cref="CefLifeSpanHandler"/> reports it when it exists.
    /// Returns false if the request failed.
    /// </summary>
    public static bool CreateBrowser(CefWindowInfo windowInfo, CefClient client, string url,
        CefBrowserSettings? settings = null, CefRequestContext? requestContext = null)
    {
        ArgumentNullException.ThrowIfNull(windowInfo);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(url);
        var info = windowInfo.ToNative();
        var nativeSettings = default(cef_browser_settings_t);
        try
        {
            (settings ?? new CefBrowserSettings()).CopyTo(&nativeSettings);
            fixed (char* chars = url)
            {
                var urlValue = CefStrings.View(chars, url);

                // CEF takes ownership of the references to the client and the request context
                var context = requestContext is null ? null : (cef_request_context_t*)requestContext.AddRefNative();
                return Methods.cef_browser_host_create_browser(info, (cef_client_t*)client.AddRefNative(), &urlValue,
                    &nativeSettings, null, context) != 0;
            }
        }
        finally
        {
            CefWindowInfo.FreeNative(info);
        }
    }

    /// <summary>
    /// The browser of this host.
    /// </summary>
    public CefBrowser GetBrowser()
    {
        var self = Self;
        return new CefBrowser(self->get_browser(self));
    }

    /// <summary>
    /// Requests closing; with forceClose the page cannot cancel it. <see cref="CefLifeSpanHandler"/> reports the end.
    /// </summary>
    public void CloseBrowser(bool forceClose)
    {
        var self = Self;
        self->close_browser(self, forceClose ? 1 : 0);
    }

    /// <summary>
    /// The request context of the browser.
    /// </summary>
    public CefRequestContext? GetRequestContext()
    {
        var self = Self;
        var context = self->get_request_context(self);
        return context == null ? null : new CefRequestContext(context);
    }

    /// <summary>
    /// Gives or takes keyboard focus.
    /// </summary>
    public void SetFocus(bool focus)
    {
        var self = Self;
        self->set_focus(self, focus ? 1 : 0);
    }

    /// <summary>
    /// Off-screen: the view size changed, CEF asks <see cref="CefRenderHandler"/> for it again.
    /// </summary>
    public void WasResized()
    {
        var self = Self;
        self->was_resized(self);
    }

    /// <summary>
    /// Off-screen: the view was hidden or shown.
    /// </summary>
    public void WasHidden(bool hidden)
    {
        var self = Self;
        self->was_hidden(self, hidden ? 1 : 0);
    }

    /// <summary>
    /// Off-screen: the screen information (scale) changed.
    /// </summary>
    public void NotifyScreenInfoChanged()
    {
        var self = Self;
        self->notify_screen_info_changed(self);
    }

    /// <summary>
    /// Off-screen: requests a repaint.
    /// </summary>
    public void Invalidate(CefPaintElementType type = CefPaintElementType.View)
    {
        var self = Self;
        self->invalidate(self, (cef_paint_element_type_t)type);
    }

    /// <summary>
    /// Off-screen: frame rate limit, 1-60 (higher with external begin frames).
    /// </summary>
    public void SetWindowlessFrameRate(int frameRate)
    {
        var self = Self;
        self->set_windowless_frame_rate(self, frameRate);
    }

    /// <summary>
    /// Mutes or unmutes the audio of the browser.
    /// </summary>
    public void SetAudioMuted(bool muted)
    {
        var self = Self;
        self->set_audio_muted(self, muted ? 1 : 0);
    }

    /// <summary>
    /// Sends a DevTools protocol message (UTF-8 JSON with id, method and params). Call on the UI thread.
    /// Results arrive at the observers added with <see cref="AddDevToolsMessageObserver"/>.
    /// </summary>
    public bool SendDevToolsMessage(ReadOnlySpan<byte> utf8Json)
    {
        var self = Self;
        fixed (byte* message = utf8Json)
        {
            return self->send_dev_tools_message(self, message, (nuint)utf8Json.Length) != 0;
        }
    }

    /// <summary>
    /// Adds a DevTools message observer; it stays registered until the registration is disposed. Call on the UI thread.
    /// </summary>
    public CefRegistration? AddDevToolsMessageObserver(CefDevToolsMessageObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        var self = Self;
        var registration = self->add_dev_tools_message_observer(self, (cef_dev_tools_message_observer_t*)observer.AddRefNative());
        return registration == null ? null : new CefRegistration(registration);
    }
}

/// <summary>
/// A registration (cef_registration_t): disposing it unregisters.
/// </summary>
public sealed unsafe class CefRegistration : CefObject
{
    internal CefRegistration(cef_registration_t* ptr)
        : base(ptr)
    {
    }
}
