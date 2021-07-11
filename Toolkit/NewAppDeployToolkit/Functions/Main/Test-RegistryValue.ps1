#region Function Test-RegistryValue
Function Test-RegistryValue {
<#
.SYNOPSIS
	Test if a registry value exists.
.DESCRIPTION
	Checks a registry key path to see if it has a value with a given name. Can correctly handle cases where a value simply has an empty or null value.
.PARAMETER Key
	Path of the registry key.
.PARAMETER Value
	Specify the registry key value to check the existence of.
.PARAMETER SID
	The security identifier (SID) for a user. Specifying this parameter will convert a HKEY_CURRENT_USER registry key to the HKEY_USERS\$SID format.
	Specify this parameter from the Invoke-HKCURegistrySettingsForAllUsers function to read/edit HKCU registry settings for all users on the system.
.EXAMPLE
	Test-RegistryValue -Key 'HKLM:SYSTEM\CurrentControlSet\Control\Session Manager' -Value 'PendingFileRenameOperations'
.NOTES
	To test if registry key exists, use Test-Path function like so:
	Test-Path -Path $Key -PathType 'Container'
.LINK
	http://psappdeploytoolkit.com
#>
	Param (
		[Parameter(Mandatory=$true,Position=0,ValueFromPipeline=$true,ValueFromPipelineByPropertyName=$true)]
		[ValidateNotNullOrEmpty()]$Key,
		[Parameter(Mandatory=$true,Position=1)]
		[ValidateNotNullOrEmpty()]$Value,
		[Parameter(Mandatory=$false,Position=2)]
		[ValidateNotNullorEmpty()]
		[string]$SID
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## If the SID variable is specified, then convert all HKEY_CURRENT_USER key's to HKEY_USERS\$SID
		Try {
			If ($PSBoundParameters.ContainsKey('SID')) {
				[string]$Key = Convert-RegistryPath -Key $Key -SID $SID
			}
			Else {
				[string]$Key = Convert-RegistryPath -Key $Key
			}
		}
		Catch {
			Throw
		}
		[boolean]$IsRegistryValueExists = $false
		Try {
			If (Test-Path -LiteralPath $Key -ErrorAction 'Stop') {
				[string[]]$PathProperties = Get-Item -LiteralPath $Key -ErrorAction 'Stop' | Select-Object -ExpandProperty 'Property' -ErrorAction 'Stop'
				If ($PathProperties -contains $Value) { $IsRegistryValueExists = $true }
			}
		}
		Catch { }

		If ($IsRegistryValueExists) {
			Write-Log -Message "Registry key value [$Key] [$Value] does exist." -Source ${CmdletName}
		}
		Else {
			Write-Log -Message "Registry key value [$Key] [$Value] does not exist." -Source ${CmdletName}
		}
		Write-Output -InputObject $IsRegistryValueExists
	}
	End {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion
