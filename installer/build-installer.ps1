[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(?:\.\d+)?$')]
    [string]$AppVersion = '1.0.0',

    [string]$CustomerName = 'Customer',

    [switch]$SkipBuild,

    [string]$IsccPath = '',

    [string]$ConfuserCliPath = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot 'LSTool\LSTool.csproj'
$profilePath = Join-Path `
    $repositoryRoot `
    'LSTool\Resources\Settings\ReleaseProfile.dat'
$installerScript = Join-Path $PSScriptRoot 'LSTools.iss'
$outputDirectory = Join-Path $PSScriptRoot 'dist'
$stagingDirectory = Join-Path $PSScriptRoot 'staging'
$protectionDirectory = Join-Path $PSScriptRoot 'protection-maps'
$protectionScript = Join-Path $PSScriptRoot 'protect-release.ps1'
$protectionVerifier = Join-Path `
    $repositoryRoot `
    'tests\ObfuscationKernelTests\ObfuscationKernelTests.csproj'
$protectionVerifierNuGetConfig = Join-Path `
    $repositoryRoot `
    'tests\ObfuscationKernelTests\NuGet.Config'

function Get-InnoCompilerPath {
    param([string]$RequestedPath)

    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) {
        if (-not (Test-Path -LiteralPath $RequestedPath -PathType Leaf)) {
            throw "ISCC.exe was not found at: $RequestedPath"
        }

        return (Resolve-Path -LiteralPath $RequestedPath).Path
    }

    $command = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    $candidates = @(
        'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
        'C:\Program Files\Inno Setup 6\ISCC.exe'
    )
    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return $candidate
        }
    }

    throw (
        'Inno Setup 6 is not installed. Install it and retry, ' +
        'or pass -IsccPath with the full path to ISCC.exe.'
    )
}

function Assert-ReleaseProfile {
    if (-not (Test-Path -LiteralPath $profilePath -PathType Leaf)) {
        throw "Missing release profile: $profilePath"
    }

    $encodedProfile = (Get-Content -LiteralPath $profilePath -Raw).Trim()
    try {
        $credential = [Text.Encoding]::UTF8.GetString(
            [Convert]::FromBase64String($encodedProfile))
    }
    catch {
        throw 'ReleaseProfile.dat is not valid Base64.'
    }

    if ($credential -notmatch '^LST-(?:[A-F0-9]{4}-){5}[A-F0-9]{4}$') {
        throw 'ReleaseProfile.dat does not contain a valid LSTools profile.'
    }
}

function Assert-PublishedPackage {
    param(
        [string]$PackagePath,
        [string]$ManifestPath,
        [string]$RevitVersion
    )

    if (-not (Test-Path -LiteralPath $PackagePath -PathType Container)) {
        throw "Missing Revit $RevitVersion package: $PackagePath"
    }

    $requiredPaths = @(
        $ManifestPath,
        (Join-Path $PackagePath 'LSTool.dll'),
        (Join-Path `
            $PackagePath `
            'Resources\Settings\ReleaseChannel.json')
    )
    foreach ($requiredPath in $requiredPaths) {
        if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
            throw "Revit $RevitVersion package is missing: $requiredPath"
        }
    }

    $looseProfile = @(
        Get-ChildItem `
            -LiteralPath $PackagePath `
            -Recurse `
            -File `
            -Filter 'ReleaseProfile.dat'
    )
    if ($looseProfile.Count -ne 0) {
        throw (
            "Revit $RevitVersion package contains a loose " +
            'ReleaseProfile.dat file.'
        )
    }

    $obsoleteConfiguration = @(
        Get-ChildItem `
            -LiteralPath $PackagePath `
            -Recurse `
            -File `
            -Filter 'LicenseServer.json'
    )
    if ($obsoleteConfiguration.Count -ne 0) {
        throw (
            "Revit $RevitVersion package contains obsolete " +
            'LicenseServer.json.'
        )
    }
}

Assert-ReleaseProfile

