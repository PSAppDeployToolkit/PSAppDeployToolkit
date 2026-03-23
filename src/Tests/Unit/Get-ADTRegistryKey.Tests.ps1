BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTRegistryKey' {
    BeforeAll {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestRegistry', Justification = 'This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.')]
        $TestRegistry = (New-Item -Path 'TestRegistry:\TestLocation' -ItemType Directory).PSPath
        New-Item -Path "$TestRegistry\Empty" -ItemType Container
        New-ItemProperty -LiteralPath $TestRegistry -Name 'EnvironmentVariable' -Value '%WinDir%\System32\cmd.exe' -PropertyType ExpandString | Out-Null

        function Compare-ADTRegistry
        {
            [CmdletBinding()]
            [OutputType([System.Boolean])]
            param
            (
                [Parameter(Mandatory = $true, ValueFromPipeline = $true, Position = 0)]
                [ValidateNotNullOrEmpty()]
                $Left,

                [Parameter(Mandatory = $true, Position = 1)]
                [ValidateNotNullOrEmpty()]
                $Right
            )

            process
            {
                foreach ($property in $Left.PSObject.Properties)
                {
                    if ($property.MemberType -ne [System.Management.Automation.PSMemberTypes]::NoteProperty)
                    {
                        continue
                    }

                    if (-not $Right.PSObject.Properties.Name.Contains($property.Name))
                    {
                        return $false
                    }

                    if ($property.Value -ne $Right.$($property.Name))
                    {
                        return $false
                    }
                }

                foreach ($property in $Right.PSObject.Properties)
                {
                    if ($property.MemberType -ne [System.Management.Automation.PSMemberTypes]::NoteProperty)
                    {
                        continue
                    }

                    if (-not $Left.PSObject.Properties.Name.Contains($property.Name))
                    {
                        return $false
                    }

                    if ($property.Value -ne $Left.$($property.Name))
                    {
                        return $false
                    }
                }

                return $true
            }
        }

        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should return the same output as Get-ItemProperty' {
            $left = Get-ADTRegistrykey -LiteralPath $TestRegistry
            $right = Get-ItemProperty -LiteralPath $TestRegistry
            Compare-ADTRegistry -Left $left -Right $right | Should -BeTrue
        }
        It 'Should return an empty registry key if it exists' {
            Get-ADTRegistryKey -Key "$TestRegistry\Empty" | Should -BeNull
            Get-ADTRegistryKey -Key "$TestRegistry\Empty" -ReturnEmptyKeyIfExists | Should -Not -BeNull
        }
        It 'Should not expand environment variables' {
            Get-ADTRegistryKey -Key $TestRegistry -Name 'EnvironmentVariable' | Should -Be "$env:WinDir\System32\cmd.exe"
            Get-ADTRegistryKey -Key $TestRegistry -Name 'EnvironmentVariable' -DoNotExpandEnvironmentNames | Should -Be '%WinDir%\System32\cmd.exe'
        }
        It 'Should return $null when the property does not exist' {
            Get-ADTRegistrykey -LiteralPath $TestRegistry -Name 'DoesNotExist' | Should -BeNull
        }
    }

    Context 'Input Validation' {
        It 'Should verify that Path is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Get-ADTRegistryKey'
            }
            { Get-ADTRegistryKey -Path $null } | Should @shouldParams
            { Get-ADTRegistryKey -Path '' } | Should @shouldParams
            { Get-ADTRegistryKey -Path ' ' } | Should @shouldParams
        }
        It 'Should verify that LiteralPath is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Get-ADTRegistryKey'
            }
            { Get-ADTRegistryKey -LiteralPath $TestRegistry -Name $null } | Should @shouldParams
            { Get-ADTRegistryKey -LiteralPath $TestRegistry -Name '' } | Should @shouldParams
            { Get-ADTRegistryKey -LiteralPath $TestRegistry -Name ' ' } | Should @shouldParams
        }
        It 'Should verify that Name is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Get-ADTRegistryKey'
            }
            { Get-ADTRegistryKey -Path $TestRegistry -Name $null } | Should @shouldParams
            { Get-ADTRegistryKey -Path $TestRegistry -Name '' } | Should @shouldParams
            { Get-ADTRegistryKey -Path $TestRegistry -Name ' ' } | Should @shouldParams
        }
        It 'Should verify that SID is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Get-ADTRegistryKey -LiteralPath $TestRegistry -SID $null } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Get-ADTRegistryKey'
            { Get-ADTRegistryKey -LiteralPath $TestRegistry -SID '' } | Should @shouldParams -ErrorId 'ParameterArgumentTransformationError,Get-ADTRegistryKey'
            { Get-ADTRegistryKey -LiteralPath $TestRegistry -SID ' ' } | Should @shouldParams -ErrorId 'ParameterArgumentTransformationError,Get-ADTRegistryKey'
        }
    }
}
