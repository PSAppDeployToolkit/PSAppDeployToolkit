BeforeDiscovery {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"

    # Taking ownership away from the account that holds it needs a privilege an ordinary user does not
    # have, so those tests are skipped rather than failing for want of elevation.
    $script:IsElevated = Test-ADTCallerElevated
}

BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

    # BUILTIN\Users, referred to by SID throughout so that a localised Windows does not change the answer.
    $script:UsersSid = 'S-1-5-32-545'
    $script:AdminsSid = 'S-1-5-32-544'

    function Get-RuleCount
    {
        [CmdletBinding()]
        [OutputType([System.Int32])]
        param
        (
            [Parameter(Mandatory = $true)]
            [System.String]$Path,

            [Parameter(Mandatory = $true)]
            [System.String]$Sid
        )

        $matched = 0
        foreach ($rule in (Get-Acl -LiteralPath $Path).Access)
        {
            if ($rule.IdentityReference.Translate([System.Security.Principal.SecurityIdentifier]).Value.Equals($Sid))
            {
                $matched++
            }
        }
        return $matched
    }
}

Describe 'Set-ADTItemPermission' {
    BeforeEach {
        $script:Target = "$TestDrive\Item$([System.Guid]::NewGuid().ToString('N'))"
        $null = New-Item -Path $script:Target -ItemType Directory -Force
    }

    Context 'Access rules' {
        It 'Adds a rule for the user it was given' {
            Set-ADTItemPermission -LiteralPath $script:Target -User "*$script:UsersSid" -Permission Read
            Get-RuleCount -Path $script:Target -Sid $script:UsersSid | Should -Be 1
        }

        It 'Leaves the rules that were already there' {
            # Adding a rule is not the same as replacing the whole list, and a deployment that wiped the
            # inherited rules would leave a folder nobody else could read.
            $before = @((Get-Acl -LiteralPath $script:Target).Access).Count
            Set-ADTItemPermission -LiteralPath $script:Target -User "*$script:UsersSid" -Permission Read
            @((Get-Acl -LiteralPath $script:Target).Access).Count | Should -Be ($before + 1)
        }

        It 'Removes a rule when told to' {
            Set-ADTItemPermission -LiteralPath $script:Target -User "*$script:UsersSid" -Permission Read
            Set-ADTItemPermission -LiteralPath $script:Target -User "*$script:UsersSid" -Permission Read -Method RemoveAccessRule
            Get-RuleCount -Path $script:Target -Sid $script:UsersSid | Should -Be 0
        }

        It 'Accepts an account name as well as a SID' {
            # SIDs are prefixed with an asterisk precisely so that both forms can share the parameter.
            Set-ADTItemPermission -LiteralPath $script:Target -User ([System.Security.Principal.SecurityIdentifier]::new($script:UsersSid).Translate([System.Security.Principal.NTAccount]).Value) -Permission Read
            Get-RuleCount -Path $script:Target -Sid $script:UsersSid | Should -Be 1
        }

        It 'Drops the flags a file cannot carry' {
            # Inheritance flags are meaningless on a file, and Windows refuses a rule that carries them.
            $file = "$script:Target\Item.txt"
            Set-Content -LiteralPath $file -Value 'content'
            Set-ADTItemPermission -LiteralPath $file -User "*$script:UsersSid" -Permission FullControl -Inheritance ContainerInherit
            $rule = (Get-Acl -LiteralPath $file).Access | & { process { if ($_.IdentityReference.Translate([System.Security.Principal.SecurityIdentifier]).Value.Equals($script:UsersSid)) { return $_ } } }
            $rule.InheritanceFlags | Should -Be ([System.Security.AccessControl.InheritanceFlags]::None)
        }

        It 'Changes nothing with -WhatIf' {
            Set-ADTItemPermission -LiteralPath $script:Target -User "*$script:UsersSid" -Permission Read -WhatIf
            Get-RuleCount -Path $script:Target -Sid $script:UsersSid | Should -Be 0
        }
    }

    Context 'Inheritance' {
        It 'Disables inheritance' {
            (Get-Acl -LiteralPath $script:Target).AreAccessRulesProtected | Should -BeFalse
            Set-ADTItemPermission -LiteralPath $script:Target -DisableInheritance -User "*$script:UsersSid" -Permission Read
            (Get-Acl -LiteralPath $script:Target).AreAccessRulesProtected | Should -BeTrue
        }

        It 'Keeps the inherited rules as explicit ones when it does' {
            # Protecting a folder without copying the rules down first would lock everyone out of it.
            Set-ADTItemPermission -LiteralPath $script:Target -DisableInheritance -User "*$script:UsersSid" -Permission Read
            @((Get-Acl -LiteralPath $script:Target).Access).Count | Should -BeGreaterThan 1
        }

        It 'Enables inheritance again' {
            Set-ADTItemPermission -LiteralPath $script:Target -DisableInheritance -User "*$script:UsersSid" -Permission Read
            Set-ADTItemPermission -LiteralPath $script:Target -EnableInheritance
            (Get-Acl -LiteralPath $script:Target).AreAccessRulesProtected | Should -BeFalse
        }

        It 'Clears the explicit rules when asked' {
            Set-ADTItemPermission -LiteralPath $script:Target -DisableInheritance -User "*$script:UsersSid" -Permission Read
            Set-ADTItemPermission -LiteralPath $script:Target -EnableInheritance -RemoveExplicitRules
            @((Get-Acl -LiteralPath $script:Target).GetAccessRules($true, $false, [System.Security.Principal.SecurityIdentifier])).Count | Should -Be 0
        }
    }

    Context 'Ownership' -Skip:(!$script:IsElevated) {
        It 'Sets the owner it was given' {
            $me = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
            Set-ADTItemPermission -LiteralPath $script:Target -Owner "*$($me.Value)"
            (Get-Acl -LiteralPath $script:Target).GetOwner([System.Security.Principal.SecurityIdentifier]) | Should -Be $me
        }

        It 'Hands ownership back again' {
            $me = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
            Set-ADTItemPermission -LiteralPath $script:Target -Owner "*$($me.Value)"
            Set-ADTItemPermission -LiteralPath $script:Target -Owner "*$script:AdminsSid"
            (Get-Acl -LiteralPath $script:Target).GetOwner([System.Security.Principal.SecurityIdentifier]).Value | Should -BeExactly $script:AdminsSid
        }

        It 'Leaves the rules alone when only the owner was asked for' {
            # Taking ownership to regain access is a step of its own, ahead of deciding what the rules
            # should become.
            $before = @((Get-Acl -LiteralPath $script:Target).Access).Count
            Set-ADTItemPermission -LiteralPath $script:Target -Owner "*$([System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value)"
            @((Get-Acl -LiteralPath $script:Target).Access).Count | Should -Be $before
        }

        It 'Changes nothing with -WhatIf' {
            $before = (Get-Acl -LiteralPath $script:Target).GetOwner([System.Security.Principal.SecurityIdentifier])
            Set-ADTItemPermission -LiteralPath $script:Target -Owner "*$([System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value)" -WhatIf
            (Get-Acl -LiteralPath $script:Target).GetOwner([System.Security.Principal.SecurityIdentifier]) | Should -Be $before
        }
    }

    Context 'Applying an access control list' {
        It 'Applies a list that was built up in memory' {
            $acl = Get-Acl -LiteralPath $script:Target
            $acl.AddAccessRule([System.Security.AccessControl.FileSystemAccessRule]::new([System.Security.Principal.SecurityIdentifier]::new($script:UsersSid), 'FullControl', 'None', 'None', 'Allow'))
            Set-ADTItemPermission -LiteralPath $script:Target -AccessControlList $acl
            Get-RuleCount -Path $script:Target -Sid $script:UsersSid | Should -Be 1
        }

        It 'Copies a list read from another path' {
            # Copying permissions from one item to another is the whole reason to hand it a list, and it
            # is the one case where the list arrives with nothing marked as modified.
            $source = "$TestDrive\AclSource$([System.Guid]::NewGuid().ToString('N'))"
            $null = New-Item -Path $source -ItemType Directory -Force
            Set-ADTItemPermission -LiteralPath $source -User "*$script:UsersSid" -Permission FullControl
            Set-ADTItemPermission -LiteralPath $script:Target -AccessControlList (Get-Acl -LiteralPath $source)
            Get-RuleCount -Path $script:Target -Sid $script:UsersSid | Should -Be 1
        }

        It 'Changes nothing with -WhatIf' {
            $acl = Get-Acl -LiteralPath $script:Target
            $acl.AddAccessRule([System.Security.AccessControl.FileSystemAccessRule]::new([System.Security.Principal.SecurityIdentifier]::new($script:UsersSid), 'FullControl', 'None', 'None', 'Allow'))
            Set-ADTItemPermission -LiteralPath $script:Target -AccessControlList $acl -WhatIf
            Get-RuleCount -Path $script:Target -Sid $script:UsersSid | Should -Be 0
        }
    }

    Context 'Input Validation' {
        It 'Refuses a path that is not there' {
            { Set-ADTItemPermission -LiteralPath "$TestDrive\NeverExisted" -User "*$script:UsersSid" -Permission Read } | Should -Throw -ErrorId 'InvalidLiteralPathParameterValue,Set-ADTItemPermission'
        }

        It 'Refuses a path that is not on a filesystem' {
            # Registry permissions are a different animal and would need a different rule type entirely.
            { Set-ADTItemPermission -LiteralPath 'HKCU:\Software' -User "*$script:UsersSid" -Permission Read } | Should -Throw -ErrorId 'NonFileSystemInfoObjectError,Set-ADTItemPermission'
        }

        It 'Refuses a method the list does not have' {
            { Set-ADTItemPermission -LiteralPath $script:Target -User "*$script:UsersSid" -Permission Read -Method 'Nonsense' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Set-ADTItemPermission'
        }

        It 'Refuses the same user twice' {
            { Set-ADTItemPermission -LiteralPath $script:Target -User 'SomeUser', 'SomeUser' -Permission Read } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Set-ADTItemPermission'
        }

        It 'Refuses to both enable and disable inheritance' {
            { Set-ADTItemPermission -LiteralPath $script:Target -EnableInheritance -DisableInheritance -User "*$script:UsersSid" -Permission Read } | Should -Throw -ErrorId 'AmbiguousParameterSet,Set-ADTItemPermission'
        }
    }
}
