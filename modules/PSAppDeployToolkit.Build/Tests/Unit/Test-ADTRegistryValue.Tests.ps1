BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest
}
Describe 'Test-ADTRegistryValue' {
    BeforeAll {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestRegistry', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $TestRegistry = (New-Item -Path 'TestRegistry:\TestLocation' -ItemType Directory).PSPath
        New-ItemProperty -LiteralPath $TestRegistry -Name 'Test' -Value 0 -PropertyType DWord | Out-Null

        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should return $true' {
            Test-ADTRegistryValue -Key $TestRegistry -Name 'Test' | Should -BeTrue
        }
        It 'Should return $false' {
            Test-ADTRegistryValue -Key $TestRegistry -Name 'DoesNotExist' | Should -BeFalse
        }
    }

    Context 'Values it has to find or not find' {
        It 'Should find the default value' {
            # Stored under the empty name rather than under '(Default)', so the two have to be mapped.
            $key = (New-Item -Path "TestRegistry:\Default$([System.Guid]::NewGuid().ToString('N'))" -ItemType Directory).PSPath
            $null = New-ItemProperty -LiteralPath $key -Name '(Default)' -Value 'content' -PropertyType String
            Test-ADTRegistryValue -Key $key -Name '(Default)' | Should -BeTrue
        }

        It 'Should return $false for a key that does not exist' {
            # A missing key is an answer rather than an error, since callers ask this before creating one.
            # Named below a key that does exist, because the path has to name a hive the function can
            # resolve before it can report the key beneath it as absent.
            $key = (New-Item -Path "TestRegistry:\Absent$([System.Guid]::NewGuid().ToString('N'))" -ItemType Directory).PSPath
            Test-ADTRegistryValue -Key "$key\NeverExisted" -Name 'Anything' | Should -BeFalse
        }

        It 'Should look under the user a SID names' {
            # The SID rewrites HKEY_CURRENT_USER to that user's hive under HKEY_USERS, so the caller's own
            # SID has to give the same answer as asking without one.
            $sid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value
            Test-ADTRegistryValue -Key 'HKEY_CURRENT_USER\Software' -Name "ADTNeverExists$([System.Guid]::NewGuid().ToString('N'))" -SID $sid | Should -BeFalse
        }
    }

    Context 'Input Validation' {
        It 'Should verify that Key is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Test-ADTRegistryValue'
            }
            { Test-ADTRegistryValue -Key $null -Name 'Anything' } | Should @shouldParams
            { Test-ADTRegistryValue -Key '' -Name 'Anything' } | Should @shouldParams
            { Test-ADTRegistryValue -Key " `f`n`r`t`v" -Name 'Anything' } | Should @shouldParams
        }
        It 'Should verify that Name is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Test-ADTRegistryValue'
            }
            { Test-ADTRegistryValue -Key 'Anything' -Name $null } | Should @shouldParams
            { Test-ADTRegistryValue -Key 'Anything' -Name '' } | Should @shouldParams
            { Test-ADTRegistryValue -Key 'Anything' -Name " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should verify that SID is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Test-ADTRegistryValue -Key 'Anything' -Name 'Test' -SID $null } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Test-ADTRegistryValue'
            { Test-ADTRegistryValue -Key 'Anything' -Name 'Test' -SID '' } | Should @shouldParams -ErrorId 'ParameterArgumentTransformationError,Test-ADTRegistryValue'
            { Test-ADTRegistryValue -Key 'Anything' -Name 'Test' -SID " `f`n`r`t`v" } | Should @shouldParams -ErrorId 'ParameterArgumentTransformationError,Test-ADTRegistryValue'
        }
    }
}
