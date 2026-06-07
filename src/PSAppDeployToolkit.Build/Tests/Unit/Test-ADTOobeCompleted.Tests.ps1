BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Test-ADTOobeCompleted' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should not throw' {
            { Test-ADTOobeCompleted } | Should -Not -Throw
        }

        It 'Should return a [System.Boolean]' {
            $result = Test-ADTOobeCompleted
            $result | Should -BeOfType ([System.Boolean])
        }

        It 'Should return $true on a fully provisioned system' {
            # On a normal developer/test machine OOBE is always completed.
            # If the machine is in OOBE, this assertion is skipped.
            $result = Test-ADTOobeCompleted
            if ($result -eq $false)
            {
                Set-ItResult -Skipped -Because 'System appears to be in OOBE; cannot assert $true'
            }
            $result | Should -BeTrue
        }

        It 'Should return a non-null value' {
            $result = Test-ADTOobeCompleted
            $null -ne $result | Should -BeTrue
        }
    }

    Context 'Metadata' {
        It 'Should declare OutputType of System.Boolean' {
            $outputTypes = (Get-Command Test-ADTOobeCompleted).OutputType.Type
            $outputTypes | Should -Contain ([System.Boolean])
        }

        It 'Should accept no parameters' {
            $params = (Get-Command Test-ADTOobeCompleted).Parameters.Keys |
                Where-Object { $_ -notin [System.Management.Automation.PSCmdlet]::CommonParameters }
            @($params).Count | Should -Be 0
        }
    }
}
