#-----------------------------------------------------------------------------
#
# MARK: Exit-ADTInvocation
#
#-----------------------------------------------------------------------------

function Private:Exit-ADTInvocation
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.Int32]]$ExitCode,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoShellExit,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Force
    )

    # Invoke on-exit callbacks.
    $callbackErrors = foreach ($callback in (Get-ADTModuleCallback -Hookpoint OnExit | & { process { return $_ } }))
    {
        try
        {
            & $callback
        }
        catch
        {
            $_
        }
    }

    # Attempt to close down any dialog or client/server process here as an additional safety item.
    $clientOpen = if (Test-ADTClientServerActive)
    {
        if (($clientServerInstance = Get-ADTClientServerInstance).ProgressDialogOpenAsync().ConfigureAwait($false).GetAwaiter().GetResult())
        {
            try
            {
                Close-ADTInstallationProgress
            }
            catch
            {
                $_
            }
        }
        if ($clientServerInstance.NotifyIconOpenAsync().ConfigureAwait($false).GetAwaiter().GetResult())
        {
            try
            {
                Close-ADTNotifyIcon
            }
            catch
            {
                $_
            }
        }
        try
        {
            Close-ADTClientServerInstance
        }
        catch
        {
            $_
        }
    }

    # Invoke a silent restart on the device if specified.
    if ((Test-ADTModuleInitialized) -and ($null -ne ($restartOnExitData = (Get-ADTModuleState).RestartOnExitOptions)))
    {
        $icsoParams = @{
            User = [PSADT.AccountManagement.AccountUtilities]::CallerRunAsActiveUser
            SilentRestart = $true
            Options = $restartOnExitData
            NoWait = $true
        }
        Invoke-ADTClientServerOperation @icsoParams
    }

    # Flag the module as uninitialized upon last session closure.
    Reset-ADTModuleState

    # If a callback failed and we're in a proper console, forcibly exit the process.
    # The proper closure of a blocking dialog can stall a traditional exit indefinitely.
    if ($Force -or ($Host.Name.Equals('ConsoleHost') -and ($callbackErrors -or $clientOpen)))
    {
        [System.Environment]::Exit($ExitCode)
    }

    # Forcibly set the LASTEXITCODE so it's available if we're breaking
    # or running Close-ADTSession from a PowerShell runspace, etc.
    [System.Environment]::ExitCode = $ExitCode
    $Global:LASTEXITCODE = $ExitCode

    # If we're not to exit the shell (i.e. we're running from the command line),
    # break instead of exit so the window stays open but an exit is simulated.
    if ($NoShellExit)
    {
        break
    }
    exit $ExitCode
}
