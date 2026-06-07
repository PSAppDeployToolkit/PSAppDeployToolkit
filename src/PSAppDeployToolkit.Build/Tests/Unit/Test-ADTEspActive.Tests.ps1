BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Test-ADTEspActive' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should not throw' {
            { Test-ADTEspActive } | Should -Not -Throw
        }

        It 'Should return a Boolean value regardless of ESP state' {
            $result = Test-ADTEspActive
            $result | Should -BeOfType ([System.Boolean])
        }

        It 'Should return exactly one value' {
            $result = @(Test-ADTEspActive)
            $result.Count | Should -Be 1
        }
    }

    Context 'Metadata' {
        It 'Should declare OutputType of System.Boolean' {
            $outputTypes = (Get-Command Test-ADTEspActive).OutputType.Type
            $outputTypes | Should -Contain ([System.Boolean])
        }

        It 'Should accept no custom parameters' {
            $params = (Get-Command Test-ADTEspActive).Parameters.Keys
            $customParams = $params | Where-Object { $_ -notin [System.Management.Automation.Cmdlet]::CommonParameters }
            $customParams | Should -BeNullOrEmpty
        }
    }
}
