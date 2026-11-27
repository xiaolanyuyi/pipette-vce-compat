<#
.SYNOPSIS
    Builds PipetteVceCompat.dll and copies it into the mod/ folder.

.EXAMPLE
    .\build.ps1
    .\build.ps1 -GameManagedDir "D:\SteamLibrary\steamapps\common\Probably Stolen\Probably Stolen_Data\Managed"
#>
param(
    [string]$GameManagedDir = "",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not $GameManagedDir) {
    $candidates = @()
    if ($env:PROBABLY_STOLEN_MANAGED) { $candidates += $env:PROBABLY_STOLEN_MANAGED }
    foreach ($root in @("C:\Program Files (x86)\Steam", "C:\Program Files\Steam", "D:\SteamLibrary", "E:\SteamLibrary", "F:\SteamLibrary", "G:\SteamLibrary")) {
        if (Test-Path $root) {
            $candidates += "$root\steamapps\common\Probably Stolen\Probably Stolen_Data\Managed"
        }
    }
    foreach ($c in $candidates) {
        if (Test-Path (Join-Path $c "Assembly-CSharp.dll")) { $GameManagedDir = $c; break }
    }
}

if (-not $GameManagedDir -or -not (Test-Path (Join-Path $GameManagedDir "Assembly-CSharp.dll"))) {
    throw "Cannot find the game's Managed folder. Pass -GameManagedDir '...\Probably Stolen_Data\Managed' (or set PROBABLY_STOLEN_MANAGED)."
}

Write-Host "Game assemblies: $GameManagedDir"
& dotnet build (Join-Path $repoRoot "src\PipetteVceCompat\PipetteVceCompat.csproj") -c $Configuration -p:GameManagedDir="$GameManagedDir" --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed ($LASTEXITCODE)" }

$dll = Join-Path $repoRoot "src\PipetteVceCompat\bin\PipetteVceCompat.dll"
$target = Join-Path $repoRoot "mod\PipetteVceCompat.dll"
Copy-Item -LiteralPath $dll -Destination $target -Force
Write-Host "Copied to mod\PipetteVceCompat.dll"