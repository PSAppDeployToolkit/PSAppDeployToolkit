BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Set-ADTDeferHistory' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Build a fake session whose SetDeferHistory records the four forwarded arguments.
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'NewRecordingSession', Justification = 'Invoked inside It blocks.')]
        $NewRecordingSession = {
            $record = [PSCustomObject]@{
                Called                   = $false
                DeferTimesRemaining      = 'unset'
                DeferDeadline            = 'unset'
                DeferRunInterval         = 'unset'
                DeferRunIntervalLastTime = 'unset'
            }
            $session = [PSCustomObject]@{ Record = $record }
            $session | Add-Member -MemberType ScriptMethod -Name SetDeferHistory -Value {
                param ($deferTimesRemaining, $deferDeadline, $deferRunInterval, $deferRunIntervalLastTime)
                $this.Record.Called = $true
                $this.Record.DeferTimesRemaining = $deferTimesRemaining
                $this.Record.DeferDeadline = $deferDeadline
                $this.Record.DeferRunInterval = $deferRunInterval
                $this.Record.DeferRunIntervalLastTime = $deferRunIntervalLastTime
            }
            return $session
        }
    }

    Context 'Forwarding behaviour' {
        It 'Forwards DeferTimesRemaining and passes null for unspecified parameters' {
            $session = & $NewRecordingSession
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $session }

            Set-ADTDeferHistory -DeferTimesRemaining 5
            $session.Record.Called | Should -BeTrue
            $session.Record.DeferTimesRemaining | Should -Be 5
            $session.Record.DeferDeadline | Should -BeNullOrEmpty
            $session.Record.DeferRunInterval | Should -BeNullOrEmpty
            $session.Record.DeferRunIntervalLastTime | Should -BeNullOrEmpty
        }

        It 'Forwards DeferDeadline as a DateTime' {
            $session = & $NewRecordingSession
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $session }
            $deadline = [System.DateTime]'2026-06-01T12:00:00'

            Set-ADTDeferHistory -DeferDeadline $deadline
            $session.Record.DeferDeadline | Should -Be $deadline
            $session.Record.DeferDeadline | Should -BeOfType ([System.DateTime])
            $session.Record.DeferTimesRemaining | Should -BeNullOrEmpty
        }

        It 'Forwards DeferRunInterval as a TimeSpan' {
            $session = & $NewRecordingSession
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $session }
            $interval = [System.TimeSpan]::FromHours(4)

            Set-ADTDeferHistory -DeferRunInterval $interval
            $session.Record.DeferRunInterval | Should -Be $interval
            $session.Record.DeferRunInterval | Should -BeOfType ([System.TimeSpan])
        }

        It 'Forwards DeferRunIntervalLastTime as a DateTime' {
            $session = & $NewRecordingSession
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $session }
            $lastTime = [System.DateTime]'2026-06-05T09:30:00'

            Set-ADTDeferHistory -DeferRunIntervalLastTime $lastTime
            $session.Record.DeferRunIntervalLastTime | Should -Be $lastTime
            $session.Record.DeferRunIntervalLastTime | Should -BeOfType ([System.DateTime])
        }

        It 'Forwards every parameter when all are specified' {
            $session = & $NewRecordingSession
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $session }
            $deadline = [System.DateTime]'2026-07-01T00:00:00'
            $interval = [System.TimeSpan]::FromMinutes(90)
            $lastTime = [System.DateTime]'2026-06-30T23:00:00'

            Set-ADTDeferHistory -DeferTimesRemaining 2 -DeferDeadline $deadline -DeferRunInterval $interval -DeferRunIntervalLastTime $lastTime
            $session.Record.DeferTimesRemaining | Should -Be 2
            $session.Record.DeferDeadline | Should -Be $deadline
            $session.Record.DeferRunInterval | Should -Be $interval
            $session.Record.DeferRunIntervalLastTime | Should -Be $lastTime
        }

        It 'Invokes Get-ADTSession exactly once' {
            $session = & $NewRecordingSession
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $session }

            Set-ADTDeferHistory -DeferTimesRemaining 1
            Should -Invoke -ModuleName PSAppDeployToolkit Get-ADTSession -Times 1 -Exactly
        }

        It 'Returns no output' {
            $session = & $NewRecordingSession
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $session }

            $result = Set-ADTDeferHistory -DeferTimesRemaining 1
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Round-trip across set, get and reset' {
        It 'Persists values through Set, reads them back via Get, and clears them via Reset' {
            # Single fake session backed by an in-memory store shared by all three wrappers.
            $store = [PSCustomObject]@{ Times = $null; Deadline = $null; Interval = $null; LastTime = $null }
            $session = [PSCustomObject]@{ Store = $store }
            $session | Add-Member -MemberType ScriptMethod -Name SetDeferHistory -Value {
                param ($deferTimesRemaining, $deferDeadline, $deferRunInterval, $deferRunIntervalLastTime)
                $this.Store.Times = $deferTimesRemaining
                $this.Store.Deadline = $deferDeadline
                $this.Store.Interval = $deferRunInterval
                $this.Store.LastTime = $deferRunIntervalLastTime
            }
            $session | Add-Member -MemberType ScriptMethod -Name GetDeferHistory -Value {
                if ($null -eq $this.Store.Times -and $null -eq $this.Store.Deadline -and $null -eq $this.Store.LastTime)
                {
                    return $null
                }
                return [PSAppDeployToolkit.Foundation.DeferHistory]::new($this.Store.Times, $this.Store.Deadline, $this.Store.LastTime)
            }
            $session | Add-Member -MemberType ScriptMethod -Name ResetDeferHistory -Value {
                $this.Store.Times = $null
                $this.Store.Deadline = $null
                $this.Store.LastTime = $null
            }
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $session }

            $deadline = [System.DateTime]'2026-08-01T00:00:00'
            $lastTime = [System.DateTime]'2026-07-31T22:00:00'

            Set-ADTDeferHistory -DeferTimesRemaining 4 -DeferDeadline $deadline -DeferRunIntervalLastTime $lastTime

            $history = Get-ADTDeferHistory
            $history | Should -BeOfType ([PSAppDeployToolkit.Foundation.DeferHistory])
            $history.DeferTimesRemaining | Should -Be 4
            $history.DeferDeadline | Should -Be $deadline
            $history.DeferRunIntervalLastTime | Should -Be $lastTime

            Reset-ADTDeferHistory
            Get-ADTDeferHistory | Should -BeNullOrEmpty
        }
    }

    Context 'No-parameter guard' {
        It 'Throws SetDeferHistoryNoParamSpecified when called with no defer parameters' {
            $session = & $NewRecordingSession
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $session }

            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.InvalidOperationException]
                ErrorId = 'SetDeferHistoryNoParamSpecified,Set-ADTDeferHistory'
            }
            { Set-ADTDeferHistory } | Should @shouldParams
        }

        It 'Does not invoke the session when no defer parameters are supplied' {
            $session = & $NewRecordingSession
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $session }

            { Set-ADTDeferHistory } | Should -Throw
            $session.Record.Called | Should -BeFalse
        }

        It 'Throws SetDeferHistoryNoParamSpecified when only common parameters are supplied' {
            $session = & $NewRecordingSession
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $session }

            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.InvalidOperationException]
                ErrorId = 'SetDeferHistoryNoParamSpecified,Set-ADTDeferHistory'
            }
            { Set-ADTDeferHistory -Verbose } | Should @shouldParams
        }
    }

    Context 'Input validation' {
        It 'Rejects a non-numeric DeferTimesRemaining with a transformation error' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentTransformationError,Set-ADTDeferHistory'
            }
            { Set-ADTDeferHistory -DeferTimesRemaining 'notanumber' } | Should @shouldParams
        }

        It 'Rejects a non-date DeferDeadline with a transformation error' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentTransformationError,Set-ADTDeferHistory'
            }
            { Set-ADTDeferHistory -DeferDeadline 'notadate' } | Should @shouldParams
        }

        It 'Rejects a non-timespan DeferRunInterval with a transformation error' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentTransformationError,Set-ADTDeferHistory'
            }
            { Set-ADTDeferHistory -DeferRunInterval 'notatimespan' } | Should @shouldParams
        }
    }

    Context 'Error handling' {
        It 'Surfaces a terminating error when the session method throws' {
            $session = [PSCustomObject]@{ }
            $session | Add-Member -MemberType ScriptMethod -Name SetDeferHistory -Value {
                throw [System.InvalidOperationException]::new('SetDeferHistory failed.')
            }
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $session }

            { Set-ADTDeferHistory -DeferTimesRemaining 1 } | Should -Throw -ExpectedMessage '*SetDeferHistory failed.*'
        }

        It 'Surfaces a terminating error when no active session exists' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { throw [System.InvalidOperationException]::new('No active session.') }
            { Set-ADTDeferHistory -DeferTimesRemaining 1 } | Should -Throw -ExpectedMessage 'No active session.'
        }
    }

    Context 'Parameter metadata' {
        It 'Declares <Name> as a non-mandatory parameter' -ForEach @(
            @{ Name = 'DeferTimesRemaining' }
            @{ Name = 'DeferDeadline' }
            @{ Name = 'DeferRunInterval' }
            @{ Name = 'DeferRunIntervalLastTime' }
        ) {
            $attributes = (Get-Command Set-ADTDeferHistory).Parameters[$Name].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] })
            $attributes.Mandatory | Should -Contain $false
            $attributes.Mandatory | Should -Not -Contain $true
        }

        It 'Types <Name> as <Type>' -ForEach @(
            @{ Name = 'DeferTimesRemaining'; Type = [System.Nullable[System.UInt32]] }
            @{ Name = 'DeferDeadline'; Type = [System.DateTime] }
            @{ Name = 'DeferRunInterval'; Type = [System.TimeSpan] }
            @{ Name = 'DeferRunIntervalLastTime'; Type = [System.DateTime] }
        ) {
            (Get-Command Set-ADTDeferHistory).Parameters[$Name].ParameterType | Should -Be $Type
        }
    }
}
