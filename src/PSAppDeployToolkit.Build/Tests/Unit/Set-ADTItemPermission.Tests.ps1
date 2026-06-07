BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Set-ADTItemPermission' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        BeforeEach {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestFile', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $TestFile = "$TestDrive\TestFile.txt"
            New-Item -Path $TestFile -ItemType File -Force | Out-Null

            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestDir', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $TestDir = "$TestDrive\TestDir"
            New-Item -Path $TestDir -ItemType Directory -Force | Out-Null

            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'CurrentUser', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $CurrentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
        }

        It 'Should add a Read permission for the current user on a file (AddAccessRule)' {
            Set-ADTItemPermission -LiteralPath $TestFile -User $CurrentUser -Permission Read -Method AddAccessRule

            $acl = Get-Acl -LiteralPath $TestFile
            $rules = $acl.GetAccessRules($true, $false, [System.Security.Principal.NTAccount])
            $match = $rules | Where-Object {
                $_.IdentityReference.Value -eq $CurrentUser -and
                ($_.FileSystemRights -band [System.Security.AccessControl.FileSystemRights]::Read) -ne 0 -and
                $_.AccessControlType -eq [System.Security.AccessControl.AccessControlType]::Allow
            }
            $match | Should -Not -BeNullOrEmpty
        }

        It 'Should add a Write permission for the current user on a folder (AddAccessRule)' {
            Set-ADTItemPermission -LiteralPath $TestDir -User $CurrentUser -Permission Write -Method AddAccessRule

            $acl = Get-Acl -LiteralPath $TestDir
            $rules = $acl.GetAccessRules($true, $false, [System.Security.Principal.NTAccount])
            $match = $rules | Where-Object {
                $_.IdentityReference.Value -eq $CurrentUser -and
                ($_.FileSystemRights -band [System.Security.AccessControl.FileSystemRights]::Write) -ne 0 -and
                $_.AccessControlType -eq [System.Security.AccessControl.AccessControlType]::Allow
            }
            $match | Should -Not -BeNullOrEmpty
        }

        It 'Should add a Deny permission for the current user (PermissionType Deny)' {
            Set-ADTItemPermission -LiteralPath $TestFile -User $CurrentUser -Permission Read -PermissionType Deny -Method AddAccessRule

            $acl = Get-Acl -LiteralPath $TestFile
            $rules = $acl.GetAccessRules($true, $false, [System.Security.Principal.NTAccount])
            $match = $rules | Where-Object {
                $_.IdentityReference.Value -eq $CurrentUser -and
                ($_.FileSystemRights -band [System.Security.AccessControl.FileSystemRights]::Read) -ne 0 -and
                $_.AccessControlType -eq [System.Security.AccessControl.AccessControlType]::Deny
            }
            $match | Should -Not -BeNullOrEmpty
        }

        It 'Should remove a permission via RemoveAccessRule' {
            # First add the rule, then remove it.
            Set-ADTItemPermission -LiteralPath $TestFile -User $CurrentUser -Permission Read -Method AddAccessRule

            $aclBefore = Get-Acl -LiteralPath $TestFile
            $rulesBefore = $aclBefore.GetAccessRules($true, $false, [System.Security.Principal.NTAccount])
            $matchBefore = $rulesBefore | Where-Object {
                $_.IdentityReference.Value -eq $CurrentUser -and
                ($_.FileSystemRights -band [System.Security.AccessControl.FileSystemRights]::Read) -ne 0 -and
                $_.AccessControlType -eq [System.Security.AccessControl.AccessControlType]::Allow
            }
            $matchBefore | Should -Not -BeNullOrEmpty

            Set-ADTItemPermission -LiteralPath $TestFile -User $CurrentUser -Permission Read -Method RemoveAccessRule

            $aclAfter = Get-Acl -LiteralPath $TestFile
            $rulesAfter = $aclAfter.GetAccessRules($true, $false, [System.Security.Principal.NTAccount])
            $matchAfter = $rulesAfter | Where-Object {
                $_.IdentityReference.Value -eq $CurrentUser -and
                $_.FileSystemRights -eq [System.Security.AccessControl.FileSystemRights]::Read -and
                $_.AccessControlType -eq [System.Security.AccessControl.AccessControlType]::Allow
            }
            $matchAfter | Should -BeNullOrEmpty
        }

        It 'Should apply an ACL object directly via the AccessControlList parameter set' {
            $existingAcl = Get-Acl -LiteralPath $TestFile
            { Set-ADTItemPermission -LiteralPath $TestFile -AccessControlList $existingAcl } | Should -Not -Throw
        }

        It 'Should enable inheritance on a folder via -EnableInheritance' {
            # Disable inheritance first so we can then re-enable it.
            $acl = Get-Acl -LiteralPath $TestDir
            $acl.SetAccessRuleProtection($true, $true)
            Set-Acl -LiteralPath $TestDir -AclObject $acl

            { Set-ADTItemPermission -LiteralPath $TestDir -EnableInheritance } | Should -Not -Throw

            $aclAfter = Get-Acl -LiteralPath $TestDir
            $aclAfter.AreAccessRulesProtected | Should -BeFalse
        }
    }

    Context 'Input Validation' {
        It 'Should throw when LiteralPath does not exist' {
            { Set-ADTItemPermission -LiteralPath "$TestDrive\DoesNotExist.txt" -User 'BUILTIN\Users' -Permission Read } |
                Should -Throw -ExceptionType ([System.ArgumentException])
        }

        It 'Should accept a SID prefixed with asterisk (*) as a user identifier' {
            # S-1-5-32-545 = BUILTIN\Users — always present.
            $TestFile2 = "$TestDrive\SidTest.txt"
            New-Item -Path $TestFile2 -ItemType File -Force | Out-Null
            { Set-ADTItemPermission -LiteralPath $TestFile2 -User '*S-1-5-32-545' -Permission Read -Method AddAccessRule } | Should -Not -Throw
        }

        It 'Should verify that Method must be one of the valid set values' {
            $TestFile3 = "$TestDrive\MethodTest.txt"
            New-Item -Path $TestFile3 -ItemType File -Force | Out-Null
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Set-ADTItemPermission'
            }
            { Set-ADTItemPermission -LiteralPath $TestFile3 -User 'BUILTIN\Users' -Permission Read -Method 'InvalidMethod' } | Should @shouldParams
        }

        It 'Should not throw when LiteralPath is a valid existing path' {
            $TestFile4 = "$TestDrive\ValidPath.txt"
            New-Item -Path $TestFile4 -ItemType File -Force | Out-Null
            { Set-ADTItemPermission -LiteralPath $TestFile4 -User 'BUILTIN\Users' -Permission Read -Method AddAccessRule } | Should -Not -Throw
        }
    }
}
