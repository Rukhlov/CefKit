using System.Runtime.InteropServices;
using CefKit.Interop;
using CefKit.Native;

namespace CefKit;

/// <summary>
/// Process-level callbacks (cef_app_t), given to <see cref="CefRuntime.Initialize"/> and <see cref="CefRuntime.ExecuteProcess"/>.
/// </summary>
public class CefApp : CefHandler<cef_app_t>
{
    /// <summary>
    /// Browser process callbacks.
    /// </summary>
    public CefBrowserProcessHandler? BrowserProcessHandler { get; set; }

    /// <summary>
    /// Last chance to change the command line of a process before CEF and Chromium parse it: the browser process
    /// (empty process type) or a sub-process. The browser process command line has the switches of CEF already,
    /// among them --disable-features with the features CEF turns off (add to such lists with
    /// <see cref="CefCommandLine.AppendToListSwitch"/>), and on Linux --no-zygote from CefKit.
    /// </summary>
    protected virtual void OnBeforeCommandLineProcessing(string processType, CefCommandLine commandLine)
    {
    }

    private protected override unsafe void Fill(cef_app_t* native)
    {
        native->on_before_command_line_processing = &BeforeCommandLineProcessing;
        native->get_browser_process_handler = &GetBrowserProcessHandler;
    }

    [UnmanagedCallersOnly]
    private static unsafe void BeforeCommandLineProcessing(cef_app_t* self, cef_string_utf16_t* processType, cef_command_line_t* commandLine)
    {
        try
        {
            using var line = CefCommandLine.From(commandLine);
            if (line is not null)
            {
                var type = CefStrings.Read(processType) ?? string.Empty;
                if (type.Length == 0)
                {
                    CefRuntime.AddBrowserSwitches(line);
                }

                FromNative<CefApp>(self).OnBeforeCommandLineProcessing(type, line);
            }
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe cef_browser_process_handler_t* GetBrowserProcessHandler(cef_app_t* self)
    {
        try
        {
            var handler = FromNative<CefApp>(self).BrowserProcessHandler;
            return handler is null ? null : (cef_browser_process_handler_t*)handler.AddRefNative();
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
            return null;
        }
    }
}

/// <summary>
/// Browser process callbacks (cef_browser_process_handler_t).
/// </summary>
public class CefBrowserProcessHandler : CefHandler<cef_browser_process_handler_t>
{
    /// <summary>
    /// CEF finished initializing on the UI thread.
    /// </summary>
    protected virtual void OnContextInitialized()
    {
    }

    /// <summary>
    /// A child process is about to start; its command line can still change.
    /// </summary>
    protected virtual void OnBeforeChildProcessLaunch(CefCommandLine commandLine)
    {
    }

    /// <summary>
    /// Another process started with the same root cache path and handed over its command line. Return true when
    /// handled; false lets CEF open a default Chrome window.
    /// </summary>
    protected virtual bool OnAlreadyRunningAppRelaunch(CefCommandLine commandLine, string currentDirectory) => false;

    private protected override unsafe void Fill(cef_browser_process_handler_t* native)
    {
        native->on_context_initialized = &ContextInitialized;
        native->on_before_child_process_launch = &BeforeChildProcessLaunch;
        native->on_already_running_app_relaunch = &AlreadyRunningAppRelaunch;
    }

    [UnmanagedCallersOnly]
    private static unsafe void ContextInitialized(cef_browser_process_handler_t* self)
    {
        try
        {
            FromNative<CefBrowserProcessHandler>(self).OnContextInitialized();
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe void BeforeChildProcessLaunch(cef_browser_process_handler_t* self, cef_command_line_t* commandLine)
    {
        try
        {
            using var line = CefCommandLine.From(commandLine);
            if (line is not null)
            {
                FromNative<CefBrowserProcessHandler>(self).OnBeforeChildProcessLaunch(line);
            }
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe int AlreadyRunningAppRelaunch(cef_browser_process_handler_t* self, cef_command_line_t* commandLine, cef_string_utf16_t* currentDirectory)
    {
        try
        {
            using var line = CefCommandLine.From(commandLine);
            return line is not null &&
                FromNative<CefBrowserProcessHandler>(self).OnAlreadyRunningAppRelaunch(line, CefStrings.Read(currentDirectory) ?? string.Empty) ? 1 : 0;
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
            return 0;
        }
    }
}

/// <summary>
/// A task posted to a CEF thread.
/// </summary>
internal sealed class CefActionTask(Action action) : CefHandler<cef_task_t>
{
    private protected override unsafe void Fill(cef_task_t* native) => native->execute = &Execute;

    [UnmanagedCallersOnly]
    private static unsafe void Execute(cef_task_t* self)
    {
        try
        {
            FromNative<CefActionTask>(self).Run();
        }
        catch (Exception ex)
        {
            CefRuntime.ReportCallbackException(ex);
        }
    }

    private void Run() => action();
}
