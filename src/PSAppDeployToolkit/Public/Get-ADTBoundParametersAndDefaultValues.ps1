#---------------------------------------------------------------------------
#
# MARK: Get-ADTBoundParametersAndDefaultValues
#
#---------------------------------------------------------------------------

function Get-ADTBoundParametersAndDefaultValues
{
    <#
    .SYNOPSIS
        Returns a hashtable with the output of $PSBoundParameters and default-valued parameters for the given InvocationInfo.

    .DESCRIPTION
        This function processes the provided InvocationInfo and combines the results of $PSBoundParameters and default-valued parameters via the InvocationInfo's ScriptBlock AST (Abstract Syntax Tree).

    .PARAMETER Invocation
        The script or function's InvocationInfo ($MyInvocation) to process.

    .PARAMETER ParameterSetName
        The ParameterSetName to use as a filter against the Invocation's parameters.

    .PARAMETER HelpMessage
        The HelpMessage field to use as a filter against the Invocation's parameters.

    .PARAMETER Exclude
        One or more parameter names to exclude from the results.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Collections.Generic.Dictionary[System.String, System.Object]

        Get-ADTBoundParametersAndDefaultValues returns a dictionary of the same base type as $PSBoundParameters for API consistency.

    .EXAMPLE
        Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation

        Returns a $PSBoundParameters-compatible dictionary with the bound parameters and any default values.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'ParameterSetName', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'HelpMessage', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Exclude', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
    [CmdletBinding()]
    [OutputType([System.Collections.Generic.Dictionary[System.String, System.Object]])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.InvocationInfo]$Invocation,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ParameterSetName,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$HelpMessage,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Exclude
    )

    begin
    {
        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Internal function for testing parameter attributes.
        function Test-NamedAttributeArgumentAst
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Argument', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
            [CmdletBinding()]
            [OutputType([System.Boolean])]
            param
            (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullOrEmpty()]
                [System.Management.Automation.Language.ParameterAst]$Parameter,

                [Parameter(Mandatory = $true)]
                [ValidateNotNullOrEmpty()]
                [System.String]$Argument,

                [Parameter(Mandatory = $true)]
                [ValidateNotNullOrEmpty()]
                [System.String]$Value
            )

            # Test whether we have AttributeAst objects.
            if (!($attributes = $Parameter.Attributes | & { process { if ($_ -is [System.Management.Automation.Language.AttributeAst]) { return $_ } } }))
            {
                return $false
            }

            # Test whether we have NamedAttributeArgumentAst objects.
            if (!($namedArguments = $attributes.NamedArguments | & { process { if ($_.ArgumentName.Equals($Argument)) { return $_ } } }))
            {
                return $false
            }

            # Test whether any NamedAttributeArgumentAst objects match our value.
            return $namedArguments.Argument.Value.Contains($Value)
        }
    }

    process
    {
        try
        {
            try
            {
                # Get the parameters from the provided invocation. This can vary between simple/advanced functions and scripts.
                $parameters = if ($Invocation.MyCommand.ScriptBlock.Ast -is [System.Management.Automation.Language.FunctionDefinitionAst])
                {
                    # Test whether this is a simple or advanced function.
                    if ($Invocation.MyCommand.ScriptBlock.Ast.Parameters -and $Invocation.MyCommand.ScriptBlock.Ast.Parameters.Count)
                    {
                        $Invocation.MyCommand.ScriptBlock.Ast.Parameters
                    }
                    elseif ($Invocation.MyCommand.ScriptBlock.Ast.Body.ParamBlock -and $Invocation.MyCommand.ScriptBlock.Ast.Body.ParamBlock.Parameters.Count)
                    {
                        $Invocation.MyCommand.ScriptBlock.Ast.Body.ParamBlock.Parameters
                    }
                }
                elseif ($Invocation.MyCommand.ScriptBlock.Ast.ParamBlock -and $Invocation.MyCommand.ScriptBlock.Ast.ParamBlock.Parameters.Count)
                {
                    $Invocation.MyCommand.ScriptBlock.Ast.ParamBlock.Parameters
                }

                # Throw if we don't have any parameters at all.
                if (!$parameters -or !$parameters.Count)
                {
                    $naerParams = @{
                        Exception = [System.InvalidOperationException]::new("Unable to find parameters within the provided invocation's scriptblock AST.")
                        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                        ErrorId = 'InvocationParametersNotFound'
                        TargetObject = $Invocation.MyCommand.ScriptBlock.Ast
                        RecommendedAction = "Please verify your function or script parameter configuration and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }

                # Open dictionary to store all params and their values to return.
                $obj = [System.Collections.Generic.Dictionary[System.String, System.Object]]::new()

                # Build out the dictionary for returning.
                $parameters | & {
                    process
                    {
                        # Filter out excluded values.
                        if ($Exclude -and $Exclude.Contains($_.Name.VariablePath.UserPath))
                        {
                            return
                        }

                        # Filter out values based on the specified parameter set.
                        if ($ParameterSetName -and !(Test-NamedAttributeArgumentAst -Parameter $_ -Argument ParameterSetName -Value $ParameterSetName))
                        {
                            return
                        }

                        # Filter out values based on the specified help message.
                        if ($HelpMessage -and !(Test-NamedAttributeArgumentAst -Parameter $_ -Argument HelpMessage -Value $HelpMessage))
                        {
                            return
                        }

                        # Add the parameter and its value, favouring a bound parameter over a default value.
                        if ($Invocation.BoundParameters.ContainsKey($_.Name.VariablePath.UserPath))
                        {
                            $obj.Add($_.Name.VariablePath.UserPath, $Invocation.BoundParameters.($_.Name.VariablePath.UserPath))
                        }
                        elseif ($_.DefaultValue)
                        {
                            switch ($_)
                            {
                                { $_.DefaultValue -is [System.Management.Automation.Language.HashtableAst] }
                                {
                                    $obj.Add($_.Name.VariablePath.UserPath, $_.DefaultValue.SafeGetValue())
                                }
                                default
                                {
                                    $obj.Add($_.Name.VariablePath.UserPath, $_.DefaultValue.Value)
                                }
                            }
                        }
                    }
                }

                # Return dictionary to the caller, even if it's empty.
                return $obj
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
