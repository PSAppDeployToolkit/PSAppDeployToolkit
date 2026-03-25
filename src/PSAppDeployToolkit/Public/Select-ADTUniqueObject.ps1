#-----------------------------------------------------------------------------
#
# MARK: Select-ADTUniqueObject
#
#-----------------------------------------------------------------------------

function Select-ADTUniqueObject
{
    <#
    .SYNOPSIS
        Provides a consistent way to get unique objects from the given input, with consistent StringComparer equality between Windows PowerShell and PowerShell 7.

    .DESCRIPTION
        This function provides a consistent way to get unique objects from the given input, with consistent StringComparer equality between Windows PowerShell and PowerShell 7.

    .PARAMETER InputObject
        The input to process for uniqueness. Can be pipelined input or an array objects.

    .PARAMETER CaseSensitivity
        The StringComparison value to use when checking for string equality.

    .INPUTS
        System.Object

        One or more objects that will be collected for processing their uniqueness.

    .OUTPUTS
        System.Object[]

        An array of unique objects derived from the provided input.

    .EXAMPLE
        1, 2, 2, 3 | Select-ADTUniqueObject

        Returns a unique array of integers to the caller.

    .EXAMPLE
        'string1', 'string2', 'String2', 'string3' | Select-ADTUniqueObject

        Returns a unique array of integers to the caller, irrespective of case.

    .EXAMPLE
        'string1', 'string2', 'String2', 'string3' | Select-ADTUniqueObject -CaseSensitivity Ordinal

        Returns a unique array of integers to the caller, preserving strings of different casing.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Select-ADTUniqueObject
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [AllowNull()][AllowEmptyCollection()]
        [System.Object[]]$InputObject,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.StringComparison]$CaseSensitivity = [System.StringComparison]::OrdinalIgnoreCase
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $buffer = [System.Collections.Generic.List[System.Object]]::new()
    }

    process
    {
        $InputObject | & {
            process
            {
                if (![System.String]::IsNullOrWhiteSpace(($_ | Out-String)))
                {
                    $buffer.Add($_)
                }
            }
        }
    }

    end
    {
        try
        {
            if (!$buffer.Count)
            {
                return
            }
            if (($bufferTypes = $([System.Linq.Enumerable]::Distinct([System.Type[]]($buffer | & { process { $_.GetType() } })))) -eq [System.String])
            {
                return [System.Linq.Enumerable]::Distinct([System.String[]]$buffer, [System.StringComparer]::$CaseSensitivity)
            }
            if ($bufferTypes -is [System.Type])
            {
                return [System.Linq.Enumerable]::Distinct([System.Management.Automation.LanguagePrimitives]::ConvertTo($buffer, $bufferTypes.MakeArrayType()))
            }
            return [System.Linq.Enumerable]::Distinct($buffer)
        }
        finally
        {
            Complete-ADTFunction -Cmdlet $PSCmdlet
        }
    }
}
