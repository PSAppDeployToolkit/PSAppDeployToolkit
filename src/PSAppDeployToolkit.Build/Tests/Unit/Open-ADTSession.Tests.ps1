BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Open-ADTSession' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Helper to ensure no session leaks between It blocks within this file.
        function Close-AnyActiveADTSession
        {
            while (Test-ADTSessionActive)
            {
                Close-ADTSession -ExitCode 0 -NoShellExit
            }
        }
    }

    Context 'Creating a session (Silent mode, no UI)' {
        AfterEach {
            # Tear down whatever session the It opened so nothing leaks to siblings.
            Close-AnyActiveADTSession
        }

        It 'Creates an active session' {
            $null = Open-ADTSession -DeploymentType Install -DeployMode Silent -AppName 'OpenApp' -AppVendor 'OpenVendor' -AppVersion '1.0.0' -ScriptDirectory $env:TEMP -NoSessionDetection -PassThru
            Test-ADTSessionActive | Should -BeTrue
        }

        It 'Returns a DeploymentSession object when -PassThru is specified' {
            $session = Open-ADTSession -DeploymentType Install -DeployMode Silent -AppName 'OpenApp' -AppVendor 'OpenVendor' -AppVersion '1.0.0' -ScriptDirectory $env:TEMP -NoSessionDetection -PassThru
            $session | Should -BeOfType ([PSAppDeployToolkit.Foundation.DeploymentSession])
        }

        It 'Returns no output when -PassThru is not specified' {
            $result = Open-ADTSession -DeploymentType Install -DeployMode Silent -AppName 'OpenApp' -AppVendor 'OpenVendor' -AppVersion '1.0.0' -ScriptDirectory $env:TEMP -NoSessionDetection
            $result | Should -BeNullOrEmpty
        }

        It 'Populates the session with the supplied App metadata' {
            $session = Open-ADTSession -DeploymentType Install -DeployMode Silent -AppName 'MetaApp' -AppVendor 'MetaVendor' -AppVersion '3.2.1' -ScriptDirectory $env:TEMP -NoSessionDetection -PassThru
            $session.AppName | Should -Be 'MetaApp'
            $session.AppVendor | Should -Be 'MetaVendor'
            $session.AppVersion | Should -Be '3.2.1'
        }

        It 'Records the supplied DeploymentType on the session' {
            $session = Open-ADTSession -DeploymentType Uninstall -DeployMode Silent -AppName 'TypeApp' -AppVendor 'TypeVendor' -AppVersion '1.0.0' -ScriptDirectory $env:TEMP -NoSessionDetection -PassThru
            $session.DeploymentType | Should -Be ([PSAppDeployToolkit.Foundation.DeploymentType]::Uninstall)
        }

        It 'Records the supplied DeployMode on the session' {
            $session = Open-ADTSession -DeploymentType Install -DeployMode Silent -AppName 'ModeApp' -AppVendor 'ModeVendor' -AppVersion '1.0.0' -ScriptDirectory $env:TEMP -NoSessionDetection -PassThru
            $session.DeployMode | Should -Be ([PSAppDeployToolkit.Foundation.DeployMode]::Silent)
        }

        It 'Sets the session InstallPhase to Execution after opening' {
            $session = Open-ADTSession -DeploymentType Install -DeployMode Silent -AppName 'PhaseApp' -AppVendor 'PhaseVendor' -AppVersion '1.0.0' -ScriptDirectory $env:TEMP -NoSessionDetection -PassThru
            $session.InstallPhase | Should -Be 'Execution'
        }

        It 'Generates a default InstallName from vendor, app and version' {
            $session = Open-ADTSession -DeploymentType Install -DeployMode Silent -AppName 'NameApp' -AppVendor 'NameVendor' -AppVersion '4.5.6' -ScriptDirectory $env:TEMP -NoSessionDetection -PassThru
            $session.InstallName | Should -Be 'NameVendor_NameApp_4.5.6'
        }

        It 'Honors an explicit -InstallName override' {
            $session = Open-ADTSession -DeploymentType Install -DeployMode Silent -AppName 'OverrideApp' -AppVendor 'OverrideVendor' -AppVersion '1.0.0' -InstallName 'CustomInstallName' -ScriptDirectory $env:TEMP -NoSessionDetection -PassThru
            $session.InstallName | Should -Be 'CustomInstallName'
        }
    }

    Context 'Input Validation' {
        It 'Throws ParameterArgumentTransformationError when DeploymentType is an invalid enum value' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentTransformationError,Open-ADTSession'
            }
            { Open-ADTSession -DeploymentType 'NotARealType' -NoSessionDetection } | Should @shouldParams
        }

        It 'Throws ParameterArgumentTransformationError when DeployMode is an invalid enum value' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentTransformationError,Open-ADTSession'
            }
            { Open-ADTSession -DeployMode 'NotARealMode' -NoSessionDetection } | Should @shouldParams
        }

        It 'Throws when -ScriptDirectory points to a non-existent path' {
            { Open-ADTSession -DeploymentType Install -DeployMode Silent -AppName 'BadDirApp' -ScriptDirectory 'C:\ThisPathDoesNotExist_PSADT_12345' -NoSessionDetection } | Should -Throw
        }

        It 'AppName parameter is not mandatory' {
            $attrs = (Get-Command Open-ADTSession).Parameters['AppName'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] })
            $attrs.Mandatory | Should -Not -Contain $true
        }
    }

    Context 'Metadata' {
        It 'Declares an OutputType of PSAppDeployToolkit.Foundation.DeploymentSession' {
            $outputTypes = (Get-Command Open-ADTSession).OutputType.Type
            $outputTypes | Should -Contain ([PSAppDeployToolkit.Foundation.DeploymentSession])
        }

        It 'Is exported from the module' {
            (Get-Module PSAppDeployToolkit).ExportedFunctions.Keys | Should -Contain 'Open-ADTSession'
        }
    }
}
