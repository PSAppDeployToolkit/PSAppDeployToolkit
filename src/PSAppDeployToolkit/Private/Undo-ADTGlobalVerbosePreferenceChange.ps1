#-----------------------------------------------------------------------------
#
# MARK: Undo-ADTGlobalVerbosePreferenceChange
#
#-----------------------------------------------------------------------------

function Undo-ADTGlobalVerbosePreferenceChange
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]$Cmdlet
    )

    # Restore original global verbosity if a value was archived off.
    if ($null -ne ($OriginalVerbosity = $Cmdlet.SessionState.PSVariable.GetValue('OriginalVerbosity', $null)))
    {
        $Global:VerbosePreference = $OriginalVerbosity
    }
}
