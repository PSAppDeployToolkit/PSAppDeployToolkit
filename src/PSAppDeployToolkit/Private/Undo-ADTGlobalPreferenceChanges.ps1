#-----------------------------------------------------------------------------
#
# MARK: Undo-ADTGlobalPreferenceChanges
#
#-----------------------------------------------------------------------------

function Undo-ADTGlobalPreferenceChanges
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Cmdlet', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
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
