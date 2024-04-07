Function Get-MsiExitCodeMessage {
	<#
.SYNOPSIS

    Get message for MSI error code

.DESCRIPTION

    Get message for MSI error code by reading it from msimsg.dll

.PARAMETER MsiErrorCode

    MSI error code

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the message for the MSI error code.

.EXAMPLE

    Get-MsiExitCodeMessage -MsiErrorCode 1618

.NOTES

    This is an internal script function and should typically not be called directly.

.LINK

    http://msdn.microsoft.com/en-us/library/aa368542(v=vs.85).aspx

.LINK

    https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $true)]
		[ValidateNotNullorEmpty()]
		[Int32]$MsiExitCode
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			Write-Log -Message "Getting message for exit code [$MsiExitCode]." -Source ${CmdletName}
			[String]$MsiExitCodeMsg = [PSADT.Msi]::GetMessageFromMsiExitCode($MsiExitCode)
			Write-Output -InputObject ($MsiExitCodeMsg)
		} Catch {
			Write-Log -Message "Failed to get message for exit code [$MsiExitCode]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
