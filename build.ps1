$ErrorActionPreference = "Stop"

Write-Host "Checking for running RimWorld process..."
Get-Process RimWorldWin64 -ErrorAction SilentlyContinue | Stop-Process -Force

$sourceRoot = "c:\Users\22737\source\repos\FactionGearModification"
$destRoot = "D:\SteamLibrary\steamapps\common\RimWorld\Mods\FactionGearCustomizer"

Write-Host "Building project..."
dotnet build "$sourceRoot\FactionGearModification\FactionGearModification.csproj" -c Debug

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit 1
}

$dllSource = "$sourceRoot\FactionGearModification\bin\Debug\net48\FactionGearCustomizer.dll"
$dllDest = "$sourceRoot\1.6\Assemblies"

if (-not (Test-Path $dllSource)) {
    Write-Error "DLL not found at $dllSource"
    exit 1
}

Write-Host "Copying DLL to local 1.6/Assemblies..."
if (-not (Test-Path $dllDest)) { New-Item -ItemType Directory -Path $dllDest -Force }
Copy-Item $dllSource $dllDest -Force

Write-Host "Deploying to Game Mod Folder: $destRoot"
if (-not (Test-Path $destRoot)) { New-Item -ItemType Directory -Path $destRoot -Force }

Write-Host "Copying About..."
Copy-Item "$sourceRoot\About" "$destRoot" -Recurse -Force

Write-Host "Copying 1.6... (includes Languages folder)"

# Preserve game-mod Logo.png — backup before copy, restore after
$logoDest = "$destRoot\1.6\Textures\UI\Logo.png"
$logoBackup = "$env:TEMP\FGC_Logo_backup.png"
$hadLogo = Test-Path $logoDest
if ($hadLogo) { Copy-Item $logoDest $logoBackup -Force }

Copy-Item "$sourceRoot\1.6" "$destRoot" -Recurse -Force

# Restore the game-mod Logo.png as the canonical copy
if ($hadLogo -and (Test-Path $logoBackup)) {
    $logoDir = Split-Path $logoDest -Parent
    if (-not (Test-Path $logoDir)) { New-Item -ItemType Directory -Path $logoDir -Force }
    Copy-Item $logoBackup $logoDest -Force
    Remove-Item $logoBackup -Force
    Write-Host "  Logo.png preserved from game mod folder"
}

Write-Host "Cleaning up stale icon files (superseded by Logo.png)..."
@(
    "$destRoot\1.6\Textures\UI\Buttons\MainButtons\FactionGearEdit.png",
    "$destRoot\1.6\Textures\UI\FactionGearCustomizer_ModIcon.png"
) | ForEach-Object {
    if (Test-Path $_) {
        Remove-Item $_ -Force
        Write-Host "  Removed: $_"
    }
}

Write-Host "Copying VersionLog.txt..."
Copy-Item "$sourceRoot\VersionLog.txt" "$destRoot\VersionLog.txt" -Force
Copy-Item "$sourceRoot\VersionLog_en.txt" "$destRoot\VersionLog_en.txt" -Force

Write-Host "Build and deploy complete."
