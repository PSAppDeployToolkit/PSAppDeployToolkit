Function ConvertTo-NTAccountOrSID {
	<#
.SYNOPSIS

Convert between NT Account names and their security identifiers (SIDs).

.DESCRIPTION

Specify either the NT Account name or the SID and get the other. Can also convert well known sid types.

.PARAMETER AccountName

The Windows NT Account name specified in <domain>\<username> format.
Use fully qualified account names (e.g., <domain>\<username>) instead of isolated names (e.g, <username>) because they are unambiguous and provide better performance.

.PARAMETER SID

The Windows NT Account SID.

.PARAMETER WellKnownSIDName

Specify the Well Known SID name translate to the actual SID (e.g., LocalServiceSid).

To get all well known SIDs available on system: [Enum]::GetNames([Security.Principal.WellKnownSidType])

.PARAMETER WellKnownToNTAccount

Convert the Well Known SID to an NTAccount name

.INPUTS

System.String

Accepts a string containing the NT Account name or SID.

.OUTPUTS

System.String

Returns the NT Account name or SID.

.EXAMPLE

ConvertTo-NTAccountOrSID -AccountName 'CONTOSO\User1'

Converts a Windows NT Account name to the corresponding SID

.EXAMPLE

ConvertTo-NTAccountOrSID -SID 'S-1-5-21-1220945662-2111687655-725345543-14012660'

Converts a Windows NT Account SID to the corresponding NT Account Name

.EXAMPLE

ConvertTo-NTAccountOrSID -WellKnownSIDName 'NetworkServiceSid'

Converts a Well Known SID name to a SID

.NOTES

This is an internal script function and should typically not be called directly.

The conversion can return an empty result if the user account does not exist anymore or if translation fails.

http://blogs.technet.com/b/askds/archive/2011/07/28/troubleshooting-sid-translation-failures-from-the-obvious-to-the-not-so-obvious.aspx

.LINK

https://psappdeploytoolkit.com

.LINK

http://msdn.microsoft.com/en-us/library/system.security.principal.wellknownsidtype(v=vs.110).aspx

#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $true, ParameterSetName = 'NTAccountToSID', ValueFromPipelineByPropertyName = $true)]
		[ValidateNotNullOrEmpty()]
		[String]$AccountName,
		[Parameter(Mandatory = $true, ParameterSetName = 'SIDToNTAccount', ValueFromPipelineByPropertyName = $true)]
		[ValidateNotNullOrEmpty()]
		[String]$SID,
		[Parameter(Mandatory = $true, ParameterSetName = 'WellKnownName', ValueFromPipelineByPropertyName = $true)]
		[ValidateNotNullOrEmpty()]
		[String]$WellKnownSIDName,
		[Parameter(Mandatory = $false, ParameterSetName = 'WellKnownName')]
		[ValidateNotNullOrEmpty()]
		[Switch]$WellKnownToNTAccount
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			Switch ($PSCmdlet.ParameterSetName) {
				'SIDToNTAccount' {
					[String]$msg = "the SID [$SID] to an NT Account name"
					Write-Log -Message "Converting $msg." -Source ${CmdletName}

					Try {
						$NTAccountSID = New-Object -TypeName 'System.Security.Principal.SecurityIdentifier' -ArgumentList ($SID)
						$NTAccount = $NTAccountSID.Translate([Security.Principal.NTAccount])
						Write-Output -InputObject ($NTAccount)
					} Catch {
						Write-Log -Message "Unable to convert $msg. It may not be a valid account anymore or there is some other problem. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
					}
				}
				'NTAccountToSID' {
					[String]$msg = "the NT Account [$AccountName] to a SID"
					Write-Log -Message "Converting $msg." -Source ${CmdletName}

					Try {
						$NTAccount = New-Object -TypeName 'System.Security.Principal.NTAccount' -ArgumentList ($AccountName)
						$NTAccountSID = $NTAccount.Translate([Security.Principal.SecurityIdentifier])
						Write-Output -InputObject ($NTAccountSID)
					} Catch {
						Write-Log -Message "Unable to convert $msg. It may not be a valid account anymore or there is some other problem. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
					}
				}
				'WellKnownName' {
					If ($WellKnownToNTAccount) {
						[String]$ConversionType = 'NTAccount'
					} Else {
						[String]$ConversionType = 'SID'
					}
					[String]$msg = "the Well Known SID Name [$WellKnownSIDName] to a $ConversionType"
					Write-Log -Message "Converting $msg." -Source ${CmdletName}

					#  Get the SID for the root domain
					Try {
						$MachineRootDomain = (Get-WmiObject -Class 'Win32_ComputerSystem' -ErrorAction 'Stop').Domain.ToLower()
						$ADDomainObj = New-Object -TypeName 'System.DirectoryServices.DirectoryEntry' -ArgumentList ("LDAP://$MachineRootDomain")
						$DomainSidInBinary = $ADDomainObj.ObjectSid
						$DomainSid = New-Object -TypeName 'System.Security.Principal.SecurityIdentifier' -ArgumentList ($DomainSidInBinary[0], 0)
					} Catch {
						Write-Log -Message 'Unable to get Domain SID from Active Directory. Setting Domain SID to $null.' -Severity 2 -Source ${CmdletName}
						$DomainSid = $null
					}

					#  Get the SID for the well known SID name
					$WellKnownSidType = [Security.Principal.WellKnownSidType]::$WellKnownSIDName
					$NTAccountSID = New-Object -TypeName 'System.Security.Principal.SecurityIdentifier' -ArgumentList ($WellKnownSidType, $DomainSid)

					If ($WellKnownToNTAccount) {
						$NTAccount = $NTAccountSID.Translate([Security.Principal.NTAccount])
						Write-Output -InputObject ($NTAccount)
					} Else {
						Write-Output -InputObject ($NTAccountSID)
					}
				}
			}
		} Catch {
			Write-Log -Message "Failed to convert $msg. It may not be a valid account anymore or there is some other problem. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
