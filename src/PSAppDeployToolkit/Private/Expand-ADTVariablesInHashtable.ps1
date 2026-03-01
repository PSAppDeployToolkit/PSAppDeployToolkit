#-----------------------------------------------------------------------------
#
# MARK: Expand-ADTVariablesInHashtable
#
#-----------------------------------------------------------------------------

function Private:Expand-ADTVariablesInHashtable
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Hashtable]$Hashtable,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SessionState]$SessionState
    )

    process
    {
        # Go recursive if we've received a hashtable, otherwise just update the values.
        foreach ($section in $($Hashtable.GetEnumerator()))
        {
            if ($section.Value -is [System.String])
            {
                $Hashtable.($section.Key) = $SessionState.InvokeCommand.ExpandString($section.Value)
            }
            elseif ($section.Value -is [System.Collections.Hashtable])
            {
                & $MyInvocation.MyCommand -Hashtable $section.Value -SessionState $SessionState
            }
        }
    }
}
