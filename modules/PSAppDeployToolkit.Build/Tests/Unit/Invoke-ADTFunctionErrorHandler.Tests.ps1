BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}
Describe 'Invoke-ADTFunctionErrorHandler' {
    Context 'Functionality' {
        BeforeAll {
            # Mirrors how every module function uses the pair: initialise, do the work, and hand any failure
            # to the handler, which decides whether to rethrow based on what the caller asked for.
            function Test-Probe
            {
                [CmdletBinding()]
                [OutputType([System.String])]
                param
                (
                    [Parameter(Mandatory = $false)]
                    [System.Collections.Hashtable]$HandlerSplat = @{}
                )

                Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
                try
                {
                    throw 'the deliberate failure'
                }
                catch
                {
                    Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ @HandlerSplat
                }
                return 'reached the end'
            }
        }

        It 'Rethrows when the caller asked to stop' {
            { Test-Probe -ErrorAction Stop } | Should -Throw -ExpectedMessage '*the deliberate failure*'
        }

        It 'Lets the caller carry on when they asked to continue' {
            # The caller's own ErrorAction is what decides this, recovered from the OriginalErrorAction that
            # Initialize-ADTFunction archived off.
            Test-Probe -ErrorAction SilentlyContinue -ErrorVariable probeErrors | Should -BeExactly 'reached the end'
        }

        It 'Carries the original failure into the error it writes' {
            $null = Test-Probe -ErrorAction SilentlyContinue -ErrorVariable probeErrors
            $probeErrors[0].Exception.Message | Should -BeLike '*the deliberate failure*'
        }

        It 'Stays quiet with -Silent' {
            { Test-Probe -HandlerSplat @{ Silent = $true } -ErrorAction Stop } | Should -Throw
        }

        It 'Rejects -Silent alongside a log message' {
            # They sit in different parameter sets: a silent handler has nothing to log.
            { Test-Probe -HandlerSplat @{ Silent = $true; LogMessage = 'anything' } } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Rejects two error resolution modes at once' {
            { Test-Probe -HandlerSplat @{ DisableErrorResolving = $true; ResolveErrorProperties = 'Message' } } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Requires an error record to handle' {
            { Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
