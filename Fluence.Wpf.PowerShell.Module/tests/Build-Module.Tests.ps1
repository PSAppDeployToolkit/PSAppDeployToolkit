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


BeforeAll {
    $script:buildScript = Join-Path $PSScriptRoot '../build/Build-Module.ps1'
    $script:encoding = New-Object System.Text.UTF8Encoding($true)
}

Describe 'Build-Module version alignment before staging' {
    BeforeEach {
        $repo = Join-Path $TestDrive ([System.Guid]::NewGuid().ToString('N'))
        $moduleRoot = Join-Path $repo 'Fluence.Wpf.PowerShell.Module'
        $buildRoot = Join-Path $moduleRoot 'build'
        $moduleSource = Join-Path $moduleRoot 'src/Fluence.Wpf.PowerShell'
        $stagedLibrary = Join-Path $moduleSource 'lib/previous-target/keep.txt'
        $null = New-Item -ItemType Directory -Path $buildRoot, (Split-Path $stagedLibrary -Parent) -Force
        Copy-Item -LiteralPath $script:buildScript -Destination $buildRoot
        [System.IO.File]::WriteAllText($stagedLibrary, 'prior staging must survive rejection', $script:encoding)
        foreach ($tfm in 'net472', 'net8.0-windows10.0.26100.0')
        {
            $source = Join-Path $repo "Fluence.Wpf/bin/Release/$tfm"
            $null = New-Item -ItemType Directory -Path $source -Force
            # A real managed assembly lets the staging script inspect metadata without building WPF.
            Copy-Item -LiteralPath ([System.Xml.XmlDocument].Assembly.Location) -Destination (Join-Path $source 'Fluence.Wpf.dll')
        }
        $script:isolatedScript = Join-Path $buildRoot 'Build-Module.ps1'
        $script:manifestPath = Join-Path $moduleSource 'Fluence.Wpf.PowerShell.psd1'
        $script:propsPath = Join-Path $repo 'Directory.Build.props'
    }

    It 'stages both frameworks when <Label> versions match' -ForEach @(
        @{ Label = 'prerelease'; SuffixXml = '<VersionSuffix>pre</VersionSuffix>'; ManifestSuffix = 'pre' }
        @{ Label = 'stable'; SuffixXml = '<VersionSuffix></VersionSuffix>'; ManifestSuffix = '' }
        @{ Label = 'implicit stable'; SuffixXml = ''; ManifestSuffix = '' }
    ) {
        [System.IO.File]::WriteAllText($script:propsPath, "<Project><PropertyGroup><VersionPrefix>0.9.0</VersionPrefix>$SuffixXml</PropertyGroup></Project>", $script:encoding)
        [System.IO.File]::WriteAllText($script:manifestPath, "@{ ModuleVersion = '0.9.0'; PrivateData = @{ PSData = @{ Prerelease = '$ManifestSuffix' } } }", $script:encoding)

        & $script:isolatedScript | Out-Null

        Test-Path -LiteralPath $stagedLibrary | Should -BeFalse
        foreach ($tfm in 'net472', 'net8.0-windows10.0.26100.0')
        {
            $sourceDll = Join-Path $repo "Fluence.Wpf/bin/Release/$tfm/Fluence.Wpf.dll"
            $stagedDll = Join-Path $moduleSource "lib/$tfm/Fluence.Wpf.dll"
            (Get-FileHash -LiteralPath $stagedDll).Hash | Should -Be (Get-FileHash -LiteralPath $sourceDll).Hash
        }
    }

    It 'rejects <Label> without changing existing staging' -ForEach @(
        @{ Label = 'different numeric versions'; Prefix = '1.0.0'; TreeSuffix = 'pre'; ModuleSuffix = 'pre' }
        @{ Label = 'different prerelease identifiers'; Prefix = '0.9.0'; TreeSuffix = 'rc.1'; ModuleSuffix = 'pre' }
        @{ Label = 'a stable module against a prerelease tree'; Prefix = '0.9.0'; TreeSuffix = 'pre'; ModuleSuffix = '' }
        @{ Label = 'a prerelease module against a stable tree'; Prefix = '0.9.0'; TreeSuffix = ''; ModuleSuffix = 'pre' }
        @{ Label = 'a missing tree version'; Prefix = ''; TreeSuffix = 'pre'; ModuleSuffix = 'pre' }
    ) {
        [System.IO.File]::WriteAllText($script:propsPath, "<Project><PropertyGroup><VersionPrefix>$Prefix</VersionPrefix><VersionSuffix>$TreeSuffix</VersionSuffix></PropertyGroup></Project>", $script:encoding)
        [System.IO.File]::WriteAllText($script:manifestPath, "@{ ModuleVersion = '0.9.0'; PrivateData = @{ PSData = @{ Prerelease = '$ModuleSuffix' } } }", $script:encoding)
        $originalHash = (Get-FileHash -LiteralPath $stagedLibrary).Hash

        { & $script:isolatedScript | Out-Null } | Should -Throw '*version*'

        (Get-FileHash -LiteralPath $stagedLibrary).Hash | Should -Be $originalHash
        @(Get-ChildItem -LiteralPath (Join-Path $moduleSource 'lib') -Recurse -File).Count | Should -Be 1
    }
}

