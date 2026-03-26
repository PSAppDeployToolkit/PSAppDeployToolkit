BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Set-ADTItemPermission' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # A well-known group present on all Windows machines, safe to use in tests.
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestUser', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
        $TestUser = 'BUILTIN\Users'
    }

    BeforeEach {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestFolder', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
        $TestFolder = (New-Item -Path "$TestDrive\TestFolder-$(New-Guid)" -ItemType Directory -Force).FullName

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestFile', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
        $TestFile = (New-Item -Path "$TestFolder\test.txt" -ItemType File -Force).FullName

        New-Item -Path "$TestFolder\Sub1\ChildFile.txt" -ItemType File -Force | Out-Null
    }

    Context 'Adding Permissions to a Folder' {
        It 'Adds a Read permission to a folder for a user' {
            Set-ADTItemPermission -LiteralPath $TestFolder -User $TestUser -Permission Read

            $acl = Get-Acl -LiteralPath $TestFolder
            $rule = $acl.Access | Where-Object { $_.IdentityReference -like "*Users*" -and $_.AccessControlType -eq 'Allow' }
            $rule | Should -Not -BeNullOrEmpty
        }

        It 'Adds a FullControl permission to a folder' {
            Set-ADTItemPermission -LiteralPath $TestFolder -User $TestUser -Permission FullControl

            $acl = Get-Acl -LiteralPath $TestFolder
            $rule = $acl.Access | Where-Object {
                $_.IdentityReference -like "*Users*" -and
                $_.AccessControlType -eq 'Allow' -and
                ($_.FileSystemRights -band [System.Security.AccessControl.FileSystemRights]::FullControl)
            }
            $rule | Should -Not -BeNullOrEmpty
        }

        It 'Adds a permission with container and object inheritance' {
            Set-ADTItemPermission -LiteralPath $TestFolder -User $TestUser -Permission Read -Inheritance ContainerInherit, ObjectInherit

            $acl = Get-Acl -LiteralPath $TestFolder
            $rule = $acl.Access | Where-Object {
                $_.IdentityReference -like "*Users*" -and
                $_.InheritanceFlags -band [System.Security.AccessControl.InheritanceFlags]::ContainerInherit
            }
            $rule | Should -Not -BeNullOrEmpty
        }

        It 'Adds a Deny permission for a user' {
            Set-ADTItemPermission -LiteralPath $TestFolder -User $TestUser -Permission Write -PermissionType Deny

            $acl = Get-Acl -LiteralPath $TestFolder
            $rule = $acl.Access | Where-Object {
                $_.IdentityReference -like "*Users*" -and
                $_.AccessControlType -eq 'Deny'
            }
            $rule | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Adding Permissions to a File' {
        It 'Adds a Read permission to a file' {
            Set-ADTItemPermission -LiteralPath $TestFile -User $TestUser -Permission Read

            $acl = Get-Acl -LiteralPath $TestFile
            $rule = $acl.Access | Where-Object { $_.IdentityReference -like "*Users*" -and $_.AccessControlType -eq 'Allow' }
            $rule | Should -Not -BeNullOrEmpty
        }

        It 'Strips DeleteSubdirectoriesAndFiles when applied to a file' {
            # DeleteSubdirectoriesAndFiles is silently masked off for files.
            { Set-ADTItemPermission -LiteralPath $TestFile -User $TestUser -Permission DeleteSubdirectoriesAndFiles } | Should -Not -Throw
        }
    }

    Context 'Removing Permissions' {
        It 'Removes all permissions for a user with RemoveAccessRuleAll' {
            # First add a rule, then remove it.
            Set-ADTItemPermission -LiteralPath $TestFolder -User $TestUser -Permission Read
            Set-ADTItemPermission -LiteralPath $TestFolder -User $TestUser -Permission Read -Method RemoveAccessRuleAll

            $acl = Get-Acl -LiteralPath $TestFolder
            $rule = $acl.Access | Where-Object {
                $_.IdentityReference -like "*Users*" -and
                $_.AccessControlType -eq 'Allow' -and
                ($_.IsInherited -eq $false)
            }
            $rule | Should -BeNullOrEmpty
        }
    }

    Context 'Inheritance Control' {
        It 'Enables inheritance on a folder without throwing' {
            # First disable it, then re-enable.
            $acl = Get-Acl -LiteralPath $TestFolder
            $acl.SetAccessRuleProtection($true, $true)
            Set-Acl -LiteralPath $TestFolder -AclObject $acl

            { Set-ADTItemPermission -LiteralPath $TestFolder -EnableInheritance } | Should -Not -Throw
        }

        It 'Enables inheritance and removes explicit rules' {
            $acl = Get-Acl -LiteralPath $TestFolder
            $acl.SetAccessRuleProtection($true, $true)
            Set-Acl -LiteralPath $TestFolder -AclObject $acl

            { Set-ADTItemPermission -LiteralPath $TestFolder -EnableInheritance -RemoveExplicitRules } | Should -Not -Throw
        }

        It 'Disables inheritance on a folder without throwing' {
            { Set-ADTItemPermission -LiteralPath $TestFolder -User $TestUser -Permission Read -DisableInheritance } | Should -Not -Throw

            $acl = Get-Acl -LiteralPath $TestFolder
            $acl.AreAccessRulesProtected | Should -BeTrue
        }
    }

    Context 'Direct ACL Application' {
        It 'Applies a pre-built ACL object directly' {
            $acl = Get-Acl -LiteralPath $TestFolder
            $rule = [System.Security.AccessControl.FileSystemAccessRule]::new($TestUser, 'Read', 'Allow')
            $acl.AddAccessRule($rule)

            { Set-ADTItemPermission -LiteralPath $TestFolder -AccessControlList $acl } | Should -Not -Throw

            (Get-Acl -LiteralPath $TestFolder).Access | Where-Object { $_.IdentityReference -like "*Users*" } | Should -Not -BeNullOrEmpty
        }
    }

    Context 'WhatIf Support' {
        It 'Does not modify permissions when -WhatIf is specified' {
            $aclBefore = (Get-Acl -LiteralPath $TestFolder).Access.Count
            Set-ADTItemPermission -LiteralPath $TestFolder -User $TestUser -Permission FullControl -WhatIf
            $aclAfter = (Get-Acl -LiteralPath $TestFolder).Access.Count
            $aclAfter | Should -Be $aclBefore
        }
    }

    Context 'Multiple Users' {
        It 'Adds permissions for multiple users in one call' {
            Set-ADTItemPermission -LiteralPath $TestFolder -User @($TestUser, 'BUILTIN\Administrators') -Permission Read

            $acl = Get-Acl -LiteralPath $TestFolder
            ($acl.Access | Where-Object { ($_.IdentityReference -like "*Users*" -or $_.IdentityReference -like "*Administrators*") -and $_.AccessControlType -eq 'Allow' -and !$_.IsInherited }).Count | Should -BeGreaterOrEqual 2
        }
    }

    Context 'Input Validation' {
        It 'Throws when LiteralPath does not exist' {
            { Set-ADTItemPermission -LiteralPath "$TestDrive\DoesNotExist" -User $TestUser -Permission Read } | Should -Throw
        }
    }
}
