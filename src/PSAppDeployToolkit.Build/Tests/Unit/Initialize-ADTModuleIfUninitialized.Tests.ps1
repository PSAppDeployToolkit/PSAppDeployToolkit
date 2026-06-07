BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    # Helper: wraps Initialize-ADTModuleIfUninitialized so $PSCmdlet is populated.
    function Invoke-InitIfUninitialized
    {
        [CmdletBinding()]
        param
        (
            [System.Management.Automation.SwitchParameter]$PassThruActiveSession
        )

        if ($PassThruActiveSession)
        {
            Initialize-ADTModuleIfUninitialized -Cmdlet $PSCmdlet -PassThruActiveSession
        }
        else
        {
            Initialize-ADTModuleIfUninitialized -Cmdlet $PSCmdlet
        }
    }
}
Describe 'Initialize-ADTModuleIfUninitialized' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    AfterAll {
        # Restore initialized state after the suite so sibling test files are unaffected.
        Initialize-ADTModule
    }

    Context 'Functionality — module uninitialized at entry' {
        BeforeEach {
            # Drive the module back to an uninitialized state without unloading it.
            $m = Get-Module PSAppDeployToolkit
            & $m { $Script:ADT.Initialized = $false }
        }

        AfterEach {
            # Re-initialize so subsequent tests and AfterAll find a clean slate.
            Initialize-ADTModule -ErrorAction SilentlyContinue
        }

        It 'Initializes the module when it was uninitialized (Test-ADTModuleInitialized becomes true)' {
            Invoke-InitIfUninitialized
            Test-ADTModuleInitialized | Should -BeTrue
        }

        It 'Produces no output when called without -PassThruActiveSession' {
            $result = Invoke-InitIfUninitialized
            $result | Should -BeNullOrEmpty
        }

        It 'Returns $null when -PassThruActiveSession is set but no session is active' {
            # No active session — PassThruActiveSession returns nothing.
            $result = Invoke-InitIfUninitialized -PassThruActiveSession
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Idempotency — module already initialized' {
        BeforeEach {
            # Guarantee the module IS initialized before each idempotency test.
            Initialize-ADTModule
        }

        It 'Does not throw when the module is already initialized' {
            { Invoke-InitIfUninitialized } | Should -Not -Throw
        }

        It 'Leaves the module initialized when already initialized (idempotent)' {
            Invoke-InitIfUninitialized
            Test-ADTModuleInitialized | Should -BeTrue
        }

        It 'Still produces no output when already initialized' {
            $result = Invoke-InitIfUninitialized
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Cmdlet parameter is mandatory' {
            (Get-Command Initialize-ADTModuleIfUninitialized).Parameters['Cmdlet'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'ScriptDirectory parameter is not mandatory' {
            (Get-Command Initialize-ADTModuleIfUninitialized).Parameters['ScriptDirectory'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }

        It 'PassThruActiveSession parameter is not mandatory' {
            (Get-Command Initialize-ADTModuleIfUninitialized).Parameters['PassThruActiveSession'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }

        It 'Throws ParameterArgumentValidationError when Cmdlet is null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Initialize-ADTModuleIfUninitialized'
            }
            { Initialize-ADTModuleIfUninitialized -Cmdlet $null } | Should @shouldParams
        }
    }

    Context 'Metadata' {
        It 'Is exported from the module' {
            (Get-Module PSAppDeployToolkit).ExportedFunctions.Keys | Should -Contain 'Initialize-ADTModuleIfUninitialized'
        }

        It 'Declares OutputType of PSAppDeployToolkit.Foundation.DeploymentSession' {
            $outputTypes = (Get-Command Initialize-ADTModuleIfUninitialized).OutputType.Type
            $outputTypes | Should -Contain ([PSAppDeployToolkit.Foundation.DeploymentSession])
        }
    }
}
