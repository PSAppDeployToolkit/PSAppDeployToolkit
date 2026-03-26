BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    # Capture original culture state so we can restore it after tests that invoke the real reflection code.
    $script:OriginalCulture = [System.Threading.Thread]::CurrentThread.CurrentCulture
    $script:OriginalUICulture = [System.Threading.Thread]::CurrentThread.CurrentUICulture
}

AfterAll {
    # Best-effort restore of culture to avoid side effects on other test files.
    try
    {
        $resolver = [System.Reflection.Assembly]::Load('System.Management.Automation').GetType('Microsoft.PowerShell.NativeCultureResolver')
        if ($resolver)
        {
            $flags = [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Static
            $cField = $resolver.GetField('m_Culture', $flags)
            if ($cField) { $cField.SetValue($null, $script:OriginalCulture) }
            $uField = $resolver.GetField('m_uiCulture', $flags)
            if ($uField) { $uField.SetValue($null, $script:OriginalUICulture) }
        }
    }
    catch
    {
        $null = $_
    }
}

Describe 'Set-ADTPowerShellCulture' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
    }

    Context 'Culture Not Installed' {
        BeforeAll {
            # Mock to expose only en-US so that any other culture is "not installed".
            Mock -ModuleName PSAppDeployToolkit Get-WinUserLanguageList {
                return [PSCustomObject]@{ LanguageTag = 'en-US' }
            }
        }

        It 'Throws when the requested culture is not in the installed list' {
            { Set-ADTPowerShellCulture -CultureInfo ([System.Globalization.CultureInfo]::new('fr-FR')) } | Should -Throw
        }

        It 'Error message identifies the unrecognised culture' {
            $err = $null
            try { Set-ADTPowerShellCulture -CultureInfo ([System.Globalization.CultureInfo]::new('fr-FR')) }
            catch { $err = $_ }
            $err | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Throws when CultureInfo is null' {
            { Set-ADTPowerShellCulture -CultureInfo $null } | Should -Throw
        }

        It 'Throws when CultureInfo is an empty string' {
            { Set-ADTPowerShellCulture -CultureInfo '' } | Should -Throw
        }
    }
}
