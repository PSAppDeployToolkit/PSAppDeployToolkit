function Show-ADTBlockedAppDialog
{
    [CmdletBinding()]
    param
    (
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        try
        {
            $adtSession = Get-ADTSession
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }
    }

    process
    {
        # Return early if someone happens to call this in a non-async mode.
        if (!$adtSession.GetPropertyValue('InstallPhase').Equals('Asynchronous'))
        {
            return
        }

        try
        {
            try
            {
                # If we're here, we're not to log anything.
                $adtSession.SetPropertyValue('DisableLogging', $true)

                # Create a mutex and specify a name without acquiring a lock on the mutex.
                $showBlockedAppDialogMutexName = "Global\$((Get-ADTEnvironment).appDeployToolkitName)_ShowBlockedAppDialog_Message"
                $showBlockedAppDialogMutex = [System.Threading.Mutex]::new($false, $showBlockedAppDialogMutexName)

                # Attempt to acquire an exclusive lock on the mutex, attempt will fail after 1 millisecond if unable to acquire exclusive lock.
                if (($showBlockedAppDialogMutexLocked = Test-ADTIsMutexAvailable -MutexName $showBlockedAppDialogMutexName) -and $showBlockedAppDialogMutex.WaitOne(1))
                {
                    Show-ADTInstallationPrompt -Title $adtSession.GetPropertyValue('InstallTitle') -Message (Get-ADTStrings).BlockExecution.Message -Icon Warning -ButtonRightText OK
                }
                else
                {
                    # If attempt to acquire an exclusive lock on the mutex failed, then exit script as another blocked app dialog window is already open.
                    Write-ADTLogEntry -Message "Unable to acquire an exclusive lock on mutex [$showBlockedAppDialogMutexName] because another blocked application dialog window is already open. Exiting script..." -Severity 2
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
