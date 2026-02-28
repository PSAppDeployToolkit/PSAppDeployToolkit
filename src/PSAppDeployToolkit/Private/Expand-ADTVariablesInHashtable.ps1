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
            if ($section.get_Value() -is [System.String])
            {
                $Hashtable.($section.get_Key()) = $SessionState.get_InvokeCommand().ExpandString($section.get_Value())
            }
            elseif ($section.get_Value() -is [System.Collections.Hashtable])
            {
                & $MyInvocation.get_MyCommand() -Hashtable $section.get_Value() -SessionState $SessionState
            }
        }
    }
}
