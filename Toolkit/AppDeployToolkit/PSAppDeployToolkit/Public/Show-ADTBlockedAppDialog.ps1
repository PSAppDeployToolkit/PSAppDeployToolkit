function Show-ADTBlockedAppDialog
{
    # Return early if someone happens to call this in a non-async mode.
    if (!($adtSession = Get-ADTSession).GetPropertyValue('InstallPhase').Equals('Asynchronous'))
    {
        return
    }

    # If we're here, we're not to log anything.
    $adtSession.SetPropertyValue('DisableLogging', $true)

    try {
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
        exit 0
    }
    catch
    {
        Write-ADTLogEntry -Message "There was an error in displaying the Installation Prompt.`n$(Resolve-Error)" -Severity 3
        exit 60005
    }
    finally
    {
        if ($showBlockedAppDialogMutexLocked)
        {
            [System.Void]$showBlockedAppDialogMutex.ReleaseMutex()
        }
        if ($showBlockedAppDialogMutex)
        {
            $showBlockedAppDialogMutex.Close()
        }
    }
}
