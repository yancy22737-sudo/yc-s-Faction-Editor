param(
    [string]$ProjectDir
)

$ErrorActionPreference = "Stop"

$versionFile = Join-Path $ProjectDir "VersionLog.txt"
$propsDir = Join-Path $ProjectDir "Properties"
if (-not (Test-Path $propsDir)) {
    New-Item -ItemType Directory -Path $propsDir | Out-Null
}
$versionInfoPath = Join-Path $propsDir "VersionInfo.cs"
$csprojPath = Join-Path $ProjectDir "FactionGearModification.csproj"

# Read current version from csproj
if (-not (Test-Path $csprojPath)) {
    Write-Error "Project file not found: $csprojPath"
    exit 1
}

$csprojContent = Get-Content -Path $csprojPath -Raw
$versionRegex = [regex]"(?<=<Version>)(.*?)(?=</Version>)"
$match = $versionRegex.Match($csprojContent)

if ($match.Success) {
    $currentVersion = $match.Value
} else {
    $currentVersion = "1.0.0"
}

# Increment build version
try {
    # Handle versions like 1.0 which Version class parses as Major.Minor
    $parts = $currentVersion.Split('.')
    if ($parts.Count -lt 3) {
        $major = $parts[0]
        $minor = if ($parts.Count -gt 1) { $parts[1] } else { "0" }
        $build = 0
    } else {
        $major = $parts[0]
        $minor = $parts[1]
        $build = $parts[2]
    }
    
    $newBuild = [int]$build + 1
    $newVersion = "$major.$minor.$newBuild"
} catch {
    $newVersion = "1.0.1"
}

# Update csproj
$newCsprojContent = $versionRegex.Replace($csprojContent, $newVersion)
Set-Content -Path $csprojPath -Value $newCsprojContent -Encoding UTF8

# Generate VersionInfo.cs
$versionInfoContent = @"
using System.Reflection;

[assembly: AssemblyVersion("$newVersion")]
[assembly: AssemblyFileVersion("$newVersion")]
[assembly: AssemblyInformationalVersion("$newVersion")]
"@
Set-Content -Path $versionInfoPath -Value $versionInfoContent -Encoding UTF8

# Append to log
$logEntry = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - Version updated from $currentVersion to $newVersion"
Add-Content -Path $versionFile -Value $logEntry -Encoding UTF8

Write-Host "Version updated to $newVersion"
