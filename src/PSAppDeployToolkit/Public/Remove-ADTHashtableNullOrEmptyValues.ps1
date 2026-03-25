#-----------------------------------------------------------------------------
#
# MARK: Remove-ADTHashtableNullOrEmptyValues
#
#-----------------------------------------------------------------------------

function Remove-ADTHashtableNullOrEmptyValues
{
    <#
    .SYNOPSIS
        Returns a new hashtable that contains entries from the source where the value is not null, empty, or consists only of whitespace.

    .DESCRIPTION
        This function returns a new hashtable that contains entries from the source where the value is not null, empty, or consists only of whitespace.. When the **-Recurse** switch is specified, the function will also traverse any nested hashtables and apply the same filtering to them.

    .PARAMETER Hashtable
        The hashtable to remove null values from.

    .PARAMETER Recurse
        Specifies to recursively remove nested hashtable values that are null, empty, or whitespace.

    .PARAMETER Depth
        Specifies how many recursive levels to remove null, empty, or whitespace values. The default value is 5.

    .INPUTS
        System.Collections.Hashtable

        The hashtable to remove null or empty entries from.

    .OUTPUTS
        System.Collections.Hashtable

        Returns a new hashtable with only key/values where the value isn't null.

    .EXAMPLE
        Remove-ADTHashtableNullOrEmptyValues -Hashtable @{ Key1 = 'Value1'; Key2 = $null }

        Returns a new hashtable with only key/values where the value isn't null.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Remove-ADTHashtableNullOrEmptyValues
    #>

    [CmdletBinding(DefaultParameterSetname = 'Default')]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, Position = 0)]
        [AllowEmptyCollection()]
        [System.Collections.Hashtable]$Hashtable,

        [Parameter(Mandatory = $true, ParameterSetName = 'Recurse')]
        [System.Management.Automation.SwitchParameter]$Recurse,

        [Parameter(Mandatory = $false, ParameterSetName = 'Recurse')]
        [System.UInt32]$Depth = 5
    )

    process
    {
        # Build a new hashtable with only valid values and then return it to the caller.
        $obj = @{}; foreach ($section in $Hashtable.GetEnumerator())
        {
            # Recursively remove null/empty/whitespace keys from the bottom up, if the Recurse parameter is provided.
            if (($section.Value -is [System.Collections.Hashtable]) -and $Recurse -and ($Depth -gt 1))
            {
                $section.Value = & $MyInvocation.MyCommand -Hashtable $section.Value -Recurse -Depth ($Depth - 1)
            }
            if (![System.String]::IsNullOrWhiteSpace((Out-String -InputObject $section.Value)))
            {
                $obj.Add($section.Key, $section.Value)
            }
        }
        return $obj
    }
}
