param(
    [string]$sourceRoot = "C:\Users\Administrator\source\repos\FactionGearModification",
    [string]$destRoot = "E:\SteamLibrary\steamapps\common\RimWorld\Mods\FactionGearCustomizer"
)

$ErrorActionPreference = "Stop"

Write-Host "Checking for running RimWorld process..."
Get-Process RimWorldWin64 -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "Deploying to Game Mod Folder: $destRoot"
if (-not (Test-Path $destRoot)) { New-Item -ItemType Directory -Path $destRoot -Force | Out-Null }

Write-Host "Copying About..."
Copy-Item "$sourceRoot\About" "$destRoot" -Recurse -Force

Write-Host "Copying 1.6... (includes Languages folder)"
Copy-Item "$sourceRoot\1.6" "$destRoot" -Recurse -Force

Write-Host "Copying VersionLog.txt..."
Copy-Item "$sourceRoot\VersionLog.txt" "$destRoot\VersionLog.txt" -Force

Write-Host "Deploy complete."
