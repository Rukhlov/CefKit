using System.Diagnostics;
using System.Globalization;
using CefKit;

namespace CefKit.Smoke;

/// <summary>
/// Command line of the smoke check.
/// </summary>
internal sealed record Options(int Seconds, int Width, int Height, bool SharedTexture, bool DisableGpu, bool NoCrash, bool SelfSubprocess, string Url)
{
    /// <summary>
    /// An animated page: every frame changes the canvas, so an off-screen browser paints continuously.
    /// </summary>
    private const string AnimatedPage =
        "<!doctype html><title>CefKit smoke</title><body style=\"margin:0\"><canvas id=c width=320 height=180></canvas><script>" +
        "let n=0;const g=document.getElementById('c').getContext('2d');" +
        "(function f(){n++;g.fillStyle='hsl('+(n%360)+',80%,50%)';g.fillRect(0,0,320,180);g.fillStyle='#000';" +
        "g.fillText(String(n),10,20);requestAnimationFrame(f);})();</script>";

    public static Options Parse(string[] args)
    {
        string? Value(string name)
        {
            var index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        return new Options(
            int.Parse(Value("--seconds") ?? "3", CultureInfo.InvariantCulture),
            int.Parse(Value("--width") ?? "800", CultureInfo.InvariantCulture),
            int.Parse(Value("--height") ?? "600", CultureInfo.InvariantCulture),
            args.Contains("--shared-texture"),
            args.Contains("--disable-gpu"),
            args.Contains("--no-crash"),
            args.Contains("--self-subprocess"),
            Value("--url") ?? "data:text/html;charset=utf-8," + Uri.EscapeDataString(AnimatedPage));
    }
}

/// <summary>
/// Chromium switches of the browser process.
/// </summary>
internal sealed class SmokeApp(bool disableGpu) : CefApp
{
    protected override void OnBeforeCommandLineProcessing(string processType, CefCommandLine commandLine)
    {
        if (processType.Length == 0 && disableGpu)
        {
            commandLine.AppendSwitch("disable-gpu");
            commandLine.AppendSwitch("disable-gpu-compositing");
        }
    }
}

/// <summary>
/// State of the one browser of the check, fed by its handlers on the CEF UI thread.
/// </summary>
internal sealed class SmokePage
{
    private readonly int _width;
    private readonly int _height;
    private int _frames;
    private TaskCompletionSource _loaded = NewSignal();

    public SmokePage(int width, int height)
    {
        _width = width;
        _height = height;
        LifeSpan = new LifeSpanHandler(this);
        Load = new LoadHandler(this);
        Display = new DisplayHandler();
        Render = new RenderHandler(this);
        Requests = new RequestHandler(this);
    }

