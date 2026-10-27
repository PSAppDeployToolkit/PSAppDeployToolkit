<#
Copyright (c) 2026, Dan Cunningham. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#>


<#
.SYNOPSIS
    Packages the Fluence.Wpf.PowerShell module as a ZIP and a PowerShell Gallery .nupkg.
.DESCRIPTION
    Builds current library source and stages assemblies through Build-Module.ps1, then produces two
    distributable artifacts in the output directory:

      - Fluence.Wpf.PowerShell-<version>.zip   (drop-in for PSModulePath)
      - Fluence.Wpf.PowerShell.<version>.nupkg  (PowerShell Gallery format)

    The .nupkg is produced by Publish-Module targeting a temporary local file-based
    PSRepository. Nothing is published to PSGallery. A compatible NuGet provider must already be
    available; this script does not bootstrap one. Install the build prerequisites separately.

    Both artifacts are staged before replacing final files. Handled promotion failures attempt
    to restore their prior contents and presence. Failed restoration retains backup paths in
    the error message. This recovery does not make the two-file update atomic across a crash.
.PARAMETER Configuration
    MSBuild configuration passed to Build-Module.ps1. Defaults to 'Release'.
.PARAMETER OutputPath
    Directory that receives the zip and nupkg. Defaults to
    <repo>\Fluence.Wpf.PowerShell.Module\artifacts.
.PARAMETER UseExistingBuild
    Reuses existing library outputs instead of compiling. The caller must ensure they came from
    the intended source revision, for example verified artifacts from the same CI run.
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
    [string]$OutputPath = '',

    [Parameter()]
    [switch]$UseExistingBuild
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
if ($UseExistingBuild)
{
    & $buildScript -Configuration $Configuration
}
else
{
    & $buildScript -Configuration $Configuration -Build
}

# 2. Read the module version from the manifest
$manifest = Import-PowerShellDataFile -Path $psd1
$version  = $manifest.ModuleVersion

# A prerelease lives in PSData.Prerelease, not in ModuleVersion, so the manifest version alone
# would omit the prerelease suffix while Publish-Module includes it. Fold it in
# so the ZIP, the NUPKG and the package metadata all carry the same string.
$prerelease = $manifest.PrivateData.PSData.Prerelease
if (-not [string]::IsNullOrWhiteSpace($prerelease))
{
    $version = '{0}-{1}' -f $version, $prerelease.TrimStart('-')
}

Write-Output "Module version: $version"

# Promote only after both artifacts and all prior-file backups exist.
function Publish-FluenceArtifactPair
{
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Private packaging routine; writes only the explicitly selected output artifact pair.')]
    param
    (
        [Parameter(Mandatory = $true)]
        [object[]]$Artifacts,

        [Parameter(Mandatory = $true)]
        [hashtable]$State
    )

    foreach ($artifact in $Artifacts)
    {
        if ($artifact.Existed)
        {
            Copy-Item -LiteralPath $artifact.Final -Destination $artifact.Backup -ErrorAction Stop
        }
    }

    try
    {
        foreach ($artifact in $Artifacts)
        {
            Move-Item -LiteralPath $artifact.Staged -Destination $artifact.Final -Force -ErrorAction Stop
        }
    }
    catch
    {
        $promotionFailure = $_
        $rollbackFailures = [System.Collections.Generic.List[string]]::new()
        foreach ($artifact in $Artifacts)
        {
            try
            {
                if ($artifact.Existed)
                {
                    Copy-Item -LiteralPath $artifact.Backup -Destination $artifact.Final -Force -ErrorAction Stop
                }
                elseif (Test-Path -LiteralPath $artifact.Final)
                {
                    Remove-Item -LiteralPath $artifact.Final -Force -ErrorAction Stop
                }
            }
            catch
            {
                $rollbackFailures.Add(('{0}: {1}; backup: {2}' -f $artifact.Final, $_.Exception.Message, $artifact.Backup))
            }
        }
        if ($rollbackFailures.Count -gt 0)
        {
            $State.PreserveStaging = $true
            throw ('Artifact promotion failed: {0}. Rollback failed; retain recoverable backups at: {1}' -f
                $promotionFailure.Exception.Message, ($rollbackFailures -join '; '))
        }
        throw $promotionFailure
    }
}

