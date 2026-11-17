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

## Packages

| Package | Contents |
| --- | --- |
| `CefKit` | the managed library (`lib/net10.0`), any OS |
| `CefKit.Runtime.win-x64`, `CefKit.Runtime.linux-x64` | libcef, its resources and locales, `cefkit-helper` |

A runtime package copies its files next to the application output and to the publish directory: for its runtime
identifier, or for a build without one on the matching OS (`<CefKitCopyRuntime>false</CefKitCopyRuntime>` turns
this off). Its version is the CEF version and a package revision (`156.0.15.1`: CEF 156.0.15, revision 1, which is
`runtimeRevision` in `cef-version.json`); `CefKit` loads any libcef that supports its pinned CEF API version.
On Linux `libcef.so` is stripped of debug symbols: 1.4 GB with them.

The packages are not on nuget.org yet: `pwsh eng/pack.ps1` puts them in `nupkgs/`, and CI uploads them as the
`packages-Windows` and `packages-Linux` artifacts of every run.

## Using it

```csharp
using CefKit;

CefRuntime.Load();                       // libcef next to the application; checks the CEF API version
if (args.Any(a => a.StartsWith("--type="))) return CefRuntime.ExecuteProcess();   // only without the helper

var settings = new CefSettings
{
    BrowserSubprocessPath = CefRuntime.HelperPath,     // cefkit-helper next to libcef
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
- **Command line.** Chromium switches go in `CefApp.OnBeforeCommandLineProcessing`. The browser process command
  line has CEF's switches there already, among them `--disable-features` with the features CEF turns off:
  `AppendSwitch` replaces a value (the last one counts), `AppendToListSwitch` adds to such a list.
- **Linux.**
  - `CefSettings.DisableSignalHandlers` is true by default: the .NET runtime has its own signal handlers.
  - CEF starts sub-processes directly: CefKit adds `--no-zygote` to the browser process (also with
    `CommandLineArgsDisabled`), which needs `NoSandbox`. Chromium's zygote launches no children from
    `cefkit-helper` or a .NET executable yet and then aborts the process (`GPU process launch failed:
    error_code=1002`, `GPU process isn't usable`): `CefSettings.LinuxZygote` is for debugging only.
  - CEF needs an X server or a Wayland compositor (`DISPLAY`, `WAYLAND_DISPLAY`), otherwise `Initialize` returns
    false; off-screen browsers run without one with `--ozone-platform=headless`.
  - Shared textures are dma-buf planes: they need GPU compositing and a GBM device. Without GPU compositing the
    frames come to `OnPaint`, without a GBM device (e.g. WSL) none come at all.
  - WSL: OpenGL is llvmpipe by default, which Chromium blocklists (software compositing, no WebGL);
    `GALLIUM_DRIVER=d3d12` gives it the GPU of the host.

## Building

Requirements: .NET 10 SDK or newer with its current patch (NativeAOT compiles the SDK's runtime into the helper,
and NuGet audit fails a vulnerable one with NU1903), PowerShell 7; for the NativeAOT helper the Visual Studio C++
tools on Windows, `clang` and `zlib1g-dev` on Linux. On Linux `fetch-cef.ps1` unpacks with `lbzip2` or `bzip2`,
CEF needs the libraries that `.github/workflows/ci.yml` installs (`libnss3`, `libasound2t64`...), and the smoke
check a display: `xvfb-run -a` or `--switch ozone-platform=headless`.

```
pwsh eng/fetch-cef.ps1                 # CEF of cef-version.json for this OS -> artifacts/cef (SHA1-checked)
pwsh eng/publish-helper.ps1            # cefkit-helper for this OS -> artifacts/helper/<rid>
dotnet build CefKit.slnx
dotnet test tests/CefKit.Tests         # struct layout of the interop, reference counting, list switches
artifacts/bin/CefKit.Smoke/debug/CefKit.Smoke     # end-to-end check with a real CEF
```

The smoke check opens an animated page off-screen, counts frames, evaluates JavaScript through DevTools, crashes
the render process and reloads, then closes and shuts down; `--shared-texture` (frames must be GPU textures),
`--disable-gpu`, `--self-subprocess`, `--seconds N` and `--switch name[=value]` vary it.

Packages, and the same smoke check built from them as an application gets them:

```
pwsh eng/pack.ps1 [-Package CefKit,Runtime] [-VersionSuffix dev.1]   # -> nupkgs/
dotnet build samples/CefKit.Smoke.Packages -c Release
artifacts/bin/CefKit.Smoke.Packages/release/CefKit.Smoke
```

The runtime package is packed for the current OS only (NativeAOT does not cross-compile); on Linux the script
needs `strip` (binutils). Bump `runtimeRevision` in `cef-version.json` when a runtime package changes without a CEF
update, e.g. a new helper.

## Updating CEF

1. Set `cefVersion` (and `apiVersion` for a new branch) in `cef-version.json`, and `runtimeRevision` back to 1.
2. `pwsh eng/fetch-cef.ps1 -Platform windows64` and `-Platform linux64`: generation needs the headers of both.
3. `pwsh eng/generate.ps1` and review the diff of `src/CefKit/Interop` and `tests/CefKit.Tests/Interop`.
4. Build, test, run the smoke check on both platforms.

## Layout

```
cef-version.json                CEF version, pinned CEF API version, runtime package revision
eng/                            fetch-cef, generate (ClangSharp), publish-helper, pack, CefRuntime.targets
src/CefKit/                     Interop/Generated* (generated), the managed layer
src/CefKit.Helper/              cefkit-helper, the NativeAOT sub-process
src/CefKit.Runtime/             CefKit.Runtime.<rid> packages: project and build targets
samples/CefKit.Smoke/           end-to-end check
samples/CefKit.Smoke.Packages/  the end-to-end check built from the packages
tests/CefKit.Tests/             generated layout tests and unit tests
```

## License

MIT, see [LICENSE](LICENSE). CEF and Chromium licenses: [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
