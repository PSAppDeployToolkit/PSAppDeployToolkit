#-----------------------------------------------------------------------------
#
# MARK: Exit-ADTInvocation
#
#-----------------------------------------------------------------------------

function Exit-ADTInvocation
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
    $progressOpen = if (Test-ADTInstallationProgressRunning)
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

    # Flag the module as uninitialized upon last session closure.
    $Script:ADT.Initialized = $false

    # If a callback failed and we're in a proper console, forcibly exit the process.
    # The proper closure of a blocking dialog can stall a traditional exit indefinitely.
    if ($Force -or ($Host.Name.Equals('ConsoleHost') -and $progressOpen))
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
