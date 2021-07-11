#region Function Get-UserProfiles
Function Get-UserProfiles {
<#
.SYNOPSIS
	Get the User Profile Path, User Account Sid, and the User Account Name for all users that log onto the machine and also the Default User (which does not log on).
.DESCRIPTION
	Get the User Profile Path, User Account Sid, and the User Account Name for all users that log onto the machine and also the Default User (which does  not log on).
	Please note that the NTAccount property may be empty for some user profiles but the SID and ProfilePath properties will always be populated.
.PARAMETER ExcludeNTAccount
	Specify NT account names in Domain\Username format to exclude from the list of user profiles.
.PARAMETER ExcludeSystemProfiles
	Exclude system profiles: SYSTEM, LOCAL SERVICE, NETWORK SERVICE. Default is: $true.
.PARAMETER ExcludeDefaultUser
	Exclude the Default User. Default is: $false.
.EXAMPLE
	Get-UserProfiles
	Returns the following properties for each user profile on the system: NTAccount, SID, ProfilePath
.EXAMPLE
	Get-UserProfiles -ExcludeNTAccount 'CONTOSO\Robot','CONTOSO\ntadmin'
.EXAMPLE
	[string[]]$ProfilePaths = Get-UserProfiles | Select-Object -ExpandProperty 'ProfilePath'
	Returns the user profile path for each user on the system. This information can then be used to make modifications under the user profile on the filesystem.
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[string[]]$ExcludeNTAccount,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ExcludeSystemProfiles = $true,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[switch]$ExcludeDefaultUser = $false
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			Write-Log -Message 'Getting the User Profile Path, User Account SID, and the User Account Name for all users that log onto the machine.' -Source ${CmdletName}

			## Get the User Profile Path, User Account Sid, and the User Account Name for all users that log onto the machine
			[string]$UserProfileListRegKey = 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList'
			[psobject[]]$UserProfiles = Get-ChildItem -LiteralPath $UserProfileListRegKey -ErrorAction 'Stop' |
			ForEach-Object {
				$ProfileProperties = Get-ItemProperty -LiteralPath $_.PSPath -ErrorAction 'Stop' | Where-Object { ($_.ProfileImagePath) } |
				Select-Object @{ Label = 'NTAccount'; Expression = { $(ConvertTo-NTAccountOrSID -SID $_.PSChildName).Value } }, @{ Label = 'SID'; Expression = { $_.PSChildName } }, @{ Label = 'ProfilePath'; Expression = { $_.ProfileImagePath } }
				## This removes "defaultuser0" account, which is Windows's 10 bug
				if ($ProfileProperties.NTAccount) {$ProfileProperties}
			}
			If ($ExcludeSystemProfiles) {
				[string[]]$SystemProfiles = 'S-1-5-18', 'S-1-5-19', 'S-1-5-20'
				[psobject[]]$UserProfiles = $UserProfiles | Where-Object { $SystemProfiles -notcontains $_.SID }
			}
			If ($ExcludeNTAccount) {
				[psobject[]]$UserProfiles = $UserProfiles | Where-Object { $ExcludeNTAccount -notcontains $_.NTAccount }
			}

			## Find the path to the Default User profile
			If (-not $ExcludeDefaultUser) {
				[string]$UserProfilesDirectory = (Get-ItemProperty -LiteralPath $UserProfileListRegKey -Name 'ProfilesDirectory' -ErrorAction 'Stop').ProfilesDirectory

				#  On Windows Vista or higher
				If (([version]$envOSVersion).Major -gt 5) {
					# Path to Default User Profile directory on Windows Vista or higher: By default, C:\Users\Default
					[string]$DefaultUserProfileDirectory = (Get-ItemProperty -LiteralPath $UserProfileListRegKey -Name 'Default' -ErrorAction 'Stop').Default
				}
				#  On Windows XP or lower
				Else {
					#  Default User Profile Name: By default, 'Default User'
					[string]$DefaultUserProfileName = (Get-ItemProperty -LiteralPath $UserProfileListRegKey -Name 'DefaultUserProfile' -ErrorAction 'Stop').DefaultUserProfile

					#  Path to Default User Profile directory: By default, C:\Documents and Settings\Default User
					[string]$DefaultUserProfileDirectory = Join-Path -Path $UserProfilesDirectory -ChildPath $DefaultUserProfileName
				}

				## Create a custom object for the Default User profile.
				#  Since the Default User is not an actual User account, it does not have a username or a SID.
				#  We will make up a SID and add it to the custom object so that we have a location to load the default registry hive into later on.
				[psobject]$DefaultUserProfile = New-Object -TypeName 'PSObject' -Property @{
					NTAccount = 'Default User'
					SID = 'S-1-5-21-Default-User'
					ProfilePath = $DefaultUserProfileDirectory
				}

				## Add the Default User custom object to the User Profile list.
				$UserProfiles += $DefaultUserProfile
			}

			Write-Output -InputObject $UserProfiles
		}
		Catch {
			Write-Log -Message "Failed to create a custom object representing all user profiles on the machine. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
		}
	}
	End {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion
