Function Update-GroupPolicy {
	<#
.SYNOPSIS

Performs a gpupdate command to refresh Group Policies on the local machine.

.DESCRIPTION

Performs a gpupdate command to refresh Group Policies on the local machine.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Update-GroupPolicy

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $false)]
		[ValidateNotNullorEmpty()]
		[Boolean]$ContinueOnError = $true
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		[String[]]$GPUpdateCmds = '/C echo N | gpupdate.exe /Target:Computer /Force', '/C echo N | gpupdate.exe /Target:User /Force'
		[Int32]$InstallCount = 0
		ForEach ($GPUpdateCmd in $GPUpdateCmds) {
			Try {
				If ($InstallCount -eq 0) {
					[String]$InstallMsg = 'Updating Group Policies for the Machine'
				} Else {
					[String]$InstallMsg = 'Updating Group Policies for the User'
				}
				Write-Log -Message "$($InstallMsg)..." -Source ${CmdletName}
				[PSObject]$ExecuteResult = Execute-Process -Path "$envWinDir\System32\cmd.exe" -Parameters $GPUpdateCmd -WindowStyle 'Hidden' -PassThru -ExitOnProcessFailure $false

				If ($ExecuteResult.ExitCode -ne 0) {
					If ($ExecuteResult.ExitCode -eq 60002) {
						Throw "Execute-Process function failed with exit code [$($ExecuteResult.ExitCode)]."
					} Else {
						Throw "gpupdate.exe failed with exit code [$($ExecuteResult.ExitCode)]."
					}
				}
				$InstallCount++
			} Catch {
				Write-Log -Message "$($InstallMsg) failed. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
				If (-not $ContinueOnError) {
					Throw "$($InstallMsg) failed: $($_.Exception.Message)"
				}
				Continue
			}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