Describe 'Package-Module prerequisites' {
    It 'rejects a missing NuGet provider before staging or replacing artifacts' {
        $repo = Join-Path $TestDrive 'missing-provider'
        $buildRoot = Join-Path $repo 'Fluence.Wpf.PowerShell.Module/build'
        $outputRoot = Join-Path $repo 'artifacts'
        $null = New-Item -ItemType Directory -Path $buildRoot, $outputRoot -Force
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../build/Package-Module.ps1') -Destination $buildRoot
        [System.IO.File]::WriteAllText((Join-Path $buildRoot 'Build-Module.ps1'), "throw 'Staging must not execute without a provider.'", $script:encoding)
        $priorArtifact = Join-Path $outputRoot 'existing.zip'
        [System.IO.File]::WriteAllText($priorArtifact, 'prior artifact must survive rejection', $script:encoding)
        $priorHash = (Get-FileHash -LiteralPath $priorArtifact).Hash
        Mock Get-PackageProvider { $null }

        { & (Join-Path $buildRoot 'Package-Module.ps1') -OutputPath $outputRoot } | Should -Throw '*NuGet provider 2.8.5.208 or later must already be installed*'

        (Get-FileHash -LiteralPath $priorArtifact).Hash | Should -Be $priorHash
        @(Get-ChildItem -LiteralPath $outputRoot -File).Count | Should -Be 1
        Should -Invoke Get-PackageProvider -Times 1 -Exactly -ParameterFilter { -not $ForceBootstrap }
    }
}

