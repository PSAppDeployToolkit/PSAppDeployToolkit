#-----------------------------------------------------------------------------
#
# MARK: Convert-ADTValueType
#
#-----------------------------------------------------------------------------

function Convert-ADTValueType
{
    <#
    .SYNOPSIS
        Casts the provided value to the requested type without range errors.

    .DESCRIPTION
        The `Convert-ADTValueType` function uses C# code to cast the provided value to the requested type. This avoids errors from PowerShell when values exceed the casted value type's range.

    .PARAMETER Value
        The value to convert.

    .PARAMETER To
        What to cast the value to.

    .INPUTS
        System.Int64

        This function accepts any value type as a signed 64-bit integer, then cast to the requested type.

    .OUTPUTS
        System.ValueType

        This function converts the provided input to the type specified in the -To parameter.

    .EXAMPLE
        Convert-ADTValueType -Value 256 -To SByte

        Invokes the `Convert-ADTValueType` function and returns the value as a byte, which would equal 0.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Convert-ADTValueType
    #>

    [CmdletBinding()]
    [OutputType([System.ValueType])]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.Int64]]$Value,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [PSADT.Utilities.ValueTypeConverter+ValueTypes]$To
    )

    begin
    {
        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $method = "To$To"
    }

    process
    {
        try
        {
            try
            {
                # Use our custom converter to get it done.
                return [PSADT.Utilities.ValueTypeConverter]::$method($Value)
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
