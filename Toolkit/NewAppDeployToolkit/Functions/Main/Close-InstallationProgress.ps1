#region Function Close-InstallationProgress
Function Close-InstallationProgress {
<#
.SYNOPSIS
	Closes the dialog created by Show-InstallationProgress.
.DESCRIPTION
	Closes the dialog created by Show-InstallationProgress.
	This function is called by the Exit-Script function to close a running instance of the progress dialog if found.
.PARAMETER WaitingTime
	How many seconds to wait, at most, for the InstallationProgress window to be initialized, before the function returns, without closing anything. Range: 1 - 60  Default: 5
.EXAMPLE
	Close-InstallationProgress
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[ValidateRange(1,60)]
		[int]$WaitingTime = 5
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		If ($deployModeSilent) {
			Write-Log -Message "Bypassing Close-InstallationProgress [Mode: $deployMode]" -Source ${CmdletName}
			Return
		}
		# Check whether the window has been created and wait for up to $WaitingTime seconds if it does not
		[int]$Timeout = $WaitingTime
		while ((-not $script:ProgressSyncHash.Window.IsInitialized) -and ($Timeout -gt 0)) {
			if ($Timeout -eq $WaitingTime) {
				Write-Log -Message "The installation progress dialog does not exist. Waiting up to $WaitingTime seconds..." -Source ${CmdletName}
			}
			$Timeout -= 1
			Start-Sleep -Seconds 1
		}
		# Return if we still have no window
		if (-not $script:ProgressSyncHash.Window.IsInitialized) {
			Write-Log -Message "The installation progress dialog was not created within $WaitingTime seconds." -Source ${CmdletName} -Severity 2
			return
		}
		# If the thread is suspended, resume it
		if ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [system.threading.threadstate]::Suspended) {
			Write-Log -Message 'The thread for the installation progress dialog is suspended. Resuming the thread.' -Source ${CmdletName}
			try {
				$script:ProgressSyncHash.Window.Dispatcher.Thread.Resume()
			}
			catch {
				Write-Log -Message 'Failed to resume the thread for the installation progress dialog.' -Source ${CmdletName} -Severity 2
			}
		}
		# If the thread is changing its state, wait
		[int]$Timeout = 0
		while ((($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [system.threading.threadstate]::Aborted) -or ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [system.threading.threadstate]::AbortRequested) -or ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [system.threading.threadstate]::StopRequested) -or ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [system.threading.threadstate]::Unstarted) -or ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [system.threading.threadstate]::WaitSleepJoin)) -and ($Timeout -le $WaitingTime)) {
			if (-not $Timeout) {
				Write-Log -Message "The thread for the installation progress dialog is changing its state. Waiting up to $WaitingTime seconds..." -Source ${CmdletName} -Severity 2
			}
			$Timeout += 1
			Start-Sleep -Seconds 1
		}
		# If the thread is running, stop it
		if ((-not ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [system.threading.threadstate]::Stopped)) -and (-not ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -band [system.threading.threadstate]::Unstarted))) {
			Write-Log -Message 'Closing the installation progress dialog.' -Source ${CmdletName}
			$script:ProgressSyncHash.Window.Dispatcher.InvokeShutdown()
		}

		if ($script:ProgressRunspace) {
			# If the runspace is still opening, wait
			[int]$Timeout = 0
			while ((($script:ProgressRunspace.RunspaceStateInfo.State -eq [system.management.automation.runspaces.runspacestate]::Opening) -or ($script:ProgressRunspace.RunspaceStateInfo.State -eq [system.management.automation.runspaces.runspacestate]::BeforeOpen)) -and ($Timeout -le $WaitingTime)) {
				if (-not $Timeout) {
					Write-Log -Message "The runspace for the installation progress dialog is still opening. Waiting up to $WaitingTime seconds..." -Source ${CmdletName} -Severity 2
				}
				$Timeout += 1
				Start-Sleep -Seconds 1
			}
			# If the runspace is opened, close it
			if ($script:ProgressRunspace.RunspaceStateInfo.State -eq [system.management.automation.runspaces.runspacestate]::Opened) {
				Write-Log -Message "Closing the installation progress dialog`'s runspace." -Source ${CmdletName}
				$script:ProgressRunspace.Close()
			}
		} else {
			Write-Log -Message 'The runspace for the installation progress dialog is already closed.' -Source ${CmdletName} -Severity 2
		}

		if ($script:ProgressSyncHash) {
			# Clear sync hash
			$script:ProgressSyncHash.Clear()
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
#endregion
