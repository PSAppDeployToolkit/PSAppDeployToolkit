#-----------------------------------------------------------------------------
#
# MARK: Resolve-ADTBoundParameters
#
#-----------------------------------------------------------------------------

function Resolve-ADTBoundParameters
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

    .NOTES
    This function can be called without an active ADT session.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Exclude', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
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
                if (!(& $Script:CommandTable.'Test-Path' -LiteralPath Microsoft.PowerShell.Core\Variable::paramsArr))
                {
                    $thisFunc = $MyInvocation.MyCommand
                    $paramsArr = [System.Collections.Specialized.StringCollection]::new()
                }

                # Process the piped hashtable.
                foreach ($param in ($InputObject.GetEnumerator() | & { process { if ($Exclude -notcontains $_.Key) { return $_ } } }))
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
                        $null = $paramsArr.Add("-$($param.Key)$(if ($val) {":$val"})")
                    }
                    else
                    {
                        $param.Value | & $thisFunc
                    }
                }

                # Join the array and return as a string to the caller.
                if ((& $Script:CommandTable.'Get-PSCallStack' | & { process { if ($_.Command.Equals($thisFunc.Name)) { return $_.Command } } }) -is [System.String])
                {
                    return ($paramsArr -join ' ')
                }
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
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
