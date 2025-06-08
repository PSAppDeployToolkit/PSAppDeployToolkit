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
        [System.Int32]$ExitCode,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoShellExit,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Force
    )

    # Attempt to close down any progress dialog here as an additional safety item.
    $progressOpen = if ($Script:ADT.ClientServerProcess -and $Script:ADT.ClientServerProcess.ProgressDialogOpen())
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

    # Attempt to close down any remaining client/server process as an additional safety item.
    $clientOpen = if ($Script:ADT.ClientServerProcess -and $Script:ADT.ClientServerProcess.IsRunning)
    {
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

    # Invoke on-exit callbacks.
    try
    {
        foreach ($callback in $($Script:ADT.Callbacks.([PSADT.Module.CallbackType]::OnExit)))
        {
            & $callback
        }
    }
    catch
    {
        # Do not under any circumstance let a bad callback de-stabilise our exit procedure.
        $Host.UI.WriteErrorLine((Out-String -InputObject $_ -Width ([System.Int32]::MaxValue)))
    }

    # If a callback failed and we're in a proper console, forcibly exit the process.
    # The proper closure of a blocking dialog can stall a traditional exit indefinitely.
    if ($Force -or ($Host.Name.Equals('ConsoleHost') -and ($progressOpen -or $clientOpen)))
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
