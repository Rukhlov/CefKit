using CefKit.Interop;

namespace CefKit;

/// <summary>
/// A browser (cef_browser_t). In the browser process its methods may be called on any thread unless noted.
/// </summary>
public sealed unsafe class CefBrowser : CefObject
{
    internal CefBrowser(cef_browser_t* ptr)
        : base(ptr)
    {
    }

    /// <summary>
    /// Unique id of the browser in the process; wrappers of the same browser share it.
    /// </summary>
    public int Identifier
    {
        get
        {
            var self = Self;
            return self->get_identifier(self);
        }
    }

    /// <summary>
    /// The browser is not closed yet.
    /// </summary>
    public bool IsValid
    {
        get
        {
            var self = Self;
            return self->is_valid(self) != 0;
        }
    }

    /// <summary>
    /// The browser is loading.
    /// </summary>
    public bool IsLoading
    {
        get
        {
            var self = Self;
            return self->is_loading(self) != 0;
        }
    }

    /// <summary>
    /// A document has been loaded.
    /// </summary>
    public bool HasDocument
    {
        get
        {
            var self = Self;
            return self->has_document(self) != 0;
        }
    }

    private cef_browser_t* Self => (cef_browser_t*)NativePtr;

    /// <summary>
    /// The browser host, used in the browser process.
    /// </summary>
    public CefBrowserHost GetHost()
    {
        var self = Self;
        return new CefBrowserHost(self->get_host(self));
    }

    /// <summary>
    /// The main frame, or null while the browser is closing.
    /// </summary>
    public CefFrame? GetMainFrame()
    {
        var self = Self;
        return CefFrame.From(self->get_main_frame(self));
    }

    /// <summary>
    /// Reloads the page.
    /// </summary>
    public void Reload()
    {
        var self = Self;
        self->reload(self);
    }

    /// <summary>
    /// Reloads the page bypassing the cache.
    /// </summary>
    public void ReloadIgnoreCache()
    {
        var self = Self;
        self->reload_ignore_cache(self);
    }

    /// <summary>
    /// Stops loading.
    /// </summary>
    public void StopLoad()
    {
        var self = Self;
        self->stop_load(self);
    }

    /// <summary>
    /// Another reference to the same browser that stays valid after this one is disposed.
    /// </summary>
    public CefBrowser Clone() => new((cef_browser_t*)AddRefNative());

    internal static CefBrowser? From(cef_browser_t* ptr) => ptr == null ? null : new CefBrowser(ptr);
}

/// <summary>
/// A frame of a page (cef_frame_t).
/// </summary>
public sealed unsafe class CefFrame : CefObject
{
    internal CefFrame(cef_frame_t* ptr)
        : base(ptr)
    {
    }

    /// <summary>
    /// The frame still exists.
    /// </summary>
    public bool IsValid
    {
        get
        {
            var self = Self;
            return self->is_valid(self) != 0;
        }
    }

    /// <summary>
    /// The main frame of its browser.
    /// </summary>
    public bool IsMain
    {
        get
        {
            var self = Self;
            return self->is_main(self) != 0;
        }
    }

    /// <summary>
    /// URL of the current document.
    /// </summary>
    public string Url
    {
        get
        {
            var self = Self;
            return Native.CefStrings.ReadUserFree(self->get_url(self)) ?? string.Empty;
        }
    }

    private cef_frame_t* Self => (cef_frame_t*)NativePtr;

    /// <summary>
    /// Navigates the frame.
    /// </summary>
    public void LoadUrl(string url)
    {
        ArgumentNullException.ThrowIfNull(url);
        var self = Self;
        fixed (char* chars = url)
        {
            var value = Native.CefStrings.View(chars, url);
            self->load_url(self, &value);
        }
    }

    /// <summary>
    /// Runs JavaScript in the frame without waiting for a result.
    /// </summary>
    public void ExecuteJavaScript(string code, string? scriptUrl = null, int startLine = 1)
    {
        ArgumentNullException.ThrowIfNull(code);
        var self = Self;
        fixed (char* codeChars = code)
        fixed (char* urlChars = scriptUrl)
        {
            var codeValue = Native.CefStrings.View(codeChars, code);
            var urlValue = Native.CefStrings.View(urlChars, scriptUrl);
            self->execute_java_script(self, &codeValue, &urlValue, startLine);
        }
    }

    /// <summary>
    /// Another reference to the same frame that stays valid after this one is disposed.
    /// </summary>
    public CefFrame Clone() => new((cef_frame_t*)AddRefNative());

    internal static CefFrame? From(cef_frame_t* ptr) => ptr == null ? null : new CefFrame(ptr);
}
