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
    An active ADT session is NOT required to use this function.

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
                if (!(Test-Path -LiteralPath Microsoft.PowerShell.Core\Variable::paramsArr))
                {
                    $thisFunc = $MyInvocation.MyCommand
                    $paramsArr = [System.Collections.Specialized.StringCollection]::new()
                }

                # Process the piped hashtable.
                $InputObject.GetEnumerator() | & {
                    process
                    {
                        # Return early if the key is excluded.
                        if ($Exclude -contains $_.Key)
                        {
                            return
                        }

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
                            elseif ($_.Value -isnot [System.Management.Automation.SwitchParameter])
                            {
                                $_.Value
                            }
                            $null = $paramsArr.Add("-$($_.Key)$(if ($val) {":$val"})")
                        }
                        else
                        {
                            $_.Value | & $thisFunc
                        }
                    }
                }

                # Join the array and return as a string to the caller.
                if ((Get-PSCallStack | & { process { if ($_.Command.Equals($thisFunc.Name)) { return $_.Command } } }) -is [System.String])
                {
                    return ($paramsArr -join ' ')
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
