// CEF sub-process: CEF starts this executable with --type=renderer, gpu-process, utility... and it runs until
// CEF ends it. libcef is next to the executable.
using System.Runtime.InteropServices;
using CefKit;

if (OperatingSystem.IsWindows())
{
    // A crashed sub-process must not wait for a Windows Error Reporting dialog: CEF reports the crash to the browser
    Win32.SetErrorMode(Win32.SemFailCriticalErrors | Win32.SemNoGpFaultErrorBox);
}

CefRuntime.Load(AppContext.BaseDirectory);
return CefRuntime.ExecuteProcess();

/// <summary>
/// Kernel32 functions of the helper.
/// </summary>
internal static partial class Win32
{
    public const uint SemFailCriticalErrors = 0x0001;
    public const uint SemNoGpFaultErrorBox = 0x0002;

    [LibraryImport("kernel32.dll")]
    public static partial uint SetErrorMode(uint mode);
}
