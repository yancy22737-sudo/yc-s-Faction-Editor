param (
    [string]$File = "FactionGearModification.csproj"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $File)) {
    Write-Error "File not found: $File"
    exit 1
}

$content = Get-Content $File -Raw
# Look for <Version>x.y.z</Version>
if ($content -match '<Version>(\d+)\.(\d+)\.(\d+)</Version>') {
    $major = $matches[1]
    $minor = $matches[2]
    $build = [int]$matches[3] + 1
    $newVersionTag = "<Version>$major.$minor.$build</Version>"
    $content = $content -replace '<Version>\d+\.\d+\.\d+</Version>', $newVersionTag
    [System.IO.File]::WriteAllText((Resolve-Path $File).Path, $content)
    Write-Host "Updated version to $major.$minor.$build"
} else {
    # If not found, try to insert into PropertyGroup
    if ($content -match '</PropertyGroup>') {
        $content = $content -replace '</PropertyGroup>', "    <Version>1.0.0</Version>`r`n  </PropertyGroup>"
        [System.IO.File]::WriteAllText((Resolve-Path $File).Path, $content)
        Write-Host "Initialized version to 1.0.0"
    } else {
        Write-Error "Could not find <Version> tag or </PropertyGroup> to insert version."
        exit 1
    }
}
