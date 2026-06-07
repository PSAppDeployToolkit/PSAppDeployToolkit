BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'New-ADTLogFileName' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Build a lightweight fake session with the two members that New-ADTLogFileName uses.
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'FakeSession', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $FakeSession = [PSCustomObject]@{ LogPath = [System.IO.DirectoryInfo]::new('C:\Logs') }
        $FakeSession | Add-Member -MemberType ScriptMethod -Name 'NewLogFileName' -Value { param([System.String]$discriminator); return "TestApp_${discriminator}_Install.log" }

        Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $FakeSession }
    }

    Context 'Functionality' {
        It 'Returns a [System.String]' {
            New-ADTLogFileName -Discriminator 'Setup' -FileNameOnly | Should -BeOfType ([System.String])
        }
        It 'Returns only the file name when -FileNameOnly is specified' {
            $result = New-ADTLogFileName -Discriminator 'Setup' -FileNameOnly
            $result | Should -Be 'TestApp_Setup_Install.log'
            $result | Should -Not -Match '\\'
        }
        It 'Returns a full path when -FileNameOnly is $false' {
            $result = New-ADTLogFileName -Discriminator 'Setup' -FileNameOnly:$false
            $result | Should -Match '^C:\\Logs\\'
            $result | Should -Match 'TestApp_Setup_Install\.log$'
        }
        It 'Embeds the Discriminator in the produced file name' {
            $result = New-ADTLogFileName -Discriminator 'PreReqs' -FileNameOnly
            $result | Should -Match 'PreReqs'
        }
        It 'Result has .log extension' {
            $result = New-ADTLogFileName -Discriminator 'Setup' -FileNameOnly
            $result | Should -Match '\.log$'
        }
    }

    Context 'Input Validation' {
        It 'Throws ParameterArgumentValidationError when Discriminator is <Label>' -ForEach @(
            @{ Label = 'null';       Value = $null }
            @{ Label = 'empty';      Value = '' }
            @{ Label = 'whitespace'; Value = " `f`n`r`t`v" }
        ) {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,New-ADTLogFileName'
            }
            { New-ADTLogFileName -Discriminator $Value -FileNameOnly } | Should @shouldParams
        }
    }
}
