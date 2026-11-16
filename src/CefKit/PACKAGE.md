# CefKit

A .NET 10 binding to the [Chromium Embedded Framework](https://github.com/chromiumembedded/cef) (CEF) C API for
Windows and Linux x64.

- **Generated interop.** `CefKit.Interop` is generated with ClangSharp from the CEF C API headers: every struct,
  enum and function, with function pointers instead of delegates. The CEF API version is pinned.
- **Thin managed layer.** Browsers in child windows or off-screen (CPU frames, D3D11 shared textures on Windows,
  dma-buf planes on Linux), the common handlers, request contexts, CEF threads, and JavaScript evaluation through
  the DevTools protocol, with no code in the render process.
- **Native sub-process.** CEF sub-processes run `cefkit-helper`, a NativeAOT executable: no .NET runtime start-up in
  every renderer, GPU and utility process.
- **No C++/CLI, no wrapper DLL.** Plain P/Invoke into libcef; the library is AOT-compatible.

CEF itself comes with a runtime package for the target: `CefKit.Runtime.win-x64` or `CefKit.Runtime.linux-x64`.
It puts libcef, its resources and `cefkit-helper` next to the application.

```csharp
using CefKit;

CefRuntime.Load();                       // libcef next to the application; checks the CEF API version
var settings = new CefSettings
{
    BrowserSubprocessPath = CefRuntime.HelperPath,
    WindowlessRenderingEnabled = true,
    RootCachePath = Path.Combine(Path.GetTempPath(), "my-app-cef"),
};
CefRuntime.Initialize(settings);

var client = new CefClient { RenderHandler = new MyRenderHandler(), LifeSpanHandler = new MyLifeSpanHandler() };
CefBrowserHost.CreateBrowser(CefWindowInfo.Offscreen(sharedTexture: true), client, "https://duckduckgo.com");

// close the browsers, dispose CefKit objects, then from the thread that called Initialize:
CefRuntime.Shutdown();
```

Status: early; the API grows on demand. Documentation, rules of the road and sources:
[github.com/Rukhlov/CefKit](https://github.com/Rukhlov/CefKit).
