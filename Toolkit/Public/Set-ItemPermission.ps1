Function Set-ItemPermission {
	<#
.SYNOPSIS

    Allow you to easily change permissions on files or folders

.DESCRIPTION

    Allow you to easily change permissions on files or folders for a given user or group.
    You can add, remove or replace permissions, set inheritance and propagation.

.PARAMETER Path

    Path to the folder or file you want to modify (ex: C:\Temp)

.PARAMETER User

    One or more user names (ex: BUILTIN\Users, DOMAIN\Admin) to give the permissions to. If you want to use SID, prefix it with an asterisk * (ex: *S-1-5-18)

.PARAMETER Permission

    Permission or list of permissions to be set/added/removed/replaced. To see all the possible permissions go to 'http://technet.microsoft.com/fr-fr/library/ff730951.aspx'.

    Permission DeleteSubdirectoriesAndFiles does not apply to files.

.PARAMETER PermissionType

    Sets Access Control Type of the permissions. Allowed options: Allow, Deny   Default: Allow

.PARAMETER Inheritance

    Sets permission inheritance. Does not apply to files. Multiple options can be specified. Allowed options: ObjectInherit, ContainerInherit, None  Default: None

    None - The permission entry is not inherited by child objects, ObjectInherit - The permission entry is inherited by child leaf objects. ContainerInherit - The permission entry is inherited by child container objects.

.PARAMETER Propagation

    Sets how to propagate inheritance. Does not apply to files. Allowed options: None, InheritOnly, NoPropagateInherit  Default: None

    None - Specifies that no inheritance flags are set. NoPropagateInherit - Specifies that the permission entry is not propagated to child objects. InheritOnly - Specifies that the permission entry is propagated only to child objects. This includes both container and leaf child objects.

.PARAMETER Method

    Specifies which method will be used to apply the permissions. Allowed options: Add, Set, Reset.

    Add - adds permissions rules but it does not remove previous permissions, Set - overwrites matching permission rules with new ones, Reset - removes matching permissions rules and then adds permission rules, Remove - Removes matching permission rules, RemoveSpecific - Removes specific permissions, RemoveAll - Removes all permission rules for specified user/s
    Default: Add

.PARAMETER EnableInheritance

    Enables inheritance on the files/folders.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

    Will grant FullControl permissions to 'John' and 'Users' on 'C:\Temp' and its files and folders children.

    PS C:\>Set-ItemPermission -Path 'C:\Temp' -User 'DOMAIN\John', 'BUILTIN\Utilisateurs' -Permission FullControl -Inheritance ObjectInherit,ContainerInherit

.EXAMPLEan

    Will grant Read permissions to 'John' on 'C:\Temp\pic.png'

    PS C:\>Set-ItemPermission -Path 'C:\Temp\pic.png' -User 'DOMAIN\John' -Permission 'Read'

.EXAMPLE

    Will remove all permissions to 'John' on 'C:\Temp\Private'

    PS C:\>Set-ItemPermission -Path 'C:\Temp\Private' -User 'DOMAIN\John' -Permission 'None' -Method 'RemoveAll'

.NOTES

    Original Author: Julian DA CUNHA - dacunha.julian@gmail.com, used with permission

.LINK

    https://psappdeploytoolkit.com
#>

	[CmdletBinding()]
	Param (
		[Parameter( Mandatory = $true, Position = 0, HelpMessage = 'Path to the folder or file you want to modify (ex: C:\Temp)', ParameterSetName = 'DisableInheritance' )]
		[Parameter( Mandatory = $true, Position = 0, HelpMessage = 'Path to the folder or file you want to modify (ex: C:\Temp)', ParameterSetName = 'EnableInheritance' )]
		[ValidateNotNullOrEmpty()]
		[Alias('File', 'Folder')]
		[String]$Path,

		[Parameter( Mandatory = $true, Position = 1, HelpMessage = 'One or more user names (ex: BUILTIN\Users, DOMAIN\Admin). If you want to use SID, prefix it with an asterisk * (ex: *S-1-5-18)', ParameterSetName = 'DisableInheritance')]
		[Alias('Username', 'Users', 'SID', 'Usernames')]
		[String[]]$User,

		[Parameter( Mandatory = $true, Position = 2, HelpMessage = "Permission or list of permissions to be set/added/removed/replaced. To see all the possible permissions go to 'http://technet.microsoft.com/fr-fr/library/ff730951.aspx'", ParameterSetName = 'DisableInheritance')]
		[Alias('Acl', 'Grant', 'Permissions', 'Deny')]
		[ValidateSet('AppendData', 'ChangePermissions', 'CreateDirectories', 'CreateFiles', 'Delete', `
				'DeleteSubdirectoriesAndFiles', 'ExecuteFile', 'FullControl', 'ListDirectory', 'Modify', `
				'Read', 'ReadAndExecute', 'ReadAttributes', 'ReadData', 'ReadExtendedAttributes', 'ReadPermissions', `
				'Synchronize', 'TakeOwnership', 'Traverse', 'Write', 'WriteAttributes', 'WriteData', 'WriteExtendedAttributes', 'None')]
		[String[]]$Permission,

		[Parameter( Mandatory = $false, Position = 3, HelpMessage = 'Whether you want to set Allow or Deny permissions', ParameterSetName = 'DisableInheritance')]
		[Alias('AccessControlType')]
		[ValidateSet('Allow', 'Deny')]
		[String]$PermissionType = 'Allow',

		[Parameter( Mandatory = $false, Position = 4, HelpMessage = 'Sets how permissions are inherited', ParameterSetName = 'DisableInheritance')]
		[ValidateSet('ContainerInherit', 'None', 'ObjectInherit')]
		[String[]]$Inheritance = 'None',

		[Parameter( Mandatory = $false, Position = 5, HelpMessage = 'Sets how to propage inheritance flags', ParameterSetName = 'DisableInheritance')]
		[ValidateSet('None', 'InheritOnly', 'NoPropagateInherit')]
		[String]$Propagation = 'None',

		[Parameter( Mandatory = $false, Position = 6, HelpMessage = 'Specifies which method will be used to add/remove/replace permissions.', ParameterSetName = 'DisableInheritance')]
		[ValidateSet('Add', 'Set', 'Reset', 'Remove', 'RemoveSpecific', 'RemoveAll')]
		[Alias('ApplyMethod', 'ApplicationMethod')]
		[String]$Method = 'Add',

		[Parameter( Mandatory = $true, Position = 1, HelpMessage = 'Enables inheritance, which removes explicit permissions.', ParameterSetName = 'EnableInheritance')]
		[Switch]$EnableInheritance
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}

	Process {
		# Test elevated perms
		If (-not $IsAdmin) {
			Write-Log -Message 'Unable to use the function [Set-ItemPermission] without elevated permissions.' -Source ${CmdletName}
			Throw 'Unable to use the function [Set-ItemPermission] without elevated permissions.'
		}

		# Check path existence
		If (-not (Test-Path -Path $Path -ErrorAction 'Stop')) {
			Write-Log -Message "Specified path does not exist [$Path]." -Source ${CmdletName}
			Throw "Specified path does not exist [$Path]."
		}

		If ($EnableInheritance) {
			# Get object acls
			$Acl = (Get-Item -Path $Path -ErrorAction 'Stop').GetAccessControl('Access')
			# Enable inherance
			$Acl.SetAccessRuleProtection($false, $true)
			Write-Log -Message "Enabling Inheritance on path [$Path]." -Source ${CmdletName}
			$null = Set-Acl -Path $Path -AclObject $Acl -ErrorAction 'Stop'
			Return
		}
		# Permissions
		[System.Security.AccessControl.FileSystemRights]$FileSystemRights = New-Object -TypeName 'System.Security.AccessControl.FileSystemRights'
		If ($Permission -ne 'None') {
			ForEach ($Entry in $Permission) {
				$FileSystemRights = $FileSystemRights -bor [System.Security.AccessControl.FileSystemRights]$Entry
			}
		}

		# InheritanceFlags
		$InheritanceFlag = New-Object -TypeName 'System.Security.AccessControl.InheritanceFlags'
		ForEach ($IFlag in $Inheritance) {
			$InheritanceFlag = $InheritanceFlag -bor [System.Security.AccessControl.InheritanceFlags]$IFlag
		}

		# PropagationFlags
		$PropagationFlag = [System.Security.AccessControl.PropagationFlags]$Propagation

		# Access Control Type
		$Allow = [System.Security.AccessControl.AccessControlType]$PermissionType

		# Modify variables to remove file incompatible flags if this is a file
		If (Test-Path -Path $Path -ErrorAction 'Stop' -PathType 'Leaf') {
			$FileSystemRights = $FileSystemRights -band (-bnot [System.Security.AccessControl.FileSystemRights]::DeleteSubdirectoriesAndFiles)
			$InheritanceFlag = [System.Security.AccessControl.InheritanceFlags]::None
			$PropagationFlag = [System.Security.AccessControl.PropagationFlags]::None
		}

		# Get object acls
		$Acl = (Get-Item -Path $Path -ErrorAction 'Stop').GetAccessControl('Access')
		# Disable inherance, Preserve inherited permissions
		$Acl.SetAccessRuleProtection($true, $true)
		$null = Set-Acl -Path $Path -AclObject $Acl -ErrorAction 'Stop'
		# Get updated acls - without inheritance
		$Acl = $null
		$Acl = (Get-Item -Path $Path -ErrorAction 'Stop').GetAccessControl('Access')
		# Apply permissions on Users
		ForEach ($U in $User) {
			# Trim whitespace and skip if empty
			$U = $U.Trim()
			If ($U.Length -eq 0) {
				Continue
			}
			# Set Username
			If ($U.StartsWith('*')) {
				# This is a SID, remove the *
				$U = $U.remove(0, 1)
				Try {
					# Translate the SID
					$UsersAccountName = ConvertTo-NTAccountOrSID -SID $U
				} Catch {
					Write-Log "Failed to translate SID [$U]. Skipping..." -Source ${CmdletName} -Severity 2
					Continue
				}

				$Username = New-Object -TypeName 'System.Security.Principal.NTAccount' -ArgumentList ($UsersAccountName)
			} Else {
				$Username = New-Object -TypeName 'System.Security.Principal.NTAccount' -ArgumentList ($U)
			}

			# Set/Add/Remove/Replace permissions and log the changes
			$Rule = New-Object -TypeName 'System.Security.AccessControl.FileSystemAccessRule' -ArgumentList ($Username, $FileSystemRights, $InheritanceFlag, $PropagationFlag, $Allow)
			Switch ($Method) {
				'Add' {
					Write-Log -Message "Setting permissions [Permissions:$FileSystemRights, InheritanceFlags:$InheritanceFlag, PropagationFlags:$PropagationFlag, AccessControlType:$Allow, Method:$Method] on path [$Path] for user [$Username]." -Source ${CmdletName}
					$Acl.AddAccessRule($Rule)
					Break
				}
				'Set' {
					Write-Log -Message "Setting permissions [Permissions:$FileSystemRights, InheritanceFlags:$InheritanceFlag, PropagationFlags:$PropagationFlag, AccessControlType:$Allow, Method:$Method] on path [$Path] for user [$Username]." -Source ${CmdletName}
					$Acl.SetAccessRule($Rule)
					Break
				}
				'Reset' {
					Write-Log -Message "Setting permissions [Permissions:$FileSystemRights, InheritanceFlags:$InheritanceFlag, PropagationFlags:$PropagationFlag, AccessControlType:$Allow, Method:$Method] on path [$Path] for user [$Username]." -Source ${CmdletName}
					$Acl.ResetAccessRule($Rule)
					Break
				}
				'Remove' {
					Write-Log -Message "Removing permissions [Permissions:$FileSystemRights, InheritanceFlags:$InheritanceFlag, PropagationFlags:$PropagationFlag, AccessControlType:$Allow, Method:$Method] on path [$Path] for user [$Username]." -Source ${CmdletName}
					$Acl.RemoveAccessRule($Rule)
					Break
				}
				'RemoveSpecific' {
					Write-Log -Message "Removing permissions [Permissions:$FileSystemRights, InheritanceFlags:$InheritanceFlag, PropagationFlags:$PropagationFlag, AccessControlType:$Allow, Method:$Method] on path [$Path] for user [$Username]." -Source ${CmdletName}
					$Acl.RemoveAccessRuleSpecific($Rule)
					Break
				}
				'RemoveAll' {
					Write-Log -Message "Removing permissions [Permissions:$FileSystemRights, InheritanceFlags:$InheritanceFlag, PropagationFlags:$PropagationFlag, AccessControlType:$Allow, Method:$Method] on path [$Path] for user [$Username]." -Source ${CmdletName}
					$Acl.RemoveAccessRuleAll($Rule)
					Break
				}
			}
		}
		# Use the prepared ACL
		$null = Set-Acl -Path $Path -AclObject $Acl -ErrorAction 'Stop'
	}

	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
