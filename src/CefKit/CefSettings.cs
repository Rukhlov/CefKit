using CefKit.Interop;
using CefKit.Native;

namespace CefKit;

/// <summary>
/// Settings of <see cref="CefRuntime.Initialize"/> (cef_settings_t). Unset values keep CEF's defaults.
/// </summary>
public sealed class CefSettings
{
    /// <summary>
    /// Executable for the sub-processes (renderer, GPU, utility). Null: the current executable, which then must
    /// call <see cref="CefRuntime.ExecuteProcess"/> at startup.
    /// </summary>
    public string? BrowserSubprocessPath { get; set; }

    /// <summary>
    /// Disables the Chromium sandbox. True by default: the sandbox needs a special executable setup.
    /// </summary>
    public bool NoSandbox { get; set; } = true;

    /// <summary>
    /// Runs the browser process message loop on a CEF thread (Windows and Linux). True by default.
    /// </summary>
    public bool MultiThreadedMessageLoop { get; set; } = true;

    /// <summary>
    /// Enables windowless (off-screen) browsers.
    /// </summary>
    public bool WindowlessRenderingEnabled { get; set; }

    /// <summary>
    /// Ignores the process command line for CEF and Chromium switches.
    /// </summary>
    public bool CommandLineArgsDisabled { get; set; }

    /// <summary>
    /// Root of all cache directories; one process can use a root at a time.
    /// </summary>
    public string? RootCachePath { get; set; }

    /// <summary>
    /// Cache of the global request context, a child of <see cref="RootCachePath"/>. Empty: in memory.
    /// </summary>
    public string? CachePath { get; set; }

    /// <summary>
    /// Keeps session cookies of the global request context on disk.
    /// </summary>
    public bool PersistSessionCookies { get; set; }

    /// <summary>
    /// User-Agent header instead of the default one.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Locale such as en-US.
    /// </summary>
    public string? Locale { get; set; }

    /// <summary>
    /// Debug log file.
    /// </summary>
    public string? LogFile { get; set; }

    /// <summary>
    /// Minimum severity of the debug log.
    /// </summary>
    public CefLogSeverity LogSeverity { get; set; } = CefLogSeverity.Default;

    /// <summary>
    /// Directory of the .pak resources and icudtl.dat. Null: next to libcef.
    /// </summary>
    public string? ResourcesDirPath { get; set; }

    /// <summary>
    /// Directory of the locale .pak files. Null: locales next to libcef.
    /// </summary>
    public string? LocalesDirPath { get; set; }

    /// <summary>
    /// Remote debugging port (1024-65535), 0 to disable.
    /// </summary>
    public int RemoteDebuggingPort { get; set; }

    /// <summary>
    /// Background color (ARGB) behind pages without one.
    /// </summary>
    public uint BackgroundColor { get; set; }

    /// <summary>
    /// Accept-Language header, comma-separated.
    /// </summary>
    public string? AcceptLanguageList { get; set; }

    /// <summary>
    /// Do not install CEF signal handlers on POSIX systems. True by default on Linux: the .NET runtime has its own,
    /// e.g. for NullReferenceException.
    /// </summary>
    public bool DisableSignalHandlers { get; set; } = OperatingSystem.IsLinux();

    /// <summary>
    /// Linux: start sub-processes through Chromium's zygote. False by default: CEF then starts every sub-process
    /// directly (--no-zygote on the process command line, ignored with <see cref="CommandLineArgsDisabled"/>),
    /// which needs <see cref="NoSandbox"/>; with the NativeAOT helper the zygote does not launch children yet.
    /// </summary>
    public bool LinuxZygote { get; set; }

    /// <summary>
    /// Fills a zeroed cef_settings_t; strings are CEF copies, free them with <see cref="Clear"/>.
    /// </summary>
    internal unsafe void CopyTo(cef_settings_t* settings)
    {
        settings->size = (nuint)sizeof(cef_settings_t);
        settings->no_sandbox = NoSandbox ? 1 : 0;
        CefStrings.Set(&settings->browser_subprocess_path, BrowserSubprocessPath);
        settings->multi_threaded_message_loop = MultiThreadedMessageLoop ? 1 : 0;
        settings->windowless_rendering_enabled = WindowlessRenderingEnabled ? 1 : 0;
        settings->command_line_args_disabled = CommandLineArgsDisabled ? 1 : 0;
        CefStrings.Set(&settings->root_cache_path, RootCachePath);
        CefStrings.Set(&settings->cache_path, CachePath);
        settings->persist_session_cookies = PersistSessionCookies ? 1 : 0;
        CefStrings.Set(&settings->user_agent, UserAgent);
        CefStrings.Set(&settings->locale, Locale);
        CefStrings.Set(&settings->log_file, LogFile);
        settings->log_severity = (cef_log_severity_t)LogSeverity;
        CefStrings.Set(&settings->resources_dir_path, ResourcesDirPath);
        CefStrings.Set(&settings->locales_dir_path, LocalesDirPath);
        settings->remote_debugging_port = RemoteDebuggingPort;
        settings->background_color = BackgroundColor;
        CefStrings.Set(&settings->accept_language_list, AcceptLanguageList);
        settings->disable_signal_handlers = DisableSignalHandlers ? 1 : 0;
    }

    /// <summary>
    /// Frees the strings set by <see cref="CopyTo"/>.
    /// </summary>
    internal static unsafe void Clear(cef_settings_t* settings)
    {
        CefStrings.Clear(&settings->browser_subprocess_path);
        CefStrings.Clear(&settings->root_cache_path);
        CefStrings.Clear(&settings->cache_path);
        CefStrings.Clear(&settings->user_agent);
        CefStrings.Clear(&settings->locale);
        CefStrings.Clear(&settings->log_file);
        CefStrings.Clear(&settings->resources_dir_path);
        CefStrings.Clear(&settings->locales_dir_path);
        CefStrings.Clear(&settings->accept_language_list);
    }
}
