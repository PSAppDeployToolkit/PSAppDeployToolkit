﻿#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Remove-ADTSessionFinishingCallback
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.CommandInfo[]]$Callback
    )

    # Send it off to the backend function.
    try
    {
        Invoke-ADTSessionCallbackOperation -Type Finishing -Action Remove @PSBoundParameters
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}