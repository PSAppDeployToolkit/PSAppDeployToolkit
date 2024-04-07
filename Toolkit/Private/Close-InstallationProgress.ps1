Function Close-InstallationProgress {
	<#
.SYNOPSIS

Closes the dialog created by Show-InstallationProgress.

.DESCRIPTION

Closes the dialog created by Show-InstallationProgress.

This function is called by the Exit-Script function to close a running instance of the progress dialog if found.

.PARAMETER WaitingTime

How many seconds to wait, at most, for the InstallationProgress window to be initialized, before the function returns, without closing anything. Range: 1 - 60  Default: 5

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the version of the specified file.

.EXAMPLE

Close-InstallationProgress

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $false)]
		[ValidateRange(1, 60)]
		[Int32]$WaitingTime = 5
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		If ($deployModeSilent) {
			Write-Log -Message "Bypassing Close-InstallationProgress [Mode: $deployMode]" -Source ${CmdletName}
			Return
		}
		# Check whether the window has been created and wait for up to $WaitingTime seconds if it does not
		[Int32]$Timeout = $WaitingTime
		While ((-not $script:ProgressSyncHash.Window.IsInitialized) -and ($Timeout -gt 0)) {
			If ($Timeout -eq $WaitingTime) {
				Write-Log -Message "The installation progress dialog does not exist. Waiting up to $WaitingTime seconds..." -Source ${CmdletName}
			}
			$Timeout -= 1
			Start-Sleep -Seconds 1
		}
		# Return if we still have no window
		If (-not $script:ProgressSyncHash.Window.IsInitialized) {
			Write-Log -Message "The installation progress dialog was not created within $WaitingTime seconds." -Source ${CmdletName} -Severity 2
			Return
		}
		# If the thread is suspended, resume it
		If ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [System.Threading.ThreadState]::Suspended) {
			Write-Log -Message 'The thread for the installation progress dialog is suspended. Resuming the thread.' -Source ${CmdletName}
			Try {
				$script:ProgressSyncHash.Window.Dispatcher.Thread.Resume()
			} Catch {
				Write-Log -Message 'Failed to resume the thread for the installation progress dialog.' -Source ${CmdletName} -Severity 2
			}
		}
		# If the thread is changing its state, wait
		[Int32]$Timeout = 0
		While ((($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [System.Threading.ThreadState]::Aborted) -or ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [System.Threading.ThreadState]::AbortRequested) -or ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [System.Threading.ThreadState]::StopRequested) -or ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [System.Threading.ThreadState]::Unstarted) -or ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [System.Threading.ThreadState]::WaitSleepJoin)) -and ($Timeout -le $WaitingTime)) {
			If (-not $Timeout) {
				Write-Log -Message "The thread for the installation progress dialog is changing its state. Waiting up to $WaitingTime seconds..." -Source ${CmdletName} -Severity 2
			}
			$Timeout += 1
			Start-Sleep -Seconds 1
		}
		# If the thread is running, stop it
		If ((-not ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [System.Threading.ThreadState]::Stopped)) -and (-not ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [System.Threading.ThreadState]::Unstarted))) {
			Write-Log -Message 'Closing the installation progress dialog.' -Source ${CmdletName}
			$script:ProgressSyncHash.Window.Dispatcher.InvokeShutdown()
		}

		If ($script:ProgressRunspace) {
			# If the runspace is still opening, wait
			[Int32]$Timeout = 0
			While ((($script:ProgressRunspace.RunspaceStateInfo.State -eq [System.Management.Automation.Runspaces.RunspaceState]::Opening) -or ($script:ProgressRunspace.RunspaceStateInfo.State -eq [System.Management.Automation.Runspaces.RunspaceState]::BeforeOpen)) -and ($Timeout -le $WaitingTime)) {
				If (-not $Timeout) {
					Write-Log -Message "The runspace for the installation progress dialog is still opening. Waiting up to $WaitingTime seconds..." -Source ${CmdletName} -Severity 2
				}
				$Timeout += 1
				Start-Sleep -Seconds 1
			}
			# If the runspace is opened, close it
			If ($script:ProgressRunspace.RunspaceStateInfo.State -eq [System.Management.Automation.Runspaces.RunspaceState]::Opened) {
				Write-Log -Message "Closing the installation progress dialog`'s runspace." -Source ${CmdletName}
				$script:ProgressRunspace.Close()
			}
		} Else {
			Write-Log -Message 'The runspace for the installation progress dialog is already closed.' -Source ${CmdletName} -Severity 2
		}

		If ($script:ProgressSyncHash) {
			# Clear sync hash
			$script:ProgressSyncHash.Clear()
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
