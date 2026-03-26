BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    # Create a global test wrapper inside the module scope so that $PSCmdlet and
    # $ExecutionContext.SessionState come from within the module, satisfying the
    # requirements of Invoke-ADTFunctionErrorHandler.
    InModuleScope PSAppDeployToolkit {
        function global:Invoke-ADTFunctionErrorHandlerTestWrapper
        {
            <#
            .SYNOPSIS
                Test wrapper that calls Invoke-ADTFunctionErrorHandler from within the module scope.
            #>
            [CmdletBinding()]
            param
            (
                [Parameter(Mandatory = $true)]
                [System.Management.Automation.ErrorRecord]$ErrorRecord,

                [Parameter(Mandatory = $false)]
                [System.Management.Automation.ActionPreference]$OriginalErrorActionOverride,

                [Parameter(Mandatory = $false)]
                [System.String]$LogMessage,

                [Parameter(Mandatory = $false)]
                [System.Management.Automation.SwitchParameter]$Silent
            )

            # Set $OriginalErrorAction in this scope so Invoke-ADTFunctionErrorHandler can
            # retrieve it via Get-Variable -Name OriginalErrorAction -Scope 1.
            if ($PSBoundParameters.ContainsKey('OriginalErrorActionOverride'))
            {
                $OriginalErrorAction = $OriginalErrorActionOverride
            }
            else
            {
                $OriginalErrorAction = $ErrorActionPreference
            }

            $handlerParams = @{
                Cmdlet       = $PSCmdlet
                SessionState = $ExecutionContext.SessionState
                ErrorRecord  = $ErrorRecord
            }
            if ($PSBoundParameters.ContainsKey('LogMessage')) { $handlerParams.LogMessage = $LogMessage }
            if ($Silent) { $handlerParams.Silent = $true }

            # DisableErrorResolving avoids calling Resolve-ADTErrorRecord when
            # $OriginalErrorAction is non-SilentlyContinue (which would be the case for
            # Stop tests).  We pass it only when OriginalErrorAction is Stop so that log
            # message tests can still exercise the default code path.
            if ($OriginalErrorAction -eq [System.Management.Automation.ActionPreference]::Stop)
            {
                $handlerParams.DisableErrorResolving = $true
            }

            Invoke-ADTFunctionErrorHandler @handlerParams
        }
    }
}

AfterAll {
    # Remove the global wrapper to avoid polluting other test runs.
    Remove-Item -Path 'Function:\Invoke-ADTFunctionErrorHandlerTestWrapper' -ErrorAction SilentlyContinue
}

