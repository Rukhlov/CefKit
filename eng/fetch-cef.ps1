<#
.SYNOPSIS
Downloads a CEF binary distribution from the Spotify CDN, verifies its SHA1 and extracts it.

.DESCRIPTION
The version comes from cef-version.json. The distribution is extracted to artifacts/cef/<version>/<platform>
without the top-level folder of the archive. Archives are cached in artifacts/cef/downloads.

.EXAMPLE
pwsh eng/fetch-cef.ps1                      # the current OS
pwsh eng/fetch-cef.ps1 -Platform linux64    # headers and binaries for Linux, e.g. to generate interop on Windows
#>
param(
    [ValidateSet('windows64', 'linux64')]
    [string]$Platform,
    [ValidateSet('minimal', 'standard')]
    [string]$Type = 'minimal',
    [switch]$Force
)
. "$PSScriptRoot/common.ps1"

$Platform = Get-CefPlatform $Platform
$version = (Get-CefVersionInfo).CefVersion
$target = Get-CefDistributionDir $Platform
$marker = Join-Path $target '.cefkit-complete'
if ((Test-Path $marker) -and -not $Force) {
    Write-Host "CEF $version ($Platform) is already at $target"
    return
}

# The build index (~10 MB) lists every build with its files and SHA1; reuse it for a day
New-Item -ItemType Directory -Force $CefArtifacts | Out-Null
$index = Join-Path $CefArtifacts 'index.json'
if (-not (Test-Path $index) -or ((Get-Date) - (Get-Item $index).LastWriteTime).TotalHours -gt 24) {
    Write-Host 'Downloading the CEF build index'
    Invoke-WebRequest -Uri 'https://cef-builds.spotifycdn.com/index.json' -OutFile $index -UseBasicParsing
}

$build = (Get-Content $index -Raw | ConvertFrom-Json).$Platform.versions |
    Where-Object { $_.cef_version -eq $version } | Select-Object -First 1
if (-not $build) { throw "CEF $version is not in the build index for $Platform" }
$file = $build.files | Where-Object { $_.type -eq $Type } | Select-Object -First 1
if (-not $file) { throw "CEF $version for $Platform has no '$Type' distribution" }

$downloads = Join-Path $CefArtifacts 'downloads'
New-Item -ItemType Directory -Force $downloads | Out-Null
$archive = Join-Path $downloads $file.name
$expected = [string]$file.sha1
$valid = (Test-Path $archive) -and ((Get-FileHash $archive -Algorithm SHA1).Hash.ToLowerInvariant() -eq $expected)
if (-not $valid) {
    $url = 'https://cef-builds.spotifycdn.com/' + [uri]::EscapeDataString($file.name)
    Write-Host ('Downloading {0} ({1:N0} MB)' -f $file.name, ($file.size / 1MB))
    $ProgressPreference = 'SilentlyContinue'
    Invoke-WebRequest -Uri $url -OutFile $archive -UseBasicParsing
    if ((Get-FileHash $archive -Algorithm SHA1).Hash.ToLowerInvariant() -ne $expected) {
        throw "SHA1 mismatch for $($file.name)"
    }
}

# The tar of Windows (bsdtar) has bzip2 built in; GNU tar runs a program for it, by default lbzip2 on Ubuntu
$tarArguments = @('-xf', $archive, '-C', $target, '--strip-components=1')
if (-not $IsWindows) {
    $bzip2 = 'lbzip2', 'bzip2' | Where-Object { Get-Command $_ -CommandType Application -ErrorAction SilentlyContinue } |
        Select-Object -First 1
    if (-not $bzip2) { throw 'Unpacking the CEF archive (.tar.bz2) needs lbzip2 or bzip2, e.g. sudo apt-get install lbzip2' }
    $tarArguments = @("--use-compress-program=$bzip2") + $tarArguments
}

Write-Host "Extracting to $target"
if (Test-Path $target) { Remove-Item $target -Recurse -Force }
New-Item -ItemType Directory -Force $target | Out-Null
tar @tarArguments
if ($LASTEXITCODE -ne 0) { throw "tar failed with exit code $LASTEXITCODE" }
Set-Content $marker $expected
Write-Host "CEF $version ($Platform, $Type) is ready"
