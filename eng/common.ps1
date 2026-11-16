# Shared helpers for the CefKit engineering scripts. Dot-source it: . "$PSScriptRoot/common.ps1"
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$CefArtifacts = Join-Path $RepoRoot 'artifacts/cef'

<#
.SYNOPSIS
CEF version and the pinned CEF API version from cef-version.json.
#>
function Get-CefVersionInfo {
    $json = Get-Content (Join-Path $RepoRoot 'cef-version.json') -Raw | ConvertFrom-Json
    [pscustomobject]@{ CefVersion = [string]$json.cefVersion; ApiVersion = [int]$json.apiVersion }
}

<#
.SYNOPSIS
CEF platform name (windows64, linux64): the given one or the current OS.
#>
function Get-CefPlatform([string]$Platform) {
    if ($Platform) { return $Platform }
    if ($IsWindows) { return 'windows64' }
    if ($IsLinux) { return 'linux64' }
    throw 'Unsupported OS: CefKit targets windows64 and linux64'
}

<#
.SYNOPSIS
.NET runtime identifier of the current OS (win-x64, linux-x64): NativeAOT builds and runtime packages are per RID.
#>
function Get-CefRid {
    if ($IsWindows) { return 'win-x64' }
    if ($IsLinux) { return 'linux-x64' }
    throw 'Unsupported OS: CefKit targets win-x64 and linux-x64'
}

<#
.SYNOPSIS
Directory of the extracted CEF distribution: artifacts/cef/<version>/<platform>.
#>
function Get-CefDistributionDir([string]$Platform) {
    Join-Path $CefArtifacts "$((Get-CefVersionInfo).CefVersion)/$Platform"
}
