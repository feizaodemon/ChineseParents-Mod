# Builds the release DLL and packs it with BepInEx into one zip that installs by unzipping into the game folder.
# Usage: pwsh tools/package.ps1 [-GameDir <path with a working BepInEx install>]
param(
    [string]$GameDir = 'E:\Steam\steamapps\common\ChineseParent'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repo 'src\ChineseParentsMod'

$version = [regex]::Match((Get-Content (Join-Path $project 'Plugin.cs') -Raw), '"Chinese Parents Cheat Panel", "([^"]+)"').Groups[1].Value
if (-not $version) { throw 'Could not read the plugin version from Plugin.cs.' }

dotnet build $project -c Release --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

$stage = Join-Path $repo 'build\package'
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force (Join-Path $stage 'BepInEx\plugins\ChineseParentsMod') | Out-Null

foreach ($file in 'winhttp.dll', 'doorstop_config.ini', '.doorstop_version') {
    Copy-Item (Join-Path $GameDir $file) $stage
}
Copy-Item (Join-Path $GameDir 'BepInEx\core') (Join-Path $stage 'BepInEx\core') -Recurse
Copy-Item (Join-Path $project 'bin\Release\netstandard2.1\ChineseParentsMod.dll') (Join-Path $stage 'BepInEx\plugins\ChineseParentsMod')
Copy-Item (Join-Path $repo 'docs\使用说明.txt') $stage

$zip = Join-Path $repo "build\ChineseParentsMod-$version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
# Compress-Archive skips names starting with a dot, so add the folder contents with .NET directly.
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($stage, $zip)
Write-Host "Packed $zip"
