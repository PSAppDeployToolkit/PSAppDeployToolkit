BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Set-ADTPowerShellCulture' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Input Validation' {
        It 'Should have a mandatory CultureInfo parameter' {
            (Get-Command Set-ADTPowerShellCulture).Parameters['CultureInfo'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Should type the CultureInfo parameter as System.Globalization.CultureInfo' {
            (Get-Command Set-ADTPowerShellCulture).Parameters['CultureInfo'].ParameterType | Should -Be ([System.Globalization.CultureInfo])
        }
    }

    Context 'Culture not installed' {
        BeforeAll {
            # Constrain the installed-culture set so an arbitrary valid tag is treated as not installed.
            Mock -ModuleName PSAppDeployToolkit Get-WinUserLanguageList {
                return [PSCustomObject]@{ LanguageTag = 'en-US' }
            }
        }

        It 'Throws an ArgumentException with ErrorId CultureNotInstalled when the culture is not installed' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.ArgumentException]
                ErrorId       = 'CultureNotInstalled,Set-ADTPowerShellCulture'
            }
            { Set-ADTPowerShellCulture -CultureInfo 'fr-FR' -ErrorAction Stop } | Should @shouldParams
        }

        It 'Throws CultureNotInstalled for an unknown culture tag as well' {
            # An arbitrary non-empty string transforms into an (unknown) CultureInfo rather than
            # failing parameter binding, so it surfaces as CultureNotInstalled against the mocked list.
            { Set-ADTPowerShellCulture -CultureInfo 'zz-ZZ-unknown' -ErrorAction Stop } | Should -Throw -ExceptionType ([System.ArgumentException]) -ErrorId 'CultureNotInstalled,Set-ADTPowerShellCulture'
        }
    }

    Context 'Culture installed' {
        BeforeAll {
            # Pretend the requested culture is installed.
            Mock -ModuleName PSAppDeployToolkit Get-WinUserLanguageList {
                return [PSCustomObject]@{ LanguageTag = 'fr-FR' }
            }
        }

        BeforeEach {
            # Preserve the thread culture; the function mutates it reflectively on supported runtimes.
            $script:savedCulture = [System.Threading.Thread]::CurrentThread.CurrentCulture
            $script:savedUICulture = [System.Threading.Thread]::CurrentThread.CurrentUICulture
        }

        AfterEach {
            [System.Threading.Thread]::CurrentThread.CurrentCulture = $script:savedCulture
            [System.Threading.Thread]::CurrentThread.CurrentUICulture = $script:savedUICulture
        }

        It 'Should not throw when the requested culture is reported as installed' {
            { Set-ADTPowerShellCulture -CultureInfo 'fr-FR' } | Should -Not -Throw
        }

        It 'Should change the resolved PowerShell culture to the requested value' {
            # Get-Culture reflects the change on both editions: Windows PowerShell via the
            # NativeCultureResolver fields, PowerShell 7+ via the current thread's culture.
            Set-ADTPowerShellCulture -CultureInfo 'fr-FR'
            (Get-Culture).Name | Should -Be 'fr-FR'
        }

        It 'Should query the installed language list to validate the culture' {
            Set-ADTPowerShellCulture -CultureInfo 'fr-FR'
            Should -Invoke -ModuleName PSAppDeployToolkit Get-WinUserLanguageList -Times 1 -Exactly
        }
    }
}
