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
