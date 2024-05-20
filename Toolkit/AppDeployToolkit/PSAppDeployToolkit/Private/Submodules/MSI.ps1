#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Get-MsiExitCodeMessage {
    <#
.SYNOPSIS

    Get message for MSI error code

.DESCRIPTION

    Get message for MSI error code by reading it from msimsg.dll

.PARAMETER MsiExitCode

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
        Write-DebugHeader
    }
    Process {
        Try {
            Write-ADTLogEntry -Message "Getting message for exit code [$MsiExitCode]."
            [String]$MsiExitCodeMsg = [PSADT.Msi]::GetMessageFromMsiExitCode($MsiExitCode)
            Write-Output -InputObject ($MsiExitCodeMsg)
        }
        Catch {
            Write-ADTLogEntry -Message "Failed to get message for exit code [$MsiExitCode]. `r`n$(Resolve-Error)" -Severity 3
        }
    }
    End {
        Write-DebugFooter
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Test-IsMutexAvailable {
    <#
.SYNOPSIS

Wait, up to a timeout value, to check if current thread is able to acquire an exclusive lock on a system mutex.

.DESCRIPTION

A mutex can be used to serialize applications and prevent multiple instances from being opened at the same time.
Wait, up to a timeout (default is 1 millisecond), for the mutex to become available for an exclusive lock.

.PARAMETER MutexName

The name of the system mutex.

.PARAMETER MutexWaitTimeInMilliseconds

The number of milliseconds the current thread should wait to acquire an exclusive lock of a named mutex. Default is: 1 millisecond.
A wait timeof -1 milliseconds means to wait indefinitely. A wait time of zero does not acquire an exclusive lock but instead tests the state of the wait handle and returns immediately.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Boolean

Returns $true if the current thread acquires an exclusive lock on the named mutex, $false otherwise.

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

.LINK

    https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateLength(1, 260)]
        [String]$MutexName,
        [Parameter(Mandatory = $false)]
        [ValidateScript({ ($_ -ge -1) -and ($_ -le [Int32]::MaxValue) })]
        [Int32]$MutexWaitTimeInMilliseconds = 1
    )

    Begin {
        Write-DebugHeader

        ## Initialize Variables
        [Timespan]$MutexWaitTime = [Timespan]::FromMilliseconds($MutexWaitTimeInMilliseconds)
        If ($MutexWaitTime.TotalMinutes -ge 1) {
            [String]$WaitLogMsg = "$($MutexWaitTime.TotalMinutes) minute(s)"
        }
        ElseIf ($MutexWaitTime.TotalSeconds -ge 1) {
            [String]$WaitLogMsg = "$($MutexWaitTime.TotalSeconds) second(s)"
        }
        Else {
            [String]$WaitLogMsg = "$($MutexWaitTime.Milliseconds) millisecond(s)"
        }
        [Boolean]$IsUnhandledException = $false
        [Boolean]$IsMutexFree = $false
        [Threading.Mutex]$OpenExistingMutex = $null
    }
    Process {
        Write-ADTLogEntry -Message "Checking to see if mutex [$MutexName] is available. Wait up to [$WaitLogMsg] for the mutex to become available."
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
            Write-ADTLogEntry -Message "Unable to check if mutex [$MutexName] is available due to an unhandled exception. Will default to return value of [$true]. `r`n$(Resolve-Error)" -Severity 3
            $IsMutexFree = $true
        }
        Finally {
            If ($IsMutexFree) {
                If (-not $IsUnhandledException) {
                    Write-ADTLogEntry -Message "Mutex [$MutexName] is available for an exclusive lock."
                }
            }
            Else {
                If ($MutexName -eq 'Global\_MSIExecute') {
                    ## Get the command line for the MSI installation in progress
                    Try {
                        [String]$msiInProgressCmdLine = Get-WmiObject -Class 'Win32_Process' -Filter "name = 'msiexec.exe'" -ErrorAction 'Stop' | Where-Object { $_.CommandLine } | Select-Object -ExpandProperty 'CommandLine' | Where-Object { $_ -match '\.msi' } | ForEach-Object { $_.Trim() }
                    }
                    Catch {
                    }
                    Write-ADTLogEntry -Message "Mutex [$MutexName] is not available for an exclusive lock because the following MSI installation is in progress [$msiInProgressCmdLine]." -Severity 2
                }
                Else {
                    Write-ADTLogEntry -Message "Mutex [$MutexName] is not available because another thread already has an exclusive lock on it."
                }
            }

            If (($null -ne $OpenExistingMutex) -and ($IsMutexFree)) {
                ## Release exclusive lock on the mutex
                $null = $OpenExistingMutex.ReleaseMutex()
                $OpenExistingMutex.Close()
            }
            If ($private:previousErrorActionPreference) {
                $ErrorActionPreference = $private:previousErrorActionPreference
            }
        }
    }
    End {
        Write-Output -InputObject ($IsMutexFree)

        Write-DebugFooter
    }
}
