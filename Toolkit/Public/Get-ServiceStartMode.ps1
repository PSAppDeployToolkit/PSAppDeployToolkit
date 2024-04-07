Function Get-ServiceStartMode {
	<#
.SYNOPSIS

Get the service startup mode.

.DESCRIPTION

Get the service startup mode.

.PARAMETER Name

Specify the name of the service.

.PARAMETER ComputerName

Specify the name of the computer. Default is: the local computer.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.ServiceProcess.ServiceController.

Returns the service object.

.EXAMPLE

Get-ServiceStartMode -Name 'wuauserv'

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
			Write-Log -Message "Getting the service [$Name] startup mode." -Source ${CmdletName}
			[String]$ServiceStartMode = (Get-WmiObject -ComputerName $ComputerName -Class 'Win32_Service' -Filter "Name='$Name'" -Property 'StartMode' -ErrorAction 'Stop').StartMode
			## If service start mode is set to 'Auto', change value to 'Automatic' to be consistent with 'Set-ServiceStartMode' function
			If ($ServiceStartMode -eq 'Auto') {
				$ServiceStartMode = 'Automatic'
			}

			## If on Windows Vista or higher, check to see if service is set to Automatic (Delayed Start)
			If (($ServiceStartMode -eq 'Automatic') -and (([Version]$envOSVersion).Major -gt 5)) {
				Try {
					[String]$ServiceRegistryPath = "Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\$Name"
					[Int32]$DelayedAutoStart = Get-ItemProperty -LiteralPath $ServiceRegistryPath -ErrorAction 'Stop' | Select-Object -ExpandProperty 'DelayedAutoStart' -ErrorAction 'Stop'
					If ($DelayedAutoStart -eq 1) {
						$ServiceStartMode = 'Automatic (Delayed Start)'
					}
				} Catch {
				}
			}

			Write-Log -Message "Service [$Name] startup mode is set to [$ServiceStartMode]." -Source ${CmdletName}
			Write-Output -InputObject ($ServiceStartMode)
		} Catch {
			Write-Log -Message "Failed to get the service [$Name] startup mode. `r`n$(Resolve-Error)" -Source ${CmdletName} -Severity 3
			If (-not $ContinueOnError) {
				Throw "Failed to get the service [$Name] startup mode: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
