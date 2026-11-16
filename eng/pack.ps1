<#
.SYNOPSIS
Packs CefKit and CefKit.Runtime.<rid> of the current OS into nupkgs/.

.DESCRIPTION
CefKit is the managed library and packs on any OS. CefKit.Runtime.<rid> holds the CEF binaries and resources of
cef-version.json and cefkit-helper: the script fetches CEF, publishes the helper, stages the runtime in
artifacts/runtime/<rid> and packs it. NativeAOT does not cross-compile, so the Linux runtime is packed on Linux;
there libcef.so is stripped of its debug symbols (about 1.4 GB with them).

The packages of the smoke check (samples/CefKit.Smoke.Packages) are removed from its own packages folder, so that it
restores the new ones even when the version did not change.

.EXAMPLE
pwsh eng/pack.ps1                           # CefKit and the runtime of this OS
pwsh eng/pack.ps1 -Package Runtime          # the runtime only
pwsh eng/pack.ps1 -VersionSuffix dev.3      # CefKit 0.1.0-dev.3, e.g. for a local feed
#>
param(
    [ValidateSet('CefKit', 'Runtime')]
    [string[]]$Package = @('CefKit', 'Runtime'),
    [string]$VersionSuffix,
    [string]$Output
)
. "$PSScriptRoot/common.ps1"

# nuget.org rejects larger packages
$NuGetOrgLimit = 250MB

if (-not $Output) { $Output = Join-Path $RepoRoot 'nupkgs' }
New-Item -ItemType Directory -Force $Output | Out-Null
$started = Get-Date

if ('CefKit' -in $Package) {
    $arguments = @('pack', (Join-Path $RepoRoot 'src/CefKit/CefKit.csproj'), '-c', 'Release', '-o', $Output)
    if ($VersionSuffix) { $arguments += @('--version-suffix', $VersionSuffix) }
    dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet pack CefKit failed with exit code $LASTEXITCODE" }
}

if ('Runtime' -in $Package) {
    $platform = Get-CefPlatform
    $rid = Get-CefRid
    & "$PSScriptRoot/fetch-cef.ps1" -Platform $platform
    & "$PSScriptRoot/publish-helper.ps1"

    $distribution = Get-CefDistributionDir $platform
    $stage = Join-Path $RepoRoot "artifacts/runtime/$rid"
    Write-Host "Staging the runtime in $stage"
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
    New-Item -ItemType Directory -Force $stage | Out-Null

    # Release: without import libraries, the sandbox bootstrap executables (Windows) and the setuid sandbox (Linux):
    # CefKit runs without the Chromium sandbox
    Get-ChildItem (Join-Path $distribution 'Release') |
        Where-Object { $_.Name -notlike '*.lib' -and $_.Name -notlike 'bootstrap*.exe' -and $_.Name -ne 'chrome-sandbox' } |
        Copy-Item -Destination $stage -Recurse
    Copy-Item (Join-Path $distribution 'Resources/*') $stage -Recurse
    $helperName = if ($IsWindows) { 'cefkit-helper.exe' } else { 'cefkit-helper' }
    Copy-Item (Join-Path $RepoRoot "artifacts/helper/$rid/$helperName") $stage

    if ($IsLinux) {
        $libcef = Join-Path $stage 'libcef.so'
        $before = (Get-Item $libcef).Length
        strip --strip-unneeded $libcef
        if ($LASTEXITCODE -ne 0) { throw "strip failed with exit code $LASTEXITCODE" }
        Write-Host ('libcef.so stripped: {0:N0} MB -> {1:N0} MB' -f ($before / 1MB), ((Get-Item $libcef).Length / 1MB))
        chmod +x (Join-Path $stage $helperName)
    }

    dotnet pack (Join-Path $RepoRoot 'src/CefKit.Runtime/CefKit.Runtime.csproj') -c Release -o $Output "-p:CefKitRuntimeDir=$stage"
    if ($LASTEXITCODE -ne 0) { throw "dotnet pack CefKit.Runtime failed with exit code $LASTEXITCODE" }
}

$smokePackages = Join-Path $RepoRoot 'artifacts/package-smoke/packages'
if (Test-Path $smokePackages) {
    Get-ChildItem $smokePackages -Directory -Filter 'cefkit*' | Remove-Item -Recurse -Force
}

$packed = Get-ChildItem $Output -File -Filter '*.*nupkg' | Where-Object { $_.LastWriteTime -ge $started }
foreach ($file in $packed) {
    Write-Host ('{0,-60} {1,8:N1} MB' -f $file.Name, ($file.Length / 1MB))
    if ($file.Length -gt $NuGetOrgLimit) {
        Write-Warning "$($file.Name) is larger than the $($NuGetOrgLimit / 1MB) MB that nuget.org accepts"
    }
}
