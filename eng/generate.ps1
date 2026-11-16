<#
.SYNOPSIS
Generates the raw CEF C API interop (src/CefKit/Interop) with ClangSharp.

.DESCRIPTION
Three parts:
  common   - every C API struct, enum and function except the platform-specific ones; parsed from the Windows
             headers, platform types are mapped to neutral ones (handles to nint, platform structs to void*).
  windows  - cef_main_args_t, cef_window_info_t, cef_accelerated_paint_info_t for Windows (CefKit.Interop.Windows).
  linux    - the same structs for Linux (CefKit.Interop.Linux), parsed for x86_64-linux-gnu.

The output is committed: regenerate after changing cef-version.json and review the diff.
Needs the distributions of both platforms: pwsh eng/fetch-cef.ps1 -Platform windows64 / linux64.
#>
param(
    [ValidateSet('all', 'common', 'windows', 'linux')]
    [string]$Part = 'all'
)
. "$PSScriptRoot/common.ps1"

$version = Get-CefVersionInfo
$work = Join-Path $RepoRoot 'artifacts/generate'
$interop = Join-Path $RepoRoot 'src/CefKit/Interop'
$tests = Join-Path $RepoRoot 'tests/CefKit.Tests/Interop'
New-Item -ItemType Directory -Force $work | Out-Null

Push-Location $RepoRoot
try {
    dotnet tool restore | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'dotnet tool restore failed' }
}
finally {
    Pop-Location
}

# Platform types in the common part: window, cursor and event handles are pointer-sized on both platforms;
# platform structs are passed by pointer only and become void*
$neutralRemaps = @(
    'cef_window_handle_t=nint', 'HWND=nint',
    'cef_cursor_handle_t=nint', 'HCURSOR=nint',
    'cef_event_handle_t=nint',
    'cef_shared_texture_handle_t=nint',
    'cef_main_args_t=void', '_cef_main_args_t=void',
    'cef_window_info_t=void', '_cef_window_info_t=void',
    'cef_accelerated_paint_info_t=void', '_cef_accelerated_paint_info_t=void'
)

# wchar_t differs between Windows and Linux; CefKit uses UTF-16 strings only. cef_version_info_t has extra
# Windows-only fields (installer, wchar_t path): use cef_version_info() and cef_version_full() instead
$wideExclusions = @('cef_string_wide*', 'cef_string_userfree_wide*', 'cef_string_*_to_wide', '_cef_string_wide_t', 'cef_string_wide_t',
    '_cef_version_info_t', 'cef_version_info_t', 'cef_version_info_all')

<#
.SYNOPSIS
Runs ClangSharp with a response file and replaces the output directory.
#>
function Invoke-ClangSharp([string]$Name, [string[]]$Arguments, [string]$Output, [string]$TestOutput) {
    foreach ($dir in $Output, $TestOutput) {
        if (Test-Path $dir) { Remove-Item $dir -Recurse -Force }
        New-Item -ItemType Directory -Force $dir | Out-Null
    }
    $rsp = Join-Path $work "$Name.rsp"
    @('--output', $Output) + $Arguments | ForEach-Object { if ($_ -match '\s') { '"' + $_ + '"' } else { $_ } } | Set-Content $rsp
    Push-Location $RepoRoot
    try {
        $log = dotnet tool run ClangSharpPInvokeGenerator -- "@$rsp" 2>&1
        $exit = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }

    $problems = $log | Where-Object { "$_" -match '(?i)\b(error|warning)\b' -and "$_" -notmatch 'deprecated' }
    $problems | ForEach-Object { Write-Warning "${Name}: $_" }
    if ($exit -ne 0) { throw "ClangSharp failed for $Name (exit $exit)" }

    # CEF_CALLBACK is __stdcall on Windows and the default convention elsewhere; on x64 they are the same,
    # so the interop uses the platform default and stays valid on Linux
    Get-ChildItem $Output, $TestOutput -Recurse -Filter *.cs | ForEach-Object {
        $text = [IO.File]::ReadAllText($_.FullName)
        $fixed = $text.Replace('delegate* unmanaged[Stdcall]<', 'delegate* unmanaged<').
            Replace('delegate* unmanaged[Cdecl]<', 'delegate* unmanaged<').Replace("`r`n", "`n")
        if ($fixed -ne $text) { [IO.File]::WriteAllText($_.FullName, $fixed) }
    }
    $count = (Get-ChildItem $Output -Recurse -Filter *.cs).Count
    Write-Host "${Name}: $count files in $Output"
}

<#
.SYNOPSIS
Common ClangSharp arguments: C17, .NET 10 codegen, one type per file.
#>
function Get-BaseArguments([string]$Root, [string]$Namespace) {
    @(
        '--language', 'c', '--std', 'c17',
        '--include-directory', $Root,
        '--define-macro', "CEF_API_VERSION=$($version.ApiVersion)",
        '--namespace', $Namespace,
        '--library-path', 'libcef',
        '--type-prefix-strip', '_',
        '--native-type-names-to-strip', ' __attribute__((stdcall))',
        '--remap', 'char16_t=char',
        '--config', 'codegen=default', 'file=multi',
        '--generate', 'file-scoped-namespaces', 'funcs-with-body=false', 'generated-code=none'
    )
}

$windowsRoot = Get-CefDistributionDir 'windows64'
$linuxRoot = Get-CefDistributionDir 'linux64'

