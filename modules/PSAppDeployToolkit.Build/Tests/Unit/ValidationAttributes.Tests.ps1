BeforeDiscovery {
    Set-Location -Path $PSScriptRoot
    $ModuleName = 'PSAppDeployToolkit'

    # Anchored on $PSScriptRoot because the .NET file APIs below resolve a relative path against the process
    # working directory, which Set-Location does not move.
    $ModuleRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($PSScriptRoot, '..', '..', '..', $ModuleName))
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # What each validator is specified to accept, transcribed from the arguments its subclasses pass to
    # BaseValidateNotEmptyOrWhiteSpaceAttribute. Stated here rather than read back off the attribute so that
    # a disagreement between the two fails, instead of the test agreeing with whatever the code does.
    $validatorPolicy = @{
        ValidateNotNullOrWhiteSpaceAttribute = @{ AllowsNull = $false; AllowsEmpty = $false }
        AllowEmptyButNotNullOrWhiteSpaceAttribute = @{ AllowsNull = $false; AllowsEmpty = $true }
        AllowNullButNotEmptyOrWhiteSpaceAttribute = @{ AllowsNull = $true; AllowsEmpty = $false }
    }

    # Every subclass of the base validator, carrying whether the table above accounts for it. A subclass
    # added later would otherwise drop out of the binding sweep without a word.
    $script:ValidatorTypes = foreach ($type in [PSAppDeployToolkit.Attributes.BaseValidateNotEmptyOrWhiteSpaceAttribute].Assembly.GetTypes())
    {
        if ($type.IsSubclassOf([PSAppDeployToolkit.Attributes.BaseValidateNotEmptyOrWhiteSpaceAttribute]))
        {
            @{ Validator = $type.Name; TypeName = $type.FullName; Mapped = $validatorPolicy.ContainsKey($type.Name) }
        }
    }

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
                            # The declaration verbatim, so a stub can bind exactly what the module declares.
                            Declaration = $parameter.Extent.Text
                        }
                        break
                    }
                }
            }
        }
    )

    # One case per parameter per input worth binding. Only allowEmpty is conditional, and only for scalar
    # text: an empty collection or dictionary is rejected whatever the subclass allows.
    $script:BindingCases = foreach ($validated in $script:ValidatedParameters)
    {
        $policy = $validatorPolicy[$validated.Validator]
        if (!$policy)
        {
            continue
        }
        $whiteSpace = " `f`n`r`t`v"
        $cases = @(
            @{ Case = 'null'; Value = $null; Rejected = !$policy.AllowsNull }
            if ($validated.ParameterType -eq [System.String])
            {
                @{ Case = 'an empty string'; Value = [System.String]::Empty; Rejected = !$policy.AllowsEmpty }
                @{ Case = 'white space'; Value = $whiteSpace; Rejected = $true }
            }
            elseif ($validated.ParameterType.IsArray -and ($validated.ParameterType.GetElementType() -eq [System.String]))
            {
                @{ Case = 'an empty collection'; Value = @(); Rejected = $true }
                @{ Case = 'a collection holding an empty string'; Value = @([System.String]::Empty); Rejected = !$policy.AllowsEmpty }
                @{ Case = 'a collection holding white space'; Value = @($whiteSpace); Rejected = $true }
            }
            elseif ([System.Collections.IDictionary].IsAssignableFrom($validated.ParameterType))
            {
                @{ Case = 'an empty dictionary'; Value = @{}; Rejected = $true }
            }
        )
        foreach ($case in $cases)
        {
            $validated + $case
        }
    }
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

    Context 'Parameter binding' {
        # Each case binds the parameter's own declaration into a stub with no body, so the validator runs
        # against exactly what the module declares while no module code can execute. Binding the real
        # commands would run them for any parameter whose validation failed to fire, which is the one thing
        # this is here to catch.
        BeforeAll {
            function New-ParameterStub
            {
                param
                (
                    [Parameter(Mandatory = $true)]
                    [System.String]$Declaration
                )

                # The trailing string is what a successful bind returns, so success is distinguishable from a
                # stub that quietly produced nothing at all.
                return [System.Management.Automation.ScriptBlock]::Create("param($Declaration)`n'bound'")
            }
        }

        It 'Accounts for the <Validator> subclass' -ForEach $script:ValidatorTypes {
            $Mapped | Should -BeTrue -Because 'each validator needs its accepted input recorded before the cases below can cover it'
        }

        It '<Validator> binds a valid string, so a rejection below means something' -ForEach $script:ValidatorTypes {
            $stub = New-ParameterStub -Declaration "[$TypeName()][System.String]`$Value"
            & $stub -Value 'text' | Should -BeExactly 'bound'
        }

        It '<Validator> rejects <Case> for [<TypeName>] $<Parameter> in <ScriptName>' -ForEach ($script:BindingCases | & { process { if ($_.Rejected) { return $_ } } }) {
            $stub = New-ParameterStub -Declaration $Declaration
            $splat = @{ $Parameter = $Value }
            { & $stub @splat } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException]) -ErrorId 'ParameterArgumentValidationError'
        }

        It '<Validator> accepts <Case> for [<TypeName>] $<Parameter> in <ScriptName>' -ForEach ($script:BindingCases | & { process { if (!$_.Rejected) { return $_ } } }) {
            $stub = New-ParameterStub -Declaration $Declaration
            $splat = @{ $Parameter = $Value }
            & $stub @splat | Should -BeExactly 'bound'
        }
    }
}
