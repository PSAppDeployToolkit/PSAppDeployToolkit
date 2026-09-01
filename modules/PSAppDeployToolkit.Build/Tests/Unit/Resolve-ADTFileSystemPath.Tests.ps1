BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    function Resolve-Probe
    {
        param
        (
            [Parameter(Mandatory = $true)]
            [System.Collections.Hashtable]$Splat
        )

        InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ S = $Splat } {
            Resolve-ADTFileSystemPath @S
        }
    }
}

Describe 'Resolve-ADTFileSystemPath' {
    Context 'Functionality' {
        BeforeAll {
            $script:Dir = "$TestDrive\resolve"
            $null = New-Item -Path $script:Dir -ItemType Directory
            Set-Content -LiteralPath "$script:Dir\file.txt" -Value 'x'
            Set-Content -LiteralPath "$script:Dir\script.ps1" -Value "'ran'"
        }

        It 'Resolves an existing directory with -Directory' {
            Resolve-Probe -Splat @{ LiteralPath = $script:Dir; Directory = $true } | Should -BeExactly $script:Dir
        }

        It 'Resolves an existing file with -File' {
            Resolve-Probe -Splat @{ LiteralPath = "$script:Dir\file.txt"; File = $true } | Should -BeExactly "$script:Dir\file.txt"
        }

        It 'Rejects a file when a directory was asked for' {
            # The type is enforced, not merely the existence, so a file cannot stand in for a directory.
            { Resolve-Probe -Splat @{ LiteralPath = "$script:Dir\file.txt"; Directory = $true } } | Should -Throw -ErrorId 'LiteralPathNotFound,Resolve-ADTFileSystemPath'
        }

        It 'Rejects a directory when a file was asked for' {
            { Resolve-Probe -Splat @{ LiteralPath = $script:Dir; File = $true } } | Should -Throw -ErrorId 'LiteralPathNotFound,Resolve-ADTFileSystemPath'
        }

        It 'Throws for a path that does not exist' {
            { Resolve-Probe -Splat @{ LiteralPath = "$script:Dir\missing.txt"; File = $true } } | Should -Throw -ErrorId 'LiteralPathNotFound,Resolve-ADTFileSystemPath'
        }

        It 'Returns a path that does not exist when -ResolveOnly is given' {
            # Used where the caller is about to create the file, so it must not require it up front.
            Resolve-Probe -Splat @{ LiteralPath = "$script:Dir\missing.txt"; File = $true; ResolveOnly = $true } | Should -BeExactly "$script:Dir\missing.txt"
        }

        It 'Finds a bare file name under -ExtraPaths' {
            Resolve-Probe -Splat @{ LiteralPath = 'file.txt'; File = $true; ExtraPaths = $script:Dir } | Should -BeExactly "$script:Dir\file.txt"
        }

        It 'Appends -DefaultExtension when the path has none' {
            Resolve-Probe -Splat @{ LiteralPath = "$script:Dir\script"; File = $true; DefaultExtension = '.ps1' } | Should -BeExactly "$script:Dir\script.ps1"
        }

        It 'Requires -DefaultExtension to start with a period' {
            { Resolve-Probe -Splat @{ LiteralPath = "$script:Dir\script"; File = $true; DefaultExtension = 'ps1' } } | Should -Throw -ErrorId 'InvalidDefaultExtensionParameterValue,Resolve-ADTFileSystemPath'
        }

        It 'Rejects a blank -DefaultExtension' {
            # An extension made of nothing would be appended to every bare name and resolve nothing.
            $file = "$TestDrive\Blank.exe"
            Set-Content -LiteralPath $file -Value 'content'
            { Resolve-Probe -Splat @{ File = $true; LiteralPath = $file; DefaultExtension = '   ' } } | Should -Throw -ErrorId 'InvalidDefaultExtensionParameterValue,Resolve-ADTFileSystemPath'
        }

        It 'Strips the provider from a provider-qualified filesystem path' {
            # Paths handed around inside the module carry their provider, and what comes back has to be a
            # plain filesystem path that anything else can use.
            $file = "$TestDrive\Qualified.exe"
            Set-Content -LiteralPath $file -Value 'content'
            Resolve-Probe -Splat @{ File = $true; LiteralPath = "Microsoft.PowerShell.Core\FileSystem::$file" } | Should -BeExactly $file
        }

        It 'Rejects a provider-qualified path belonging to another provider' {
            # A registry path would otherwise be carried through as though it were a file.
            { Resolve-Probe -Splat @{ File = $true; LiteralPath = 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE' } } | Should -Throw -ErrorId 'ProviderQualifiedPathNotFileSystemPath,Resolve-ADTFileSystemPath'
        }
        It 'Offers -<Parameter> only alongside -File' -ForEach @(
            @{ Parameter = 'ExtraPaths'; Value = 'C:\' }
            @{ Parameter = 'DefaultExtension'; Value = '.ps1' }
        ) {
            # Both are dynamic parameters that the Container set deliberately does not surface.
            { Resolve-Probe -Splat @{ LiteralPath = $script:Dir; Directory = $true; $Parameter = $Value } } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Requires either -File or -Directory' {
            { Resolve-Probe -Splat @{ LiteralPath = $script:Dir } } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }

    Context 'Within a deployment session' {
        BeforeAll {
            Import-ADTModuleUnderTest -Force
            Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
            Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
            Initialize-ADTTestModule -Path $TestDrive

            $script:Deploy = "$TestDrive\Deploy"
            $null = New-Item -Path "$script:Deploy\Files" -ItemType Directory -Force
            $null = New-Item -Path "$script:Deploy\SupportFiles" -ItemType Directory -Force
            Set-Content -LiteralPath "$script:Deploy\SupportFiles\Helper.exe" -Value 'content'
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'ResolvePaths' -DeployMode Silent -ScriptDirectory $script:Deploy -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
            Import-ADTModuleUnderTest -Force
        }

        It 'Finds a bare name in the deployment''s SupportFiles' {
            # A deployment refers to its helpers by name, and they sit beside it rather than on the path.
            Resolve-Probe -Splat @{ File = $true; LiteralPath = 'Helper.exe' } | Should -BeExactly "$script:Deploy\SupportFiles\Helper.exe"
        }

        It 'Still reports a name that is nowhere' {
            { Resolve-Probe -Splat @{ File = $true; LiteralPath = 'NoSuchHelper.exe' } } | Should -Throw -ErrorId 'LiteralPathNotFound,Resolve-ADTFileSystemPath'
        }
    }
}
