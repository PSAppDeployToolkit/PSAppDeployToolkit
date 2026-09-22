function ConvertTo-FluenceButtonList
{
    <#
    .SYNOPSIS
        Normalizes a mixed array of strings and Fluence.Button objects into a Fluence.Button list.
    .DESCRIPTION
        Strings are converted via New-FluenceButton -Text. Fluence.Button objects pass through
        unchanged. Any other type raises a terminating error naming the bad item.
    .PARAMETER InputObject
        An array of strings or Fluence.Button objects.
    .OUTPUTS
        Fluence.Button
    .NOTES
        Private helper. Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType('Fluence.Button')]
    param
    (
        [Parameter(Mandatory = $true)]
        [object[]]$InputObject
    )

    $result = @()
    foreach ($item in $InputObject)
    {
        if ($item -is [string])
        {
            if ($item -eq 'Cancel')
            {
                $result += New-FluenceButton -Text $item -IsCancel
            }
            else
            {
                $result += New-FluenceButton -Text $item
            }
        }
        elseif ($item.PSObject.TypeNames -contains 'Fluence.Button')
        {
            $result += $item
        }
        elseif ($null -eq $item)
        {
            # Without this branch the null falls through to GetType() in the throw below and the
            # caller sees 'You cannot call a method on a null-valued expression' instead of the
            # diagnostic that names the parameter.
            throw "ConvertTo-FluenceButtonList: a $null item is not a valid button specification."
        }
        else
        {
            throw "ConvertTo-FluenceButtonList: unsupported item type '$($item.GetType().FullName)'. Expected [string] or [Fluence.Button]."
        }
    }
    return $result
}
