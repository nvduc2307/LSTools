[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackagePath,

    [Parameter(Mandatory)]
    [string]$StagingPath,

    [Parameter(Mandatory)]
    [string]$ProtectionOutputPath,

    [Parameter(Mandatory)]
    [string]$RevitVersion,

    [string]$ConfuserCliPath = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$confuserVersion = '1.6.0'
$confuserDownloadUrl = (
    'https://github.com/mkaring/ConfuserEx/releases/download/' +
    'v1.6.0/ConfuserEx-CLI.zip'
)
$confuserArchiveSha256 = (
    'A00DE7CDDC740F7EDB1BAAB4C6C9073553DCC88F7E873D15B7FD34DDD33753D7'
)
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$templatePath = Join-Path `
    $PSScriptRoot `
    'confuser\LSTools.safe.crproj.template'
$toolDirectory = Join-Path `
    $repositoryRoot `
    ('.tools\confuserex2\' + $confuserVersion)

function Get-NormalizedFullPath {
    param([Parameter(Mandatory)][string]$Path)

    return [IO.Path]::GetFullPath($Path).TrimEnd(
        [IO.Path]::DirectorySeparatorChar,
        [IO.Path]::AltDirectorySeparatorChar)
}

function Assert-PathWithinRoot {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$AllowedRoot
    )

    $fullPath = Get-NormalizedFullPath -Path $Path
    $fullRoot = Get-NormalizedFullPath -Path $AllowedRoot
    $rootPrefix = $fullRoot + [IO.Path]::DirectorySeparatorChar

    if (-not $fullPath.StartsWith(
            $rootPrefix,
            [StringComparison]::OrdinalIgnoreCase)) {
        throw "Generated path is outside the allowed root: $fullPath"
    }

    return $fullPath
}

function Reset-GeneratedDirectory {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$AllowedRoot
    )

    $fullPath = Assert-PathWithinRoot `
        -Path $Path `
        -AllowedRoot $AllowedRoot

    if (Test-Path -LiteralPath $fullPath) {
        Remove-Item -LiteralPath $fullPath -Recurse -Force
    }

    New-Item `
        -ItemType Directory `
        -Path $fullPath `
        -Force | Out-Null

    return $fullPath
}

function Resolve-ConfuserCli {
    if (-not [string]::IsNullOrWhiteSpace($ConfuserCliPath)) {
        if (-not (Test-Path -LiteralPath $ConfuserCliPath -PathType Leaf)) {
            throw "ConfuserEx2 CLI was not found at: $ConfuserCliPath"
        }

        return (Resolve-Path -LiteralPath $ConfuserCliPath).Path
    }

    $cachedCli = Join-Path $toolDirectory 'Confuser.CLI.exe'
    if (Test-Path -LiteralPath $cachedCli -PathType Leaf) {
        return (Resolve-Path -LiteralPath $cachedCli).Path
    }

    New-Item `
        -ItemType Directory `
        -Path $toolDirectory `
        -Force | Out-Null

    $archivePath = Join-Path `
        $toolDirectory `
        'ConfuserEx-CLI.zip.download'

    Write-Host "Downloading ConfuserEx2 CLI $confuserVersion..."
    [Net.ServicePointManager]::SecurityProtocol = `
        [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest `
        -Uri $confuserDownloadUrl `
        -OutFile $archivePath `
        -UseBasicParsing

    $actualHash = (
        Get-FileHash `
            -LiteralPath $archivePath `
            -Algorithm SHA256
    ).Hash
    if ($actualHash -ne $confuserArchiveSha256) {
        Remove-Item -LiteralPath $archivePath -Force
        throw (
            'ConfuserEx2 archive hash mismatch. ' +
            "Expected $confuserArchiveSha256 but received $actualHash."
        )
    }

    Expand-Archive `
        -LiteralPath $archivePath `
        -DestinationPath $toolDirectory `
        -Force
    Remove-Item -LiteralPath $archivePath -Force

    if (-not (Test-Path -LiteralPath $cachedCli -PathType Leaf)) {
        throw "ConfuserEx2 CLI was not extracted to: $cachedCli"
    }

    return (Resolve-Path -LiteralPath $cachedCli).Path
}

function Convert-ToXmlAttribute {
    param([Parameter(Mandatory)][string]$Value)

    return [Security.SecurityElement]::Escape($Value)
}

function Get-ConfuserProbePaths {
    param(
        [Parameter(Mandatory)][string]$AssemblyPath,
        [Parameter(Mandatory)][string]$ConfuserCli
    )

    $probePaths = [Collections.Generic.HashSet[string]]::new(
        [StringComparer]::OrdinalIgnoreCase)

    $sourceDirectory = Split-Path -Parent $AssemblyPath
    [void]$probePaths.Add($sourceDirectory)

    $dnlibPath = Join-Path `
        (Split-Path -Parent $ConfuserCli) `
        'dnlib.dll'
    if (-not (Test-Path -LiteralPath $dnlibPath -PathType Leaf)) {
        throw "ConfuserEx2 dependency reader was not found: $dnlibPath"
    }

    [Reflection.Assembly]::LoadFrom($dnlibPath) | Out-Null
    $module = [dnlib.DotNet.ModuleDefMD]::Load($AssemblyPath)
    try {
        $assemblyReferences = @($module.GetAssemblyRefs())
    }
    finally {
        $module.Dispose()
    }

    $revitPackageNames = @{
        RevitAPI = 'nice3point.revit.api.revitapi'
        RevitAPIUI = 'nice3point.revit.api.revitapiui'
        AdWindows = 'nice3point.revit.api.adwindows'
    }
    $globalPackages = if (
        [string]::IsNullOrWhiteSpace($env:NUGET_PACKAGES)
    ) {
        Join-Path $env:USERPROFILE '.nuget\packages'
    }
    else {
        $env:NUGET_PACKAGES
    }

    foreach ($assemblyReference in $assemblyReferences) {
        $referenceName = $assemblyReference.Name.ToString()
        if (-not $revitPackageNames.ContainsKey($referenceName)) {
            continue
        }

        $packageRoot = Join-Path `
            $globalPackages `
            $revitPackageNames[$referenceName]
        $matchingAssembly = Get-ChildItem `
            -LiteralPath $packageRoot `
            -Recurse `
            -File `
            -Filter ($referenceName + '.dll') `
            -ErrorAction SilentlyContinue |
            Where-Object {
                try {
                    [Reflection.AssemblyName]::GetAssemblyName(
                        $_.FullName
                    ).Version -eq $assemblyReference.Version
                }
                catch {
                    $false
                }
            } |
            Sort-Object FullName -Descending |
            Select-Object -First 1

        if ($null -eq $matchingAssembly) {
            throw (
                "Could not find $referenceName " +
                "$($assemblyReference.Version) in the NuGet cache."
            )
        }

        [void]$probePaths.Add($matchingAssembly.DirectoryName)
    }

    $dotnetCommand = Get-Command 'dotnet.exe' -ErrorAction SilentlyContinue
    if ($null -eq $dotnetCommand) {
        $dotnetCommand = Get-Command 'dotnet' -ErrorAction SilentlyContinue
    }
    if ($null -ne $dotnetCommand) {
        $dotnetRoot = Split-Path -Parent $dotnetCommand.Source
        foreach ($packName in @(
                'Microsoft.NETCore.App.Ref',
                'Microsoft.WindowsDesktop.App.Ref'
            )) {
            $packRoot = Join-Path `
                (Join-Path $dotnetRoot 'packs') `
                $packName
            if (-not (Test-Path -LiteralPath $packRoot -PathType Container)) {
                continue
            }

            $referenceDirectory = Get-ChildItem `
                -LiteralPath $packRoot `
                -Directory |
                Where-Object {
                    Test-Path `
                        -LiteralPath (
                            Join-Path $_.FullName 'ref\net8.0'
                        ) `
                        -PathType Container
                } |
                Sort-Object { [Version]$_.Name } -Descending |
                ForEach-Object {
                    Join-Path $_.FullName 'ref\net8.0'
                } |
                Select-Object -First 1
            if ($null -ne $referenceDirectory) {
                [void]$probePaths.Add($referenceDirectory)
            }
        }
    }

    $netFrameworkReferences = (
        'C:\Program Files (x86)\Reference Assemblies\' +
        'Microsoft\Framework\.NETFramework\v4.8'
    )
    if (Test-Path -LiteralPath $netFrameworkReferences -PathType Container) {
        [void]$probePaths.Add($netFrameworkReferences)
    }

    return @($probePaths)
}

