BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTApplication' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Output contract' {
        It 'Returns zero or more PSADT.AppManagement.InstalledApplication objects' {
            $result = Get-ADTApplication
            if ($result)
            {
                $result | ForEach-Object {
                    $_ | Should -BeOfType ([PSADT.AppManagement.InstalledApplication])
                }
            }
            else
            {
                Set-ItResult -Skipped -Because 'No installed applications found on this machine to verify output type.'
            }
        }

        It 'Returned objects have a non-empty DisplayName property' {
            $result = Get-ADTApplication
            if (!$result)
            {
                Set-ItResult -Skipped -Because 'No installed applications found on this machine.'
                return
            }
            $result | ForEach-Object {
                $_.DisplayName | Should -Not -BeNullOrEmpty
            }
        }

        It 'Returned objects expose the expected shape of properties' {
            $result = Get-ADTApplication
            if (!$result)
            {
                Set-ItResult -Skipped -Because 'No installed applications found on this machine.'
                return
            }
            $first = $result | Select-Object -First 1
            $first.PSObject.Properties.Name | Should -Contain 'DisplayName'
            $first.PSObject.Properties.Name | Should -Contain 'UninstallString'
            $first.PSObject.Properties.Name | Should -Contain 'DisplayVersion'
            $first.PSObject.Properties.Name | Should -Contain 'Publisher'
            $first.PSObject.Properties.Name | Should -Contain 'WindowsInstaller'
            $first.PSObject.Properties.Name | Should -Contain 'ProductCode'
            $first.PSObject.Properties.Name | Should -Contain 'Is64BitApplication'
        }
    }

    Context 'Name filtering' {
        BeforeAll {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'AllApps', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $AllApps = Get-ADTApplication
        }

        It 'Name filter with Contains match returns only apps whose DisplayName contains the search term' {
            if (!$AllApps)
            {
                Set-ItResult -Skipped -Because 'No installed applications found on this machine.'
                return
            }
            # Pick the first app's name and verify filtering works.
            $searchName = $AllApps[0].DisplayName
            $result = Get-ADTApplication -Name $searchName -NameMatch Exact
            $result | Should -Not -BeNullOrEmpty
            $result | ForEach-Object {
                $_.DisplayName | Should -Be $searchName
            }
        }

        It 'Name filter with Contains match ignores apps not containing the term' {
            $result = Get-ADTApplication -Name 'zzzzThisApplicationCannotExist99999'
            $result | Should -BeNullOrEmpty
        }

        It 'Name filter with Exact match returns nothing for a bogus exact name' {
            $result = Get-ADTApplication -Name 'zzzzThisApplicationCannotExist99999' -NameMatch Exact
            $result | Should -BeNullOrEmpty
        }

        It 'Name filter with Wildcard match returns nothing for a bogus wildcard pattern' {
            $result = Get-ADTApplication -Name 'zzzzThisApplicationCannotExist*' -NameMatch Wildcard
            $result | Should -BeNullOrEmpty
        }

        It 'Name filter with Regex match returns nothing for a bogus regex pattern' {
            $result = Get-ADTApplication -Name '^zzzzThisApplicationCannotExist\d{20}' -NameMatch Regex
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'ApplicationType filtering' {
        It 'ApplicationType MSI returns only apps where WindowsInstaller is truthy' {
            $result = Get-ADTApplication -ApplicationType MSI
            if (!$result)
            {
                Set-ItResult -Skipped -Because 'No MSI applications found on this machine.'
                return
            }
            $result | ForEach-Object {
                $_.WindowsInstaller | Should -Be $true
            }
        }

        It 'ApplicationType EXE returns only apps where WindowsInstaller is falsy' {
            $result = Get-ADTApplication -ApplicationType EXE
            if (!$result)
            {
                Set-ItResult -Skipped -Because 'No EXE applications found on this machine.'
                return
            }
            $result | ForEach-Object {
                $_.WindowsInstaller | Should -Be $false
            }
        }

        It 'ApplicationType All returns results that include both MSI and EXE apps when both exist' {
            $allResult = Get-ADTApplication -ApplicationType All
            $msiResult = Get-ADTApplication -ApplicationType MSI
            $exeResult = Get-ADTApplication -ApplicationType EXE
            if (!$allResult)
            {
                Set-ItResult -Skipped -Because 'No installed applications found on this machine.'
                return
            }
            $allResult.Count | Should -BeGreaterOrEqual ($msiResult.Count + $exeResult.Count)
        }
    }

    Context 'ProductCode filtering' {
        It 'ProductCode filter returns nothing for a bogus GUID' {
            $result = Get-ADTApplication -ProductCode ([System.Guid]::new('00000000-0000-0000-0000-000000000000'))
            $result | Should -BeNullOrEmpty
        }

        It 'ProductCode filter returns matching MSI app when given a real product code' {
            $msiApps = Get-ADTApplication -ApplicationType MSI
            if (!$msiApps)
            {
                Set-ItResult -Skipped -Because 'No MSI applications found on this machine to test ProductCode filtering.'
                return
            }
            $firstWithCode = $msiApps | Where-Object { $_.ProductCode -and $_.ProductCode -ne [System.Guid]::Empty } | Select-Object -First 1
            if (!$firstWithCode)
            {
                Set-ItResult -Skipped -Because 'No MSI application with a ProductCode found on this machine.'
                return
            }
            $result = Get-ADTApplication -ProductCode $firstWithCode.ProductCode
            $result | Should -Not -BeNullOrEmpty
            $result[0].ProductCode | Should -Be $firstWithCode.ProductCode
        }
    }

    Context 'FilterScript parameter' {
        It 'FilterScript narrows results to only matching apps' {
            $result = Get-ADTApplication -FilterScript { $false }
            $result | Should -BeNullOrEmpty
        }

        It 'FilterScript accepting all returns same count as unfiltered' {
            $unfiltered = Get-ADTApplication
            $filtered = Get-ADTApplication -FilterScript { $true }
            $filtered.Count | Should -Be $unfiltered.Count
        }
    }

    Context 'Input Validation' {
        It 'NameMatch only accepts Contains, Exact, Wildcard, or Regex' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Get-ADTApplication'
            }
            { Get-ADTApplication -NameMatch 'InvalidMatch' } | Should @shouldParams
        }

        It 'ApplicationType only accepts All, MSI, or EXE' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Get-ADTApplication'
            }
            { Get-ADTApplication -ApplicationType 'InvalidType' } | Should @shouldParams
        }
    }
}
