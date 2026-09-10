<#
    Builds the release zips.

        powershell -ExecutionPolicy Bypass -File packaging\build-package.ps1

    Produces, in dist\:
      CraftFromChests-<version>-thunderstore.zip
          manifest.json, icon.png, README.md, CHANGELOG.md and the dll at the root.
          Upload this to Thunderstore, or feed it to r2modman / Thunderstore Mod
          Manager through "Import local mod".
      CraftFromChests-<version>-nexus.zip
          BepInEx\plugins\CraftFromChests\CraftFromChests.dll, so the player can
          extract it straight over the Valheim folder. Upload this to Nexus Mods.

    The version comes from <Version> in CraftFromChests.csproj and from nowhere
    else, so bump it there and everything follows.
#>
[CmdletBinding()]
param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Drawing

$root      = Split-Path -Parent $PSScriptRoot
$csproj    = Join-Path $root 'CraftFromChests.csproj'
$dist      = Join-Path $root 'dist'
$stage     = Join-Path $root 'obj\package'
$dll       = Join-Path $root 'bin\Release\CraftFromChests.dll'
$icon      = Join-Path $PSScriptRoot 'icon.png'
$template  = Join-Path $PSScriptRoot 'manifest.template.json'

# ---- version, single source of truth
$version = ([xml](Get-Content $csproj)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if ($version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Version '$version' in the csproj must be x.y.z, Thunderstore rejects anything else."
}
Write-Host "version $version"

# ---- build
if (-not $SkipBuild) {
    Write-Host 'building...'
    & dotnet build $csproj -c Release -v minimal
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed' }
}
if (-not (Test-Path $dll)) { throw "missing $dll, build it first" }

$dllVersion = [System.Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString()
if (-not $dllVersion.StartsWith($version)) {
    throw "dll is version $dllVersion but the csproj says $version, rebuild"
}

# ---- icon has to be exactly 256x256
$img = [System.Drawing.Image]::FromFile($icon)
try {
    if ($img.Width -ne 256 -or $img.Height -ne 256) {
        throw "icon.png is $($img.Width)x$($img.Height), Thunderstore requires exactly 256x256"
    }
} finally { $img.Dispose() }

# ---- manifest
$manifestText = (Get-Content $template -Raw).Replace('{{VERSION}}', $version)
$manifest = $manifestText | ConvertFrom-Json
if ($manifest.name -notmatch '^[a-zA-Z0-9_]+$') { throw "manifest name '$($manifest.name)' may only contain letters, digits and underscore" }
if ($manifest.description.Length -gt 250)       { throw "manifest description is $($manifest.description.Length) chars, the limit is 250" }
if (-not $manifest.dependencies)                { Write-Warning 'manifest has no dependencies, players will have to install BepInEx themselves' }

if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force -Path $stage, $dist | Out-Null
$manifestPath = Join-Path $stage 'manifest.json'
[System.IO.File]::WriteAllText($manifestPath, $manifestText.TrimEnd() + "`n", (New-Object System.Text.UTF8Encoding($false)))

# ---- zip writer, entry names use forward slashes so the archives behave everywhere
function New-Zip {
    param([string]$Path, [hashtable[]]$Entries)

    if (Test-Path $Path) { Remove-Item $Path -Force }
    $archive = [System.IO.Compression.ZipFile]::Open($Path, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($e in $Entries) {
            if (-not (Test-Path $e.Source)) { throw "missing packaged file $($e.Source)" }
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $archive, (Resolve-Path $e.Source).Path, $e.Name,
                [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    } finally { $archive.Dispose() }
}

$readme    = Join-Path $root 'README.md'
$changelog = Join-Path $root 'CHANGELOG.md'

$thunderstoreZip = Join-Path $dist "CraftFromChests-$version-thunderstore.zip"
New-Zip -Path $thunderstoreZip -Entries @(
    @{ Source = $manifestPath; Name = 'manifest.json' }
    @{ Source = $icon;         Name = 'icon.png' }
    @{ Source = $readme;       Name = 'README.md' }
    @{ Source = $changelog;    Name = 'CHANGELOG.md' }
    @{ Source = $dll;          Name = 'CraftFromChests.dll' }
)

$nexusZip = Join-Path $dist "CraftFromChests-$version-nexus.zip"
New-Zip -Path $nexusZip -Entries @(
    @{ Source = $dll;       Name = 'BepInEx/plugins/CraftFromChests/CraftFromChests.dll' }
    @{ Source = $readme;    Name = 'README.md' }
    @{ Source = $changelog; Name = 'CHANGELOG.md' }
)

Remove-Item $stage -Recurse -Force

foreach ($z in @($thunderstoreZip, $nexusZip)) {
    $item = Get-Item $z
    $hash = (Get-FileHash $z -Algorithm SHA256).Hash
    Write-Host ("{0}  {1:N0} bytes  sha256 {2}" -f $item.Name, $item.Length, $hash)
}
