using System.Runtime.InteropServices;
using CefKit.Interop;

namespace CefKit;

/// <summary>
/// A rectangle in CEF coordinates; same layout as cef_rect_t.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct CefRect(int X, int Y, int Width, int Height)
{
    internal cef_rect_t ToNative() => new() { x = X, y = Y, width = Width, height = Height };

    internal static CefRect FromNative(in cef_rect_t rect) => new(rect.x, rect.y, rect.width, rect.height);
}

/// <summary>
/// Screen of an off-screen browser: scale and bounds in device-independent pixels.
/// </summary>
public struct CefScreenInfo
{
    /// <summary>
    /// Device pixels per DIP.
    /// </summary>
    public float DeviceScaleFactor;

    /// <summary>
    /// Bits per pixel.
    /// </summary>
    public int Depth;

    /// <summary>
    /// Bits per color component.
    /// </summary>
    public int DepthPerComponent;

    /// <summary>
    /// The screen is monochrome.
    /// </summary>
    public bool IsMonochrome;

    /// <summary>
    /// Bounds of the screen in DIP.
    /// </summary>
    public CefRect Rect;

    /// <summary>
    /// Bounds available to windows in DIP.
    /// </summary>
    public CefRect AvailableRect;
}

/// <summary>
/// CEF threads of the browser process.
/// </summary>
public enum CefThreadId
{
    /// <summary>
    /// The main thread of the browser process: browser objects and most callbacks live here.
    /// </summary>
    UI = (int)cef_thread_id_t.TID_UI,

    /// <summary>
    /// Background file thread, lowest priority.
    /// </summary>
    FileBackground = (int)cef_thread_id_t.TID_FILE_BACKGROUND,

    /// <summary>
    /// File thread for user-visible work.
    /// </summary>
    FileUserVisible = (int)cef_thread_id_t.TID_FILE_USER_VISIBLE,

    /// <summary>
    /// File thread for user-blocking work.
    /// </summary>
    FileUserBlocking = (int)cef_thread_id_t.TID_FILE_USER_BLOCKING,

    /// <summary>
    /// Launches and terminates processes.
    /// </summary>
    ProcessLauncher = (int)cef_thread_id_t.TID_PROCESS_LAUNCHER,

    /// <summary>
    /// Network and IPC.
    /// </summary>
    IO = (int)cef_thread_id_t.TID_IO,
}

/// <summary>
/// Log severity of CEF.
/// </summary>
public enum CefLogSeverity
{
    /// <summary>
    /// CEF's default: Info.
    /// </summary>
    Default = (int)cef_log_severity_t.LOGSEVERITY_DEFAULT,

    /// <summary>
    /// Verbose and above.
    /// </summary>
    Verbose = (int)cef_log_severity_t.LOGSEVERITY_VERBOSE,

    /// <summary>
    /// Info and above.
    /// </summary>
    Info = (int)cef_log_severity_t.LOGSEVERITY_INFO,

    /// <summary>
    /// Warnings and above.
    /// </summary>
    Warning = (int)cef_log_severity_t.LOGSEVERITY_WARNING,

    /// <summary>
    /// Errors and above.
    /// </summary>
    Error = (int)cef_log_severity_t.LOGSEVERITY_ERROR,

    /// <summary>
    /// Fatal errors only.
    /// </summary>
    Fatal = (int)cef_log_severity_t.LOGSEVERITY_FATAL,

    /// <summary>
    /// No logging.
    /// </summary>
    Disable = (int)cef_log_severity_t.LOGSEVERITY_DISABLE,
}

/// <summary>
/// Browser style: Alloy (no Chrome UI) or Chrome.
/// </summary>
public enum CefRuntimeStyle
{
    /// <summary>
    /// CEF chooses: Chrome for windowed browsers, Alloy for windowless ones.
    /// </summary>
    Default = (int)cef_runtime_style_t.CEF_RUNTIME_STYLE_DEFAULT,

    /// <summary>
    /// Chrome style.
    /// </summary>
    Chrome = (int)cef_runtime_style_t.CEF_RUNTIME_STYLE_CHROME,

    /// <summary>
    /// Alloy style: no Chrome UI, bubbles and prompts.
    /// </summary>
    Alloy = (int)cef_runtime_style_t.CEF_RUNTIME_STYLE_ALLOY,
}

/// <summary>
/// What an off-screen paint is for.
/// </summary>
public enum CefPaintElementType
{
    /// <summary>
    /// The page.
    /// </summary>
    View = (int)cef_paint_element_type_t.PET_VIEW,

    /// <summary>
    /// A popup widget such as a select list.
    /// </summary>
    Popup = (int)cef_paint_element_type_t.PET_POPUP,
}

/// <summary>
/// Why a render process ended.
/// </summary>
public enum CefTerminationStatus
{
    /// <summary>
    /// Non-zero exit status.
    /// </summary>
    AbnormalTermination = (int)cef_termination_status_t.TS_ABNORMAL_TERMINATION,

    /// <summary>
    /// Killed by a signal or TerminateProcess.
    /// </summary>
    ProcessWasKilled = (int)cef_termination_status_t.TS_PROCESS_WAS_KILLED,

    /// <summary>
    /// Crashed.
    /// </summary>
    ProcessCrashed = (int)cef_termination_status_t.TS_PROCESS_CRASHED,

    /// <summary>
    /// Ran out of memory.
    /// </summary>
    ProcessOutOfMemory = (int)cef_termination_status_t.TS_PROCESS_OOM,

    /// <summary>
    /// Failed to launch.
    /// </summary>
    LaunchFailed = (int)cef_termination_status_t.TS_LAUNCH_FAILED,

    /// <summary>
    /// Failed a code integrity check.
    /// </summary>
    IntegrityFailure = (int)cef_termination_status_t.TS_INTEGRITY_FAILURE,
}

/// <summary>
/// Where a focus request came from.
/// </summary>
public enum CefFocusSource
{
    /// <summary>
    /// Navigation to a new page.
    /// </summary>
    Navigation = (int)cef_focus_source_t.FOCUS_SOURCE_NAVIGATION,

    /// <summary>
    /// The system, e.g. a click.
    /// </summary>
    System = (int)cef_focus_source_t.FOCUS_SOURCE_SYSTEM,
}

/// <summary>
/// Kind of a JavaScript dialog.
/// </summary>
public enum CefJsDialogType
{
    /// <summary>
    /// alert().
    /// </summary>
    Alert = (int)cef_jsdialog_type_t.JSDIALOGTYPE_ALERT,

    /// <summary>
    /// confirm().
    /// </summary>
    Confirm = (int)cef_jsdialog_type_t.JSDIALOGTYPE_CONFIRM,

    /// <summary>
    /// prompt().
    /// </summary>
    Prompt = (int)cef_jsdialog_type_t.JSDIALOGTYPE_PROMPT,
}

/// <summary>
/// Pixel format of an off-screen frame.
/// </summary>
public enum CefColorType
{
    /// <summary>
    /// RGBA, 8 bits per component.
    /// </summary>
    Rgba8888 = (int)cef_color_type_t.CEF_COLOR_TYPE_RGBA_8888,

    /// <summary>
    /// BGRA, 8 bits per component.
    /// </summary>
    Bgra8888 = (int)cef_color_type_t.CEF_COLOR_TYPE_BGRA_8888,
}
