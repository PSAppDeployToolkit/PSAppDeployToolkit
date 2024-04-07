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

.PARAMETER Wow6432Node

Specify this switch to check the 32-bit registry (Wow6432Node) on 64-bit systems.

.INPUTS

System.String

Accepts a string value for the registry key path.

.OUTPUTS

System.String

Returns $true if the registry value exists, $false if it does not.

.EXAMPLE

Test-RegistryValue -Key 'HKLM:SYSTEM\CurrentControlSet\Control\Session Manager' -Value 'PendingFileRenameOperations'

.NOTES

To test if registry key exists, use Test-Path function like so:

Test-Path -Path $Key -PathType 'Container'

.LINK

https://psappdeploytoolkit.com
#>
	Param (
		[Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
		[ValidateNotNullOrEmpty()]$Key,
		[Parameter(Mandatory = $true, Position = 1)]
		[ValidateNotNullOrEmpty()]$Value,
		[Parameter(Mandatory = $false, Position = 2)]
		[ValidateNotNullorEmpty()]
		[String]$SID,
		[Parameter(Mandatory = $false)]
		[Switch]$Wow6432Node = $false
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## If the SID variable is specified, then convert all HKEY_CURRENT_USER key's to HKEY_USERS\$SID
		Try {
			If ($PSBoundParameters.ContainsKey('SID')) {
				[String]$Key = Convert-RegistryPath -Key $Key -Wow6432Node:$Wow6432Node -SID $SID
			} Else {
				[String]$Key = Convert-RegistryPath -Key $Key -Wow6432Node:$Wow6432Node
			}
		} Catch {
			Throw
		}
		[Boolean]$IsRegistryValueExists = $false
		Try {
			If (Test-Path -LiteralPath $Key -ErrorAction 'Stop') {
				[String[]]$PathProperties = Get-Item -LiteralPath $Key -ErrorAction 'Stop' | Select-Object -ExpandProperty 'Property' -ErrorAction 'Stop'
				If ($PathProperties -contains $Value) {
					$IsRegistryValueExists = $true
				}
			}
		} Catch {
		}

		If ($IsRegistryValueExists) {
			Write-Log -Message "Registry key value [$Key] [$Value] does exist." -Source ${CmdletName}
		} Else {
			Write-Log -Message "Registry key value [$Key] [$Value] does not exist." -Source ${CmdletName}
		}
		Write-Output -InputObject ($IsRegistryValueExists)
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
