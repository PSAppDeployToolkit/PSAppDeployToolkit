BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Set-ADTActiveSetup' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        function script:New-MockRunAsActiveUser
        {
            $nt = [System.Security.Principal.NTAccount]::new('TEST\user')
            $sid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-21-1-2-3-1001')
            return [PSADT.Foundation.RunAsActiveUser]::new($nt, $sid, [System.UInt32]1, $null)
        }

        function script:New-MockUserProfile
        {
            $nt = [System.Security.Principal.NTAccount]::new('TEST\user')
            $sid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-21-1-2-3-1001')
            $dir = [System.IO.DirectoryInfo]::new($TestDrive)
            return [PSADT.AccountManagement.UserProfileInfo]::new($nt, $sid, $dir, $dir, $dir, $dir, $dir, $dir, $dir, $null, $null, [System.Globalization.CultureInfo]::InvariantCulture)
        }
    }

    Context 'Input Validation' {
        It 'Should have a mandatory StubExePath parameter in the Create parameter set' {
            (Get-Command Set-ADTActiveSetup).Parameters['StubExePath'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Should have a mandatory PurgeActiveSetupKey parameter in the Purge parameter set' {
            (Get-Command Set-ADTActiveSetup).Parameters['PurgeActiveSetupKey'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Should reject a StubExePath with an unsupported file extension' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Set-ADTActiveSetup'
            }
            { Set-ADTActiveSetup -StubExePath 'C:\Tool\config.txt' -Key 'K' -Description 'D' } | Should @shouldParams
        }

        It 'Should accept the supported StubExePath extension [<Ext>]' -ForEach @(
            @{ Ext = '.exe' }
            @{ Ext = '.vbs' }
            @{ Ext = '.cmd' }
            @{ Ext = '.bat' }
            @{ Ext = '.ps1' }
            @{ Ext = '.js' }
        ) {
            $attr = (Get-Command Set-ADTActiveSetup).Parameters['StubExePath'].Attributes.Where({ $_ -is [PSAppDeployToolkit.Attributes.ValidateExtensionAttribute] })
            $attr.ExtensionNames | Should -Contain $Ext
        }

        It 'Should reject a Version containing more than four octets' {
            { Set-ADTActiveSetup -StubExePath 'C:\Tool\app.exe' -Key 'K' -Description 'D' -Version '1.2.3.4.5' } | Should -Throw
        }

        It 'Should reject a Version containing non-numeric, non-separator characters' {
            { Set-ADTActiveSetup -StubExePath 'C:\Tool\app.exe' -Key 'K' -Description 'D' -Version 'abc' } | Should -Throw
        }
    }

    Context 'Session requirement' {
        It 'Should expose Key and Description as dynamic parameters when no session is active' {
            $command = Get-Command Set-ADTActiveSetup
            $command.Parameters.ContainsKey('Key') | Should -BeTrue
            $command.Parameters.ContainsKey('Description') | Should -BeTrue
        }
    }

    Context 'Purge path' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { New-MockRunAsActiveUser }
            Mock -ModuleName PSAppDeployToolkit Remove-ADTRegistryKey { }
            Mock -ModuleName PSAppDeployToolkit Get-ADTRegistryKey { return $null }
            Mock -ModuleName PSAppDeployToolkit Get-ADTUserProfiles { return (New-MockUserProfile) }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTAllUsersRegistryAction { }
            Mock -ModuleName PSAppDeployToolkit Set-ADTRegistryKey { }
        }

        It 'Should remove the HKLM Active Setup key when purging' {
            Set-ADTActiveSetup -Key 'ProgramUserConfig' -PurgeActiveSetupKey
            Should -Invoke -ModuleName PSAppDeployToolkit Remove-ADTRegistryKey -Times 1 -Exactly -ParameterFilter {
                ($Key -like '*HKEY_LOCAL_MACHINE*Active Setup\Installed Components\ProgramUserConfig') -and $Recurse
            }
        }

        It 'Should iterate all user hives to purge HKCU entries' {
            Set-ADTActiveSetup -Key 'ProgramUserConfig' -PurgeActiveSetupKey
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTAllUsersRegistryAction -Times 1 -Exactly
        }

        It 'Should not write any Active Setup values when purging' {
            Set-ADTActiveSetup -Key 'ProgramUserConfig' -PurgeActiveSetupKey
            Should -Invoke -ModuleName PSAppDeployToolkit Set-ADTRegistryKey -Times 0 -Exactly
        }

        It 'Should respect -WhatIf and not remove anything' {
            Set-ADTActiveSetup -Key 'ProgramUserConfig' -PurgeActiveSetupKey -WhatIf
            Should -Invoke -ModuleName PSAppDeployToolkit Remove-ADTRegistryKey -Times 0 -Exactly
        }
    }

    Context 'Create path - HKLM key writes' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { New-MockRunAsActiveUser }
            Mock -ModuleName PSAppDeployToolkit Set-ADTRegistryKey { }
            # No existing HKLM entry => Test-ADTActiveSetup returns false => current-user execution is skipped.
            Mock -ModuleName PSAppDeployToolkit Get-ADTRegistryKey { return $null }
            Mock -ModuleName PSAppDeployToolkit Copy-ADTFile { }
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { }
            Mock -ModuleName PSAppDeployToolkit Test-Path { return $true }
        }

        It 'Should write the (Default) description value to the HKLM Active Setup key' {
            Set-ADTActiveSetup -StubExePath 'C:\Tool\app.exe' -Key 'MyKey' -Description 'My Desc'
            Should -Invoke -ModuleName PSAppDeployToolkit Set-ADTRegistryKey -Times 1 -Exactly -ParameterFilter {
                ($LiteralPath -like '*HKEY_LOCAL_MACHINE*Installed Components\MyKey') -and ($Name -eq '(Default)') -and ($Value -eq 'My Desc')
            }
        }

        It 'Should write the Version value with comma separators to the HKLM key' {
            Set-ADTActiveSetup -StubExePath 'C:\Tool\app.exe' -Key 'MyKey' -Description 'My Desc' -Version '1.2.3'
            Should -Invoke -ModuleName PSAppDeployToolkit Set-ADTRegistryKey -Times 1 -Exactly -ParameterFilter {
                ($Name -eq 'Version') -and ($Value -eq '1,2,3')
            }
        }

        It 'Should write the StubPath value as an ExpandString to the HKLM key' {
            Set-ADTActiveSetup -StubExePath 'C:\Tool\app.exe' -Key 'MyKey' -Description 'My Desc'
            Should -Invoke -ModuleName PSAppDeployToolkit Set-ADTRegistryKey -Times 1 -Exactly -ParameterFilter {
                ($Name -eq 'StubPath') -and ($Type -eq 'ExpandString')
            }
        }

        It 'Should write IsInstalled as 1 (enabled) to the HKLM key by default' {
            Set-ADTActiveSetup -StubExePath 'C:\Tool\app.exe' -Key 'MyKey' -Description 'My Desc'
            Should -Invoke -ModuleName PSAppDeployToolkit Set-ADTRegistryKey -Times 1 -Exactly -ParameterFilter {
                ($Name -eq 'IsInstalled') -and ($Value -eq 1) -and ($Type -eq 'DWord')
            }
        }

        It 'Should write IsInstalled as 0 (disabled) when -DisableActiveSetup is supplied' {
            Set-ADTActiveSetup -StubExePath 'C:\Tool\app.exe' -Key 'MyKey' -Description 'My Desc' -DisableActiveSetup
            Should -Invoke -ModuleName PSAppDeployToolkit Set-ADTRegistryKey -Times 1 -Exactly -ParameterFilter {
                ($Name -eq 'IsInstalled') -and ($Value -eq 0)
            }
        }

        It 'Should write the Locale value to the HKLM key when -Locale is supplied' {
            Set-ADTActiveSetup -StubExePath 'C:\Tool\app.exe' -Key 'MyKey' -Description 'My Desc' -Locale 'en'
            Should -Invoke -ModuleName PSAppDeployToolkit Set-ADTRegistryKey -Times 1 -Exactly -ParameterFilter {
                ($Name -eq 'Locale') -and ($Value -eq 'en')
            }
        }

        It 'Should not write the Locale value when -Locale is omitted' {
            Set-ADTActiveSetup -StubExePath 'C:\Tool\app.exe' -Key 'MyKey' -Description 'My Desc'
            Should -Invoke -ModuleName PSAppDeployToolkit Set-ADTRegistryKey -Times 0 -Exactly -ParameterFilter { $Name -eq 'Locale' }
        }

        It 'Should target the WOW6432Node path when -Wow6432Node is supplied on a 64-bit OS' {
            if (-not [System.Environment]::Is64BitOperatingSystem)
            {
                Set-ItResult -Skipped -Because 'WOW6432Node path is only taken on a 64-bit OS.'
                return
            }
            Set-ADTActiveSetup -StubExePath 'C:\Tool\app.exe' -Key 'MyKey' -Description 'My Desc' -Wow6432Node
            Should -Invoke -ModuleName PSAppDeployToolkit Set-ADTRegistryKey -Times 1 -Exactly -ParameterFilter {
                ($Name -eq '(Default)') -and ($LiteralPath -like '*Wow6432Node*')
            }
        }

        It 'Should respect -WhatIf and not write any HKLM values' {
            Set-ADTActiveSetup -StubExePath 'C:\Tool\app.exe' -Key 'MyKey' -Description 'My Desc' -WhatIf
            Should -Invoke -ModuleName PSAppDeployToolkit Set-ADTRegistryKey -Times 0 -Exactly
        }

        It 'Should not execute the StubPath for the current user when -NoExecuteForCurrentUser is supplied' {
            Set-ADTActiveSetup -StubExePath 'C:\Tool\app.exe' -Key 'MyKey' -Description 'My Desc' -NoExecuteForCurrentUser
            Should -Invoke -ModuleName PSAppDeployToolkit Start-ADTProcess -Times 0 -Exactly
        }
    }

    Context 'Create path - missing StubPath file' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { New-MockRunAsActiveUser }
            Mock -ModuleName PSAppDeployToolkit Set-ADTRegistryKey { }
            Mock -ModuleName PSAppDeployToolkit Get-ADTRegistryKey { return $null }
            Mock -ModuleName PSAppDeployToolkit Copy-ADTFile { }
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { }
            Mock -ModuleName PSAppDeployToolkit Test-Path { return $false }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTFunctionErrorHandler { }
        }

        It 'Should not write any registry values when the StubPath file is missing' {
            Set-ADTActiveSetup -StubExePath 'C:\Tool\app.exe' -Key 'MyKey' -Description 'My Desc'
            Should -Invoke -ModuleName PSAppDeployToolkit Set-ADTRegistryKey -Times 0 -Exactly
        }

        It 'Should route the missing-file error through the function error handler' {
            Set-ADTActiveSetup -StubExePath 'C:\Tool\app.exe' -Key 'MyKey' -Description 'My Desc'
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTFunctionErrorHandler -Times 1 -Exactly
        }

        It 'Should skip the file-existence check for paths containing environment variables' {
            Set-ADTActiveSetup -StubExePath '%SystemRoot%\System32\regedit.exe' -Key 'MyKey' -Description 'My Desc'
            Should -Invoke -ModuleName PSAppDeployToolkit Set-ADTRegistryKey -ParameterFilter { $Name -eq '(Default)' }
        }
    }

    Context 'Create path - current user execution and HKCU writes' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { New-MockRunAsActiveUser }
            Mock -ModuleName PSAppDeployToolkit Set-ADTRegistryKey { }
            # HKLM entry present and enabled, HKCU absent => Test-ADTActiveSetup returns $true => StubPath executes.
            Mock -ModuleName PSAppDeployToolkit Get-ADTRegistryKey {
                if ($LiteralPath -like '*HKEY_LOCAL_MACHINE*')
                {
                    return [PSCustomObject]@{ IsInstalled = 1; Version = '1,2,3' }
                }
                return $null
            }
            Mock -ModuleName PSAppDeployToolkit Copy-ADTFile { }
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { }
            Mock -ModuleName PSAppDeployToolkit Test-Path { return $true }
        }

        It 'Should execute the StubPath for the current user when Active Setup indicates a run is required' {
            Set-ADTActiveSetup -StubExePath 'C:\Tool\app.exe' -Key 'MyKey' -Description 'My Desc'
            Should -Invoke -ModuleName PSAppDeployToolkit Start-ADTProcess -Times 1 -Exactly
        }

        It 'Should write the HKCU Active Setup (Default) value after executing for the current user' {
            Set-ADTActiveSetup -StubExePath 'C:\Tool\app.exe' -Key 'MyKey' -Description 'My Desc'
            Should -Invoke -ModuleName PSAppDeployToolkit Set-ADTRegistryKey -Times 1 -Exactly -ParameterFilter {
                ($LiteralPath -like '*HKEY_CURRENT_USER*Installed Components\MyKey') -and ($Name -eq '(Default)')
            }
        }

        It 'Should not write IsInstalled to the HKCU key' {
            Set-ADTActiveSetup -StubExePath 'C:\Tool\app.exe' -Key 'MyKey' -Description 'My Desc'
            Should -Invoke -ModuleName PSAppDeployToolkit Set-ADTRegistryKey -Times 0 -Exactly -ParameterFilter {
                ($LiteralPath -like '*HKEY_CURRENT_USER*') -and ($Name -eq 'IsInstalled')
            }
        }

        It 'Should resolve the StubPath via wscript.exe for a .vbs StubExePath' {
            Set-ADTActiveSetup -StubExePath 'C:\Tool\script.vbs' -Key 'MyKey' -Description 'My Desc'
            Should -Invoke -ModuleName PSAppDeployToolkit Start-ADTProcess -Times 1 -Exactly -ParameterFilter {
                $FilePath -like '*wscript.exe'
            }
        }
    }
}