if (-not (Test-Path -LiteralPath $PackagePath -PathType Container)) {
    throw "Release package was not found: $PackagePath"
}
if (-not (Test-Path -LiteralPath $templatePath -PathType Leaf)) {
    throw "ConfuserEx2 template was not found: $templatePath"
}

$resolvedPackagePath = (Resolve-Path -LiteralPath $PackagePath).Path
$sourceAssembly = Join-Path $resolvedPackagePath 'LSTool.dll'
if (-not (Test-Path -LiteralPath $sourceAssembly -PathType Leaf)) {
    throw "LSTool.dll was not found in: $resolvedPackagePath"
}

$stagingRoot = Join-Path $PSScriptRoot 'staging'
$protectionRoot = Join-Path $PSScriptRoot 'protection-maps'
$resolvedStagingPath = Reset-GeneratedDirectory `
    -Path $StagingPath `
    -AllowedRoot $stagingRoot
$resolvedProtectionPath = Reset-GeneratedDirectory `
    -Path $ProtectionOutputPath `
    -AllowedRoot $protectionRoot

Get-ChildItem -LiteralPath $resolvedPackagePath -Force |
    Copy-Item `
        -Destination $resolvedStagingPath `
        -Recurse `
        -Force

$template = Get-Content -LiteralPath $templatePath -Raw
$confuserCli = Resolve-ConfuserCli
$probePathElements = Get-ConfuserProbePaths `
    -AssemblyPath $sourceAssembly `
    -ConfuserCli $confuserCli |
    ForEach-Object {
        '  <probePath>' +
        (Convert-ToXmlAttribute -Value $_) +
        '</probePath>'
    }
$configuration = $template.
    Replace(
        '__BASE_DIR__',
        (Convert-ToXmlAttribute -Value $resolvedPackagePath)).
    Replace(
        '__OUTPUT_DIR__',
        (Convert-ToXmlAttribute -Value $resolvedProtectionPath)).
    Replace(
        '__PROBE_PATHS__',
        ($probePathElements -join [Environment]::NewLine))
$configurationPath = Join-Path `
    $resolvedProtectionPath `
    "LSTools-R$RevitVersion.crproj"
[IO.File]::WriteAllText(
    $configurationPath,
    $configuration,
    [Text.UTF8Encoding]::new($false))

Write-Host "Protecting LSTools for Revit $RevitVersion..."
& $confuserCli -n $configurationPath
if ($LASTEXITCODE -ne 0) {
    throw "ConfuserEx2 failed for Revit $RevitVersion."
}

$protectedAssembly = Join-Path `
    $resolvedProtectionPath `
    'LSTool.dll'
$symbolMap = Join-Path `
    $resolvedProtectionPath `
    'symbols.map'
if (-not (Test-Path -LiteralPath $protectedAssembly -PathType Leaf)) {
    throw "ConfuserEx2 did not create: $protectedAssembly"
}
if (-not (Test-Path -LiteralPath $symbolMap -PathType Leaf)) {
    throw "ConfuserEx2 did not create: $symbolMap"
}

$sourceHash = (
    Get-FileHash `
        -LiteralPath $sourceAssembly `
        -Algorithm SHA256
).Hash
$protectedHash = (
    Get-FileHash `
        -LiteralPath $protectedAssembly `
        -Algorithm SHA256
).Hash
if ($sourceHash -eq $protectedHash) {
    throw "Protected Revit $RevitVersion DLL matches the source DLL."
}

$mapEntryCount = @(
    Get-Content -LiteralPath $symbolMap |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
).Count
if ($mapEntryCount -lt 20) {
    throw (
        "ConfuserEx2 renamed only $mapEntryCount symbols for " +
        "Revit $RevitVersion; expected at least 20."
    )
}

Copy-Item `
    -LiteralPath $protectedAssembly `
    -Destination (Join-Path $resolvedStagingPath 'LSTool.dll') `
    -Force

Write-Host "  Source SHA256:    $sourceHash"
Write-Host "  Protected SHA256: $protectedHash"
Write-Host "  Renamed symbols:  $mapEntryCount"
Write-Host "  Private map:      $symbolMap"
