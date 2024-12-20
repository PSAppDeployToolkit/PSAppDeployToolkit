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

    # Return early if this function was called from the command line.
    if ($NoShellExit -and !$Force)
    {
        $Global:LASTEXITCODE = $ExitCode
        break
    }

    # If a callback failed and we're in a proper console, forcibly exit the process.
    # The proper closure of a blocking dialog can stall a traditional exit indefinitely.
    if ($Force -or ($Host.Name.Equals('ConsoleHost') -and $progressOpen))
    {
        [System.Environment]::Exit($ExitCode)
    }
    exit $ExitCode
}