    public TaskCompletionSource<CefBrowser> Created { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource Closed { get; } = NewSignal();

    public TaskCompletionSource<CefTerminationStatus> Terminated { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public CefLifeSpanHandler LifeSpan { get; }

    public CefLoadHandler Load { get; }

    public CefDisplayHandler Display { get; }

    public CefRenderHandler Render { get; }

    public CefRequestHandler Requests { get; }

    public int Frames => Volatile.Read(ref _frames);

    public string PaintKind { get; private set; } = "no paints";

    public string PaintSize { get; private set; } = string.Empty;

    public void ResetFrames() => Interlocked.Exchange(ref _frames, 0);

    public void ResetLoad() => _loaded = NewSignal();

    public Task WaitLoadedAsync(TimeSpan timeout) => _loaded.Task.WaitAsync(timeout);

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class LifeSpanHandler(SmokePage page) : CefLifeSpanHandler
    {
        protected override bool OnBeforePopup(CefBrowser browser, CefFrame frame, string targetUrl, string targetFrameName, bool userGesture) => true;

        protected override void OnAfterCreated(CefBrowser browser) => page.Created.TrySetResult(browser.Clone());

        protected override void OnBeforeClose(CefBrowser browser) => page.Closed.TrySetResult();
    }

    private sealed class LoadHandler(SmokePage page) : CefLoadHandler
    {
        protected override void OnLoadEnd(CefBrowser browser, CefFrame frame, int httpStatusCode)
        {
            if (frame.IsMain && !frame.Url.StartsWith("chrome://", StringComparison.Ordinal))
            {
                page._loaded.TrySetResult();
            }
        }

        protected override void OnLoadError(CefBrowser browser, CefFrame frame, int errorCode, string errorText, string failedUrl)
        {
            if (frame.IsMain && !failedUrl.StartsWith("chrome://", StringComparison.Ordinal))
            {
                page._loaded.TrySetException(new InvalidOperationException($"Load error {errorCode} {errorText}: {failedUrl}"));
            }
        }
    }

    private sealed class DisplayHandler : CefDisplayHandler
    {
        protected override bool OnConsoleMessage(CefBrowser browser, CefLogSeverity level, string message, string source, int line)
        {
            Console.WriteLine($"  console {level}: {message}");
            return true;
        }
    }

    private sealed class RenderHandler(SmokePage page) : CefRenderHandler
    {
        protected override CefRect GetViewRect(CefBrowser browser) => new(0, 0, page._width, page._height);

        protected override bool GetScreenInfo(CefBrowser browser, ref CefScreenInfo screenInfo)
        {
            screenInfo.DeviceScaleFactor = 1;
            return true;
        }

        protected override void OnPaint(CefBrowser browser, CefPaintElementType type, ReadOnlySpan<CefRect> dirtyRects, nint buffer, int width, int height)
        {
            if (type == CefPaintElementType.View)
            {
                Interlocked.Increment(ref page._frames);
                page.PaintKind = "CPU buffer";
                page.PaintSize = $"{width}x{height}";
            }
        }

        protected override void OnAcceleratedPaint(CefBrowser browser, CefPaintElementType type, ReadOnlySpan<CefRect> dirtyRects, in CefAcceleratedPaintInfo info)
        {
            if (type == CefPaintElementType.View)
            {
                Interlocked.Increment(ref page._frames);
                var (width, height) = info.CodedSize;
                page.PaintKind = OperatingSystem.IsWindows()
                    ? $"shared texture {(info.SharedTextureHandle != 0 ? "with handle" : "WITHOUT handle")}"
                    : $"dma-buf with {info.Planes.Length} planes";
                page.PaintSize = $"{width}x{height} {info.Format}";
            }
        }
    }

    private sealed class RequestHandler(SmokePage page) : CefRequestHandler
    {
        protected override void OnRenderProcessTerminated(CefBrowser browser, CefTerminationStatus status, int errorCode, string errorString) =>
            page.Terminated.TrySetResult(status);
    }
}

/// <summary>
/// JavaScript dialogs are answered silently.
/// </summary>
internal sealed class SilentDialogs : CefJsDialogHandler
{
    protected override bool OnJsDialog(CefBrowser browser, string originUrl, CefJsDialogType type, string messageText, string defaultPromptText,
        CefJsDialogCallback callback, ref bool suppressMessage)
    {
        suppressMessage = true;
        return false;
    }

    protected override bool OnBeforeUnloadDialog(CefBrowser browser, string messageText, bool isReload, CefJsDialogCallback callback)
    {
        callback.Continue(true);
        return true;
    }
}

/// <summary>
/// Browsers never take focus.
/// </summary>
internal sealed class NoFocus : CefFocusHandler
{
    protected override bool OnSetFocus(CefBrowser browser, CefFocusSource source) => true;
}

/// <summary>
/// Steps and checks of the run, printed as they happen.
/// </summary>
internal sealed class Report
{
    public bool Passed { get; private set; } = true;

    public void Step(string name, Stopwatch clock) => Console.WriteLine($"  {name}: {clock.ElapsedMilliseconds} ms");

    public void Info(string text) => Console.WriteLine($"  {text}");

    public void Check(string text, bool ok)
    {
        Console.WriteLine($"  {(ok ? "ok" : "FAILED")}: {text}");
        Passed &= ok;
    }

    public void Fail(string text)
    {
        Console.WriteLine($"  FAILED: {text}");
        Passed = false;
    }
}

/// <summary>
/// CEF sub-processes of this run: count and private memory.
/// </summary>
internal static class Processes
{
    public static string Describe(string subprocess)
    {
        var name = Path.GetFileNameWithoutExtension(subprocess);
        var self = Environment.ProcessId;
        var children = Process.GetProcessesByName(name).Where(p => p.Id != self).ToList();
        var childMemory = children.Sum(p => p.PrivateMemorySize64) / (1024.0 * 1024);
        using var current = Process.GetCurrentProcess();
        var ownMemory = current.PrivateMemorySize64 / (1024.0 * 1024);
        foreach (var child in children)
        {
            child.Dispose();
        }

        return $"processes: {children.Count} {name}, private memory {childMemory:F0} MB in sub-processes, {ownMemory:F0} MB in this process";
    }
}
