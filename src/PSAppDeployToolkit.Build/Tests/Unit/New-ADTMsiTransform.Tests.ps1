BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'New-ADTMsiTransform' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should create a transform file (.mst) for a real MSI' {
            Set-ItResult -Skipped -Because 'No MSI fixture is available in the repository for headless transform generation.'
        }

        It 'Should create a transform file at the default path (<MsiBaseName>.mst) when -NewTransformPath is not specified' {
            Set-ItResult -Skipped -Because 'No MSI fixture is available in the repository for headless transform generation.'
        }

        It 'Should create a transform file at the path specified by -NewTransformPath' {
            Set-ItResult -Skipped -Because 'No MSI fixture is available in the repository for headless transform generation.'
        }

        It 'Should overwrite an existing transform file at the destination path' {
            Set-ItResult -Skipped -Because 'No MSI fixture is available in the repository for headless transform generation.'
        }
    }

    Context 'Input Validation' {
        It 'Should throw when MsiPath does not exist' {
            { New-ADTMsiTransform -MsiPath "$TestDrive\DoesNotExist.msi" -TransformProperties @{ ALLUSERS = '1' } } |
                Should -Throw -ExceptionType ([System.ArgumentException])
        }

        It 'Should throw when ApplyTransformPath does not exist' {
            # Create a dummy file so MsiPath passes validation, but ApplyTransformPath fails.
            $dummyMsi = "$TestDrive\dummy.msi"
            New-Item -Path $dummyMsi -ItemType File -Force | Out-Null
            { New-ADTMsiTransform -MsiPath $dummyMsi -ApplyTransformPath "$TestDrive\NoSuchTransform.mst" -TransformProperties @{ ALLUSERS = '1' } } |
                Should -Throw -ExceptionType ([System.ArgumentException])
        }

        It 'Should throw when TransformProperties is null or empty' {
            $dummyMsi2 = "$TestDrive\dummy2.msi"
            New-Item -Path $dummyMsi2 -ItemType File -Force | Out-Null
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { New-ADTMsiTransform -MsiPath $dummyMsi2 -TransformProperties $null } | Should @shouldParams
            { New-ADTMsiTransform -MsiPath $dummyMsi2 -TransformProperties @{ } } | Should @shouldParams
        }

        It 'Should throw when NewTransformPath is null, empty or whitespace' {
            $dummyMsi3 = "$TestDrive\dummy3.msi"
            New-Item -Path $dummyMsi3 -ItemType File -Force | Out-Null
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,New-ADTMsiTransform'
            }
            { New-ADTMsiTransform -MsiPath $dummyMsi3 -NewTransformPath $null -TransformProperties @{ ALLUSERS = '1' } } | Should @shouldParams
            { New-ADTMsiTransform -MsiPath $dummyMsi3 -NewTransformPath '' -TransformProperties @{ ALLUSERS = '1' } } | Should @shouldParams
            { New-ADTMsiTransform -MsiPath $dummyMsi3 -NewTransformPath " `f`n`r`t`v" -TransformProperties @{ ALLUSERS = '1' } } | Should @shouldParams
        }
    }
}
