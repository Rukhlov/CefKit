# CefKit

A .NET 10 binding to the [Chromium Embedded Framework](https://github.com/chromiumembedded/cef) (CEF) C API for
Windows and Linux x64.

- **Generated interop.** `CefKit.Interop` is generated with [ClangSharp](https://github.com/dotnet/ClangSharp)
  from the CEF C API headers: every struct, enum and function, with function pointers instead of delegates.
  The CEF API version is pinned, so libcef patch updates need no regeneration.
- **Thin managed layer.** Browsers in child windows or off-screen (CPU frames, D3D11 shared textures on Windows,
  dma-buf planes on Linux), the common handlers, request contexts, CEF threads, and JavaScript evaluation through
  the DevTools protocol, with no code in the render process.
- **Native sub-process.** CEF sub-processes run `cefkit-helper`, a NativeAOT executable: no .NET runtime start-up in
  every renderer, GPU and utility process, and a crashed renderer is reported in milliseconds.
- **No C++/CLI, no wrapper DLL.** Plain P/Invoke into libcef; the library is AOT-compatible.

Status: early. The API covers what an embedder of off-screen and windowed pages needs and grows on demand; the
raw interop covers the whole C API. The smoke check passes on Windows and on Linux (Ubuntu 24.04 with Xvfb,
software rendering) in CI.

CEF binaries are the standard builds from the [Spotify CDN](https://cef-builds.spotifycdn.com/index.html). They
have no proprietary codecs (H.264, AAC, HEVC): a build with them is a separate runtime, and its patent licensing
is the distributor's responsibility.

## Using it

```csharp
using CefKit;

CefRuntime.Load();                       // libcef next to the application; checks the CEF API version
if (args.Any(a => a.StartsWith("--type="))) return CefRuntime.ExecuteProcess();   // only without the helper

var settings = new CefSettings
{
    BrowserSubprocessPath = Path.Combine(AppContext.BaseDirectory, "cefkit-helper.exe"),
    WindowlessRenderingEnabled = true,
    RootCachePath = Path.Combine(Path.GetTempPath(), "my-app-cef"),
};
CefRuntime.Initialize(settings);

var client = new CefClient { RenderHandler = new MyRenderHandler(), LifeSpanHandler = new MyLifeSpanHandler() };
CefBrowserHost.CreateBrowser(CefWindowInfo.Offscreen(sharedTexture: true), client, "https://duckduckgo.com");

// ... once MyLifeSpanHandler.OnAfterCreated gave you a browser (keep browser.Clone()):
using var devTools = await CefDevToolsClient.CreateAsync(browser);
var title = await devTools.EvaluateAsync("document.title");

// close the browsers, dispose CefKit objects, then from the thread that called Initialize:
CefRuntime.Shutdown();
```

Rules of the road:

- **Ownership.** A `CefObject` (browser, frame, host...) owns one CEF reference: dispose it. Objects passed to
  handler methods are released when the method returns; keep a `Clone()` to use one later.
- **Threads.** Handlers run on CEF threads, mostly the UI thread (`CefThreadId.UI`). `CefRuntime.PostTask` and
  `CefRuntime.InvokeAsync` run code there. Exceptions in handlers never reach CEF: they go to
  `CefRuntime.CallbackException`.
- **Linux.** `CefSettings.DisableSignalHandlers` is true by default: the .NET runtime has its own signal handlers.
  CEF starts sub-processes directly (`--no-zygote`, so no sandbox): children forked by Chromium's zygote from
  the NativeAOT helper do not launch yet (`GPU process launch failed: error_code=1002`), see
  `CefSettings.LinuxZygote`.

## Building

Requirements: .NET 10 SDK or newer, PowerShell 7; for the NativeAOT helper the Visual Studio C++ tools on Windows,
`clang` and `zlib1g-dev` on Linux.

```
pwsh eng/fetch-cef.ps1                 # CEF of cef-version.json for this OS -> artifacts/cef (SHA1-checked)
pwsh eng/publish-helper.ps1            # cefkit-helper for this OS -> artifacts/helper/<rid>
dotnet build CefKit.slnx
dotnet test tests/CefKit.Tests         # struct layout of the interop, reference counting
artifacts/bin/CefKit.Smoke/debug/CefKit.Smoke     # end-to-end check with a real CEF
```

The smoke check opens an animated page off-screen, counts frames, evaluates JavaScript through DevTools, crashes
the render process and reloads, then closes and shuts down; `--shared-texture`, `--disable-gpu`,
`--self-subprocess` and `--seconds N` vary it.

## Updating CEF

1. Set `cefVersion` (and `apiVersion` for a new branch) in `cef-version.json`.
2. `pwsh eng/fetch-cef.ps1 -Platform windows64` and `-Platform linux64`: generation needs the headers of both.
3. `pwsh eng/generate.ps1` and review the diff of `src/CefKit/Interop` and `tests/CefKit.Tests/Interop`.
4. Build, test, run the smoke check on both platforms.

## Layout

```
cef-version.json        CEF version and pinned CEF API version
eng/                    fetch-cef, generate (ClangSharp), publish-helper, CefRuntime.targets
src/CefKit/             Interop/Generated* (generated), the managed layer
src/CefKit.Helper/      cefkit-helper, the NativeAOT sub-process
samples/CefKit.Smoke/   end-to-end check
tests/CefKit.Tests/     generated layout tests and unit tests
```

## License

MIT, see [LICENSE](LICENSE). CEF and Chromium licenses: [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
