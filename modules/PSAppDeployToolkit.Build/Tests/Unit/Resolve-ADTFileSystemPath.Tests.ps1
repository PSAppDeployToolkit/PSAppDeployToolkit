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
}
