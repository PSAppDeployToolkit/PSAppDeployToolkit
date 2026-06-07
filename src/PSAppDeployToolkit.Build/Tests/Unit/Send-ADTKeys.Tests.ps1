BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Send-ADTKeys' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        function script:New-MockRunAsActiveUser
        {
            $nt = [System.Security.Principal.NTAccount]::new('TEST\user')
            $sid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-21-1-2-3-1001')
            return [PSADT.Foundation.RunAsActiveUser]::new($nt, $sid, [System.UInt32]1, $null)
        }

        function script:New-MockWindow
        {
            param([System.String]$Title = 'foobar - Notepad', [System.Int64]$Handle = 12345)
            return [PSCustomObject]@{
                WindowTitle  = $Title
                WindowHandle = [System.IntPtr]::new($Handle)
            }
        }
    }

    Context 'Input Validation' {
        It 'Should have a mandatory WindowTitle parameter in the WindowTitle parameter set' {
            (Get-Command Send-ADTKeys).Parameters['WindowTitle'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Should have a mandatory WindowHandle parameter in the WindowHandle parameter set' {
            (Get-Command Send-ADTKeys).Parameters['WindowHandle'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Should have a mandatory Keys parameter' {
            (Get-Command Send-ADTKeys).Parameters['Keys'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Should throw ParameterArgumentValidationError when WindowTitle is empty or whitespace' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Send-ADTKeys'
            }
            { Send-ADTKeys -WindowTitle '' -Keys 'abc' } | Should @shouldParams
            { Send-ADTKeys -WindowTitle " `f`n`r`t`v" -Keys 'abc' } | Should @shouldParams
        }

        It 'Should throw ParameterArgumentValidationError when Keys is empty or whitespace' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Send-ADTKeys'
            }
            { Send-ADTKeys -WindowTitle 'foo' -Keys '' } | Should @shouldParams
            { Send-ADTKeys -WindowTitle 'foo' -Keys " `f`n`r`t`v" } | Should @shouldParams
        }
    }

    Context 'No active user bypass' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return $null }
            Mock -ModuleName PSAppDeployToolkit Get-ADTWindowTitle { New-MockWindow }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Should not throw when there is no active user' {
            { Send-ADTKeys -WindowTitle 'foo' -Keys 'abc' } | Should -Not -Throw
        }

        It 'Should not send keys when there is no active user' {
            Send-ADTKeys -WindowTitle 'foo' -Keys 'abc'
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 0 -Exactly
        }

        It 'Should not query for windows when there is no active user' {
            Send-ADTKeys -WindowTitle 'foo' -Keys 'abc'
            Should -Invoke -ModuleName PSAppDeployToolkit Get-ADTWindowTitle -Times 0 -Exactly
        }
    }

    Context 'No matching windows' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { New-MockRunAsActiveUser }
            Mock -ModuleName PSAppDeployToolkit Get-ADTWindowTitle { return $null }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Should not send keys when no windows match' {
            Send-ADTKeys -WindowTitle 'nonexistent' -Keys 'abc'
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 0 -Exactly
        }

        It 'Should log a warning when no windows match' {
            Send-ADTKeys -WindowTitle 'nonexistent' -Keys 'abc'
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -Times 1 -Exactly -ParameterFilter { $Message -like '*No windows matching*' }
        }
    }

    Context 'Forwarding by WindowTitle' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { New-MockRunAsActiveUser }
            Mock -ModuleName PSAppDeployToolkit Get-ADTWindowTitle { New-MockWindow -Title 'foobar - Notepad' -Handle 4242 }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Should forward a SendKeys operation for the active user' {
            Send-ADTKeys -WindowTitle 'foobar - Notepad' -Keys 'Hello world'
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 1 -Exactly -ParameterFilter { $SendKeys -and ($null -ne $User) }
        }

        It 'Should forward the WindowTitle parameter set to Get-ADTWindowTitle' {
            Send-ADTKeys -WindowTitle 'foobar - Notepad' -Keys 'Hello world'
            Should -Invoke -ModuleName PSAppDeployToolkit Get-ADTWindowTitle -Times 1 -Exactly -ParameterFilter { $WindowTitle -eq 'foobar - Notepad' }
        }

        It 'Should pass the supplied keys and window handle through the SendKeys options' {
            Send-ADTKeys -WindowTitle 'foobar - Notepad' -Keys 'Hello world'
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 1 -Exactly -ParameterFilter {
                ($Options.Keys -eq 'Hello world') -and ($Options.WindowHandle -eq ([System.IntPtr]::new(4242)))
            }
        }
    }

    Context 'Forwarding by WindowHandle' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { New-MockRunAsActiveUser }
            Mock -ModuleName PSAppDeployToolkit Get-ADTWindowTitle { New-MockWindow -Title 'Handle Window' -Handle 17368294 }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Should forward the WindowHandle parameter set to Get-ADTWindowTitle' {
            Send-ADTKeys -WindowHandle ([System.IntPtr]17368294) -Keys 'Hello World'
            Should -Invoke -ModuleName PSAppDeployToolkit Get-ADTWindowTitle -Times 1 -Exactly -ParameterFilter { $WindowHandle -eq ([System.IntPtr]17368294) }
        }

        It 'Should forward a SendKeys operation for a known window handle' {
            Send-ADTKeys -WindowHandle ([System.IntPtr]17368294) -Keys 'Hello World'
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 1 -Exactly -ParameterFilter { $SendKeys }
        }
    }

    Context 'Multiple matching windows' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { New-MockRunAsActiveUser }
            Mock -ModuleName PSAppDeployToolkit Get-ADTWindowTitle {
                @(
                    New-MockWindow -Title 'Window A' -Handle 111
                    New-MockWindow -Title 'Window B' -Handle 222
                )
            }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Should send keys to every matching window' {
            Send-ADTKeys -WindowTitle 'Window' -Keys 'abc'
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 2 -Exactly -ParameterFilter { $SendKeys }
        }
    }
}
