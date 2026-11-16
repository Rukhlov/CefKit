// End-to-end check of CefKit with a real CEF: an off-screen page with animation, frames, DevTools evaluation,
// a render process crash and recovery, closing and shutdown. Exit code 0 when every check passed.
//
//   CefKit.Smoke [--seconds 3] [--width 800 --height 600] [--shared-texture] [--disable-gpu] [--no-crash]
//                [--self-subprocess] [--url <url>]
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

var page = new SmokePage(options.Width, options.Height);
var client = new CefClient
{
    LifeSpanHandler = page.LifeSpan,
    LoadHandler = page.Load,
    DisplayHandler = page.Display,
    RenderHandler = page.Render,
    RequestHandler = page.Requests,
    JsDialogHandler = new SilentDialogs(),
    FocusHandler = new NoFocus(),
};

CefBrowser? browser = null;
CefDevToolsClient? devTools = null;
try
{
    using var context = CefRequestContext.CreateContext(new CefRequestContextSettings());
    clock.Restart();
    var info = CefWindowInfo.Offscreen(sharedTexture: options.SharedTexture);
    if (!CefBrowserHost.CreateBrowser(info, client, options.Url, new CefBrowserSettings { WindowlessFrameRate = 60 }, context))
    {
        throw new InvalidOperationException("CreateBrowser returned false");
    }

    browser = await page.Created.Task.WaitAsync(TimeSpan.FromSeconds(15));
    report.Step("browser created", clock);
    await page.WaitLoadedAsync(TimeSpan.FromSeconds(15));
    report.Step("page loaded", clock);

    // Frames of the animated page
    page.ResetFrames();
    await Task.Delay(TimeSpan.FromSeconds(options.Seconds));
    var frames = page.Frames;
    var fps = frames / options.Seconds;
    report.Check($"frames: {frames} in {options.Seconds} s ({fps:F1} fps), {page.PaintKind} {page.PaintSize}", fps >= 10);

    // JavaScript through DevTools: no code in the render process
    devTools = await CefDevToolsClient.CreateAsync(browser);
    var sum = await devTools.EvaluateAsync("1 + 2").WaitAsync(TimeSpan.FromSeconds(10));
    var title = await devTools.EvaluateAsync("document.title").WaitAsync(TimeSpan.FromSeconds(10));
    var awaited = await devTools.EvaluateAsync("new Promise(r => requestAnimationFrame(() => requestAnimationFrame(() => r(42))))", awaitPromise: true)
        .WaitAsync(TimeSpan.FromSeconds(10));
    report.Check($"devtools: 1 + 2 = {sum}, title = {title}, promise = {awaited}",
        sum.ToString() == "3" && title.GetString() == "CefKit smoke" && awaited.ToString() == "42");

    if (!options.NoCrash)
    {
        // Render process crash: CEF reports it, a new navigation starts a new render process
        clock.Restart();
        page.ResetLoad();
        using (var frame = browser.GetMainFrame())
        {
            frame?.LoadUrl("chrome://crash");
        }

        var status = await page.Terminated.Task.WaitAsync(TimeSpan.FromSeconds(20));
        report.Step($"render process terminated ({status})", clock);
        clock.Restart();
        page.ResetLoad();
        using (var frame = browser.GetMainFrame())
        {
            frame?.LoadUrl(options.Url);
        }

        await page.WaitLoadedAsync(TimeSpan.FromSeconds(15));
        var reloadMs = clock.ElapsedMilliseconds;
        page.ResetFrames();
        await Task.Delay(TimeSpan.FromSeconds(1));
        report.Check($"reloaded in {reloadMs} ms, frames in 1 s after reload: {page.Frames}", page.Frames > 0);
    }

    report.Info(Processes.Describe(subprocess));

    devTools.Dispose();
    devTools = null;
    clock.Restart();
    using (var host = browser.GetHost())
    {
        host.CloseBrowser(forceClose: true);
    }

    await page.Closed.Task.WaitAsync(TimeSpan.FromSeconds(10));
    report.Step("browser closed", clock);
}
catch (Exception ex)
{
    report.Fail(ex.ToString());
}
finally
{
    devTools?.Dispose();
    browser?.Dispose();
}

clock.Restart();
CefRuntime.Shutdown();
report.Step("shutdown", clock);
try
{
    Directory.Delete(root, recursive: true);
}
catch (IOException)
{
    // Sub-processes may still hold files for a moment
}

Console.WriteLine(report.Passed ? "RESULT: OK" : "RESULT: FAILED");
return report.Passed ? 0 : 1;
