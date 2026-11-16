using CefKit.Interop;
using CefKit.Native;

namespace CefKit;

/// <summary>
/// A request context (cef_request_context_t): cookies, cache and storage shared by the browsers that use it.
/// </summary>
public sealed unsafe class CefRequestContext : CefObject
{
    internal CefRequestContext(cef_request_context_t* ptr)
        : base(ptr)
    {
    }

    /// <summary>
    /// The global context configured by <see cref="CefSettings"/>.
    /// </summary>
    public static CefRequestContext GetGlobalContext() => new(Methods.cef_request_context_get_global_context());

    /// <summary>
    /// A new context: in memory with an empty cache path, on disk otherwise (a child of the root cache path).
    /// </summary>
    public static CefRequestContext CreateContext(CefRequestContextSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var native = default(cef_request_context_settings_t);
        native.size = (nuint)sizeof(cef_request_context_settings_t);
        try
        {
            CefStrings.Set(&native.cache_path, settings.CachePath);
            native.persist_session_cookies = settings.PersistSessionCookies ? 1 : 0;
            CefStrings.Set(&native.accept_language_list, settings.AcceptLanguageList);
            return new CefRequestContext(Methods.cef_request_context_create_context(&native, null));
        }
        finally
        {
            CefStrings.Clear(&native.cache_path);
            CefStrings.Clear(&native.accept_language_list);
        }
    }
}

/// <summary>
/// Settings of <see cref="CefRequestContext.CreateContext"/>.
/// </summary>
public sealed class CefRequestContextSettings
{
    /// <summary>
    /// Cache directory, a child of the root cache path; empty or null keeps everything in memory.
    /// </summary>
    public string? CachePath { get; set; }

    /// <summary>
    /// Keeps session cookies on disk.
    /// </summary>
    public bool PersistSessionCookies { get; set; }

    /// <summary>
    /// Accept-Language header, comma-separated.
    /// </summary>
    public string? AcceptLanguageList { get; set; }
}

/// <summary>
/// A command line (cef_command_line_t): switches of the process or of a child process.
/// </summary>
public sealed unsafe class CefCommandLine : CefObject
{
    internal CefCommandLine(cef_command_line_t* ptr)
        : base(ptr)
    {
    }

    /// <summary>
    /// The whole command line.
    /// </summary>
    public string CommandLineString
    {
        get
        {
            var self = Self;
            return CefStrings.ReadUserFree(self->get_command_line_string(self)) ?? string.Empty;
        }
    }

    private cef_command_line_t* Self => (cef_command_line_t*)NativePtr;

    /// <summary>
    /// The switch is present.
    /// </summary>
    public bool HasSwitch(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var self = Self;
        fixed (char* chars = name)
        {
            var value = CefStrings.View(chars, name);
            return self->has_switch(self, &value) != 0;
        }
    }

    /// <summary>
    /// Value of a switch, empty if it has none.
    /// </summary>
    public string GetSwitchValue(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var self = Self;
        fixed (char* chars = name)
        {
            var value = CefStrings.View(chars, name);
            return CefStrings.ReadUserFree(self->get_switch_value(self, &value)) ?? string.Empty;
        }
    }

    /// <summary>
    /// Adds a switch without a value.
    /// </summary>
    public void AppendSwitch(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var self = Self;
        fixed (char* chars = name)
        {
            var value = CefStrings.View(chars, name);
            self->append_switch(self, &value);
        }
    }

    /// <summary>
    /// Adds a switch with a value.
    /// </summary>
    public void AppendSwitch(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);
        var self = Self;
        fixed (char* nameChars = name)
        fixed (char* valueChars = value)
        {
            var nameValue = CefStrings.View(nameChars, name);
            var switchValue = CefStrings.View(valueChars, value);
            self->append_switch_with_value(self, &nameValue, &switchValue);
        }
    }

    /// <summary>
    /// Removes a switch.
    /// </summary>
    public void RemoveSwitch(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var self = Self;
        fixed (char* chars = name)
        {
            var value = CefStrings.View(chars, name);
            self->remove_switch(self, &value);
        }
    }

    internal static CefCommandLine? From(cef_command_line_t* ptr) => ptr == null ? null : new CefCommandLine(ptr);
}

/// <summary>
/// Answer to a JavaScript dialog (cef_jsdialog_callback_t).
/// </summary>
public sealed unsafe class CefJsDialogCallback : CefObject
{
    internal CefJsDialogCallback(cef_jsdialog_callback_t* ptr)
        : base(ptr)
    {
    }

    /// <summary>
    /// Closes the dialog: OK or Cancel, with the text for prompt().
    /// </summary>
    public void Continue(bool success, string? userInput = null)
    {
        var self = (cef_jsdialog_callback_t*)NativePtr;
        fixed (char* chars = userInput)
        {
            var value = CefStrings.View(chars, userInput);
            self->cont(self, success ? 1 : 0, &value);
        }
    }

    /// <summary>
    /// Another reference that stays valid after the handler call, to answer later.
    /// </summary>
    public CefJsDialogCallback Clone() => new((cef_jsdialog_callback_t*)AddRefNative());

    internal static CefJsDialogCallback? From(cef_jsdialog_callback_t* ptr) => ptr == null ? null : new CefJsDialogCallback(ptr);
}
