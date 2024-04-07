Function Set-ServiceStartMode {
	<#
.SYNOPSIS

Set the service startup mode.

.DESCRIPTION

Set the service startup mode.

.PARAMETER Name

Specify the name of the service.

.PARAMETER ComputerName

Specify the name of the computer. Default is: the local computer.

.PARAMETER StartMode

Specify startup mode for the service. Options: Automatic, Automatic (Delayed Start), Manual, Disabled, Boot, System.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Set-ServiceStartMode -Name 'wuauserv' -StartMode 'Automatic (Delayed Start)'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
	[CmdLetBinding()]
	Param (
		[Parameter(Mandatory = $true)]
		[ValidateNotNullOrEmpty()]
		[String]$Name,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullOrEmpty()]
		[String]$ComputerName = $env:ComputerName,
		[Parameter(Mandatory = $true)]
		[ValidateSet('Automatic', 'Automatic (Delayed Start)', 'Manual', 'Disabled', 'Boot', 'System')]
		[String]$StartMode,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullOrEmpty()]
		[Boolean]$ContinueOnError = $true
	)
	Begin {
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			## If on lower than Windows Vista and 'Automatic (Delayed Start)' selected, then change to 'Automatic' because 'Delayed Start' is not supported.
			If (($StartMode -eq 'Automatic (Delayed Start)') -and (([Version]$envOSVersion).Major -lt 6)) {
				$StartMode = 'Automatic'
			}

			Write-Log -Message "Set service [$Name] startup mode to [$StartMode]." -Source ${CmdletName}

			## Set the name of the start up mode that will be passed to sc.exe
			[String]$ScExeStartMode = $StartMode
			Switch ($StartMode) {
				'Automatic' {
					$ScExeStartMode = 'Auto'; Break
				}
				'Automatic (Delayed Start)' {
					$ScExeStartMode = 'Delayed-Auto'; Break
				}
				'Manual' {
					$ScExeStartMode = 'Demand'; Break
				}
			}

			## Set the start up mode using sc.exe. Note: we found that the ChangeStartMode method in the Win32_Service WMI class set services to 'Automatic (Delayed Start)' even when you specified 'Automatic' on Win7, Win8, and Win10.
			$ChangeStartMode = & "$envWinDir\System32\sc.exe" config $Name start= $ScExeStartMode

			If ($global:LastExitCode -ne 0) {
				Throw "sc.exe failed with exit code [$($global:LastExitCode)] and message [$ChangeStartMode]."
			}

			Write-Log -Message "Successfully set service [$Name] startup mode to [$StartMode]." -Source ${CmdletName}
		} Catch {
			Write-Log -Message "Failed to set service [$Name] startup mode to [$StartMode]. `r`n$(Resolve-Error)" -Source ${CmdletName} -Severity 3
			If (-not $ContinueOnError) {
				Throw "Failed to set service [$Name] startup mode to [$StartMode]: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
