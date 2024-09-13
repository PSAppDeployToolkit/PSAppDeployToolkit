#-----------------------------------------------------------------------------
#
# MARK: Undo-ADTGlobalPreferenceChanges
#
#-----------------------------------------------------------------------------

function Undo-ADTGlobalPreferenceChanges
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]$Cmdlet
    )

    # Process all potential values that we amend.
    'InformationPreference', 'VerbosePreference' | & {
        process
        {
            if ($null -ne ($original = $Cmdlet.SessionState.PSVariable.GetValue("Original$_", $null)))
            {
                Set-Variable -Name $_ -Value $original -Scope Global
            }
        }
    }
}
