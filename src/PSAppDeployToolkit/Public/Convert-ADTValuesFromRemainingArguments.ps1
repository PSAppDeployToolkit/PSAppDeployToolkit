#-----------------------------------------------------------------------------
#
# MARK: Convert-ADTValuesFromRemainingArguments
#
#-----------------------------------------------------------------------------

function Convert-ADTValuesFromRemainingArguments
{
    <#
    .SYNOPSIS
        Converts the collected values from a ValueFromRemainingArguments parameter value into a dictionary or PowerShell.exe command line arguments.

    .DESCRIPTION
        This function converts the collected values from a ValueFromRemainingArguments parameter value into a dictionary or PowerShell.exe command line arguments.

    .PARAMETER RemainingArguments
        The collected values to enumerate and process into a dictionary.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Collections.Generic.Dictionary[System.String, System.Object]

        Convert-ADTValuesFromRemainingArguments returns a dictionary of the processed input.

    .EXAMPLE
        Convert-ADTValuesFromRemainingArguments -RemainingArguments $args

        Converts an $args array into a $PSBoundParameters-compatible dictionary.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Convert-ADTValuesFromRemainingArguments
    #>

    [CmdletBinding()]
    [OutputType([System.Collections.Generic.Dictionary[System.String, System.Object]])]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowNull()][AllowEmptyCollection()]
        [System.Collections.Generic.List[System.Object]]$RemainingArguments
    )

    begin
    {
        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                # Process input into a dictionary and return it. Assume anything starting with a '-' is a new variable.
                return [PSADT.Utilities.PowerShellUtilities]::ConvertValuesFromRemainingArguments($RemainingArguments)
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
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