$releasePackages = @(
    @{
        Configuration = 'Release R24'
        RevitVersion = '2024'
        PackagePath = Join-Path `
            $repositoryRoot `
            'LSTool\bin\Release R24'
        ManifestPath = Join-Path `
            $PSScriptRoot `
            'manifests\LSTool.R24.addin'
    },
    @{
        Configuration = 'Release R25'
        RevitVersion = '2025'
        PackagePath = Join-Path `
            $repositoryRoot `
            'LSTool\bin\Release R25'
        ManifestPath = Join-Path `
            $PSScriptRoot `
            'manifests\LSTool.R25.addin'
    },
    @{
        Configuration = 'Release R26'
        RevitVersion = '2026'
        PackagePath = Join-Path `
            $repositoryRoot `
            'LSTool\bin\Release R26'
        ManifestPath = Join-Path `
            $PSScriptRoot `
            'manifests\LSTool.R26.addin'
    }
)

if (-not $SkipBuild) {
    foreach ($releasePackage in $releasePackages) {
        Write-Host (
            'Building LSTools ' +
            $releasePackage.Configuration +
            '...'
        )
        & dotnet build `
            $projectPath `
            -c $releasePackage.Configuration `
            -p:DeployRevitAddin=false `
            --nologo `
            '-clp:ErrorsOnly;Summary'
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed: $($releasePackage.Configuration)"
        }
    }
}

foreach ($releasePackage in $releasePackages) {
    Assert-PublishedPackage `
        -PackagePath $releasePackage.PackagePath `
        -ManifestPath $releasePackage.ManifestPath `
        -RevitVersion $releasePackage.RevitVersion
}

$safeCustomerName = (
    $CustomerName.Trim() -replace '[^A-Za-z0-9._-]+', '-'
).Trim('-')
if ([string]::IsNullOrWhiteSpace($safeCustomerName)) {
    $safeCustomerName = 'Customer'
}

$outputBaseFilename = (
    "LSTools-$safeCustomerName-$AppVersion-Setup"
)

$verifierArguments = @()
foreach ($releasePackage in $releasePackages) {
    $revitVersion = $releasePackage.RevitVersion
    $stagingPath = Join-Path $stagingDirectory $revitVersion
    $protectionOutputPath = Join-Path `
        $protectionDirectory `
        (Join-Path $outputBaseFilename $revitVersion)

    & $protectionScript `
        -PackagePath $releasePackage.PackagePath `
        -StagingPath $stagingPath `
        -ProtectionOutputPath $protectionOutputPath `
        -RevitVersion $revitVersion `
        -ConfuserCliPath $ConfuserCliPath
    if ($LASTEXITCODE -ne 0) {
        throw "DLL protection failed: Revit $revitVersion"
    }

    $verifierArguments += @(
        $revitVersion,
        (Join-Path $releasePackage.PackagePath 'LSTool.dll'),
        (Join-Path $stagingPath 'LSTool.dll')
    )
}

Write-Host 'Verifying protected assemblies...'
& dotnet restore `
    $protectionVerifier `
    --configfile $protectionVerifierNuGetConfig `
    --nologo
if ($LASTEXITCODE -ne 0) {
    throw 'Protected assembly verifier restore failed.'
}

& dotnet run `
    --project $protectionVerifier `
    -c Release `
    --no-restore `
    -- `
    @verifierArguments
if ($LASTEXITCODE -ne 0) {
    throw 'Protected assembly verification failed.'
}

$compilerPath = Get-InnoCompilerPath -RequestedPath $IsccPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

Write-Host "Compiling $outputBaseFilename.exe..."
& $compilerPath `
    '/Qp' `
    "/DAppVersion=$AppVersion" `
    "/DOutputBaseFilename=$outputBaseFilename" `
    $installerScript
if ($LASTEXITCODE -ne 0) {
    throw 'Inno Setup compilation failed.'
}

$installerPath = Join-Path `
    $outputDirectory `
    ($outputBaseFilename + '.exe')
if (-not (Test-Path -LiteralPath $installerPath -PathType Leaf)) {
    throw "Installer was not created: $installerPath"
}

$installerFile = Get-Item -LiteralPath $installerPath
$installerHash = Get-FileHash `
    -LiteralPath $installerPath `
    -Algorithm SHA256

Write-Host ''
Write-Host 'Installer is ready:'
Write-Host "  File: $($installerFile.FullName)"
Write-Host "  Size: $($installerFile.Length) bytes"
Write-Host "  SHA256: $($installerHash.Hash)"
