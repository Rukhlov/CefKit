using System.Diagnostics;

namespace CefKit.Smoke;

/// <summary>
/// The checks between CEF initialization and shutdown: one off-screen browser from creation to close.
/// </summary>
internal static class SmokeRun
{
    public static async Task RunAsync(Options options, Report report)
    {
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
            var clock = Stopwatch.StartNew();
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
            var fps = frames / (double)options.Seconds;
            report.Check($"frames: {frames} in {options.Seconds} s ({fps:F1} fps), {page.PaintKind} {page.PaintSize}", fps >= 10);

            // JavaScript through DevTools: no code in the render process
            devTools = await CefDevToolsClient.CreateAsync(browser);
            var sum = await devTools.EvaluateAsync("1 + 2").WaitAsync(TimeSpan.FromSeconds(10));
            var title = await devTools.EvaluateAsync("document.title").WaitAsync(TimeSpan.FromSeconds(10));
            var awaited = await devTools.EvaluateAsync("new Promise(r => requestAnimationFrame(() => requestAnimationFrame(() => r(42))))", awaitPromise: true)
                .WaitAsync(TimeSpan.FromSeconds(10));
            report.Check($"devtools: 1 + 2 = {sum}, title = {title}, promise = {awaited}",
                sum.ToString() == "3" && title.ToString() == "CefKit smoke" && awaited.ToString() == "42");

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

            report.Info(Processes.Describe(options.SelfSubprocess ? Environment.ProcessPath! : "cefkit-helper"));

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
    }
}
