BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest
}

Describe 'New-ADTErrorRecord' {
    Context 'Functionality' {
        It 'Returns an ErrorRecord carrying the mandatory values' {
            $exception = [System.InvalidOperationException]::new('something went wrong')
            $record = New-ADTErrorRecord -Exception $exception -Category InvalidOperation

            $record | Should -BeOfType ([System.Management.Automation.ErrorRecord])
            $record.Exception | Should -Be $exception
            $record.CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidOperation)
        }

        It 'Defaults the error id to NotSpecified' {
            $record = New-ADTErrorRecord -Exception ([System.Exception]::new('x')) -Category NotSpecified
            $record.FullyQualifiedErrorId | Should -BeExactly 'NotSpecified'
        }

        It 'Maps -<Parameter> onto <Target>' -ForEach @(
            @{ Parameter = 'ErrorId'; Value = 'MyErrorId'; Target = 'FullyQualifiedErrorId' }
            @{ Parameter = 'Activity'; Value = 'MyActivity'; Target = 'CategoryInfo.Activity' }
            @{ Parameter = 'TargetName'; Value = 'MyTargetName'; Target = 'CategoryInfo.TargetName' }
            @{ Parameter = 'TargetType'; Value = 'MyTargetType'; Target = 'CategoryInfo.TargetType' }
            @{ Parameter = 'Reason'; Value = 'MyReason'; Target = 'CategoryInfo.Reason' }
        ) {
            $splat = @{ Exception = [System.Exception]::new('x'); Category = 'NotSpecified'; $Parameter = $Value }
            $record = New-ADTErrorRecord @splat

            # The target is a dotted path because the values land on CategoryInfo rather than the record.
            $actual = $Target.Split('.') | & { begin { $o = $record } process { $o = $o.$_ } end { $o } }
            $actual | Should -BeExactly $Value
        }

        It 'Carries the target object through untouched' {
            $target = [PSCustomObject]@{ Name = 'target' }
            (New-ADTErrorRecord -Exception ([System.Exception]::new('x')) -Category NotSpecified -TargetObject $target).TargetObject | Should -Be $target
        }

        It 'Accepts a null target object' {
            # [AllowNull()] is on the parameter, and a null target is normal for an error with no subject.
            $record = New-ADTErrorRecord -Exception ([System.Exception]::new('x')) -Category NotSpecified -TargetObject $null
            $record.TargetObject | Should -BeNullOrEmpty
        }

        It 'Puts the recommended action on ErrorDetails without losing the message' {
            $record = New-ADTErrorRecord -Exception ([System.Exception]::new('the message')) -Category NotSpecified -RecommendedAction 'Try again'
            $record.ErrorDetails.RecommendedAction | Should -BeExactly 'Try again'
            $record.ErrorDetails.Message | Should -BeExactly 'the message'
        }

        It 'Leaves ErrorDetails alone when no recommended action is given' {
            (New-ADTErrorRecord -Exception ([System.Exception]::new('x')) -Category NotSpecified).ErrorDetails | Should -BeNullOrEmpty
        }

        It 'Produces a record that can actually be thrown' {
            $record = New-ADTErrorRecord -Exception ([System.InvalidOperationException]::new('thrown')) -Category InvalidOperation -ErrorId 'ThrownId'
            { throw $record } | Should -Throw -ExceptionType ([System.InvalidOperationException]) -ErrorId 'ThrownId'
        }
    }
}
