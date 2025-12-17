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
    $callbackErrors = foreach ($callback in $($Script:ADT.Callbacks.([PSAppDeployToolkit.Common.CallbackType]::OnExit)))
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
    $clientOpen = if ($Script:ADT.ClientServerProcess)
    {
        if ($Script:ADT.ClientServerProcess.ProgressDialogOpen())
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
        try
        {
            Close-ADTClientServerProcess
        }
        catch
        {
            $_
        }
    }

    # Flag the module as uninitialized upon last session closure.
    $Script:ADT.Initialized = $false

    # Invoke a silent restart on the device if specified.
    if ($null -ne $Script:ADT.RestartOnExitCountdown)
    {
        Invoke-ADTSilentRestart -Delay $Script:ADT.RestartOnExitCountdown
    }

    # If a callback failed and we're in a proper console, forcibly exit the process.
    # The proper closure of a blocking dialog can stall a traditional exit indefinitely.
    if ($Force -or ($Host.Name.Equals('ConsoleHost') -and ($callbackErrors -or $clientOpen)))
    {
        [System.Environment]::Exit($ExitCode)
    }

    # Forcibly set the LASTEXITCODE so it's available if we're breaking
    # or running Close-ADTSession from a PowerShell runspace, etc.
    $Global:LASTEXITCODE = $ExitCode

    # If we're not to exit the shell (i.e. we're running from the command line),
    # break instead of exit so the window stays open but an exit is simulated.
    if ($NoShellExit)
    {
        break
    }
    exit $ExitCode
}
