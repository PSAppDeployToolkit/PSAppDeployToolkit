BeforeDiscovery {
    Set-Location -Path $PSScriptRoot
    $ModuleName = 'PSAppDeployToolkit'

    # Anchored on $PSScriptRoot because the .NET file APIs below resolve a relative path against the process
    # working directory, which Set-Location does not move.
    $ModuleRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($PSScriptRoot, '..', '..', '..', $ModuleName))
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Every parameter carrying one of the not-empty-or-white-space validators, read from the AST rather than
    # from Get-Command so that private functions and nested param blocks are covered too. The module is
    # imported, so the AST resolves both the validator and the parameter's type against the real assemblies.
    #
    # Held in the script scope so the count guard below can read it at run time; a variable set in
    # BeforeDiscovery is otherwise visible only during discovery. Each entry is a hashtable rather than an
    # object because Pester makes a hashtable's keys available as variables inside the test.
    $script:ValidatedParameters = $(
        $baseValidator = [PSAppDeployToolkit.Attributes.BaseValidateNotEmptyOrWhiteSpaceAttribute]
        foreach ($folder in 'Public', 'Private')
        {
            foreach ($file in [System.IO.Directory]::GetFiles([System.IO.Path]::Combine($ModuleRoot, $folder), '*.ps1'))
            {
                $ast = [System.Management.Automation.Language.Parser]::ParseFile($file, [ref]$null, [ref]$null)
                foreach ($parameter in $ast.FindAll({ $args[0] -is [System.Management.Automation.Language.ParameterAst] }, $true))
                {
                    foreach ($attribute in $parameter.Attributes)
                    {
                        if ($attribute -isnot [System.Management.Automation.Language.AttributeAst])
                        {
                            continue
                        }
                        $attributeType = $attribute.TypeName.GetReflectionAttributeType()
                        if (!$attributeType -or !$attributeType.IsSubclassOf($baseValidator))
                        {
                            continue
                        }
                        @{
                            ScriptName = [System.IO.Path]::GetFileName($file)
                            Parameter = $parameter.Name.VariablePath.UserPath
                            ParameterType = $parameter.StaticType
                            # Named separately because Pester renders a Type inconsistently in a test name,
                            # bare for an accelerator and bracketed for anything else.
                            TypeName = $parameter.StaticType.FullName
                            Validator = $attributeType.Name
                        }
                        break
                    }
                }
            }
        }
    )
}

Describe 'Validation attributes' -Tag Unit {
    Context 'Not-empty-or-white-space validators' {
        It 'Finds parameters carrying a validator to check' -ForEach @{ Found = $script:ValidatedParameters.Count } {
            # Guards the discovery above. A rename, a failed import, or a change to how the attributes are
            # declared would otherwise leave every test below with nothing to iterate and pass silently. The
            # count is handed over as data because a variable set during discovery is not visible at run time.
            $Found | Should -BeGreaterThan 100
        }

        It '<Validator> can act on [<TypeName>] $<Parameter> in <ScriptName>' -ForEach $script:ValidatedParameters {
            # The validators reject null, and reject empty or white-space content for the types that carry
            # text. A non-nullable value type can be none of those, so the attribute does nothing at all -
            # which is how three enum parameters came to carry it unnoticed. Every reference type can at
            # least be null, and a nullable value type can too, so both have something to validate.
            $isNoOp = $ParameterType.IsValueType -and !$ParameterType.IsGenericType
            $isNoOp | Should -BeFalse -Because "[$ParameterType] can be neither null, empty nor white space, so $Validator has no effect on `$$Parameter and should be removed or replaced with a built-in validator"
        }
    }
}
