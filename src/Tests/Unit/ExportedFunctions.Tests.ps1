BeforeAll {
    Set-Location -Path $PSScriptRoot
    $ModuleName = 'PSAppDeployToolkit'
    $PathToManifest = [System.IO.Path]::Combine('..', '..', $ModuleName, "$ModuleName.psd1")
    Get-Module $ModuleName -ErrorAction SilentlyContinue | Remove-Module -Force
    Import-Module $PathToManifest -Force
    $manifestContent = Test-ModuleManifest -Path $PathToManifest

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'moduleExported', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
    $moduleExported = Get-Command -Module $ModuleName | Select-Object -ExpandProperty Name
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'manifestExported', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
    $manifestExported = ($manifestContent.ExportedFunctions).Keys
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'typeAcceleratorNames', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
    $typeAcceleratorNames = [System.Management.Automation.PowerShell].Assembly.GetType('System.Management.Automation.TypeAccelerators')::Get.Keys
}
BeforeDiscovery {
    Set-Location -Path $PSScriptRoot
    $ModuleName = 'PSAppDeployToolkit'
    $PathToManifest = [System.IO.Path]::Combine('..', '..', $ModuleName, "$ModuleName.psd1")
    $manifestContent = Test-ModuleManifest -Path $PathToManifest

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'moduleExported', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
    $moduleExported = Get-Command -Module $ModuleName | Select-Object -ExpandProperty Name
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'manifestExported', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
    $manifestExported = ($manifestContent.ExportedFunctions).Keys
}
Describe $ModuleName {
    Context 'Exported Commands' -Fixture {
        Context 'Number of commands' -Fixture {
            It 'Exports the same number of public functions as what is listed in the Module Manifest' {
                ($manifestExported | Measure-Object).Count | Should -BeExactly ($moduleExported | Measure-Object).Count
            }
        }

        Context 'Explicitly exported commands' {
            It 'Includes <_> in the Module Manifest ExportedFunctions' -ForEach $moduleExported {
                $manifestExported -contains $_ | Should -BeTrue
            }
        }
    }

    Context 'Command Help' -Fixture {
        Context '<_>' -ForEach $moduleExported {
            BeforeEach {
                [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'help', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
                $help = Get-Help -Name $_ -Full

                [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'command', Justification = 'This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.')]
                $command = Get-Command -Name $_

                [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'outputTypes', Justification = 'This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.')]
                $outputTypes = $command.OutputType | & { process { return $_.Type } }
            }

            It 'Includes a Synopsis' {
                $help.Synopsis | Should -Not -BeNullOrEmpty
            }
            It 'Includes a Description' {
                $help.description.Text | Should -Not -BeNullOrEmpty
            }
            It 'Includes an Example' {
                $help.examples.example | Should -Not -BeNullOrEmpty
            }
            It 'Includes an Input' {
                $help.inputTypes | Should -Not -BeNullOrEmpty
            }
            It 'Includes an Output' {
                $help.returnValues | Should -Not -BeNullOrEmpty
            }
            It 'All outputs defined in the comment-based help are defined in OutputType attributes' {
                $returnValueTypes = foreach ($returnValue in $help.returnValues.returnValue.Type.Name)
                {
                    $returnValueName = $returnValue.Split([System.Environment]::NewLine)[0]
                    if ($returnValueName -eq 'None')
                    {
                        continue
                    }

                    # All types should be referenced by their full name
                    $returnValueName | Should -Not -BeIn $typeAcceleratorNames -Because 'types should be referenced by their full name in the comment-based help'

                    # Validate that the type specified in the comment-based help is a valid type before we do an early return
                    $returnValueType = [System.Type]$returnValueName

                    # Functions that aren't CmdletBinding cannot have the [OutputType()] attribute
                    if (!$command.CmdletBinding)
                    {
                        continue
                    }

                    $returnValueType | Should -BeIn $outputTypes -Because 'all outputs defined the comment-based help should also be defined in OutputType attributes'

                    # Validate that the generic type arguments are referenced by their full name
                    if ($returnValueType.IsConstructedGenericType)
                    {
                        foreach ($genericArgument in $returnValueType.GenericTypeArguments)
                        {
                            $returnValueName | Should -Match ([System.Text.RegularExpressions.Regex]::Escape($genericArgument.FullName)) -Because 'generic type arguments should be referenced by their full name'
                        }
                    }

                    $returnValueType
                }

                if ($null -eq $returnValueTypes)
                {
                    $outputTypes | Should -BeNullOrEmpty -Because 'all outputs defined in OutputType attributes should be also defined in the comment-based help'
                }

                foreach ($outputType in $outputTypes)
                {
                    $outputType | Should -BeIn $returnValueTypes -Because 'all outputs defined in OutputType attributes should also be defined in the comment-based help'
                }
            }
        }
    }
}

