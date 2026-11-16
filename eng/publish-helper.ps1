<#
.SYNOPSIS
Publishes the NativeAOT sub-process executable (cefkit-helper) for the current OS.

.DESCRIPTION
NativeAOT does not cross-compile between operating systems: the Windows helper is published on Windows and the
Linux helper on Linux. Output: artifacts/helper/<rid>/ unless -Output is given.

On Windows the NativeAOT linker step runs vcvarsall.bat of Visual Studio, which calls vswhere.exe by name; the
Visual Studio Installer directory is added to PATH for this process if it is missing there.
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$Output
)
. "$PSScriptRoot/common.ps1"

$rid = Get-CefRid
if (-not $Output) { $Output = Join-Path $RepoRoot "artifacts/helper/$rid" }

if ($IsWindows -and -not (Get-Command vswhere.exe -ErrorAction SilentlyContinue)) {
    $installer = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer'
    if (Test-Path (Join-Path $installer 'vswhere.exe')) {
        $env:PATH = "$env:PATH;$installer"
    }
}

dotnet publish (Join-Path $RepoRoot 'src/CefKit.Helper/CefKit.Helper.csproj') -c $Configuration -r $rid -o $Output
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
Write-Host "Helper published to $Output"
