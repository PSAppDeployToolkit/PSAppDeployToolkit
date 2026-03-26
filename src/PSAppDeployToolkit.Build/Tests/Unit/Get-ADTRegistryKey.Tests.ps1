BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTRegistryKey' {
    BeforeAll {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestRegistry', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
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

                    if ($property.Value -ne $Right.($property.Name))
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

                    if ($property.Value -ne $Left.($property.Name))
                    {
                        return $false
                    }
                }

                return $true
            }
        }

        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Get-ADTRegistryKey calls Convert-ADTRegistryPath internally, which references
        # [PSADT.AccountManagement.AccountUtilities]::CallerSid at compile time.
        # PowerShell resolves all type literals at compile time, requiring admin rights.
        $script:IsAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    }

    BeforeEach {
        if (!$script:IsAdmin) { Set-ItResult -Skipped -Because 'Requires admin rights (AccountUtilities static constructor triggered at compile time)'; return }
    }

    Context 'Functionality' {
        It 'Should return the same output as Get-ItemProperty' {
            $left = Get-ADTRegistryKey -LiteralPath $TestRegistry
            $right = Get-ItemProperty -LiteralPath $TestRegistry
            Compare-ADTRegistry -Left $left -Right $right | Should -BeTrue
        }
        It 'Should return an empty registry key if it exists' {
            Get-ADTRegistryKey -LiteralPath "$TestRegistry\Empty" | Should -BeNull
            Get-ADTRegistryKey -LiteralPath "$TestRegistry\Empty" -ReturnEmptyKeyIfExists | Should -Not -BeNull
        }
        It 'Should not expand environment variables' {
            Get-ADTRegistryKey -LiteralPath $TestRegistry -Name 'EnvironmentVariable' | Should -Be "$env:WinDir\System32\cmd.exe"
            Get-ADTRegistryKey -LiteralPath $TestRegistry -Name 'EnvironmentVariable' -DoNotExpandEnvironmentNames | Should -Be '%WinDir%\System32\cmd.exe'
        }
        It 'Should return $null when the property does not exist' {
            Get-ADTRegistryKey -LiteralPath $TestRegistry -Name 'DoesNotExist' | Should -BeNull
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
            { Get-ADTRegistryKey -Path " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should verify that LiteralPath is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Get-ADTRegistryKey'
            }
            { Get-ADTRegistryKey -LiteralPath $null } | Should @shouldParams
            { Get-ADTRegistryKey -LiteralPath '' } | Should @shouldParams
            { Get-ADTRegistryKey -LiteralPath " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should verify that Name is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Get-ADTRegistryKey'
            }
            { Get-ADTRegistryKey -Path $TestRegistry -Name $null } | Should @shouldParams
            { Get-ADTRegistryKey -Path $TestRegistry -Name '' } | Should @shouldParams
            { Get-ADTRegistryKey -Path $TestRegistry -Name " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should verify that SID is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Get-ADTRegistryKey -LiteralPath $TestRegistry -SID $null } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Get-ADTRegistryKey'
            { Get-ADTRegistryKey -LiteralPath $TestRegistry -SID '' } | Should @shouldParams -ErrorId 'ParameterArgumentTransformationError,Get-ADTRegistryKey'
            { Get-ADTRegistryKey -LiteralPath $TestRegistry -SID " `f`n`r`t`v" } | Should @shouldParams -ErrorId 'ParameterArgumentTransformationError,Get-ADTRegistryKey'
        }
    }
}
