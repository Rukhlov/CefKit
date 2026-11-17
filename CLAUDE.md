# CefKit — context for AI agents

CefKit is a public .NET 10 binding to the CEF C API for Windows and Linux x64: interop generated with ClangSharp,
a thin managed layer, and a NativeAOT sub-process executable. It stays generic: no concepts of a particular
application in its code or API.

State and the work that is left: `ROADMAP.md`. Keep it current when a step is done or a finding changes it.

## Structure

- `cef-version.json` — the only place with the CEF version, the pinned CEF API version and the revision of the
  runtime packages; `eng/CefVersion.props` reads it for MSBuild, `eng/common.ps1` for the scripts.
- `eng/fetch-cef.ps1` — downloads a CEF distribution from the Spotify CDN (SHA1-checked) to `artifacts/cef`.
- `eng/generate.ps1` — ClangSharp over the CEF headers: `common` (all of the C API, from the Windows headers with
  platform types made neutral), `windows` and `linux` (the platform structs). Needs both distributions.
  Writes `src/CefKit/Interop/Generated*`, `CefApiVersion.cs` and `tests/CefKit.Tests/Interop/Generated*`.
- `eng/publish-helper.ps1` — publishes `cefkit-helper` for the current OS (NativeAOT does not cross-compile
  between operating systems).
- `eng/CefRuntime.targets` — puts the extracted CEF (as hard links: the Linux `libcef.so` has 1.4 GB of symbols)
  and the helper next to samples and tests.
- `eng/pack.ps1` — packages into `nupkgs/`: `CefKit` (managed, any OS) and `CefKit.Runtime.<rid>` of the current
  OS (CEF minimal distribution without import libraries and sandbox executables, plus the helper, staged in
  `artifacts/runtime/<rid>`; `libcef.so` stripped). Runtime version: CEF version + `runtimeRevision` (156.0.15.1).
- `src/CefKit/` — `CefRuntime` (load, API check, helper path, initialize, threads), `CefHandler`
  (.NET-implemented CEF interfaces: native struct with reference count), `CefObject` (CEF-implemented objects: one
  owned reference), handlers in `Handlers/`, `CefDevToolsClient`. `PACKAGE.md` — the package readme.
- `src/CefKit.Runtime/` — project of the runtime packages (not in `CefKit.slnx`, packed by `eng/pack.ps1`) and
  `build/CefKit.Runtime.<rid>.targets`: `runtime/**` of the package next to the output and publish directory for
  that RID, or for a RID-less build on that OS; opt-out `CefKitCopyRuntime=false`; chmod of the helper on Linux.
- `samples/CefKit.Smoke` — end-to-end check with a real CEF; `samples/CefKit.Smoke.Packages` — the same sources
  built from `nupkgs/` (own packages folder `artifacts/package-smoke/packages`, emptied of CefKit by `pack.ps1`).
  `tests/CefKit.Tests` — runs without libcef.

## Build and check

```
pwsh eng/fetch-cef.ps1; pwsh eng/publish-helper.ps1
dotnet build CefKit.slnx
dotnet test tests/CefKit.Tests
artifacts/bin/CefKit.Smoke/debug/CefKit.Smoke(.exe) [--shared-texture] [--self-subprocess] [--disable-gpu]
pwsh eng/pack.ps1; dotnet build samples/CefKit.Smoke.Packages -c Release
artifacts/bin/CefKit.Smoke.Packages/release/CefKit.Smoke(.exe)
```

CI (Windows and Ubuntu 24.04) runs all of it and uploads the packages as artifacts `packages-Windows` (CefKit and
the Windows runtime) and `packages-Linux` (the Linux runtime).

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
- nuget.org accepts packages up to 250 MB: `pack.ps1` warns above it (the Windows runtime is 183 MB).
