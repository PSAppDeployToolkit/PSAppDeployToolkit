filter Resolve-ADTBoundParameters
{
    <#

    .SYNOPSIS
    Resolve the parameters of a function call to a string.

    .DESCRIPTION
    Resolve the parameters of a function call to a string.

    .PARAMETER Parameter
    The name of the function this function is invoked from.

    .INPUTS
    System.Object

    .OUTPUTS
    System.Object

    .EXAMPLE
    $PSBoundParameters | Resolve-ADTBoundParameters

    .NOTES
    This is an internal script function and should typically not be called directly.

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Exclude
    )

    # Establish array to hold return string.
    if (!(Test-Path -LiteralPath 'Variable:paramsArr'))
    {
        $thisFunc = $MyInvocation.MyCommand
        $paramsArr = [System.Collections.Generic.List[System.String]]::new()
    }

    # Process the piped hashtable.
    foreach ($param in $_.GetEnumerator().Where({$Exclude -notcontains $_.Key}))
    {
        # Recursively expand child hashtables.
        if ($param.Value -isnot [System.Collections.IDictionary])
        {
            # Determine value.
            $val = if ($param.Value -is [System.String])
            {
                "'$($param.Value.Replace("'", "''"))'"
            }
            elseif ($param.Value -is [System.Collections.IEnumerable])
            {
                if ($param.Value[0] -is [System.String])
                {
                    "'$([System.String]::Join("','", $param.Value.Replace("'", "''")))'"
                }
                else
                {
                    [System.String]::Join(',', $param.Value)
                }
            }
            elseif ($param.Value -isnot [System.Management.Automation.SwitchParameter])
            {
                $param.Value
            }
            $paramsArr.Add("-$($param.Key)$(if ($val) {":$val"})")
        }
        else
        {
            $param.Value | & $thisFunc
        }
    }

    # Join the array and return as a string to the caller.
    if ((Get-PSCallStack).Command.Where({$_.Equals($thisFunc.Name)}).Count.Equals(1))
    {
        return [System.String]::Join(' ', $paramsArr)
    }
}
