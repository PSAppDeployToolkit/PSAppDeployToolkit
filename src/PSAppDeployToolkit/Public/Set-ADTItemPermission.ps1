#-----------------------------------------------------------------------------
#
# MARK: Set-ADTItemPermission
#
#-----------------------------------------------------------------------------

function Set-ADTItemPermission
{
    <#
    .SYNOPSIS
        Allows you to easily change permissions on files or folders.

    .DESCRIPTION
        Allows you to easily change permissions on files or folders for a given user or group. You can add, remove or replace permissions, set inheritance and propagation.

    .PARAMETER Path
        Path to the folder or file you want to modify (ex: C:\Temp)

    .PARAMETER AccessControlList
        The ACL object to apply to the given path.

    .PARAMETER User
        One or more user names (ex: BUILTIN\Users, DOMAIN\Admin) to give the permissions to. If you want to use SID, prefix it with an asterisk * (ex: *S-1-5-18)

    .PARAMETER Permission
        Permission or list of permissions to be set/added/removed/replaced. Permission DeleteSubdirectoriesAndFiles does not apply to files.

    .PARAMETER PermissionType
        Sets Access Control Type of the permissions.

    .PARAMETER Inheritance
        Sets permission inheritance. Does not apply to files. Multiple options can be specified.

        * None - The permission entry is not inherited by child objects.
        * ObjectInherit - The permission entry is inherited by child leaf objects.
        * ContainerInherit - The permission entry is inherited by child container objects.

    .PARAMETER Propagation
        Sets how to propagate inheritance. Does not apply to files.

        * None - Specifies that no inheritance flags are set.
        * NoPropagateInherit - Specifies that the permission entry is not propagated to child objects.
        * InheritOnly - Specifies that the permission entry is propagated only to child objects. This includes both container and leaf child objects.

    .PARAMETER Method
        Specifies which method will be used to apply the permissions.

        * AddAccessRule - Adds permissions rules but it does not remove previous permissions.
        * SetAccessRule - Overwrites matching permission rules with new ones.
        * ResetAccessRule - Removes matching permissions rules and then adds permission rules.
        * RemoveAccessRule - Removes matching permission rules.
        * RemoveAccessRuleAll - Removes all permission rules for specified user/s.
        * RemoveAccessRuleSpecific - Removes specific permissions.

    .PARAMETER EnableInheritance
        Enables inheritance on the files/folders.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Set-ADTItemPermission -Path 'C:\Temp' -User 'DOMAIN\John', 'BUILTIN\Users' -Permission FullControl -Inheritance ObjectInherit,ContainerInherit

        Will grant FullControl permissions to 'John' and 'Users' on 'C:\Temp' and its files and folders children.

    .EXAMPLE
        Set-ADTItemPermission -Path 'C:\Temp\pic.png' -User 'DOMAIN\John' -Permission 'Read'

        Will grant Read permissions to 'John' on 'C:\Temp\pic.png'.

    .EXAMPLE
        Set-ADTItemPermission -Path 'C:\Temp\Private' -User 'DOMAIN\John' -Permission 'None' -Method 'RemoveAll'

        Will remove all permissions to 'John' on 'C:\Temp\Private'.

    .NOTES
        An active ADT session is NOT required to use this function.

        Original Author: Julian DA CUNHA - dacunha.julian@gmail.com, used with permission.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Set-ADTItemPermission
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, HelpMessage = 'Path to the folder or file you want to modify (ex: C:\Temp)', ParameterSetName = 'DisableInheritance')]
        [Parameter(Mandatory = $true, HelpMessage = 'Path to the folder or file you want to modify (ex: C:\Temp)', ParameterSetName = 'EnableInheritance')]
        [Parameter(Mandatory = $true, HelpMessage = 'Path to the folder or file you want to modify (ex: C:\Temp)', ParameterSetName = 'AccessControlList')]
        [ValidateScript({
                if (!(Test-Path -LiteralPath $_))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified path does not exist.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [Alias('File', 'Folder')]
        [System.String]$Path,

        [Parameter(Mandatory = $true, HelpMessage = 'The ACL object to apply to the given path.', ParameterSetName = 'AccessControlList')]
        [ValidateNotNullOrEmpty()]
        [System.Security.AccessControl.FileSystemSecurity]$AccessControlList

        [Parameter(Mandatory = $true, HelpMessage = 'One or more user names (ex: BUILTIN\Users, DOMAIN\Admin). If you want to use SID, prefix it with an asterisk * (ex: *S-1-5-18)', ParameterSetName = 'DisableInheritance')]
        [Alias('Username', 'Users', 'SID', 'Usernames')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$User,

        [Parameter(Mandatory = $true, HelpMessage = "Permission or list of permissions to be set/added/removed/replaced. To see all the possible permissions go to 'http://technet.microsoft.com/fr-fr/library/ff730951.aspx'", ParameterSetName = 'DisableInheritance')]
        [Alias('Grant', 'Permissions', 'Deny')]
        [ValidateNotNullOrEmpty()]
        [System.Security.AccessControl.FileSystemRights]$Permission,

        [Parameter(Mandatory = $false, HelpMessage = 'Whether you want to set Allow or Deny permissions', ParameterSetName = 'DisableInheritance')]
        [Alias('AccessControlType')]
        [ValidateNotNullOrEmpty()]
        [System.Security.AccessControl.AccessControlType]$PermissionType = [System.Security.AccessControl.AccessControlType]::Allow,

        [Parameter(Mandatory = $false, HelpMessage = 'Sets how permissions are inherited', ParameterSetName = 'DisableInheritance')]
        [ValidateNotNullOrEmpty()]
        [System.Security.AccessControl.InheritanceFlags]$Inheritance = [System.Security.AccessControl.InheritanceFlags]::None,

        [Parameter(Mandatory = $false, HelpMessage = 'Sets how to propage inheritance flags', ParameterSetName = 'DisableInheritance')]
        [ValidateNotNullOrEmpty()]
        [System.Security.AccessControl.PropagationFlags]$Propagation = [System.Security.AccessControl.PropagationFlags]::None,

        [Parameter(Mandatory = $false, HelpMessage = 'Specifies which method will be used to add/remove/replace permissions.', ParameterSetName = 'DisableInheritance')]
        [ValidateSet('AddAccessRule', 'SetAccessRule', 'ResetAccessRule', 'RemoveAccessRule', 'RemoveAccessRuleAll', 'RemoveAccessRuleSpecific')]
        [Alias('ApplyMethod', 'ApplicationMethod')]
        [System.String]$Method = 'AddAccessRule',

        [Parameter(Mandatory = $true, HelpMessage = 'Enables inheritance, which removes explicit permissions.', ParameterSetName = 'EnableInheritance')]
        [System.Management.Automation.SwitchParameter]$EnableInheritance
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                # Directly apply the permissions if an ACL object has been provided.
                if ($PSCmdlet.ParameterSetName.Equals('AccessControlList'))
                {
                    Write-ADTLogEntry -Message "Setting specifieds ACL on path [$Path]."
                    $null = Set-Acl -LiteralPath $Path -AclObject $AccessControlList
                    return
                }

                # Get object ACLs and enable inheritance.
                if ($EnableInheritance)
                {
                    ($Acl = Get-Acl -LiteralPath $Path).SetAccessRuleProtection($false, $true)
                    Write-ADTLogEntry -Message "Enabling Inheritance on path [$Path]."
                    $null = Set-Acl -LiteralPath $Path -AclObject $Acl
                    return
                }

                # Modify variables to remove file incompatible flags if this is a file.
                if (Test-Path -LiteralPath $Path -PathType Leaf)
                {
                    $Permission = $Permission -band (-bnot [System.Security.AccessControl.FileSystemRights]::DeleteSubdirectoriesAndFiles)
                    $Inheritance = [System.Security.AccessControl.InheritanceFlags]::None
                    $Propagation = [System.Security.AccessControl.PropagationFlags]::None
                }

                # Get object ACLs for the given path.
                $Acl = Get-Acl -LiteralPath $Path

                # Apply permissions on each user.
                foreach ($Username in $User.Trim())
                {
                    # Return early if the string is empty.
                    if ([System.String]::IsNullOrWhiteSpace($Username))
                    {
                        continue
                    }

                    # Translate a SID to NTAccount.
                    if ($Username.StartsWith('*') -and !($Username = ConvertTo-ADTNTAccountOrSID -SID $Username.Remove(0, 1)))
                    {
                        continue
                    }

                    # Set/Add/Remove/Replace permissions and log the changes.
                    Write-ADTLogEntry -Message "Changing permissions [Permissions:$Permission, InheritanceFlags:$Inheritance, PropagationFlags:$Propagation, AccessControlType:$PermissionType, Method:$Method] on path [$Path] for user [$Username]."
                    $Acl.$Method([System.Security.AccessControl.FileSystemAccessRule]::new($Username, $Permission, $Inheritance, $Propagation, $PermissionType))
                }

                # Use the prepared ACL.
                $null = Set-Acl -LiteralPath $Path -AclObject $Acl
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
