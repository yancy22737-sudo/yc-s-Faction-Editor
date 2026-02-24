$ErrorActionPreference = "Stop"

Write-Host "Checking for running RimWorld process..."
Get-Process RimWorldWin64 -ErrorAction SilentlyContinue | Stop-Process -Force

$sourceRoot = "C:\Users\Administrator\source\repos\FactionGearModification"
$destRoot = "E:\SteamLibrary\steamapps\common\RimWorld\Mods\FactionGearCustomizer"

Write-Host "Building project..."
dotnet build "$sourceRoot\FactionGearModification\FactionGearModification.csproj" -c Release

if ($LASTEXITCODE -ne 0) { 
    Write-Error "Build failed."
    exit 1 
}

$dllSource = "$sourceRoot\FactionGearModification\bin\Release\net48\FactionGearCustomizer.dll"
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
Copy-Item "$sourceRoot\1.6" "$destRoot" -Recurse -Force

Write-Host "Copying VersionLog.txt..."
Copy-Item "$sourceRoot\VersionLog.txt" "$destRoot\VersionLog.txt" -Force

Write-Host "Build and deploy complete."
