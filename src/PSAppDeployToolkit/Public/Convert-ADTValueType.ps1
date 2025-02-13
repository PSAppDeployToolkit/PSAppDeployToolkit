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
        This function uses C# code to cast the provided value to the requested type. This avoids errors from PowerShell when values exceed the casted value type's range.

    .PARAMETER Value
        The value to convert.

    .PARAMETER To
        What to cast the value to.

    .INPUTS
        System.Int64

        Convert-ADTValueType will accept any value type as a signed 64-bit integer, then cast to the requested type.

    .OUTPUTS
        System.ValueType

        Convert-ADTValueType will convert the piped input to this type if specified by the caller.

    .EXAMPLE
        Convert-ADTValueType -Value 256 -To SByte

        Invokes the Convert-ADTValueType function and returns the value as a byte, which would equal 0.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Convert-ADTValueType
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Int64]$Value,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [PSADT.Shared.ValueTypes]$To
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
                return [PSADT.Shared.ValueTypeConverter]::$method($Value)
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
