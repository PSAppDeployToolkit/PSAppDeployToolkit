function Convert-ADTCommandParamsToDynamicParams
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.CommandInfo]$Command
    )

    # Define parameter dictionary for returning at the end.
    $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()

    # Build out parameters, redefining input into a scriptblock for consistent processing.
    foreach ($parameter in [System.Management.Automation.ScriptBlock]::Create($Command.ScriptBlock).Ast.ParamBlock.Parameters)
    {
        # Build out attributes array for the runtime-defined parameter.
        [System.Collections.ObjectModel.Collection[System.Attribute]]$attributes = foreach ($attribute in $parameter.Attributes.Where({$_ -isnot [System.Management.Automation.Language.TypeConstraintAst]}))
        {
            switch ($attribute.TypeName.FullName)
            {
                Parameter {
                    $obj = @{}; foreach ($argument in $attribute.NamedArguments)
                    {
                        $obj.Add($argument.ArgumentName, (Invoke-Expression -Command $argument.Argument.Extent.Text))
                    }
                    if ($obj.Count)
                    {
                        [System.Management.Automation.ParameterAttribute]$obj
                    }
                    break
                }

                Alias {
                    [System.Management.Automation.AliasAttribute]::new([System.String[]]$attribute.PositionalArguments.Value)
                    break
                }

                AllowNull {
                    [System.Management.Automation.AllowNullAttribute]::new()
                    break
                }

                AllowEmptyString {
                    [System.Management.Automation.AllowEmptyStringAttribute]::new()
                    break
                }

                AllowEmptyCollection {
                    [System.Management.Automation.AllowEmptyCollectionAttribute]::new()
                    break
                }

                ValidateNotNull {
                    [System.Management.Automation.ValidateNotNullAttribute]::new()
                    break
                }

                ValidateNotNullOrEmpty {
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                    break
                }

                ValidateUserDrive {
                    [System.Management.Automation.ValidateUserDriveAttribute]::new()
                    break
                }

                ValidateDrive {
                    [System.Management.Automation.ValidateDriveAttribute]::new([System.String[]]$attribute.PositionalArguments.Value)
                    break
                }

                ValidateSet {
                    [System.Management.Automation.ValidateSetAttribute]::new([System.String[]]$attribute.PositionalArguments.Value)
                    break
                }

                ValidatePattern {
                    [System.Management.Automation.ValidatePatternAttribute]::new($attribute.PositionalArguments.Value)
                    break
                }

                ValidateScript {
                    [System.Management.Automation.ValidateScriptAttribute]::new([System.Management.Automation.ScriptBlock]::Create($attribute.PositionalArguments.ScriptBlock.Extent.Text -replace '(^\{|\}$)'))
                    break
                }

                ValidateCount {
                    $min, $max = $attribute.PositionalArguments.Value
                    [System.Management.Automation.ValidateCountAttribute]::new($min, $max)
                    break
                }

                ValidateLength {
                    $min, $max = $attribute.PositionalArguments.Value
                    [System.Management.Automation.ValidateLengthAttribute]::new($min, $max)
                    break
                }

                ValidateRange {
                    $min, $max = $attribute.PositionalArguments.Value
                    [System.Management.Automation.ValidateRangeAttribute]::new($min, $max)
                    break
                }
            }
        }

        # Add the parameter into the dictionary.
        $paramDictionary.Add($parameter.Name.VariablePath.UserPath, [System.Management.Automation.RuntimeDefinedParameter]::new($parameter.Name.VariablePath.UserPath, $parameter.StaticType, $attributes))
    }

    # Return the dictionary if it contains values.
    if ($paramDictionary.Count)
    {
        return $paramDictionary
    }
}
