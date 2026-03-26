BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Unregister-ADTDll' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
        Mock -ModuleName PSAppDeployToolkit Invoke-ADTRegSvr32 { }
    }

    Context 'Input validation' {
        It 'Throws when the DLL file does not exist' {
            { Unregister-ADTDll -FilePath (Join-Path -Path $TestDrive -ChildPath 'nonexistent.dll') } | Should -Throw
        }
    }

    Context '-WhatIf support' {
        It 'Does not throw under -WhatIf' {
            $shell32 = Join-Path -Path $env:SystemRoot -ChildPath 'System32\shell32.dll'
            { Unregister-ADTDll -FilePath $shell32 -WhatIf } | Should -Not -Throw
        }

        It 'Does not call Invoke-ADTRegSvr32 under -WhatIf' {
            $shell32 = Join-Path -Path $env:SystemRoot -ChildPath 'System32\shell32.dll'
            Unregister-ADTDll -FilePath $shell32 -WhatIf
            Should -Not -Invoke Invoke-ADTRegSvr32 -ModuleName PSAppDeployToolkit -Scope It
        }
    }

    Context 'Normal invocation' {
        It 'Does not throw for an existing DLL' {
            $shell32 = Join-Path -Path $env:SystemRoot -ChildPath 'System32\shell32.dll'
            { Unregister-ADTDll -FilePath $shell32 } | Should -Not -Throw
        }

        It 'Calls Invoke-ADTRegSvr32 with -Action Unregister' {
            $shell32 = Join-Path -Path $env:SystemRoot -ChildPath 'System32\shell32.dll'
            Unregister-ADTDll -FilePath $shell32
            Should -Invoke Invoke-ADTRegSvr32 -ModuleName PSAppDeployToolkit -Scope It -ParameterFilter { $Action -eq 'Unregister' }
        }

        It 'Returns no output' {
            $shell32 = Join-Path -Path $env:SystemRoot -ChildPath 'System32\shell32.dll'
            $result = Unregister-ADTDll -FilePath $shell32
            $result | Should -BeNull
        }
    }
}
