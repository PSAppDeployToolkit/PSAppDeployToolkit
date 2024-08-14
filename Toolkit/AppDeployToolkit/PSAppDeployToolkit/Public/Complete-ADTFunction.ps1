#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Complete-ADTFunction
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]$Cmdlet
    )

    # Write debug log messages.
    Write-ADTLogEntry -Message 'Function End' -Source $Cmdlet.MyInvocation.MyCommand.Name -DebugMessage

    # Restore original global verbosity if a value was archived off.
    if ($null -ne ($OriginalVerbosity = $Cmdlet.SessionState.PSVariable.GetValue('OriginalVerbosity', $null)))
    {
        $Global:VerbosePreference = $OriginalVerbosity
    }
}
