param(
    [string]$ProjectFile = "EssayOrganizer.App\EssayOrganizer.App.csproj",
    [string]$IssFile = "EssayOrganizer.iss"
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not [System.IO.Path]::IsPathRooted($ProjectFile)) {
    $ProjectFile = Join-Path $scriptDir $ProjectFile
}
if (-not [System.IO.Path]::IsPathRooted($IssFile)) {
    $IssFile = Join-Path $scriptDir $IssFile
}

if (-not (Test-Path $ProjectFile)) {
    Write-Error "Project file not found: $ProjectFile"
    exit 1
}

$csprojContent = Get-Content -Path $ProjectFile -Raw -Encoding UTF8

if ($csprojContent -match '<Version>(\d+)\.(\d+)\.(\d+)\.(\d+)</Version>') {
    $major = [int]$matches[1]
    $minor = [int]$matches[2]
    $build = [int]$matches[3]
    $rev   = [int]$matches[4] + 1

    $oldVersion = "$($matches[1]).$($matches[2]).$($matches[3]).$($matches[4])"
    $newVersion = "$major.$minor.$build.$rev"
}
elseif ($csprojContent -match '<Version>(\d+)\.(\d+)\.(\d+)</Version>') {
    $major = [int]$matches[1]
    $minor = [int]$matches[2]
    $build = [int]$matches[3] + 1
    $rev = 0

    $oldVersion = "$($matches[1]).$($matches[2]).$($matches[3])"
    $newVersion = "$major.$minor.$build.$rev"
}
else {
    Write-Error "Could not parse <Version> from $ProjectFile"
    exit 1
}

# Update csproj
$csprojContent = $csprojContent -replace '<Version>[^<]+</Version>', "<Version>$newVersion</Version>"
$csprojContent = $csprojContent -replace '<AssemblyVersion>[^<]+</AssemblyVersion>', "<AssemblyVersion>$newVersion</AssemblyVersion>"
$csprojContent = $csprojContent -replace '<FileVersion>[^<]+</FileVersion>', "<FileVersion>$newVersion</FileVersion>"

[System.IO.File]::WriteAllText($ProjectFile, $csprojContent, [System.Text.Encoding]::UTF8)

# Update ISS if exists
if (Test-Path $IssFile) {
    $issContent = Get-Content -Path $IssFile -Raw -Encoding UTF8
    $issContent = $issContent -replace '#define MyAppVersion "[^"]+"', "#define MyAppVersion `"$newVersion`""
    [System.IO.File]::WriteAllText($IssFile, $issContent, [System.Text.Encoding]::UTF8)
}

Write-Host "Version bumped successfully: $oldVersion -> $newVersion"
# Output new version string
$newVersion
