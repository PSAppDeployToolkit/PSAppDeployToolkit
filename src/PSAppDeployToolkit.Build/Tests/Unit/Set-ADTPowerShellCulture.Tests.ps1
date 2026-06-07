BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    # Determine whether this runtime's Microsoft.PowerShell.NativeCultureResolver still
    # exposes the m_Culture/m_uiCulture static fields the function reflects against.
    # On modern PowerShell 7 these fields no longer exist, so the success path cannot run.
    $smaResolver = [System.Reflection.Assembly]::Load('System.Management.Automation').GetType('Microsoft.PowerShell.NativeCultureResolver')
    $smaFlags = [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Static
    $script:CultureResolverFieldsAvailable = ($null -ne $smaResolver.GetField('m_Culture', $smaFlags)) -and ($null -ne $smaResolver.GetField('m_uiCulture', $smaFlags))
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

        It 'Should not throw when the requested culture is reported as installed' -Skip:(-not $script:CultureResolverFieldsAvailable) {
            { Set-ADTPowerShellCulture -CultureInfo 'fr-FR' } | Should -Not -Throw
        }

        It 'Should query the installed language list to validate the culture' -Skip:(-not $script:CultureResolverFieldsAvailable) {
            Set-ADTPowerShellCulture -CultureInfo 'fr-FR'
            Should -Invoke -ModuleName PSAppDeployToolkit Get-WinUserLanguageList -Times 1 -Exactly
        }
    }
}
