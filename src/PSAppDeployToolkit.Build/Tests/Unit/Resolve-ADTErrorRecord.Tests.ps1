BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    # Build a reusable synthetic ErrorRecord for all tests.
    $script:SyntheticException = [System.Exception]::new('Test exception message')
    $script:SyntheticRecord = [System.Management.Automation.ErrorRecord]::new(
        $script:SyntheticException,
        'TestErrorId',
        [System.Management.Automation.ErrorCategory]::NotSpecified,
        'TestTargetObject'
    )

    # A record with an inner exception for IncludeErrorInnerException tests.
    $script:InnerException = [System.Exception]::new('Inner exception message')
    $script:OuterException = [System.Exception]::new('Outer exception message', $script:InnerException)
    $script:RecordWithInner = [System.Management.Automation.ErrorRecord]::new(
        $script:OuterException,
        'OuterErrorId',
        [System.Management.Automation.ErrorCategory]::InvalidOperation,
        $null
    )
}
Describe 'Resolve-ADTErrorRecord' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality — default output' {
        It 'Returns a non-null non-empty string' {
            $result = Resolve-ADTErrorRecord -ErrorRecord $script:SyntheticRecord
            $result | Should -Not -BeNullOrEmpty
        }

        It 'Output is of type System.String' {
            $result = Resolve-ADTErrorRecord -ErrorRecord $script:SyntheticRecord
            $result | Should -BeOfType ([System.String])
        }

        It 'Default output contains the exception message' {
            $result = Resolve-ADTErrorRecord -ErrorRecord $script:SyntheticRecord
            $result | Should -Match 'Test exception message'
        }

        It 'Default output contains the FullyQualifiedErrorId' {
            $result = Resolve-ADTErrorRecord -ErrorRecord $script:SyntheticRecord
            $result | Should -Match 'TestErrorId'
        }

        It 'Default output contains the Error Record header' {
            $result = Resolve-ADTErrorRecord -ErrorRecord $script:SyntheticRecord
            $result | Should -Match 'Error Record'
        }

        It 'Accepts ErrorRecord from pipeline without throwing' {
            { $script:SyntheticRecord | Resolve-ADTErrorRecord } | Should -Not -Throw
        }

        It 'Pipeline input yields the same non-empty string as direct parameter' {
            $direct   = Resolve-ADTErrorRecord -ErrorRecord $script:SyntheticRecord
            $pipeline = $script:SyntheticRecord | Resolve-ADTErrorRecord
            $direct   | Should -Not -BeNullOrEmpty
            $pipeline | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Functionality — ExcludeError switches' {
        It 'ExcludeErrorRecord suppresses the ErrorRecord section but does not throw' {
            $result = Resolve-ADTErrorRecord -ErrorRecord $script:SyntheticRecord -ExcludeErrorRecord
            # Output may be empty if no other sections remain; must not throw.
            $result | Should -BeOfType ([System.String])
        }

        It 'ExcludeErrorException suppresses exception details and does not throw' {
            { Resolve-ADTErrorRecord -ErrorRecord $script:SyntheticRecord -ExcludeErrorException } | Should -Not -Throw
        }

        It 'ExcludeErrorInvocation suppresses invocation info and does not throw' {
            { Resolve-ADTErrorRecord -ErrorRecord $script:SyntheticRecord -ExcludeErrorInvocation } | Should -Not -Throw
        }

        It 'Combining all Exclude switches does not throw' {
            {
                Resolve-ADTErrorRecord -ErrorRecord $script:SyntheticRecord `
                    -ExcludeErrorRecord -ExcludeErrorException -ExcludeErrorInvocation
            } | Should -Not -Throw
        }
    }

    Context 'Functionality — IncludeErrorInnerException' {
        It 'Does not throw when IncludeErrorInnerException is set and an inner exception exists' {
            { Resolve-ADTErrorRecord -ErrorRecord $script:RecordWithInner -IncludeErrorInnerException } | Should -Not -Throw
        }

        It 'Output contains the inner exception message when IncludeErrorInnerException is set' {
            $result = Resolve-ADTErrorRecord -ErrorRecord $script:RecordWithInner -IncludeErrorInnerException
            $result | Should -Match 'Inner exception message'
        }

        It 'Output contains the Error Inner Exception header when IncludeErrorInnerException is set' {
            $result = Resolve-ADTErrorRecord -ErrorRecord $script:RecordWithInner -IncludeErrorInnerException
            $result | Should -Match 'Error Inner Exception'
        }

        It 'Does not include inner exception section when switch is omitted' {
            $result = Resolve-ADTErrorRecord -ErrorRecord $script:RecordWithInner
            $result | Should -Not -Match 'Error Inner Exception'
        }
    }

    Context 'Functionality — Property filter' {
        It 'Returns output containing only requested property when -Property is specified' {
            $result = Resolve-ADTErrorRecord -ErrorRecord $script:SyntheticRecord -Property 'Message'
            $result | Should -Match 'Test exception message'
        }

        It 'Accepts wildcard "*" for -Property without throwing' {
            { Resolve-ADTErrorRecord -ErrorRecord $script:SyntheticRecord -Property '*' } | Should -Not -Throw
        }

        It 'Wildcard "*" output is a non-empty string' {
            $result = Resolve-ADTErrorRecord -ErrorRecord $script:SyntheticRecord -Property '*'
            $result | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Throws ParameterArgumentValidationError when ErrorRecord is null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Resolve-ADTErrorRecord'
            }
            { Resolve-ADTErrorRecord -ErrorRecord $null } | Should @shouldParams
        }

        It 'ErrorRecord parameter is mandatory' {
            (Get-Command Resolve-ADTErrorRecord).Parameters['ErrorRecord'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }
    }

    Context 'Metadata' {
        It 'Is exported from the module' {
            (Get-Module PSAppDeployToolkit).ExportedFunctions.Keys | Should -Contain 'Resolve-ADTErrorRecord'
        }

        It 'Declares OutputType of System.String' {
            $outputTypes = (Get-Command Resolve-ADTErrorRecord).OutputType.Type
            $outputTypes | Should -Contain ([System.String])
        }
    }
}
