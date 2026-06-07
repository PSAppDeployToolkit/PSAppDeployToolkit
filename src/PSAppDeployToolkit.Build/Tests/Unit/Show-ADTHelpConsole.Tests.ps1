BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
# NOTE: the 'Should pass a populated ModuleHelpMap' test is inherently slow (~several seconds)
# because Show-ADTHelpConsole builds its help map by calling Get-Help -Full over every exported
# command at startup. The ModuleHelpMap.Count > 0 assertion is intentionally NOT mocked away
# because verifying a non-empty map is the core contract of that test.
Describe 'Show-ADTHelpConsole' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        function script:New-MockRunAsActiveUser
        {
            $nt = [System.Security.Principal.NTAccount]::new('TEST\user')
            $sid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-21-1-2-3-1001')
            return [PSADT.Foundation.RunAsActiveUser]::new($nt, $sid, [System.UInt32]1, $null)
        }
    }

    Context 'Parameters' {
        It 'Should take no parameters beyond the common ones' {
            $common = [System.Management.Automation.PSCmdlet]::CommonParameters
            (Get-Command Show-ADTHelpConsole).Parameters.Keys.Where({ $common -notcontains $_ }) | Should -BeNullOrEmpty
        }
    }

    Context 'Forwarding' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { New-MockRunAsActiveUser }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Should not throw when invoked' {
            { Show-ADTHelpConsole } | Should -Not -Throw
        }

        It 'Should forward a ShowModalDialog operation for the active user' {
            Show-ADTHelpConsole
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 1 -Exactly -ParameterFilter { $ShowModalDialog -and ($null -ne $User) }
        }

        It 'Should request the HelpConsole dialog type as a no-wait dialog' {
            Show-ADTHelpConsole
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 1 -Exactly -ParameterFilter {
                ($DialogType -eq 'HelpConsole') -and $NoWait
            }
        }

        It 'Should pass a populated ModuleHelpMap in the options' {
            Show-ADTHelpConsole
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 1 -Exactly -ParameterFilter {
                ($null -ne $Options) -and ($Options.ModuleHelpMap.Count -gt 0)
            }
        }
    }
}
