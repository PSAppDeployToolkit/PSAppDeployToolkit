BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    # Wrapper that supplies a real $PSCmdlet to Complete-ADTFunction.
    function global:Invoke-ADTTestCompleteWrapper
    {
        [CmdletBinding()]
        param()
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}

AfterAll {
    Remove-Item -Path 'Function:\Invoke-ADTTestCompleteWrapper' -ErrorAction SilentlyContinue
}

Describe 'Complete-ADTFunction' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Basic invocation' {
        It 'Does not throw when called with a valid PSCmdlet' {
            { Invoke-ADTTestCompleteWrapper } | Should -Not -Throw
        }

        It 'Returns no output' {
            $result = Invoke-ADTTestCompleteWrapper
            $result | Should -BeNull
        }

        It 'Can be called multiple times consecutively without error' {
            { Invoke-ADTTestCompleteWrapper; Invoke-ADTTestCompleteWrapper } | Should -Not -Throw
        }
    }

    Context 'Logging' {
        It 'Calls Write-ADTLogEntry at least once' {
            Invoke-ADTTestCompleteWrapper
            Should -Invoke Write-ADTLogEntry -ModuleName PSAppDeployToolkit -Scope It
        }

        It 'Calls Write-ADTLogEntry with the Function End message' {
            Invoke-ADTTestCompleteWrapper
            Should -Invoke Write-ADTLogEntry -ModuleName PSAppDeployToolkit -Scope It -ParameterFilter { $Message -eq 'Function End' }
        }

        It 'Calls Write-ADTLogEntry with -DebugMessage' {
            Invoke-ADTTestCompleteWrapper
            Should -Invoke Write-ADTLogEntry -ModuleName PSAppDeployToolkit -Scope It -ParameterFilter { $DebugMessage -eq $true }
        }
    }
}