# All temporary data and backups live on the output volume in one run-owned directory.
$OutputPath = [System.IO.Path]::GetFullPath($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath))
$outputPrefix = $OutputPath.TrimEnd([char[]]'\/') + [System.IO.Path]::DirectorySeparatorChar
$runId = [System.Guid]::NewGuid().ToString('N')
$stageDir = Join-Path $OutputPath ('.fluence-package-' + $runId)
$repoDir = Join-Path $stageDir 'repository'
$repoName = 'FluenceTempRepo_' + $runId
$zipName = "Fluence.Wpf.PowerShell-$version.zip"
$nupkgName = "Fluence.Wpf.PowerShell.$version.nupkg"
$zipPath = [System.IO.Path]::GetFullPath((Join-Path $OutputPath $zipName))
$nupkgPath = [System.IO.Path]::GetFullPath((Join-Path $OutputPath $nupkgName))
if (Test-Path -LiteralPath $stageDir)
{
    throw "Run-owned package staging path already exists: $stageDir"
}
foreach ($target in @($zipPath, $nupkgPath, $stageDir))
{
    $parent = [System.IO.Path]::GetDirectoryName($target).TrimEnd([char[]]'\/')
    if (-not $target.StartsWith($outputPrefix, [System.StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals($parent, $OutputPath.TrimEnd([char[]]'\/'), [System.StringComparison]::OrdinalIgnoreCase))
    {
        throw "Artifact path must stay directly inside '$OutputPath': $target"
    }
    if ((Test-Path -LiteralPath $target) -and
        ((Get-Item -LiteralPath $target -Force).Attributes -band [System.IO.FileAttributes]::ReparsePoint))
    {
        throw "Refusing to replace or clean a reparse point: $target"
    }
    if ([System.IO.Directory]::Exists($target))
    {
        throw "Artifact file path is an existing directory: $target"
    }
}
$registered = $false
$promotionState = @{ PreserveStaging = $false }
try
{
    $null = [System.IO.Directory]::CreateDirectory($repoDir)
    $stagedZip = Join-Path $stageDir $zipName
    $stagedNupkg = Join-Path $stageDir $nupkgName
    Compress-Archive -LiteralPath $modSrc -DestinationPath $stagedZip

    Register-PSRepository -Name $repoName -SourceLocation $repoDir -PublishLocation $repoDir -InstallationPolicy Trusted
    $registered = $true
    Publish-Module -Path $modSrc -Repository $repoName -NuGetApiKey 'local' -Force
    $produced = @(Get-ChildItem -LiteralPath $repoDir -Filter '*.nupkg' -File)
    if ($produced.Count -ne 1 -or $produced[0].Name -ine $nupkgName)
    {
        throw "Publish-Module must produce exactly '$nupkgName' in: $repoDir"
    }
    Copy-Item -LiteralPath $produced[0].FullName -Destination $stagedNupkg -ErrorAction Stop
    foreach ($artifact in @($stagedZip, $stagedNupkg))
    {
        if (-not [System.IO.File]::Exists($artifact) -or (Get-Item -LiteralPath $artifact).Length -le 0)
        {
            throw "Staged artifact is missing or empty: $artifact"
        }
    }
    $artifacts = @(
        @{ Staged = $stagedZip; Final = $zipPath; Backup = (Join-Path $stageDir 'previous-zip'); Existed = [System.IO.File]::Exists($zipPath) }
        @{ Staged = $stagedNupkg; Final = $nupkgPath; Backup = (Join-Path $stageDir 'previous-nupkg'); Existed = [System.IO.File]::Exists($nupkgPath) }
    )
    Publish-FluenceArtifactPair -Artifacts $artifacts -State $promotionState
}
finally
{
    try
    {
        if ($registered)
        {
            Unregister-PSRepository -Name $repoName -ErrorAction SilentlyContinue
        }
    }
    finally
    {
        if ((Test-Path -LiteralPath $stageDir) -and -not $promotionState.PreserveStaging)
        {
            $cleanupPath = [System.IO.Path]::GetFullPath($stageDir)
            if (-not $cleanupPath.StartsWith($outputPrefix, [System.StringComparison]::OrdinalIgnoreCase) -or
                -not [string]::Equals([System.IO.Path]::GetDirectoryName($cleanupPath).TrimEnd([char[]]'\/'),
                    $OutputPath.TrimEnd([char[]]'\/'), [System.StringComparison]::OrdinalIgnoreCase))
            {
                throw "Refusing to clean a package directory outside '$OutputPath': $cleanupPath"
            }
            $cleanupEntries = @(Get-Item -LiteralPath $cleanupPath -Force)
            $cleanupEntries += @(Get-ChildItem -LiteralPath $cleanupPath -Recurse -Force)
            $reparsePoints = $cleanupEntries |
                Where-Object { $_.Attributes -band [System.IO.FileAttributes]::ReparsePoint }
            if (@($reparsePoints).Count -gt 0)
            {
                throw "Refusing to clean a package directory containing a reparse point: $cleanupPath"
            }
            [System.IO.Directory]::Delete($cleanupPath, $true)
        }
    }
}

$zipInfo = Get-Item -LiteralPath $zipPath
$nupkgInfo = Get-Item -LiteralPath $nupkgPath
Write-Output 'Artifacts:'
Write-Output "  $($zipInfo.Name)  ($([int]($zipInfo.Length / 1KB)) KB)"
Write-Output "  $($nupkgInfo.Name)  ($([int]($nupkgInfo.Length / 1KB)) KB)"