Describe 'Invoke-ADTFunctionErrorHandler' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
        # Mock expensive/noisy internal functions used by the error handler.
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
        Mock -ModuleName PSAppDeployToolkit Resolve-ADTErrorRecord { return 'Resolved error details' }
        Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $false }

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestException', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
        $TestException = [System.Exception]::new('Test error message')

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestErrorRecord', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
        $TestErrorRecord = [System.Management.Automation.ErrorRecord]::new(
            $TestException,
            'TestError',
            [System.Management.Automation.ErrorCategory]::NotSpecified,
            $null
        )
    }

    Context 'Error Propagation Behavior' {
        It 'Throws a terminating error when OriginalErrorAction is Stop' {
            { Invoke-ADTFunctionErrorHandlerTestWrapper -ErrorRecord $TestErrorRecord -OriginalErrorActionOverride Stop } | Should -Throw
        }

        It 'Does not throw when OriginalErrorAction is Continue' {
            { Invoke-ADTFunctionErrorHandlerTestWrapper -ErrorRecord $TestErrorRecord -OriginalErrorActionOverride Continue -ErrorAction SilentlyContinue } | Should -Not -Throw
        }

        It 'Does not throw when OriginalErrorAction is SilentlyContinue' {
            { Invoke-ADTFunctionErrorHandlerTestWrapper -ErrorRecord $TestErrorRecord -OriginalErrorActionOverride SilentlyContinue -ErrorAction SilentlyContinue } | Should -Not -Throw
        }

        It 'Writes a non-terminating error when OriginalErrorAction is Continue and no active session' {
            Invoke-ADTFunctionErrorHandlerTestWrapper -ErrorRecord $TestErrorRecord -OriginalErrorActionOverride Continue -ErrorAction SilentlyContinue -ErrorVariable capturedErrors
            $capturedErrors | Should -Not -BeNullOrEmpty
        }

        It 'Rethrows the original exception type when Stop causes a throw' {
            try
            {
                Invoke-ADTFunctionErrorHandlerTestWrapper -ErrorRecord $TestErrorRecord -OriginalErrorActionOverride Stop -ErrorAction Stop
            }
            catch
            {
                $_.Exception | Should -Be $TestException
            }
        }
    }

    Context 'Logging Behavior' {
        It 'Calls Write-ADTLogEntry at least once when handling the error' {
            Invoke-ADTFunctionErrorHandlerTestWrapper -ErrorRecord $TestErrorRecord -OriginalErrorActionOverride SilentlyContinue -ErrorAction SilentlyContinue
            Should -Invoke Write-ADTLogEntry -ModuleName PSAppDeployToolkit -Times 1 -Scope It
        }

        It 'Logs a custom message when -LogMessage is provided' {
            Invoke-ADTFunctionErrorHandlerTestWrapper -ErrorRecord $TestErrorRecord -LogMessage 'Custom log entry' -OriginalErrorActionOverride SilentlyContinue -ErrorAction SilentlyContinue
            Should -Invoke Write-ADTLogEntry -ModuleName PSAppDeployToolkit -ParameterFilter { $Message -match 'Custom log entry' } -Scope It
        }

        It 'Falls back to the ErrorRecord exception message when -LogMessage is omitted' {
            Invoke-ADTFunctionErrorHandlerTestWrapper -ErrorRecord $TestErrorRecord -OriginalErrorActionOverride SilentlyContinue -ErrorAction SilentlyContinue
            Should -Invoke Write-ADTLogEntry -ModuleName PSAppDeployToolkit -ParameterFilter { $Message -match 'Test error message' } -Scope It
        }

        It 'Does not call Write-ADTLogEntry when -Silent is specified' {
            Invoke-ADTFunctionErrorHandlerTestWrapper -ErrorRecord $TestErrorRecord -Silent -OriginalErrorActionOverride SilentlyContinue -ErrorAction SilentlyContinue
            Should -Not -Invoke Write-ADTLogEntry -ModuleName PSAppDeployToolkit -Scope It
        }
    }

    Context 'Activity Rewrite' {
        It 'Replaces a Write-Error Activity annotation with the calling function name' {
            # When Write-Error creates an ErrorRecord the Activity is set to 'Write-Error'.
            # Invoke-ADTFunctionErrorHandler should overwrite it with the actual caller name.
            $writeErrorRecord = [System.Management.Automation.ErrorRecord]::new(
                [System.Exception]::new('Write-Error originated error'),
                'WriteErrorTest',
                [System.Management.Automation.ErrorCategory]::NotSpecified,
                $null
            )
            $writeErrorRecord.CategoryInfo.Activity = 'Write-Error'

            Invoke-ADTFunctionErrorHandlerTestWrapper -ErrorRecord $writeErrorRecord -OriginalErrorActionOverride SilentlyContinue -ErrorAction SilentlyContinue
            $writeErrorRecord.CategoryInfo.Activity | Should -Not -Be 'Write-Error'
        }
    }

    Context 'Input Validation' {
        It 'Throws when -ErrorRecord is null' {
            { Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $null } | Should -Throw
        }

        It 'Throws when -Cmdlet is null' {
            { Invoke-ADTFunctionErrorHandler -Cmdlet $null -SessionState $ExecutionContext.SessionState -ErrorRecord $TestErrorRecord } | Should -Throw
        }
    }
}
