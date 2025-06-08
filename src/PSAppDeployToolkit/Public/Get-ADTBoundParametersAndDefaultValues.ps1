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

    .PARAMETER CommonParameters
        Specifies whether PowerShell advanced function common parameters should be included.

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

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTBoundParametersAndDefaultValues
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'ParameterSetName', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'HelpMessage', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Exclude', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
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
        [System.String[]]$Exclude,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$CommonParameters
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

                # Inject our already bound parameters into above object.
                if (!$CommonParameters)
                {
                    $Invocation.BoundParameters.GetEnumerator() | & {
                        process
                        {
                            # Filter out common parameters.
                            if ($Script:PowerShellCommonParameters -notcontains $_.Key)
                            {
                                $obj.Add($_.Key, $_.Value)
                            }
                        }
                    }
                }
                else
                {
                    $Invocation.BoundParameters.GetEnumerator() | & {
                        process
                        {
                            $obj.Add($_.Key, $_.Value)
                        }
                    }
                }

                # Build out the dictionary for returning.
                $parameters | & {
                    process
                    {
                        # Filter out excluded values.
                        if ($Exclude -contains $_.Name.VariablePath.UserPath)
                        {
                            $null = $obj.Remove($_.Name.VariablePath.UserPath)
                            return
                        }

                        # Filter out values based on the specified parameter set.
                        if ($ParameterSetName -and !(Test-NamedAttributeArgumentAst -Parameter $_ -Argument ParameterSetName -Value $ParameterSetName))
                        {
                            $null = $obj.Remove($_.Name.VariablePath.UserPath)
                            return
                        }

                        # Filter out values based on the specified help message.
                        if ($HelpMessage -and !(Test-NamedAttributeArgumentAst -Parameter $_ -Argument HelpMessage -Value $HelpMessage))
                        {
                            $null = $obj.Remove($_.Name.VariablePath.UserPath)
                            return
                        }

                        # Filter out parameters already bound.
                        if ($obj.ContainsKey($_.Name.VariablePath.UserPath))
                        {
                            return
                        }

                        # Filter out parameters without a default value.
                        if ($null -eq $_.DefaultValue)
                        {
                            return
                        }

                        # Add the parameter and its value.
                        $obj.Add($_.Name.VariablePath.UserPath, $_.DefaultValue.SafeGetValue())
                    }
                }

                # Return dictionary to the caller, even if it's empty.
                return $obj
            }
            catch
            {
                # Re-writing the ErrorRecord with Write-Error ensures the correct PositionMessage is used.
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
