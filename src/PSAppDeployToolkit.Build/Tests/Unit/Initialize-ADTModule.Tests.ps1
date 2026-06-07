BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Initialize-ADTModule' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    AfterAll {
        # Leave the module in a clean initialized state so sibling tests are unaffected.
        Initialize-ADTModule
    }

    Context 'Functionality' {
        It 'Completes without throwing' {
            { Initialize-ADTModule } | Should -Not -Throw
        }

        It 'Sets Test-ADTModuleInitialized to $true after successful call' {
            Initialize-ADTModule
            Test-ADTModuleInitialized | Should -BeTrue
        }

        It 'Is idempotent — calling twice in a row does not throw and leaves module initialized' {
            Initialize-ADTModule
            Initialize-ADTModule
            Test-ADTModuleInitialized | Should -BeTrue
        }

        It 'Produces no pipeline output' {
            $result = Initialize-ADTModule
            $result | Should -BeNullOrEmpty
        }

        It 'Throws InvalidOperationException with ErrorId InitWithActiveSessionError when a session is active' {
            # Mock Test-ADTSessionActive to simulate an active session without opening one.
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $true }
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return [PSCustomObject]@{ SessionState = 'Active' } }
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.InvalidOperationException]
                ErrorId       = 'InitWithActiveSessionError,Initialize-ADTModule'
            }
            { Initialize-ADTModule } | Should @shouldParams
        }

        It 'Populates $Script:ADT.LastExitCode to 0 after initialization' {
            Initialize-ADTModule
            $m = Get-Module PSAppDeployToolkit
            $lastExitCode = & $m { $Script:ADT.LastExitCode }
            $lastExitCode | Should -Be 0
        }

        It 'Sets $Script:ADT.Language to a non-empty string after initialization' {
            Initialize-ADTModule
            $m = Get-Module PSAppDeployToolkit
            $language = & $m { $Script:ADT.Language }
            $language | Should -Not -BeNullOrEmpty
        }

        It 'Records a ModuleInit duration that is non-negative' {
            Initialize-ADTModule
            $m = Get-Module PSAppDeployToolkit
            $duration = & $m { $Script:ADT.Durations.ModuleInit }
            $duration | Should -BeOfType ([System.TimeSpan])
            $duration.TotalMilliseconds | Should -BeGreaterOrEqual 0
        }

        It 'Accepts a valid existing directory via -ScriptDirectory without throwing' {
            $dir = $env:TEMP
            { Initialize-ADTModule -ScriptDirectory $dir } | Should -Not -Throw
            Test-ADTModuleInitialized | Should -BeTrue
        }

        It 'Throws when -ScriptDirectory points to a non-existent path' {
            { Initialize-ADTModule -ScriptDirectory 'C:\ThisPathDoesNotExist_12345' } | Should -Throw
        }
    }

    Context 'Input Validation' {
        It 'ScriptDirectory parameter is not mandatory' {
            $param = (Get-Command Initialize-ADTModule).Parameters['ScriptDirectory']
            $param.Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }

        It 'AdditionalEnvironmentVariables parameter is not mandatory' {
            $param = (Get-Command Initialize-ADTModule).Parameters['AdditionalEnvironmentVariables']
            $param.Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }
    }

    Context 'Metadata' {
        It 'Is exported from the module' {
            (Get-Module PSAppDeployToolkit).ExportedFunctions.Keys | Should -Contain 'Initialize-ADTModule'
        }
    }
}
