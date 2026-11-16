# CefKit.Runtime

The [Chromium Embedded Framework](https://github.com/chromiumembedded/cef) (CEF) for
[CefKit](https://www.nuget.org/packages/CefKit), the .NET 10 binding to the CEF C API: one package per runtime
identifier, `CefKit.Runtime.win-x64` and `CefKit.Runtime.linux-x64`.

- libcef, its resources and locales from the standard CEF minimal distribution of the
  [Spotify CDN](https://cef-builds.spotifycdn.com/index.html). On Linux `libcef.so` is stripped of debug symbols.
- `cefkit-helper`, the NativeAOT sub-process executable of CefKit: CEF starts it for the renderer, GPU and utility
  processes, so no .NET runtime starts in them.

The build copies everything next to the application output and to its publish directory: for the package's runtime
identifier, or for a build without one on the matching OS. `<CefKitCopyRuntime>false</CefKitCopyRuntime>` turns
it off.

```csharp
CefRuntime.Load();                                   // libcef next to the application
var settings = new CefSettings { BrowserSubprocessPath = CefRuntime.HelperPath };
```

The version is the CEF version and a package revision: `156.0.15.1` is CEF 156.0.15. CefKit selects its pinned CEF
API version when it loads libcef and fails clearly if this runtime does not support it.

No proprietary codecs (H.264, AAC, HEVC): a build with them is a separate runtime, and its patent licensing is the
distributor's responsibility. Licenses: `LICENSE.md` and `licenses/cef` in the package.
