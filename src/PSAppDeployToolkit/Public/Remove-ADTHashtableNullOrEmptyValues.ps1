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

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Collections.Hashtable

        Returns a new hashtable with only key/values where the value isn't null.

    .EXAMPLE
        Remove-ADTHashtableNullOrEmptyValues -Hashtable

        Returns a new hashtable with only key/values where the value isn't null.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Remove-ADTHashtableNullOrEmptyValues
    #>

    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Hashtable]$Hashtable
    )

    # Build a new hashtable with only valid values and then return it to the caller.
    $obj = @{}; foreach ($kvp in $Hashtable.GetEnumerator())
    {
        if (![System.String]::IsNullOrWhiteSpace((Out-String -InputObject $kvp.Value)))
        {
            $obj.Add($kvp.Key, $kvp.Value)
        }
    }
    return $obj
}
