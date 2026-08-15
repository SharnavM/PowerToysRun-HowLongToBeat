param(
    [string]$Architecture = "x64"
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot

$VersionFile = Join-Path $Root "VERSION"

if (-not (Test-Path $VersionFile)) {
    throw "VERSION file not found."
}

$Version = (Get-Content $VersionFile -Raw).Trim()

if ($Version -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$') {
    throw "Invalid project version: $Version"
}

$Source = Join-Path `
    $Root `
    "artifacts\plugin\HowLongToBeat"

if (-not (Test-Path $Source)) {
    throw "Plugin build output not found: $Source"
}

$ReleaseRoot = Join-Path `
    $Root `
    "artifacts\release"

$StagingRoot = Join-Path `
    $ReleaseRoot `
    "staging"

$StagingPlugin = Join-Path `
    $StagingRoot `
    "HowLongToBeat"

$ThirdPartySource = Join-Path `
    $Root `
    "third_party\licenses"

$ThirdPartyDestination = Join-Path `
    $StagingPlugin `
    "third_party\licenses"

if (-not (Test-Path $ThirdPartySource)) {
    throw "Third-party license directory not found."
}

$ArchiveName = `
    "PowerToysRun-HowLongToBeat-v$Version-$Architecture.zip"

$ArchivePath = Join-Path `
    $ReleaseRoot `
    $ArchiveName

$ChecksumPath = "$ArchivePath.sha256"

Write-Host ""
Write-Host "========================================"
Write-Host "Packaging HowLongToBeat release"
Write-Host "========================================"
Write-Host ""
Write-Host "Version:      $Version"
Write-Host "Architecture: $Architecture"
Write-Host ""

Remove-Item `
    $StagingRoot `
    -Recurse `
    -Force `
    -ErrorAction SilentlyContinue

New-Item `
    -ItemType Directory `
    -Path $StagingPlugin `
    -Force |
    Out-Null

Copy-Item `
    -Path "$Source\*" `
    -Destination $StagingPlugin `
    -Recurse `
    -Force

New-Item `
    -ItemType Directory `
    -Path $ThirdPartyDestination `
    -Force |
    Out-Null

Copy-Item `
    -Path "$ThirdPartySource\*" `
    -Destination $ThirdPartyDestination `
    -Recurse `
    -Force

Get-ChildItem `
    $StagingPlugin `
    -Recurse `
    -Filter *.pdb |
    Remove-Item -Force

$RequiredFiles = @(
    "plugin.json",
    "Community.PowerToys.Run.Plugin.HowLongToBeat.dll",
    "Bridge\hltb-bridge.exe"
)

foreach ($RelativePath in $RequiredFiles) {
    $Required = Join-Path `
        $StagingPlugin `
        $RelativePath

    if (-not (Test-Path $Required)) {
        throw "Required release file missing: $RelativePath"
    }
}

$ForbiddenFiles = @(
    "Wox.Plugin.dll",
    "PowerToys.Settings.UI.Lib.dll"
)

foreach ($ForbiddenName in $ForbiddenFiles) {
    $Found = Get-ChildItem `
        $StagingPlugin `
        -Recurse `
        -Filter $ForbiddenName `
        -ErrorAction SilentlyContinue

    if ($Found) {
        throw "Host DLL must not be packaged: $ForbiddenName"
    }
}

Remove-Item `
    $ArchivePath `
    -Force `
    -ErrorAction SilentlyContinue

Remove-Item `
    $ChecksumPath `
    -Force `
    -ErrorAction SilentlyContinue

$MaxAttempts = 5

for ($Attempt = 1; $Attempt -le $MaxAttempts; $Attempt++) {
    try {
        Remove-Item `
            $ArchivePath `
            -Force `
            -ErrorAction SilentlyContinue

        Compress-Archive `
            -Path $StagingPlugin `
            -DestinationPath $ArchivePath `
            -CompressionLevel Optimal

        break
    }
    catch {
        if ($Attempt -eq $MaxAttempts) {
            throw
        }

        Write-Host ""
        Write-Host "Archive file is temporarily locked. Retrying..."

        Start-Sleep -Seconds 1
    }
}

$Hash = Get-FileHash `
    $ArchivePath `
    -Algorithm SHA256

"$($Hash.Hash.ToLowerInvariant())  $ArchiveName" |
    Set-Content `
        -Path $ChecksumPath `
        -Encoding ascii

$ArchiveSize = (
    Get-Item $ArchivePath
).Length / 1MB

Write-Host ""
Write-Host "Release package created:"
Write-Host $ArchivePath
Write-Host ""
Write-Host (
    "Size: {0:N2} MB" -f $ArchiveSize
)
Write-Host ""
Write-Host "SHA256:"
Write-Host $Hash.Hash.ToLowerInvariant()
Write-Host ""
