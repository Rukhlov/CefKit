// End-to-end check of CefKit with a real CEF: an off-screen page with animation, frames, DevTools evaluation,
// a render process crash and recovery, closing and shutdown. Exit code 0 when every check passed.
//
//   CefKit.Smoke [--seconds 3] [--width 800 --height 600] [--shared-texture] [--disable-gpu] [--no-crash]
//                [--self-subprocess] [--keep-log] [--url <url>]
using System.Diagnostics;
using CefKit;
using CefKit.Smoke;

// Without the helper, CEF starts this executable again for its sub-processes
CefRuntime.Load(AppContext.BaseDirectory);
if (args.Any(a => a.StartsWith("--type=", StringComparison.Ordinal)))
{
    return CefRuntime.ExecuteProcess();
}

var options = Options.Parse(args);
var helperName = OperatingSystem.IsWindows() ? "cefkit-helper.exe" : "cefkit-helper";
var helper = Path.Combine(AppContext.BaseDirectory, helperName);
var subprocess = !options.SelfSubprocess && File.Exists(helper) ? helper : Environment.ProcessPath!;
var root = Path.Combine(Path.GetTempPath(), "cefkit-smoke", $"p{Environment.ProcessId}");
Directory.CreateDirectory(root);

var report = new Report();
CefRuntime.CallbackException += ex => report.Fail($"callback exception: {ex}");
Console.WriteLine($"CefKit smoke: CEF {CefRuntime.Version} (Chromium {CefRuntime.ChromiumVersion}), {Environment.OSVersion.Platform}, sub-process {Path.GetFileName(subprocess)}");
Console.WriteLine($"  CEF log: {Path.Combine(root, "cef.log")}");

var settings = new CefSettings
{
    BrowserSubprocessPath = subprocess,
    WindowlessRenderingEnabled = true,
    RootCachePath = root,
    CachePath = string.Empty,
    LogFile = Path.Combine(root, "cef.log"),
    LogSeverity = CefLogSeverity.Warning,
};

var clock = Stopwatch.StartNew();
if (!CefRuntime.Initialize(settings, new SmokeApp(options.DisableGpu)))
{
    Console.WriteLine("RESULT: FAILED (CefRuntime.Initialize returned false)");
    return 1;
}

report.Step("initialize", clock);

// CEF must be shut down from the thread that initialized it: the checks run asynchronously, this thread waits
SmokeRun.RunAsync(options, report).GetAwaiter().GetResult();

clock.Restart();
CefRuntime.Shutdown();
report.Step("shutdown", clock);
if (!options.KeepLog)
{
    try
    {
        Directory.Delete(root, recursive: true);
    }
    catch (IOException)
    {
        // Sub-processes may still hold files for a moment
    }
}

Console.WriteLine(report.Passed ? "RESULT: OK" : "RESULT: FAILED");
return report.Passed ? 0 : 1;
