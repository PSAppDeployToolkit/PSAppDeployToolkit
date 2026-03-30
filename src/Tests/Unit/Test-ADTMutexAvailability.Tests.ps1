
BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Test-ADTMutexAvailability' {
    Context 'Functionality' {
        It 'Should return $true when the mutex is not locked' {
            $mutexName = $null
            while ($true)
            {
                $mutexName = "Global\PSADT_Pester_$([System.Guid]::NewGuid().Guid)"
                $mutex = [System.Threading.Mutex]::new($false, $mutexName)
                try
                {
                    if ($isMutexLocked = $mutex.WaitOne(1))
                    {
                        break
                    }
                }
                finally
                {
                    if ($isMutexLocked)
                    {
                        $mutex.ReleaseMutex()
                    }
                    $mutex.Close()
                    $mutex.Dispose()
                }

                break
            }

            Test-ADTMutexAvailability -MutexName $mutexName | Should -BeTrue
        }
        It 'Should return $false when the mutex is locked' {
            $mutexName = "Global\PSADT_Pester_$([System.Guid]::NewGuid().Guid)"
            $command = {
                $mutex = [System.Threading.Mutex]::new($false, $mutexName)
                try
                {
                    if (-not $mutex.WaitOne(1))
                    {
                        exit 1
                    }
                    Start-Sleep -Seconds 10
                    $mutex.ReleaseMutex()
                }
                finally
                {
                    $mutex.Close()
                    $mutex.Dispose()
                }
            }

            $encodedCommand = [System.Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($command.ToString().Replace('$mutexName', "'$mutexName'")))

            # Lock the mutex in another process because locking it in this one will return $true every time WaitOne(1) is called
            $proc = Start-Process -FilePath (Join-Path -Path $PSHOME -ChildPath (('powershell.exe', 'pwsh.exe')[$PSVersionTable.PSEdition.Equals('Core')])) -ArgumentList "-NoLogo -NoProfile -NonInteractive -EncodedCommand $encodedCommand" -WindowStyle Hidden -PassThru

            # Give the new PowerShell process time to start before checking if the mutex is locked
            Start-Sleep -Seconds 5

            try
            {
                $proc.Refresh()
                if ($proc.HasExited -and ($proc.ExitCode -ne 0))
                {
                    throw "Child PowerShell process failed to acquire a lock on the mutex [$mutexName]."
                }

                Test-ADTMutexAvailability -MutexName $mutexName | Should -BeFalse
            }
            finally
            {
                $proc.Dispose()
            }
        }
        It 'Should return $true when the mutex does not exist' {
            $mutexName = $null
            while ($true)
            {
                try
                {
                    $mutexName = "Global\PSADT_Pester_$([System.Guid]::NewGuid().Guid)"
                    $mutex = [System.Threading.Mutex]::OpenExisting($mutexName)
                    $mutex.Close()
                    $mutex.Dispose()
                }
                catch [Threading.WaitHandleCannotBeOpenedException]
                {
                    # The named mutex does not exist.
                    break
                }
                catch
                {
                    $null = $null
                }
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
