<#
.SYNOPSIS
    Packages the Fluence.Wpf.PowerShell module as a ZIP and a PowerShell Gallery .nupkg.
.DESCRIPTION
    Invokes Build-Module.ps1 to ensure assemblies are staged, then produces two
    distributable artifacts in the output directory:

      - Fluence.Wpf.PowerShell-<version>.zip   (drop-in for PSModulePath)
      - Fluence.Wpf.PowerShell.<version>.nupkg  (PowerShell Gallery format)

    The .nupkg is produced by Publish-Module targeting a temporary local file-based
    PSRepository. Nothing is published to PSGallery. A compatible NuGet provider must already be
    available; this script does not bootstrap one. Install the build prerequisites separately.
.PARAMETER Configuration
    MSBuild configuration passed to Build-Module.ps1. Defaults to 'Release'.
.PARAMETER OutputPath
    Directory that receives the zip and nupkg. Defaults to
    <repo>\Fluence.Wpf.PowerShell.Module\artifacts.
.NOTES
    Run from any location. Does not require a host application.
    Requires PowerShell 5.1 or 7+.
#>
[CmdletBinding()]
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseCompatibleCmdlets', '',
    Justification = 'The PowerShellGet cmdlets (Publish-Module, Register-PSRepository, Get-PackageProvider) ship in-box with Windows PowerShell 5.1 as a module, so the 5.1 core-cmdlet compatibility profile does not list them. This is a packaging script run by a developer or CI, not shipped module code.')]
param
(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [Parameter()]
    [string]$OutputPath = ''
)

$ErrorActionPreference = 'Stop'

$repo   = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$modSrc = Join-Path $repo 'Fluence.Wpf.PowerShell.Module\src\Fluence.Wpf.PowerShell'
$psd1   = Join-Path $modSrc 'Fluence.Wpf.PowerShell.psd1'

if ([string]::IsNullOrWhiteSpace($OutputPath))
{
    $OutputPath = Join-Path $repo 'Fluence.Wpf.PowerShell.Module\artifacts'
}

# Fail before staging or writing artifacts. Desktop uses the pinned 2.8.5.208 provider;
# current PowerShell 7 ships a compatible 3.0.0.1 provider with PackageManagement.
$nugetProvider = Get-PackageProvider -Name NuGet -ListAvailable -ErrorAction SilentlyContinue |
    Where-Object { $_.Version -ge [System.Version]'2.8.5.208' } |
    Select-Object -First 1
if ($null -eq $nugetProvider)
{
    throw 'NuGet provider 2.8.5.208 or later must already be installed. In a separate setup step with network access, run: Install-PackageProvider -Name NuGet -RequiredVersion 2.8.5.208 -Scope CurrentUser -Force. Packaging does not bootstrap providers.'
}

# 1. Stage assemblies via Build-Module.ps1
$buildScript = Join-Path $PSScriptRoot 'Build-Module.ps1'
Write-Output "Invoking Build-Module.ps1 -Configuration $Configuration ..."
# Build-Module.ps1 throws on any failure (and $ErrorActionPreference is Stop), so a failed staging
# never reaches the packaging below; $LASTEXITCODE is not meaningful after a script call.
& $buildScript -Configuration $Configuration

# 2. Read the module version from the manifest
$manifest = Import-PowerShellDataFile -Path $psd1
$version  = $manifest.ModuleVersion

# A prerelease lives in PSData.Prerelease, not in ModuleVersion, so the manifest version alone
# would name the artifacts 0.9.0 while Publish-Module stamps the package 0.9.0-pre. Fold it in
# so the ZIP, the NUPKG and the package metadata all carry the same string.
$prerelease = $manifest.PrivateData.PSData.Prerelease
if (-not [string]::IsNullOrWhiteSpace($prerelease))
{
    $version = '{0}-{1}' -f $version, $prerelease.TrimStart('-')
}

Write-Output "Module version: $version"

# 3. Ensure the output directory exists
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

# 4. Produce ZIP
# Compress-Archive with the module folder path includes the folder as the top-level
# entry, giving the required Fluence.Wpf.PowerShell\ root in the archive.
$zipName = "Fluence.Wpf.PowerShell-$version.zip"
$zipPath = Join-Path $OutputPath $zipName
Write-Output "Creating ZIP: $zipPath ..."
if (Test-Path $zipPath)
{
    [System.IO.File]::Delete($zipPath)
}
Compress-Archive -Path $modSrc -DestinationPath $zipPath
Write-Output "ZIP produced: $zipPath"

# 5. Produce NUPKG via Publish-Module targeting a temporary local PSRepository.
#    A unique repository name avoids collisions in concurrent builds.
$repoName    = "FluenceTempRepo_$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
$repoDir     = Join-Path ([System.IO.Path]::GetTempPath()) $repoName
$nupkgName   = "Fluence.Wpf.PowerShell.$version.nupkg"
$nupkgPath   = Join-Path $OutputPath $nupkgName

New-Item -ItemType Directory -Path $repoDir -Force | Out-Null

Write-Output "Creating NUPKG via Publish-Module to temp repository: $repoDir ..."

Register-PSRepository -Name $repoName -SourceLocation $repoDir -PublishLocation $repoDir -InstallationPolicy Trusted

try
{
    Publish-Module -Path $modSrc -Repository $repoName -NuGetApiKey 'local' -Force

    # Find the nupkg Publish-Module deposited in the temp repo dir
    $produced = Get-ChildItem -Path $repoDir -Filter '*.nupkg' | Select-Object -First 1
    if ($null -eq $produced)
    {
        throw "Publish-Module completed but no .nupkg was found in: $repoDir"
    }

    if (Test-Path $nupkgPath)
    {
        [System.IO.File]::Delete($nupkgPath)
    }
    Copy-Item -LiteralPath $produced.FullName -Destination $nupkgPath -Force
    Write-Output "NUPKG produced: $nupkgPath"
}
finally
{
    Unregister-PSRepository -Name $repoName -ErrorAction SilentlyContinue
    if (Test-Path $repoDir)
    {
        $tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
        $cleanupPath = [System.IO.Path]::GetFullPath($repoDir)
        if (-not $cleanupPath.StartsWith($tempRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
            ((Get-Item -LiteralPath $cleanupPath).Attributes -band [System.IO.FileAttributes]::ReparsePoint))
        {
            throw "Refusing to delete a package directory outside '$tempRoot' or through a reparse point: $cleanupPath"
        }
        [System.IO.Directory]::Delete($cleanupPath, $true)
    }
}

# 6. Report produced artifacts
$zipInfo   = Get-Item $zipPath
$nupkgInfo = Get-Item $nupkgPath
Write-Output 'Artifacts:'
Write-Output "  $($zipInfo.Name)  ($([int]($zipInfo.Length / 1KB)) KB)"
Write-Output "  $($nupkgInfo.Name)  ($([int]($nupkgInfo.Length / 1KB)) KB)"
