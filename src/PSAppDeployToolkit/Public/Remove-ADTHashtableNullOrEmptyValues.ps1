#-----------------------------------------------------------------------------
#
# MARK: Remove-ADTHashtableNullOrEmptyValues
#
#-----------------------------------------------------------------------------

function Remove-ADTHashtableNullOrEmptyValues
{
    <#
    .SYNOPSIS
        Removes any key/value pairs from the supplied hashtable where the value is null.

    .DESCRIPTION
        This function removes any key/value pairs from the supplied hashtable where the value is null.

    .PARAMETER Hashtable
        The hashtable to remove null values from.

    .PARAMETER Recurse
        Specifies to recursively remove nested hashtable values that are null, empty, or whitespace.

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

    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Hashtable]$Hashtable,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Recurse
    )

    process
    {
        # Build a new hashtable with only valid values and then return it to the caller.
        $obj = @{}; foreach ($section in $Hashtable.GetEnumerator())
        {
            # Recursively remove null/empty/whitespace keys from the bottom up, if the Recurse parameter is provided.
            if ($section.Value -is [System.Collections.Hashtable] -and $Recurse)
            {
                $section.Value = & $MyInvocation.MyCommand -Hashtable $section.Value -Recurse:$Recurse
            }
            if (![System.String]::IsNullOrWhiteSpace((Out-String -InputObject $section.Value)))
            {
                $obj.Add($section.Key, $section.Value)
            }
        }
        return $obj
    }
}
