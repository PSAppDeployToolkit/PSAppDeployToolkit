#-----------------------------------------------------------------------------
#
# MARK: New-ADTFunction
#
#-----------------------------------------------------------------------------

function New-ADTFunction
{
    [CmdletBinding()]
    param
    (
    )

    begin
    {
        # Initialise function.
        & $Script:CommandTable.'Initialize-ADTFunction' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
            }
            catch
            {
                # Re-writing the ErrorRecord with Write-Object ensures the correct PositionMessage is used.
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
            }
        }
        catch
        {
            # Process the caught error, log it and throw depending on the specified ErrorAction.
            & $Script:CommandTable.'Invoke-ADTFunctionErrorHandler' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        # Finalise function.
        & $Script:CommandTable.'Complete-ADTFunction' -Cmdlet $PSCmdlet
    }
}
