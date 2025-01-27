#-----------------------------------------------------------------------------
#
# MARK: Test-ADTOobeCompleted
#
#-----------------------------------------------------------------------------

function Test-ADTOobeCompleted
{
    <#
    .SYNOPSIS
        Checks if the device's Out-of-Box Experience (OOBE) has completed or not.

    .DESCRIPTION
        This function checks if the current device has completed the Out-of-Box Experience (OOBE).

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Boolean

        Returns $true if the device has proceeded past the OOBE, otherwise $false.

    .EXAMPLE
        Test-ADTOobeCompleted

        Checks if the device has completed the OOBE or not and returns true or false.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
    )

    begin
    {
        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        # Return whether the OOBE is completed via an API call.
        try
        {
            try
            {
                return ([PSADT.Shared.Utility]::IsOOBEComplete())
            }
            catch
            {
                # Re-writing the ErrorRecord with Write-Error ensures the correct PositionMessage is used.
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            # Process the caught error, log it and throw depending on the specified ErrorAction.
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Error determining whether the OOBE has been completed or not."
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
