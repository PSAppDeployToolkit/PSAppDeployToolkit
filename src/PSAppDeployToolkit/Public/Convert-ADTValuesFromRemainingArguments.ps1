#-----------------------------------------------------------------------------
#
# MARK: Convert-ADTValuesFromRemainingArguments
#
#-----------------------------------------------------------------------------

function Convert-ADTValuesFromRemainingArguments
{
    <#
    .SYNOPSIS
        Converts the collected values from a ValueFromRemainingArguments parameter value into a dictionary or PowerShell.exe command line arguments.

    .DESCRIPTION
        This function converts the collected values from a ValueFromRemainingArguments parameter value into a dictionary or PowerShell.exe command line arguments.

    .PARAMETER RemainingArguments
        The collected values to enumerate and process into a dictionary.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Collections.Generic.Dictionary[System.String, System.Object]

        Convert-ADTValuesFromRemainingArguments returns a dictionary of the processed input.

    .EXAMPLE
        Convert-ADTValuesFromRemainingArguments -RemainingArguments $args

        Converts an $args array into a $PSBoundParameters-compatible dictionary.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
    [CmdletBinding()]
    [OutputType([System.Collections.Generic.Dictionary[System.String, System.Object]])]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowNull()][AllowEmptyCollection()]
        [System.Collections.Generic.List[System.Object]]$RemainingArguments
    )

    begin
    {
        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                # Open dictionary to hold all values, using same base type as $PSBoundParameters.
                $boundParams = [System.Collections.Generic.Dictionary[System.String, System.Object]]::new()

                # Process input into a dictionary and return it. Assume anything starting with a '-' is a new variable.
                try
                {
                    $RemainingArguments | & {
                        process
                        {
                            if ($null -eq $_)
                            {
                                return
                            }
                            if (($_ -is [System.String]) -and ($_ -match '^-'))
                            {
                                $boundParams.Add(($thisVar = $_ -replace '(^-|:$)'), [System.Management.Automation.SwitchParameter]$true)
                            }
                            else
                            {
                                $boundParams.$thisVar = $_
                            }
                        }
                    }
                }
                catch
                {
                    $naerParams = @{
                        Exception = [System.FormatException]::new("The parser was unable to process the provided arguments.", $_.Exception)
                        Category = [System.Management.Automation.ErrorCategory]::InvalidData
                        ErrorId = 'ArgumentsMalformedException'
                        TargetObject = $RemainingArguments
                        RecommendedAction = "Please ensure that only PowerShell-style arguments are provided and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }

                # Return dictionary, even if its empty to match $PSBoundParameters API.
                return $boundParams
            }
            catch
            {
                # Re-writing the ErrorRecord with Write-Object ensures the correct PositionMessage is used.
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            # Process the caught error, log it and throw depending on the specified ErrorAction.
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
