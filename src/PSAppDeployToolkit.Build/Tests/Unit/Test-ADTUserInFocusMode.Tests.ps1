BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Test-ADTUserInFocusMode' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should not throw' {
            { Test-ADTUserInFocusMode } | Should -Not -Throw
        }

        It 'Should return null or a Boolean' {
            $result = Test-ADTUserInFocusMode
            ($null -eq $result -or $result -is [System.Boolean]) | Should -BeTrue
        }

        It 'Should return at most one value' {
            $result = @(Test-ADTUserInFocusMode)
            $result.Count | Should -BeLessOrEqual 1
        }

        It 'Should return $null when no active user is logged on' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return $null }
            $result = Test-ADTUserInFocusMode
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Metadata' {
        It 'Should declare OutputType of System.Boolean' {
            $outputTypes = (Get-Command Test-ADTUserInFocusMode).OutputType.Type
            $outputTypes | Should -Contain ([System.Boolean])
        }

        It 'Should accept no custom parameters' {
            $params = (Get-Command Test-ADTUserInFocusMode).Parameters.Keys
            $customParams = $params | Where-Object { $_ -notin [System.Management.Automation.Cmdlet]::CommonParameters }
            $customParams | Should -BeNullOrEmpty
        }
    }
}
