# Roadmap

State on 2026-11-17 and the work that is left, written so that it can go on from another machine. Decisions and
findings that the code does not show are recorded here as well.

## State

- Generated interop for CEF 156.0.15 (CEF API version 15600), the managed layer, the NativeAOT `cefkit-helper`,
  514 unit tests.
- The smoke check (`samples/CefKit.Smoke`) covers off-screen browsers only:
  - Windows (development machine and CI): 60 fps with CPU frames and with D3D11 shared textures, JavaScript through
    DevTools, a crashed renderer reported in 5–36 ms, reload in about 20 ms. With the .NET executable as the
    sub-process (`--self-subprocess`) the same crash was reported after 13.9 s, and five sub-processes took 193 MB
    of private memory against 175 MB with the helper.
  - Linux (CI only: Ubuntu 24.04, Xvfb, `--disable-gpu`): passes when CEF starts sub-processes without the zygote
    (`--no-zygote`, the default of `CefSettings.LinuxZygote`).
- Packages (`eng/pack.ps1`): `CefKit` 0.1.0 and `CefKit.Runtime.<rid>` 156.0.15.1. Checked on Windows: the runtime
  package is 183 MB (nuget.org accepts up to 250 MB), the smoke check built from the packages passes, and so do a
  RID-specific publish, a reference through a library project and `CefKitCopyRuntime=false`. The Linux runtime
  package is packed and checked in CI only.

## Next

1. **Linux packages in CI.** The first CI run with packaging packs on Linux: `strip` of `libcef.so` (1.4 GB with
   symbols), the helper's executable bit after NuGet, the smoke check built from the packages. Check the size of
   `CefKit.Runtime.linux-x64` against the nuget.org limit.
2. **Zygote and sandbox on Linux**, on a Linux machine or in WSL rather than through CI runs. Known: with the zygote
   every child launch fails with `GPU process launch failed: error_code=1002`, then Chromium aborts with
   `GPU process isn't usable`; the helper alone starts and loads libcef
   (`cefkit-helper --type=gpu-process --no-sandbox`). Chromium's Linux sandbox needs the zygote, so until this works
   `NoSandbox` stays true. Steps:
   - the smoke check has no switch for the zygote yet: add `--zygote` (`CefSettings.LinuxZygote = true`);
   - run `cefsimple` of the same CEF distribution in the same environment: does its zygote launch children;
   - verbose logs of the zygote and the launcher: `--switch enable-logging=stderr --switch v=1`;
   - check what the zygote gives the helper (`--type=zygote` and its file descriptors) and whether
     `CefRuntime.ExecuteProcess` in the NativeAOT helper handles it like a C++ sub-process executable.
3. **GPU frames on Linux.** `CefAcceleratedPaintInfo.Planes` and `Modifier` (dma-buf) exist, but no run had a GPU.
   Run the smoke check with `--shared-texture` and without `--disable-gpu` on a Linux machine with an Intel or AMD
   GPU; the GPU of WSL may not export dma-buf. CEF windows on Linux are X11 (XWayland under Wayland).
4. **Windowed browsers in the smoke check.** `CefWindowInfo.Child` is not exercised: add a mode with a test window
   as the parent, Windows first. Embedders that show CEF in their own window depend on it.
5. **Publishing to nuget.org.** An account and an API key as a repository secret; a workflow job on a version tag
   that takes the `packages-Windows` and `packages-Linux` artifacts and pushes them with `--skip-duplicate`.
   Reserve the `CefKit` ID prefix. Public packages go to nuget.org: GitHub Packages needs a token even to read
   public NuGet packages.
6. **CEF updates.** Patch releases of 156 need no regeneration (156.0.30 with Chromium 156.0.8078.96 is already on
   the CDN). For long support the next branch is the LTS 160 when it is out (an LTS lives about nine months; 150 is
   supported until April 2027): new `apiVersion`, regeneration, `runtimeRevision` back to 1.

Out of scope: builds with proprietary codecs (H.264, AAC, HEVC). CefKit ships standard CEF builds only; a build with
codecs is a separate runtime package with the same layout, built and distributed by whoever licenses the codecs.

## Working on another machine

- Requirements: .NET 10 SDK or newer (`global.json` rolls forward to a newer major version), PowerShell 7, the
  NativeAOT tools (Visual Studio C++ tools on Windows; `clang` and `zlib1g-dev` on Linux). Linux run-time
  libraries and Xvfb: the `apt-get` line of `.github/workflows/ci.yml`.
- First run: `pwsh eng/fetch-cef.ps1`, `pwsh eng/publish-helper.ps1`, `dotnet build CefKit.slnx`,
  `dotnet test tests/CefKit.Tests`, then the smoke check (`xvfb-run -a` without a display; `--keep-log` keeps the CEF
  log, whose path the check prints).
- Regenerating the interop needs both distributions: `fetch-cef.ps1 -Platform windows64` and `-Platform linux64`.
- CI job logs need a signed-in GitHub user. `eng/ci-smoke.sh` turns the smoke output, the helper output and, on
  failure, the CEF log into annotations, which the public API returns without signing in:
  `GET https://api.github.com/repos/Rukhlov/CefKit/check-runs/{job id}/annotations`.
- Linux problems are debugged on Linux: when a fix does not work after a couple of CI runs, reproduce it locally
  instead of iterating in CI.
