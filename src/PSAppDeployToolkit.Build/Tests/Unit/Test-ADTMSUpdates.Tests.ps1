BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Test-ADTMSUpdates' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality - Get-HotFix path' {
        It 'Returns $true when Get-HotFix finds the KB' {
            Mock -ModuleName PSAppDeployToolkit Get-HotFix {
                return [PSCustomObject]@{ HotFixID = 'KB2549864' }
            }
            Test-ADTMSUpdates -KbNumber 'KB2549864' | Should -BeTrue
        }

        It 'Returns $false when Get-HotFix finds nothing and the COM Update Session path is not available headlessly' {
            Set-ItResult -Skipped -Because 'The COM Microsoft.Update.Session fallback path calls Marshal.FinalReleaseComObject which requires a genuine COM object; this code path cannot be exercised headlessly without a real Windows Update COM server.'
        }
    }

    Context 'Input Validation' {
        It 'Should have a mandatory KbNumber parameter' {
            (Get-Command Test-ADTMSUpdates).Parameters['KbNumber'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Throws when KbNumber is null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Test-ADTMSUpdates -KbNumber $null } | Should @shouldParams
        }

        It 'Throws when KbNumber is empty string' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Test-ADTMSUpdates -KbNumber '' } | Should @shouldParams
        }

        It 'Throws when KbNumber is whitespace only' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Test-ADTMSUpdates -KbNumber '   ' } | Should @shouldParams
        }
    }
}
