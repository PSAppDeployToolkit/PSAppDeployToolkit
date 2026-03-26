BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    # A real .dll that exists on every Windows system — used for WhatIf-safe tests.
    $script:Shell32 = "$env:SystemRoot\System32\shell32.dll"
}

Describe 'Register-ADTDll' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'ValidateScript — file must exist' {
        It 'Does not throw when the file exists (WhatIf)' {
            { Register-ADTDll -FilePath $script:Shell32 -WhatIf } | Should -Not -Throw
        }

        It 'Throws when the specified file does not exist' {
            $fakePath = "C:\NonExistent_$([System.Guid]::NewGuid().ToString('N')).dll"
            { Register-ADTDll -FilePath $fakePath } | Should -Throw
        }

        It 'Throws even for a .dll extension if the file does not exist' {
            $fakeDll = "C:\NonExistent_$([System.Guid]::NewGuid().ToString('N')).dll"
            { Register-ADTDll -FilePath $fakeDll } | Should -Throw
        }
    }

    Context '-WhatIf skips registration' {
        It 'Does not throw with -WhatIf on shell32.dll' {
            { Register-ADTDll -FilePath $script:Shell32 -WhatIf } | Should -Not -Throw
        }

        It 'Returns nothing when -WhatIf is specified' {
            $result = Register-ADTDll -FilePath $script:Shell32 -WhatIf
            $result | Should -BeNull
        }
    }
}
