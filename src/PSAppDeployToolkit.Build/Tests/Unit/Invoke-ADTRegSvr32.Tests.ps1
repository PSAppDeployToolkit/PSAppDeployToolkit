BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    # A real .dll that exists on every Windows system — used for WhatIf-safe tests.
    $script:Shell32 = "$env:SystemRoot\System32\shell32.dll"
}

Describe 'Invoke-ADTRegSvr32' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'ValidateScript — .dll extension bypasses existence check' {
        It 'Throws for a non-existent path that has a .dll extension (Register, WhatIf)' {
            $fakeDll = "C:\NonExistent_$([System.Guid]::NewGuid().ToString('N')).dll"
            { Invoke-ADTRegSvr32 -FilePath $fakeDll -Action 'Register' -WhatIf } | Should -Throw
        }

        It 'Throws for a non-existent path that has a .dll extension (Unregister, WhatIf)' {
            $fakeDll = "C:\NonExistent_$([System.Guid]::NewGuid().ToString('N')).dll"
            { Invoke-ADTRegSvr32 -FilePath $fakeDll -Action 'Unregister' -WhatIf } | Should -Throw
        }
    }

    Context 'ValidateScript — non-.dll non-existent file throws' {
        It 'Throws when the file does not exist and the extension is not .dll' {
            $fakeExe = "C:\NonExistent_$([System.Guid]::NewGuid().ToString('N')).exe"
            { Invoke-ADTRegSvr32 -FilePath $fakeExe -Action 'Register' } | Should -Throw
        }

        It 'Throws for a non-existent .txt file' {
            $fakeTxt = "C:\NonExistent_$([System.Guid]::NewGuid().ToString('N')).txt"
            { Invoke-ADTRegSvr32 -FilePath $fakeTxt -Action 'Register' } | Should -Throw
        }
    }

    Context 'WhatIf skips regsvr32 invocation' {
        It 'Does not throw when called with -WhatIf on an existing DLL (Register)' {
            { Invoke-ADTRegSvr32 -FilePath $script:Shell32 -Action 'Register' -WhatIf } | Should -Not -Throw
        }

        It 'Does not throw when called with -WhatIf on an existing DLL (Unregister)' {
            { Invoke-ADTRegSvr32 -FilePath $script:Shell32 -Action 'Unregister' -WhatIf } | Should -Not -Throw
        }
    }

    Context 'Input validation' {
        It 'Throws for an invalid -Action value' {
            { Invoke-ADTRegSvr32 -FilePath $script:Shell32 -Action 'Invalid' } | Should -Throw
        }
    }
}
