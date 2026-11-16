using System.Runtime.InteropServices;
using CefKit.Interop;

namespace CefKit;

/// <summary>
/// CEF in this process: loading libcef, sub-processes, initialization and shutdown, CEF threads.
/// </summary>
public static unsafe partial class CefRuntime
{
    private static readonly Lock s_lock = new();
    private static nint s_library;
    private static string? s_directory;
    private static nint s_mainArgs;
    private static volatile bool s_initialized;

    /// <summary>
    /// A .NET method called by CEF threw. Exceptions must not cross into CEF, so CefKit catches them and reports
    /// them here; without subscribers they go to standard error.
    /// </summary>
    public static event Action<Exception>? CallbackException;

    /// <summary>
    /// libcef is loaded and its API matches the interop.
    /// </summary>
    public static bool IsLoaded => s_library != 0;

    /// <summary>
    /// CEF is initialized in this process and not shut down.
    /// </summary>
    public static bool IsInitialized => s_initialized;

    /// <summary>
    /// Full version of the loaded libcef, e.g. 156.0.15+gcf671c0+chromium-156.0.8078.80.
    /// </summary>
    public static string Version
    {
        get
        {
            EnsureLoaded();
            return Marshal.PtrToStringUTF8((nint)Methods.cef_version_full()) ?? string.Empty;
        }
    }

    /// <summary>
    /// Chromium version of the loaded libcef, e.g. 156.0.8078.80.
    /// </summary>
    public static Version ChromiumVersion
    {
        get
        {
            EnsureLoaded();
            return new Version(Methods.cef_version_info(4), Methods.cef_version_info(5), Methods.cef_version_info(6), Methods.cef_version_info(7));
        }
    }

    /// <summary>
    /// File name of cefkit-helper, the NativeAOT sub-process executable of CefKit, on this OS.
    /// </summary>
    public static string HelperFileName => OperatingSystem.IsWindows() ? "cefkit-helper.exe" : "cefkit-helper";

    /// <summary>
    /// cefkit-helper next to the loaded libcef, where the CefKit.Runtime packages put it, or null when it is not
    /// there. Meant for <see cref="CefSettings.BrowserSubprocessPath"/>.
    /// </summary>
    public static string? HelperPath
    {
        get
        {
            EnsureLoaded();
            var path = Path.Combine(s_directory!, HelperFileName);
            return File.Exists(path) ? path : null;
        }
    }

    /// <summary>
    /// Loads libcef from a directory (default: the application directory) and selects the CEF API version of the
    /// interop. libcef must support that version, otherwise loading fails. Call before anything else in CefKit.
    /// </summary>
    public static void Load(string? directory = null)
    {
        lock (s_lock)
        {
            if (s_library != 0)
            {
                return;
            }

            var name = OperatingSystem.IsWindows() ? "libcef.dll"
                : OperatingSystem.IsLinux() ? "libcef.so"
                : throw new PlatformNotSupportedException("CefKit supports Windows and Linux");
            directory = Path.GetFullPath(directory ?? AppContext.BaseDirectory);
            var path = Path.Combine(directory, name);
            var library = NativeLibrary.Load(path);
            NativeLibrary.SetDllImportResolver(typeof(CefRuntime).Assembly,
                (libraryName, _, _) => libraryName == "libcef" ? library : 0);

            // The first call into libcef selects the API version: calling anything else before it is undefined behavior
            var hash = Marshal.PtrToStringUTF8((nint)Methods.cef_api_hash(CefApiVersion.Value, 0));
            var expected = OperatingSystem.IsWindows() ? CefApiVersion.WindowsHash : CefApiVersion.LinuxHash;
            if (hash != expected)
            {
                throw new InvalidOperationException(
                    $"{path} does not support CEF API version {CefApiVersion.Value} of CefKit (generated from CEF {CefApiVersion.CefVersion}): API hash {hash ?? "null"}, expected {expected}");
            }

            s_directory = directory;
            s_library = library;
        }
    }

    /// <summary>
    /// Runs a CEF sub-process (renderer, GPU, utility...) when the command line says so. Returns its exit code,
    /// or -1 in the browser process. Sub-process executables call it first thing in Main.
    /// </summary>
    public static int ExecuteProcess(CefApp? app = null)
    {
        EnsureLoaded();

        // CEF takes ownership of one reference to the application
        var application = app is null ? null : (cef_app_t*)app.AddRefNative();
        return Methods.cef_execute_process(GetMainArgs(), application, null);
    }

