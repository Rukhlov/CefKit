# CefKit — context for AI agents

CefKit is a public .NET 10 binding to the CEF C API for Windows and Linux x64: interop generated with ClangSharp,
a thin managed layer, and a NativeAOT sub-process executable. It stays generic: no concepts of a particular
application in its code or API.

## Structure

- `cef-version.json` — the only place with the CEF version and the pinned CEF API version.
- `eng/fetch-cef.ps1` — downloads a CEF distribution from the Spotify CDN (SHA1-checked) to `artifacts/cef`.
- `eng/generate.ps1` — ClangSharp over the CEF headers: `common` (all of the C API, from the Windows headers with
  platform types made neutral), `windows` and `linux` (the platform structs). Needs both distributions.
  Writes `src/CefKit/Interop/Generated*`, `CefApiVersion.cs` and `tests/CefKit.Tests/Interop/Generated*`.
- `eng/publish-helper.ps1` — publishes `cefkit-helper` for the current OS (NativeAOT does not cross-compile
  between operating systems).
- `eng/CefRuntime.targets` — copies the extracted CEF and the helper next to samples and tests.
- `src/CefKit/` — `CefRuntime` (load, API check, initialize, threads), `CefHandler` (.NET-implemented CEF
  interfaces: native struct with reference count), `CefObject` (CEF-implemented objects: one owned reference),
  handlers in `Handlers/`, `CefDevToolsClient`.
- `samples/CefKit.Smoke` — end-to-end check with a real CEF. `tests/CefKit.Tests` — runs without libcef.

## Build and check

```
pwsh eng/fetch-cef.ps1; pwsh eng/publish-helper.ps1
dotnet build CefKit.slnx
dotnet test tests/CefKit.Tests
artifacts/bin/CefKit.Smoke/debug/CefKit.Smoke(.exe) [--shared-texture] [--self-subprocess] [--disable-gpu]
```

## Conventions

- English everywhere: code, comments, docs, commit messages (imperative, no trailers).
- XML docs in the multi-line `/// <summary>` form only.
- Never edit generated files: change `eng/generate.ps1` and regenerate. Files are LF (`.gitattributes`).
- Platform-neutral code targets `net10.0`; platform structs live in `Interop/Generated.Windows` and
  `Interop/Generated.Linux` and are chosen at run time (`OperatingSystem.IsWindows()`).
- Every `[UnmanagedCallersOnly]` callback catches all exceptions and reports them with
  `CefRuntime.ReportCallbackException`; nothing may throw into CEF.
- CEF reference rules: object parameters of callbacks are released after the call (wrap them with `using`);
  objects passed into CEF get a new reference (`AddRefNative`); objects returned by CEF are owned by the wrapper.
- Package versions only in `Directory.Packages.props`.
- No proprietary codecs here: CefKit ships standard CEF builds only.
