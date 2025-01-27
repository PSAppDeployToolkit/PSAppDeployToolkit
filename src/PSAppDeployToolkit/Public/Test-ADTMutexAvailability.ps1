#-----------------------------------------------------------------------------
#
# MARK: Test-ADTMutexAvailability
#
#-----------------------------------------------------------------------------

function Test-ADTMutexAvailability
{
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

    .INPUTS
        None. You cannot pipe objects to this function.

    .OUTPUTS
        System.Boolean. Returns $true if the current thread acquires an exclusive lock on the named mutex, $false otherwise.

    .EXAMPLE
        Test-ADTMutexAvailability -MutexName 'Global\_MSIExecute' -MutexWaitTime 5000000

    .EXAMPLE
        Test-ADTMutexAvailability -MutexName 'Global\_MSIExecute' -MutexWaitTime (New-TimeSpan -Minutes 5)

    .EXAMPLE
        Test-ADTMutexAvailability -MutexName 'Global\_MSIExecute' -MutexWaitTime (New-TimeSpan -Seconds 60)

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        http://msdn.microsoft.com/en-us/library/aa372909(VS.85).asp

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateLength(1, 260)]
        [System.String]$MutexName,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.TimeSpan]$MutexWaitTime = [System.TimeSpan]::FromMilliseconds(1)
    )

    begin
    {
        # Initialize variables.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $WaitLogMsg = if ($MutexWaitTime.TotalMinutes -ge 1)
        {
            "$($MutexWaitTime.TotalMinutes) minute(s)"
        }
        elseif ($MutexWaitTime.TotalSeconds -ge 1)
        {
            "$($MutexWaitTime.TotalSeconds) second(s)"
        }
        else
        {
            "$($MutexWaitTime.Milliseconds) millisecond(s)"
        }
        $IsUnhandledException = $false
        $IsMutexFree = $false
        [System.Threading.Mutex]$OpenExistingMutex = $null
    }

    process
    {
        Write-ADTLogEntry -Message "Checking to see if mutex [$MutexName] is available. Wait up to [$WaitLogMsg] for the mutex to become available."
        try
        {
            # Open the specified named mutex, if it already exists, without acquiring an exclusive lock on it. If the system mutex does not exist, this method throws an exception instead of creating the system object.
            $OpenExistingMutex = [Threading.Mutex]::OpenExisting($MutexName)

            # Attempt to acquire an exclusive lock on the mutex. Use a Timespan to specify a timeout value after which no further attempt is made to acquire a lock on the mutex.
            $IsMutexFree = $OpenExistingMutex.WaitOne($MutexWaitTime, $false)
        }
        catch [Threading.WaitHandleCannotBeOpenedException]
        {
            # The named mutex does not exist.
            $IsMutexFree = $true
        }
        catch [ObjectDisposedException]
        {
            # Mutex was disposed between opening it and attempting to wait on it.
            $IsMutexFree = $true
        }
        catch [UnauthorizedAccessException]
        {
            # The named mutex exists, but the user does not have the security access required to use it.
            $IsMutexFree = $false
        }
        catch [Threading.AbandonedMutexException]
        {
            # The wait completed because a thread exited without releasing a mutex. This exception is thrown when one thread acquires a mutex object that another thread has abandoned by exiting without releasing it.
            $IsMutexFree = $true
        }
        catch
        {
            # Return $true, to signify that mutex is available, because function was unable to successfully complete a check due to an unhandled exception. Default is to err on the side of the mutex being available on a hard failure.
            Write-ADTLogEntry -Message "Unable to check if mutex [$MutexName] is available due to an unhandled exception. Will default to return value of [$true].`n$(Resolve-ADTErrorRecord -ErrorRecord $_)" -Severity 3
            $IsUnhandledException = $true
            $IsMutexFree = $true
        }
        finally
        {
            if ($IsMutexFree)
            {
                if (!$IsUnhandledException)
                {
                    Write-ADTLogEntry -Message "Mutex [$MutexName] is available for an exclusive lock."
                }
            }
            elseif (($MutexName -eq 'Global\_MSIExecute') -and ($msiInProgressCmdLine = Get-CimInstance -ClassName Win32_Process -Filter "(Name = 'msiexec.exe') AND (CommandLine like '*.msi*')" | Select-Object -ExpandProperty CommandLine))
            {
                Write-ADTLogEntry -Message "Mutex [$MutexName] is not available for an exclusive lock because the following MSI installation is in progress [$($msiInProgressCmdLine.Trim())]." -Severity 2
            }
            else
            {
                Write-ADTLogEntry -Message "Mutex [$MutexName] is not available because another thread already has an exclusive lock on it."
            }

            if (($null -ne $OpenExistingMutex) -and $IsMutexFree)
            {
                # Release exclusive lock on the mutex.
                $null = $OpenExistingMutex.ReleaseMutex()
                $OpenExistingMutex.Close()
            }
        }
        return $IsMutexFree
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
