BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # A real ErrorRecord, thrown and caught, so it carries an InvocationInfo and a stack trace the way one
    # from a failing command would. A hand-built record has neither.
    function Get-ProbeErrorRecord
    {
        try
        {
            $naerParams = @{
                Exception = [System.InvalidOperationException]::new('the probe exception message', [System.FormatException]::new('the inner exception message'))
                Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                ErrorId = 'ProbeErrorId'
                TargetObject = 'the-target-object'
            }
            throw (New-ADTErrorRecord @naerParams)
        }
        catch
        {
            return $_
        }
    }
}

Describe 'Resolve-ADTErrorRecord' {
    Context 'Functionality' {
        BeforeAll {
            $script:Probe = Get-ProbeErrorRecord
        }

        It 'Returns a string' {
            Resolve-ADTErrorRecord -ErrorRecord $script:Probe | Should -BeOfType ([System.String])
        }

        It 'Includes <Property> by default' -ForEach @(
            @{ Property = 'Message'; Expected = 'the probe exception message' }
            @{ Property = 'FullyQualifiedErrorId'; Expected = 'ProbeErrorId' }
            @{ Property = 'TargetObject'; Expected = 'the-target-object' }
        ) {
            Resolve-ADTErrorRecord -ErrorRecord $script:Probe | Should -BeLike "*$Expected*"
        }

        It 'Includes the position and stack trace by default' {
            $resolved = Resolve-ADTErrorRecord -ErrorRecord $script:Probe
            $resolved | Should -BeLike '*ScriptStackTrace*'
            $resolved | Should -BeLike '*PositionMessage*'
        }

        It 'Restricts itself to the properties asked for' {
            $resolved = Resolve-ADTErrorRecord -ErrorRecord $script:Probe -Property Message
            $resolved | Should -BeLike '*the probe exception message*'
            $resolved | Should -Not -BeLike '*ScriptStackTrace*'
        }

        It 'Accepts a wildcard for every populated property' {
            Resolve-ADTErrorRecord -ErrorRecord $script:Probe -Property '*' | Should -BeLike '*the probe exception message*'
        }

        It 'Drops the invocation details on -ExcludeErrorInvocation' {
            Resolve-ADTErrorRecord -ErrorRecord $script:Probe -ExcludeErrorInvocation | Should -Not -BeLike '*PositionMessage*'
        }

        It 'Drops the record itself on -ExcludeErrorRecord' {
            Resolve-ADTErrorRecord -ErrorRecord $script:Probe -ExcludeErrorRecord | Should -Not -BeLike '*ScriptStackTrace*'
        }

        It 'Drops the exception on -ExcludeErrorException' {
            # The record repeats the exception's message, so the check is on a property only the exception
            # carries rather than on the message text.
            Resolve-ADTErrorRecord -ErrorRecord $script:Probe -Property '*' -ExcludeErrorException | Should -Not -BeLike '*HResult*'
        }

        It 'Reports the inner exception as a property by default' {
            # InnerException is one of the default -Property values, so its type and message are already
            # there without asking for anything extra.
            Resolve-ADTErrorRecord -ErrorRecord $script:Probe | Should -BeLike '*InnerException*the inner exception message*'
        }

        It 'Adds a separate inner exception section on -IncludeErrorInnerException' {
            # The switch is not what makes the inner message visible; it breaks the inner exception out into
            # a section of its own with its properties expanded.
            $resolved = Resolve-ADTErrorRecord -ErrorRecord $script:Probe -IncludeErrorInnerException
            $resolved | Should -BeLike '*Error Inner Exception*'
            $resolved.Length | Should -BeGreaterThan (Resolve-ADTErrorRecord -ErrorRecord $script:Probe).Length
        }

        It 'Leaves that section out by default' {
            Resolve-ADTErrorRecord -ErrorRecord $script:Probe | Should -Not -BeLike '*Error Inner Exception*'
        }

        It 'Accepts an ErrorRecord from the pipeline' {
            $script:Probe | Resolve-ADTErrorRecord | Should -BeLike '*the probe exception message*'
        }
    }
}
