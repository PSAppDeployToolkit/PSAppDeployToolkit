#region Function Test-IsMutexAvailable
Function Test-IsMutexAvailable {
<#
.SYNOPSIS
	Wait, up to a timeout value, to check if current thread is able to acquire an exclusive lock on a system mutex.
.DESCRIPTION
	A mutex can be used to serialize applications and prevent multiple instances from being opened at the same time.
	Wait, up to a timeout (default is 1 millisecond), for the mutex to become available for an exclusive lock.
.PARAMETER MutexName
	The name of the system mutex.
.PARAMETER MutexWaitTime
	The number of milliseconds the current thread should wait to acquire an exclusive lock of a named mutex. Default is: 1 millisecond.
	A wait time of -1 milliseconds means to wait indefinitely. A wait time of zero does not acquire an exclusive lock but instead tests the state of the wait handle and returns immediately.
.EXAMPLE
	Test-IsMutexAvailable -MutexName 'Global\_MSIExecute' -MutexWaitTimeInMilliseconds 500
.EXAMPLE
	Test-IsMutexAvailable -MutexName 'Global\_MSIExecute' -MutexWaitTimeInMilliseconds (New-TimeSpan -Minutes 5).TotalMilliseconds
.EXAMPLE
	Test-IsMutexAvailable -MutexName 'Global\_MSIExecute' -MutexWaitTimeInMilliseconds (New-TimeSpan -Seconds 60).TotalMilliseconds
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK
	http://msdn.microsoft.com/en-us/library/aa372909(VS.85).asp
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateLength(1,260)]
		[string]$MutexName,
		[Parameter(Mandatory=$false)]
		[ValidateScript({($_ -ge -1) -and ($_ -le [int32]::MaxValue)})]
		[int32]$MutexWaitTimeInMilliseconds = 1
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

		## Initialize Variables
		[timespan]$MutexWaitTime = [timespan]::FromMilliseconds($MutexWaitTimeInMilliseconds)
		If ($MutexWaitTime.TotalMinutes -ge 1) {
			[string]$WaitLogMsg = "$($MutexWaitTime.TotalMinutes) minute(s)"
		}
		ElseIf ($MutexWaitTime.TotalSeconds -ge 1) {
			[string]$WaitLogMsg = "$($MutexWaitTime.TotalSeconds) second(s)"
		}
		Else {
			[string]$WaitLogMsg = "$($MutexWaitTime.Milliseconds) millisecond(s)"
		}
		[boolean]$IsUnhandledException = $false
		[boolean]$IsMutexFree = $false
		[Threading.Mutex]$OpenExistingMutex = $null
	}
	Process {
		Write-Log -Message "Checking to see if mutex [$MutexName] is available. Wait up to [$WaitLogMsg] for the mutex to become available." -Source ${CmdletName}
		Try {
			## Using this variable allows capture of exceptions from .NET methods. Private scope only changes value for current function.
			$private:previousErrorActionPreference = $ErrorActionPreference
			$ErrorActionPreference = 'Stop'

			## Open the specified named mutex, if it already exists, without acquiring an exclusive lock on it. If the system mutex does not exist, this method throws an exception instead of creating the system object.
			[Threading.Mutex]$OpenExistingMutex = [Threading.Mutex]::OpenExisting($MutexName)
			## Attempt to acquire an exclusive lock on the mutex. Use a Timespan to specify a timeout value after which no further attempt is made to acquire a lock on the mutex.
			$IsMutexFree = $OpenExistingMutex.WaitOne($MutexWaitTime, $false)
		}
		Catch [Threading.WaitHandleCannotBeOpenedException] {
			## The named mutex does not exist
			$IsMutexFree = $true
		}
		Catch [ObjectDisposedException] {
			## Mutex was disposed between opening it and attempting to wait on it
			$IsMutexFree = $true
		}
		Catch [UnauthorizedAccessException] {
			## The named mutex exists, but the user does not have the security access required to use it
			$IsMutexFree = $false
		}
		Catch [Threading.AbandonedMutexException] {
			## The wait completed because a thread exited without releasing a mutex. This exception is thrown when one thread acquires a mutex object that another thread has abandoned by exiting without releasing it.
			$IsMutexFree = $true
		}
		Catch {
			$IsUnhandledException = $true
			## Return $true, to signify that mutex is available, because function was unable to successfully complete a check due to an unhandled exception. Default is to err on the side of the mutex being available on a hard failure.
			Write-Log -Message "Unable to check if mutex [$MutexName] is available due to an unhandled exception. Will default to return value of [$true]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			$IsMutexFree = $true
		}
		Finally {
			If ($IsMutexFree) {
				If (-not $IsUnhandledException) {
					Write-Log -Message "Mutex [$MutexName] is available for an exclusive lock." -Source ${CmdletName}
				}
			}
			Else {
				If ($MutexName -eq 'Global\_MSIExecute') {
					## Get the command line for the MSI installation in progress
					Try {
						[string]$msiInProgressCmdLine = Get-WmiObject -Class 'Win32_Process' -Filter "name = 'msiexec.exe'" -ErrorAction 'Stop' | ForEach-Object { if ($_.CommandLine -match '\.msi') {$_.CommandLine.Trim()} }
					}
					Catch { }
					Write-Log -Message "Mutex [$MutexName] is not available for an exclusive lock because the following MSI installation is in progress [$msiInProgressCmdLine]." -Severity 2 -Source ${CmdletName}
				}
				Else {
					Write-Log -Message "Mutex [$MutexName] is not available because another thread already has an exclusive lock on it." -Source ${CmdletName}
				}
			}

			If (($null -ne $OpenExistingMutex) -and ($IsMutexFree)) {
				## Release exclusive lock on the mutex
				$null = $OpenExistingMutex.ReleaseMutex()
				$OpenExistingMutex.Close()
			}
			If ($private:previousErrorActionPreference) { $ErrorActionPreference = $private:previousErrorActionPreference }
		}
	}
	End {
		Write-Output -InputObject $IsMutexFree

		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion
