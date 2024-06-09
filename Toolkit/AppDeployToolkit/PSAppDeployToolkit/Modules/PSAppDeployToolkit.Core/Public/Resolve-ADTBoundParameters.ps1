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

    # Save off the invocation's command.
    $thisFunc = $MyInvocation.MyCommand

    # Process the piped hashtable.
    $_.GetEnumerator().Where({$Exclude -notcontains $_.Key}).ForEach({
        begin {
            # Establish array to hold return string.
            if (!(Test-Path -LiteralPath 'Variable:paramsArr'))
            {
                $paramsArr = [System.Collections.Generic.List[System.String]]::new()
            }
        }
        process {
            # Recursively expand child hashtables.
            if ($_.Value -isnot [System.Collections.IDictionary])
            {
                # Determine value.
                $val = if ($_.Value -is [System.String])
                {
                    "'$($_.Value.Replace("'", "''"))'"
                }
                elseif ($_.Value -is [System.Collections.IEnumerable])
                {
                    if ($_.Value[0] -is [System.String])
                    {
                        "'$([System.String]::Join("','", $_.Value.Replace("'", "''")))'"
                    }
                    else
                    {
                        [System.String]::Join(',', $_.Value)
                    }
                }
                else
                {
                    $_.Value
                }
                $paramsArr.Add("-$($_.Key):$val")
            }
            else
            {
                $_.Value | & $thisFunc
            }
        }
        end {
            # Join the array and return as a string to the caller.
            if ((Get-PSCallStack).Command.Where({$_.Equals($thisFunc.Name)}).Count.Equals(1))
            {
                return [System.String]::Join(' ', $paramsArr)
            }
        }
    })
}
