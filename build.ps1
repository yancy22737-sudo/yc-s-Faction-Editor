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

try { Copy-Item "$sourceRoot\1.6" "$destRoot" -Recurse -Force -ErrorAction Stop } catch { Write-Host "WARNING: Full copy failed (file locked), copying files individually..."; Get-ChildItem "$sourceRoot\1.6" -Recurse -File | ForEach-Object { $dest = $_.FullName.Replace($sourceRoot, $destRoot); $destDir = Split-Path $dest; if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force }; try { Copy-Item $_.FullName $dest -Force -ErrorAction Stop } catch { Write-Host "  SKIP: $($_.Name) (locked)" } } }

Write-Host "Cleaning up stale icon files..."
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