if ($Part -in 'all', 'common') {
    if (-not (Test-Path (Join-Path $windowsRoot 'include'))) { throw "No Windows headers: pwsh eng/fetch-cef.ps1 -Platform windows64" }
    $capi = Get-ChildItem (Join-Path $windowsRoot 'include/capi') -Filter *.h -File | Sort-Object Name
    $umbrella = Join-Path $work 'cefkit_capi.h'
    @($capi | ForEach-Object { "#include `"include/capi/$($_.Name)`"" }) + '#include "include/cef_version_info.h"' |
        Set-Content $umbrella
    $platformHeaders = 'cef_types_win.h', 'cef_types_linux.h', 'cef_types_mac.h'
    $traverse = @($capi.FullName) +
        (Get-ChildItem (Join-Path $windowsRoot 'include/internal') -Filter *.h -File | Where-Object { $_.Name -notin $platformHeaders }).FullName +
        (Join-Path $windowsRoot 'include/cef_api_hash.h') +
        (Join-Path $windowsRoot 'include/cef_version_info.h')

    $arguments = (Get-BaseArguments $windowsRoot 'CefKit.Interop') +
        @('--generate', 'helper-types', 'tests-xunit') +
        @('--test-output', (Join-Path $tests 'Generated')) +
        @('--remap') + $neutralRemaps +
        @('--exclude') + $wideExclusions +
        @('--file', $umbrella, '--traverse') + $traverse
    Invoke-ClangSharp 'common' $arguments (Join-Path $interop 'Generated') (Join-Path $tests 'Generated')

    # The API hash libcef must report for the pinned API version; cef_api_versions.h lists it per platform
    # in the order OS_WIN, OS_MAC, OS_LINUX
    $versions = Get-Content (Join-Path $windowsRoot 'include/cef_api_versions.h')
    $hashes = $versions | Select-String -Pattern "^#define CEF_API_HASH_$($version.ApiVersion) `"([0-9a-f]+)`"" |
        ForEach-Object { $_.Matches[0].Groups[1].Value }
    if ($hashes.Count -ne 3) { throw "Expected 3 platform hashes for API version $($version.ApiVersion), found $($hashes.Count)" }
    $info = @"
// Generated by eng/generate.ps1 from cef-version.json and include/cef_api_versions.h. Do not edit.
namespace CefKit.Interop;

/// <summary>
/// The CEF API version the interop was generated for and the API hashes libcef reports for it.
/// </summary>
public static class CefApiVersion
{
    /// <summary>
    /// CEF_API_VERSION of the generated structs and functions.
    /// </summary>
    public const int Value = $($version.ApiVersion);

    /// <summary>
    /// CEF version whose headers were used for generation.
    /// </summary>
    public const string CefVersion = "$($version.CefVersion)";

    /// <summary>
    /// cef_api_hash(Value, 0) of libcef on Windows.
    /// </summary>
    public const string WindowsHash = "$($hashes[0])";

    /// <summary>
    /// cef_api_hash(Value, 0) of libcef on Linux.
    /// </summary>
    public const string LinuxHash = "$($hashes[2])";
}
"@
    [IO.File]::WriteAllText((Join-Path $interop 'Generated/CefApiVersion.cs'), $info.Replace("`r`n", "`n") + "`n")
}

if ($Part -in 'all', 'windows') {
    $umbrella = Join-Path $work 'cefkit_windows.h'
    '#include "include/internal/cef_types_win.h"' | Set-Content $umbrella
    $arguments = (Get-BaseArguments $windowsRoot 'CefKit.Interop.Windows') +
        @('--generate', 'tests-xunit', '--test-output', (Join-Path $tests 'Generated.Windows')) +
        @('--with-using', '*=CefKit.Interop') +
        @('--remap', 'HINSTANCE=nint', 'HMENU=nint', 'HWND=nint', 'HANDLE=nint', 'DWORD=uint',
            'cef_window_handle_t=nint', 'cef_shared_texture_handle_t=nint') +
        @('--file', $umbrella, '--traverse', (Join-Path $windowsRoot 'include/internal/cef_types_win.h'))
    Invoke-ClangSharp 'windows' $arguments (Join-Path $interop 'Generated.Windows') (Join-Path $tests 'Generated.Windows')
}

if ($Part -in 'all', 'linux') {
    if (-not (Test-Path (Join-Path $linuxRoot 'include'))) { throw "No Linux headers: pwsh eng/fetch-cef.ps1 -Platform linux64" }
    $umbrella = Join-Path $work 'cefkit_linux.h'
    '#include "include/internal/cef_types_linux.h"' | Set-Content $umbrella
    $arguments = (Get-BaseArguments $linuxRoot 'CefKit.Interop.Linux') +
        @('--config', 'types=unix') +
        @('--additional', '--target=x86_64-unknown-linux-gnu', '--additional', '-ffreestanding',
            '--include-directory', (Join-Path $PSScriptRoot 'stubs/linux')) +
        @('--generate', 'tests-xunit', '--test-output', (Join-Path $tests 'Generated.Linux')) +
        @('--with-using', '*=CefKit.Interop') +
        @('--remap', 'cef_window_handle_t=nint',
            '_cef_accelerated_paint_native_pixmap_plane_info_t=cef_accelerated_paint_native_pixmap_plane_t') +
        @('--exclude', 'cef_get_xdisplay', 'XDisplay', 'XEvent', '_XDisplay', '_XEvent') +
        @('--file', $umbrella, '--traverse', (Join-Path $linuxRoot 'include/internal/cef_types_linux.h'))
    Invoke-ClangSharp 'linux' $arguments (Join-Path $interop 'Generated.Linux') (Join-Path $tests 'Generated.Linux')
}
