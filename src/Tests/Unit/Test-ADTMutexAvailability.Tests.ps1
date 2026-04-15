
BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Test-ADTMutexAvailability' {
    Context 'Functionality' {
        It 'Should return $true when the mutex is not locked' {
            $mutexName = $null
            $maxAttempts = 100
            $attempt = 0
            $mutex = $null
            $isMutexLocked = $false
            while ($attempt -lt $maxAttempts)
            {
                $attempt++
                $mutexName = "Global\PSADT_Pester_$([System.Guid]::NewGuid().Guid)"
                try
                {
                    $mutex = [System.Threading.Mutex]::new($false, $mutexName)
                    if ($isMutexLocked = $mutex.WaitOne(1))
                    {
                        break
                    }
                }
                finally
                {
                    if ($null -ne $mutex)
                    {
                        if ($isMutexLocked)
                        {
                            $mutex.ReleaseMutex()
                        }
                        $mutex.Dispose()
                        $mutex = $null
                    }
                }
            }
            if (-not $isMutexLocked)
            {
                throw "Failed to acquire an unlocked mutex after $maxAttempts attempts."
            }

            Test-ADTMutexAvailability -MutexName $mutexName | Should -BeTrue
        }
        It 'Should return $false when the mutex is locked' {
            # Named mutex ownership in .NET is thread-affine: WaitOne() on the same thread that
            # already holds the mutex is re-entrant and always returns $true. We therefore acquire
            # the mutex on a background thread (a dedicated PowerShell instance running via
            # BeginInvoke on a thread pool thread) so that the test thread's WaitOne() call
            # correctly sees the mutex as unavailable.
            $mutexName = "Global\PSADT_Pester_$([System.Guid]::NewGuid().Guid)"
            $mutexHoldTimeout = 30000
            $mutexAcquireTimeoutMs = 5000
            $mutexAcquired = [System.Threading.ManualResetEventSlim]::new($false)
            $cts = [System.Threading.CancellationTokenSource]::new()
            $ps = [System.Management.Automation.PowerShell]::Create()
            ($ps.Runspace = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()).Open()
            $ps.Runspace.SessionStateProxy.SetVariable('mutexName', $mutexName)
            $ps.Runspace.SessionStateProxy.SetVariable('mutexAcquired', $mutexAcquired)
            $ps.Runspace.SessionStateProxy.SetVariable('cts', $cts)
            $ps.Runspace.SessionStateProxy.SetVariable('mutexHoldTimeout', $mutexHoldTimeout)
            $asyncResult = $ps.AddScript({
                    try
                    {
                        $mutex = [System.Threading.Mutex]::new($false, $mutexName)
                        if ($mutex.WaitOne(1))
                        {
                            $mutexAcquired.Set()
                            [void]$cts.Token.WaitHandle.WaitOne($mutexHoldTimeout)
                            $mutex.ReleaseMutex()
                        }
                    }
                    finally
                    {
                        if ($null -ne $mutex)
                        {
                            $mutex.Dispose()
                        }
                    }
                }).BeginInvoke()
            if (-not $mutexAcquired.Wait($mutexAcquireTimeoutMs))
            {
                throw "Background PowerShell instance failed to acquire a lock on the mutex [$mutexName]."
            }
            try
            {
                Test-ADTMutexAvailability -MutexName $mutexName | Should -BeFalse
            }
            finally
            {
                $cts.Cancel()
                $null = $ps.EndInvoke($asyncResult)
                $ps.Runspace.Dispose()
                $ps.Dispose()
                $cts.Dispose()
                $mutexAcquired.Dispose()
            }
        }
        It 'Should return $true when the mutex does not exist' {
            $mutexName = $null
            $maxAttempts = 100
            $attempt = 0
            $foundNonExistentMutex = $false
            $mutex = $null
            while ($attempt -lt $maxAttempts)
            {
                $attempt++
                try
                {
                    $mutexName = "Global\PSADT_Pester_$([System.Guid]::NewGuid().Guid)"
                    $mutex = [System.Threading.Mutex]::OpenExisting($mutexName)
                }
                catch [System.Threading.WaitHandleCannotBeOpenedException]
                {
                    # The named mutex does not exist.
                    $foundNonExistentMutex = $true
                    break
                }
                catch
                {
                    # Intentionally ignore all other exceptions and
                    # continue searching for a non-existent mutex name.
                    continue
                }
                finally
                {
                    if ($null -ne $mutex)
                    {
                        $mutex.Dispose()
                        $mutex = $null
                    }
                }
            }
            if (-not $foundNonExistentMutex)
            {
                throw "Failed to find a non-existent mutex name after $maxAttempts attempts."
            }

            Test-ADTMutexAvailability -MutexName $mutexName | Should -BeTrue
        }
    }

    Context 'Input validation' {
        It 'Should validate that MutexName is between 1 and 260 characters long' {
            { Test-ADTMutexAvailability -MutexName 'A' } | Should -Not -Throw
            { Test-ADTMutexAvailability -MutexName '' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException]) -ErrorId 'ParameterArgumentValidationError,Test-ADTMutexAvailability'

            { Test-ADTMutexAvailability -MutexName ('A' * 260) } | Should -Not -Throw
            { Test-ADTMutexAvailability -MutexName ('A' * 261) } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException]) -ErrorId 'ParameterArgumentValidationError,Test-ADTMutexAvailability'
        }
        It 'Should validate that MutexWaitTime is not null' {
            { Test-ADTMutexAvailability -MutexName 'test' -MutexWaitTime $null } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException]) -ErrorId 'ParameterArgumentTransformationError,Test-ADTMutexAvailability'
        }
    }
}