    /// <summary>
    /// Initializes CEF in the browser process. Call <see cref="Shutdown"/> from the same thread before exit.
    /// Returns false if CEF failed, e.g. its root cache directory is in use by another process.
    /// </summary>
    public static bool Initialize(CefSettings settings, CefApp? app = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        EnsureLoaded();
        var native = default(cef_settings_t);
        try
        {
            settings.CopyTo(&native);
            var arguments = OperatingSystem.IsLinux() && !settings.LinuxZygote ? CreateLinuxMainArgs("--no-zygote") : GetMainArgs();
            var application = app is null ? null : (cef_app_t*)app.AddRefNative();
            var initialized = Methods.cef_initialize(arguments, &native, application, null) != 0;
            s_initialized = initialized;
            return initialized;
        }
        finally
        {
            CefSettings.Clear(&native);
        }
    }

    /// <summary>
    /// Shuts CEF down. All browsers must be closed and all CefKit objects disposed before.
    /// </summary>
    public static void Shutdown()
    {
        if (!s_initialized)
        {
            return;
        }

        s_initialized = false;
        Methods.cef_shutdown();
    }

    /// <summary>
    /// The calling thread is the given CEF thread.
    /// </summary>
    public static bool CurrentlyOn(CefThreadId thread)
    {
        EnsureLoaded();
        return Methods.cef_currently_on((cef_thread_id_t)thread) != 0;
    }

    /// <summary>
    /// Runs an action on a CEF thread. Returns false if the thread is not running.
    /// </summary>
    public static bool PostTask(CefThreadId thread, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        EnsureLoaded();

        // CEF takes ownership of the task reference and releases it after running or dropping the task
        var task = new CefActionTask(action);
        return Methods.cef_post_task((cef_thread_id_t)thread, (cef_task_t*)task.AddRefNative()) != 0;
    }

    /// <summary>
    /// Runs a function on a CEF thread and returns its result; runs it inline when already on that thread.
    /// </summary>
    public static Task<T> InvokeAsync<T>(CefThreadId thread, Func<T> function)
    {
        ArgumentNullException.ThrowIfNull(function);
        if (CurrentlyOn(thread))
        {
            try
            {
                return Task.FromResult(function());
            }
            catch (Exception ex)
            {
                return Task.FromException<T>(ex);
            }
        }

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var posted = PostTask(thread, () =>
        {
            try
            {
                completion.TrySetResult(function());
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        });
        if (!posted)
        {
            completion.TrySetException(new InvalidOperationException($"CEF thread {thread} is not running"));
        }

        return completion.Task;
    }

    /// <summary>
    /// Reports an exception thrown by a .NET method called by CEF.
    /// </summary>
    internal static void ReportCallbackException(Exception exception)
    {
        var handler = CallbackException;
        if (handler is null)
        {
            Console.Error.WriteLine($"CefKit: exception in a CEF callback: {exception}");
            return;
        }

        try
        {
            handler(exception);
        }
        catch (Exception)
        {
            // Reporting must not throw into CEF either
        }
    }

    private static void EnsureLoaded()
    {
        if (s_library == 0)
        {
            throw new InvalidOperationException("libcef is not loaded: call CefRuntime.Load first");
        }
    }

    /// <summary>
    /// cef_main_args_t of the process, allocated once and kept for its lifetime.
    /// </summary>
    private static void* GetMainArgs()
    {
        lock (s_lock)
        {
            if (s_mainArgs != 0)
            {
                return (void*)s_mainArgs;
            }

            if (OperatingSystem.IsWindows())
            {
                var args = (Interop.Windows.cef_main_args_t*)NativeMemory.AllocZeroed((nuint)sizeof(Interop.Windows.cef_main_args_t));
                args->instance = GetModuleHandleW(null);
                s_mainArgs = (nint)args;
            }
            else
            {
                s_mainArgs = (nint)CreateLinuxMainArgs();
            }

            return (void*)s_mainArgs;
        }
    }

    /// <summary>
    /// Linux cef_main_args_t: argv as CEF parses it, the executable and the arguments (--type=... in sub-processes),
    /// plus extra switches. Kept for the process lifetime: Chromium may keep pointers into argv.
    /// </summary>
    private static Interop.Linux.cef_main_args_t* CreateLinuxMainArgs(params string[] extraSwitches)
    {
        var arguments = Environment.GetCommandLineArgs().Concat(extraSwitches).ToArray();
        var argv = (sbyte**)NativeMemory.AllocZeroed((nuint)((arguments.Length + 1) * sizeof(nint)));
        for (var i = 0; i < arguments.Length; i++)
        {
            var value = i == 0 ? Environment.ProcessPath ?? arguments[0] : arguments[i];
            argv[i] = (sbyte*)Marshal.StringToCoTaskMemUTF8(value);
        }

        var args = (Interop.Linux.cef_main_args_t*)NativeMemory.AllocZeroed((nuint)sizeof(Interop.Linux.cef_main_args_t));
        args->argc = arguments.Length;
        args->argv = argv;
        return args;
    }

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint GetModuleHandleW(string? moduleName);
}