Describe 'Package-Module artifact production' {
    BeforeEach {
        $script:fixtureRoot = Join-Path $TestDrive ([System.Guid]::NewGuid().ToString('N'))
        $script:fixtureBuild = Join-Path $script:fixtureRoot 'Fluence.Wpf.PowerShell.Module/build'
        $script:fixtureModule = Join-Path $script:fixtureRoot 'Fluence.Wpf.PowerShell.Module/src/Fluence.Wpf.PowerShell'
        $script:fixtureOutput = Join-Path $script:fixtureRoot 'artifacts'
        $fixtureTemp = Join-Path $script:fixtureRoot 'temp'
        $null = New-Item -ItemType Directory -Path $script:fixtureBuild, (Join-Path $script:fixtureModule 'lib'), $script:fixtureOutput, $fixtureTemp -Force
        $script:priorTemp = $env:TEMP
        $script:priorTmp = $env:TMP
        $env:TEMP = $fixtureTemp
        $env:TMP = $fixtureTemp
        $script:fixtureScript = Join-Path $script:fixtureBuild 'Package-Module.ps1'
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../build/Package-Module.ps1') -Destination $script:fixtureScript
        [System.IO.File]::WriteAllText((Join-Path $script:fixtureModule 'Fluence.Wpf.PowerShell.psd1'), "@{ ModuleVersion = '0.9.0'; PrivateData = @{ PSData = @{ Prerelease = 'pre' } } }", $script:encoding)
        [System.IO.File]::WriteAllText((Join-Path $script:fixtureModule 'Fluence.Wpf.PowerShell.psm1'), '# fixture', $script:encoding)
        [System.IO.File]::WriteAllText((Join-Path $script:fixtureModule 'lib/payload.dll'), 'old binary', $script:encoding)
        [System.IO.File]::WriteAllText((Join-Path $script:fixtureBuild 'Build-Module.ps1'), @'
param([string]$Configuration, [switch]$Build)
$root = Split-Path $PSScriptRoot -Parent
[System.IO.File]::WriteAllText((Join-Path $root 'build-flag.txt'), $Build.ToString())
if ($Build)
{
    [System.IO.File]::WriteAllText((Join-Path $root 'src/Fluence.Wpf.PowerShell/lib/payload.dll'), 'fresh binary')
}
'@, $script:encoding)
        $script:zipPath = Join-Path $script:fixtureOutput 'Fluence.Wpf.PowerShell-0.9.0-pre.zip'
        $script:nupkgPath = Join-Path $script:fixtureOutput 'Fluence.Wpf.PowerShell.0.9.0-pre.nupkg'
        $script:repositoryPath = $null
        Mock Get-PackageProvider { [pscustomobject]@{ Version = [System.Version]'3.0.0.1' } }
        Mock Register-PSRepository {
            param($SourceLocation)
            $script:repositoryPath = if ($SourceLocation -is [System.Uri]) { $SourceLocation.LocalPath } else { [string]$SourceLocation }
        }
        Mock Publish-Module {
            [System.IO.File]::WriteAllText((Join-Path $script:repositoryPath 'Fluence.Wpf.PowerShell.0.9.0-pre.nupkg'), 'new package')
        }
        Mock Unregister-PSRepository { }
    }

    AfterEach {
        $env:TEMP = $script:priorTemp
        $env:TMP = $script:priorTmp
    }

    It 'preserves prior <Prior> artifacts after <Failure> fails' -ForEach @(
        @{ Prior = 'both'; Zip = $true; Nupkg = $true; Failure = 'publication' }
        @{ Prior = 'none'; Zip = $false; Nupkg = $false; Failure = 'publication' }
        @{ Prior = 'zip only'; Zip = $true; Nupkg = $false; Failure = 'publication' }
        @{ Prior = 'nupkg only'; Zip = $false; Nupkg = $true; Failure = 'publication' }
        @{ Prior = 'both'; Zip = $true; Nupkg = $true; Failure = 'registration' }
        @{ Prior = 'none'; Zip = $false; Nupkg = $false; Failure = 'registration' }
    ) {
        if ($Zip) { [System.IO.File]::WriteAllText($script:zipPath, 'old zip') }
        if ($Nupkg) { [System.IO.File]::WriteAllText($script:nupkgPath, 'old nupkg') }
        if ($Failure -eq 'publication') { Mock Publish-Module { throw 'injected publication failure' } }
        else { Mock Register-PSRepository { throw 'injected registration failure' } }

        { & $script:fixtureScript -OutputPath $script:fixtureOutput | Out-Null } | Should -Throw ('*injected ' + $Failure + ' failure*')

        Test-Path -LiteralPath $script:zipPath | Should -Be $Zip
        Test-Path -LiteralPath $script:nupkgPath | Should -Be $Nupkg
        if ($Zip) { [System.IO.File]::ReadAllText($script:zipPath) | Should -BeExactly 'old zip' }
        if ($Nupkg) { [System.IO.File]::ReadAllText($script:nupkgPath) | Should -BeExactly 'old nupkg' }
        @(Get-ChildItem -LiteralPath $script:fixtureOutput -Directory -Force).Count | Should -Be 0
        @(Get-ChildItem -LiteralPath $env:TEMP -Force -Filter 'FluenceTempRepo_*').Count | Should -Be 0
        $expectedUnregister = if ($Failure -eq 'publication') { 1 } else { 0 }
        Should -Invoke Unregister-PSRepository -Times $expectedUnregister -Exactly
    }

    It 'restores both old files when the second promotion fails' {
        [System.IO.File]::WriteAllText($script:zipPath, 'old zip')
        [System.IO.File]::WriteAllText($script:nupkgPath, 'old nupkg')
        Mock Move-Item { throw 'injected second promotion failure' } -ParameterFilter { $Destination -eq $script:nupkgPath }

        { & $script:fixtureScript -OutputPath $script:fixtureOutput | Out-Null } | Should -Throw '*injected second promotion failure*'

        [System.IO.File]::ReadAllText($script:zipPath) | Should -BeExactly 'old zip'
        [System.IO.File]::ReadAllText($script:nupkgPath) | Should -BeExactly 'old nupkg'
        @(Get-ChildItem -LiteralPath $script:fixtureOutput -Directory -Force).Count | Should -Be 0
    }

    It 'removes newly promoted files when the second promotion fails with no prior pair' {
        Mock Move-Item { throw 'injected second promotion failure' } -ParameterFilter { $Destination -eq $script:nupkgPath }
        { & $script:fixtureScript -OutputPath $script:fixtureOutput | Out-Null } | Should -Throw '*injected second promotion failure*'
        Test-Path -LiteralPath $script:zipPath | Should -BeFalse
        Test-Path -LiteralPath $script:nupkgPath | Should -BeFalse
        @(Get-ChildItem -LiteralPath $script:fixtureOutput -Directory -Force).Count | Should -Be 0
    }

    It 'retains recoverable backups and reports their location when rollback fails' {
        [System.IO.File]::WriteAllText($script:zipPath, 'old zip')
        [System.IO.File]::WriteAllText($script:nupkgPath, 'old nupkg')
        Mock Move-Item { throw 'injected second promotion failure' } -ParameterFilter { $Destination -eq $script:nupkgPath }
        Mock Copy-Item { throw 'injected rollback failure' } -ParameterFilter { $Destination -eq $script:zipPath -and (Split-Path $LiteralPath -Leaf) -like 'previous-*' }

        $failure = { & $script:fixtureScript -OutputPath $script:fixtureOutput | Out-Null } | Should -Throw -PassThru
        $failure.Exception.Message | Should -Match 'rollback'
        $stage = @(Get-ChildItem -LiteralPath $script:fixtureOutput -Directory -Force)
        $stage.Count | Should -Be 1
        $failure.Exception.Message | Should -Match ([regex]::Escape($stage[0].FullName))
        [System.IO.File]::ReadAllText((Join-Path $stage[0].FullName 'previous-zip')) | Should -BeExactly 'old zip'
        [System.IO.File]::ReadAllText((Join-Path $stage[0].FullName 'previous-nupkg')) | Should -BeExactly 'old nupkg'
    }

    It 'rejects <Problem> staged packages before replacing final files' -ForEach @(
        @{ Problem = 'missing' }
        @{ Problem = 'empty' }
        @{ Problem = 'unexpected-name' }
    ) {
        [System.IO.File]::WriteAllText($script:zipPath, 'old zip')
        [System.IO.File]::WriteAllText($script:nupkgPath, 'old nupkg')
        $script:packageProblem = $Problem
        Mock Publish-Module {
            if ($script:packageProblem -eq 'empty') { [System.IO.File]::WriteAllBytes((Join-Path $script:repositoryPath 'Fluence.Wpf.PowerShell.0.9.0-pre.nupkg'), [byte[]]@()) }
            elseif ($script:packageProblem -eq 'unexpected-name') { [System.IO.File]::WriteAllText((Join-Path $script:repositoryPath 'Unexpected.nupkg'), 'unexpected') }
        }
        { & $script:fixtureScript -OutputPath $script:fixtureOutput | Out-Null } | Should -Throw
        [System.IO.File]::ReadAllText($script:zipPath) | Should -BeExactly 'old zip'
        [System.IO.File]::ReadAllText($script:nupkgPath) | Should -BeExactly 'old nupkg'
        @(Get-ChildItem -LiteralPath $script:fixtureOutput -Directory -Force).Count | Should -Be 0
    }

    It 'packages <Expected> payload bytes with explicit reuse <Reuse>' -ForEach @(
        @{ Reuse = $false; Expected = 'fresh binary'; BuildFlag = 'True' }
        @{ Reuse = $true; Expected = 'old binary'; BuildFlag = 'False' }
    ) {
        $arguments = @{ OutputPath = $script:fixtureOutput }
        if ($Reuse) { $arguments.UseExistingBuild = $true }
        & $script:fixtureScript @arguments | Out-Null
        Get-Content -LiteralPath (Join-Path (Split-Path $script:fixtureBuild -Parent) 'build-flag.txt') | Should -BeExactly $BuildFlag
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $archive = [System.IO.Compression.ZipFile]::OpenRead($script:zipPath)
        try
        {
            $entry = @($archive.Entries | Where-Object { ($_.FullName -replace '\\', '/') -eq 'Fluence.Wpf.PowerShell/lib/payload.dll' })
            $entry.Count | Should -Be 1
            $reader = [System.IO.StreamReader]::new($entry[0].Open())
            try { $reader.ReadToEnd() | Should -BeExactly $Expected } finally { $reader.Dispose() }
        }
        finally { $archive.Dispose() }
        (Get-Item -LiteralPath $script:nupkgPath).Length | Should -BeGreaterThan 0
        Should -Invoke Register-PSRepository -Times 1 -Exactly
        Should -Invoke Unregister-PSRepository -Times 1 -Exactly
    }
}
