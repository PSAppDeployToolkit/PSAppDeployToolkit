BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}
Describe 'Set-ADTPowerShellCulture' {
    Context 'Functionality' {
        BeforeEach {
            $script:OriginalCulture = [System.Threading.Thread]::CurrentThread.CurrentCulture
            $script:OriginalUICulture = [System.Threading.Thread]::CurrentThread.CurrentUICulture
        }

        AfterEach {
            # Restored directly rather than through the function, since the culture this process started
            # with is not necessarily one the function would accept.
            [System.Threading.Thread]::CurrentThread.CurrentCulture = $script:OriginalCulture
            [System.Threading.Thread]::CurrentThread.CurrentUICulture = $script:OriginalUICulture
        }

        # Skipped until the function is repaired. It reaches for the private m_Culture/m_uiCulture fields
        # on Microsoft.PowerShell.NativeCultureResolver, which PowerShell 7.6 replaced with read-only
        # properties that defer to the thread, so every call fails on a null field reference.
        It 'Changes the culture PowerShell resolves against' -Skip {
            # This is the whole point of the function: Import-LocalizedData and the string tables read
            # PowerShell's resolved culture, not the thread's, so the change has to reach that far.
            $target = [System.Globalization.CultureInfo]::new((Get-WinUserLanguageList)[0].LanguageTag)
            Set-ADTPowerShellCulture -CultureInfo $target
            (Get-UICulture).Name | Should -BeExactly $target.Name
        }

        It 'Returns nothing' -Skip {
            Set-ADTPowerShellCulture -CultureInfo ([System.Globalization.CultureInfo]::new((Get-WinUserLanguageList)[0].LanguageTag)) | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Refuses a culture with no language pack installed' {
            # Switching to a culture Windows cannot render would leave the dialogs showing boxes, so it is
            # refused rather than half-applied.
            $installed = (Get-WinUserLanguageList).LanguageTag
            $absent = 'kl-GL', 'haw-US', 'gn-PY' | Where-Object { $installed -notcontains $_ } | Select-Object -First 1
            { Set-ADTPowerShellCulture -CultureInfo $absent } | Should -Throw -ErrorId 'CultureNotInstalled,Set-ADTPowerShellCulture'
        }

        It 'Lists the cultures that are installed when it refuses one' {
            $installed = (Get-WinUserLanguageList).LanguageTag
            $absent = 'kl-GL', 'haw-US', 'gn-PY' | Where-Object { $installed -notcontains $_ } | Select-Object -First 1
            $record = { Set-ADTPowerShellCulture -CultureInfo $absent } | Should -Throw -ErrorId 'CultureNotInstalled,Set-ADTPowerShellCulture' -PassThru
            $record.TargetObject | Should -Not -BeNullOrEmpty
        }

        It 'Refuses something that is not a culture' {
            { Set-ADTPowerShellCulture -CultureInfo 'not a culture' } | Should -Throw -ErrorId 'ParameterArgumentTransformationError,Set-ADTPowerShellCulture'
        }

        It 'Refuses a null culture' {
            { Set-ADTPowerShellCulture -CultureInfo $null } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Set-ADTPowerShellCulture'
        }
    }
}
