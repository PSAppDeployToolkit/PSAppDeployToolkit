BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Out-ADTPowerShellEncodedCommand' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Encodes a command to Base64 Unicode' {
            $command = 'Get-Process'
            $expected = [System.Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($command))
            Out-ADTPowerShellEncodedCommand -Command $command | Should -Be $expected
        }
        It 'Round-trips: decoded output matches original command for <Command>' -ForEach @(
            @{ Command = 'Get-Process' }
            @{ Command = 'Write-Host "Hello World"' }
            @{ Command = 'Import-Module PSAppDeployToolkit -Force' }
        ) {
            $encoded = Out-ADTPowerShellEncodedCommand -Command $Command
            $decoded = [System.Text.Encoding]::Unicode.GetString([System.Convert]::FromBase64String($encoded))
            $decoded | Should -Be $Command
        }
        It 'Returns a [System.String]' {
            Out-ADTPowerShellEncodedCommand -Command 'Get-Process' | Should -BeOfType ([System.String])
        }
    }

    Context 'Input Validation' {
        It 'Throws ParameterArgumentValidationError for invalid Command value: <Label>' -ForEach @(
            @{ Label = 'null';       Value = $null }
            @{ Label = 'empty';      Value = '' }
            @{ Label = 'whitespace'; Value = " `f`n`r`t`v" }
        ) {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Out-ADTPowerShellEncodedCommand'
            }
            { Out-ADTPowerShellEncodedCommand -Command $Value } | Should @shouldParams
        }
    }
}
