filter Resolve-ADTBoundParameters
{
    <#

    .SYNOPSIS
    Resolve the parameters of a function call to a string.

    .DESCRIPTION
    Resolve the parameters of a function call to a string.

    .PARAMETER InputObject
    The $PSBoundParameters object to process.

    .PARAMETER Exclude
    The names of parameters to exclude from the final result.

    .INPUTS
    System.Collections.Generic.Dictionary[System.String, System.Object]. Resolve-ADTBoundParameters accepts a $PSBoundParameters value via the pipeline for processing.

    .OUTPUTS
    System.String. Resolve-ADTBoundParameters returns a string output of all provided parameters that can be used against powershell.exe or pwsh.exe.

    .EXAMPLE
    $PSBoundParameters | Resolve-ADTBoundParameters

    .EXAMPLE
    Resolve-ADTBoundParameters -InputObject $PSBoundParameters

    .NOTES
    This is an internal script function and should typically not be called directly.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.IDictionary]$InputObject,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Exclude
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                # Establish array to hold return string.
                if (!(Test-Path -LiteralPath 'Variable:paramsArr'))
                {
                    $thisFunc = $MyInvocation.MyCommand
                    $paramsArr = [System.Collections.Generic.List[System.String]]::new()
                }

                # Process the piped hashtable.
                foreach ($param in $InputObject.GetEnumerator().Where({$Exclude -notcontains $_.Key}))
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
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
