<#
.SYNOPSIS
Build script for .Net project 

.DESCRIPTION
This script performs:
1. Clean solution
2. Get application version
3. Publish for specified platforms
4. Final report

.PARAMETER RuntimeIdentifiers
Platforms to build for (default: "win-x64,linux-x64")

.EXAMPLE
./build.ps1 -ProjectPath "./MyProject/MyProject.csproj"
#>

param (
    [Parameter(Mandatory = $true)]
    [string]$ProjectPath,
    [string]$OutputPath = "./artifacts",    
    [string[]]$RuntimeIdentifiers = @("win-x64", "linux-x64")
)

# Verify project file
if (-not (Test-Path $ProjectPath)) {
    Write-Error "Project file not found: $ProjectPath"
    exit 1
}

# Project configuration
$projectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)
$publishDir = "$OutputPath/publish"

# 1. Clean solution
Write-Host "Cleaning solution..." -ForegroundColor Cyan
Get-ChildItem ./ -include bin, obj -Recurse | ForEach-Object ($_) { remove-item $_.fullname -Force -Recurse }
if (Test-Path $OutputPath) {
    Remove-Item $OutputPath -Recurse -Force
}

# Create directories
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

# 2. Get application version
Write-Host "`nGetting application version..." -ForegroundColor Cyan
$xdoc = [System.Xml.Linq.XDocument]::Load((Resolve-Path $projectPath))
$nodes = $xdoc.Root.Element("PropertyGroup").DescendantNodes().Where({ $_.Name.LocalName -eq "Version" })
if ($nodes.Count -eq 1) {
    $versionNode = $nodes | Select-Object -First 1
    $version = $versionNode.Value.Trim()
    Write-Host "Version found in project file: $version" -ForegroundColor Green
}
else {
    $version = "1.0.0"  
    Write-Host "No version found in project file, using default: $version" -ForegroundColor Yellow 
}

# 3. Build and publish for each platform
foreach ($rid in $RuntimeIdentifiers) {
    Write-Host "`nProcessing platform: $rid" -ForegroundColor Yellow

    $sc = "self-contained"
    $platformPublishDir = "$publishDir\$rid"
    $archiveName = "$projectName-$rid-$version.zip"
    $archivePath = "$OutputPath/$archiveName"

    # Publish project
    Write-Host "Publishing for $rid..." -ForegroundColor Cyan
    dotnet publish $projectPath -c Release -r $rid -o $platformPublishDir `
        /p:DebugType=None /p:DebugSymbols=false

    # Archive results
    Write-Host "Creating archive for $rid..." -ForegroundColor Cyan
    Compress-Archive -Path "$platformPublishDir/*" -DestinationPath $archivePath -CompressionLevel Optimal

    # Clean Publishing folder
    Write-Host "Cleaning publishing folder..." -ForegroundColor Cyan
    Remove-Item  $platformPublishDir -Recurse -Force

    $archiveName = "$projectName-$sc-$rid-$version.zip"
    $archivePath = "$OutputPath/$archiveName"

    # Publish self-contained project
    Write-Host "Publishing for $sc $rid..." -ForegroundColor Cyan
    dotnet publish $projectPath -c Release -r $rid -o $platformPublishDir --sc `
        /p:DebugType=None /p:DebugSymbols=false

    # Archive results
    Write-Host "Creating archive for $sc $rid..." -ForegroundColor Cyan
    Compress-Archive -Path "$platformPublishDir/*" -DestinationPath $archivePath -CompressionLevel Optimal

    Write-Host "Platform $rid completed. Archive: $archivePath" -ForegroundColor Green
}

# 4. Final report
Write-Host "`nBuild completed successfully!" -ForegroundColor Green
Write-Host "Artifacts location: $(Resolve-Path $OutputPath)" -ForegroundColor Green
Write-Host "Generated files:" -ForegroundColor Green
Get-ChildItem $OutputPath | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize
