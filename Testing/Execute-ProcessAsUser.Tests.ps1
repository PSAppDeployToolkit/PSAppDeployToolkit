BeforeAll {
	$DebugPreference = 'Continue'
	$appName = 'Execute-ProcessAsUser.Tests'
	$DeployMode = 'NonInteractive'
	$null = . "$PSScriptRoot\..\Toolkit\AppDeployToolkit\AppDeployToolkitMain.ps1" *> $null
	#Mock Write-Host {}
	Write-Debug "$configToolkitLogDir\$logName"
}

Describe 'Execute-ProcessAsUser' {
	BeforeAll {
		$UserTestDrive = "$runasUserProfile\AppData\Local\Temp\Execute-ProcessAsUser"
		Remove-Item -Path $UserTestDrive -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
	}
	BeforeEach {
		New-Item -Path $UserTestDrive -ItemType Directory -Force -ErrorAction SilentlyContinue | Out-Null
	}
	AfterEach {
		Remove-Item -Path $UserTestDrive -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
	}

	Context 'cmd.exe' {
		It 'Should run cmd.exe' {
			$ProcessExitCode = Execute-ProcessAsUser -Path 'cmd.exe' -Parameters '/c echo Hello World' -Wait -PassThru
			$ProcessExitCode | Should -Be 0
		}
		It 'Should return exit codes' {
			$ProcessExitCode = Execute-ProcessAsUser -Path 'cmd.exe' -Parameters '/c exit 42' -Wait -PassThru
			$ProcessExitCode | Should -Be 42
		}
		It 'Should correctly interpret "' {
			Set-Content -Path "$UserTestDrive\test.txt" -Value 'Hello World'
			$ProcessExitCode = Execute-ProcessAsUser -Path 'cmd.exe' -Parameters "/c copy `"$UserTestDrive\test.txt`" `"$UserTestDrive\test 2.txt`"" -Wait -PassThru
			$ProcessExitCode | Should -Be 0
			"$UserTestDrive\test 2.txt" | Should -Exist
		}
		It 'Should correctly interpret >' {
			$ProcessExitCode = Execute-ProcessAsUser -Path 'cmd.exe' -Parameters "/c echo `"Hello World`" > `"$UserTestDrive\test.txt`"" -Wait -PassThru
			$ProcessExitCode | Should -Be 0
			"$UserTestDrive\test.txt" | Should -Exist
			"$UserTestDrive\test.txt" | Should -FileContentMatch '"Hello World"'
		}
		It 'Should correctly interpret %' {
			Set-Content -Path "$UserTestDrive\test.txt" -Value 'Hello World'
			$ProcessExitCode = Execute-ProcessAsUser -Path 'cmd.exe' -Parameters "/c copy `"$UserTestDrive\test.txt`" `"%LOCALAPPDATA%\Temp\Execute-ProcessAsUser\test 2.txt`"" -Wait -PassThru
			$ProcessExitCode | Should -Be 0
			"$UserTestDrive\test 2.txt" | Should -Exist
		}
		It 'Should correctly interpret ()' {
			Set-Content -Path "$UserTestDrive\test.txt" -Value 'Hello World'
			$ProcessExitCode = Execute-ProcessAsUser -Path 'cmd.exe' -Parameters "/c if exist `"$UserTestDrive\test.txt`" ( copy `"$UserTestDrive\test.txt`" `"$UserTestDrive\test 2.txt`" )" -Wait -PassThru
			$ProcessExitCode | Should -Be 0
			"$UserTestDrive\test 2.txt" | Should -Exist
		}
		It 'Should correctly interpret &' {
			Set-Content -Path "$UserTestDrive\test.txt" -Value 'Hello World'
			$ProcessExitCode = Execute-ProcessAsUser -Path 'cmd.exe' -Parameters "/c copy `"$UserTestDrive\test.txt`" `"$UserTestDrive\test 2.txt`" & copy `"$UserTestDrive\test.txt`" `"$UserTestDrive\test 3.txt`"" -Wait -PassThru
			$ProcessExitCode | Should -Be 0
			"$UserTestDrive\test 2.txt" | Should -Exist
			"$UserTestDrive\test 3.txt" | Should -Exist
		}
		It 'Should not strip " from single words' {
			$ProcessExitCode = Execute-ProcessAsUser -Path 'cmd.exe' -Parameters "/c echo `"Hello`" > `"$UserTestDrive\test.txt`"" -Wait -PassThru
			$ProcessExitCode | Should -Be 0
			"$UserTestDrive\test.txt" | Should -Exist
			"$UserTestDrive\test.txt" | Should -FileContentMatch '"Hello"'
		}
		It 'Should run a .cmd file' {
			$ScriptContent = 'echo Hello World > "%LOCALAPPDATA%\Temp\Execute-ProcessAsUser\test.txt"'
			Set-Content -Path "$UserTestDrive\test.cmd" -Value $ScriptContent -Encoding ASCII
			$ProcessExitCode = Execute-ProcessAsUser -Path 'cmd.exe' -Parameters '/c test.cmd' -WorkingDirectory $UserTestDrive -Wait -PassThru
			$ProcessExitCode | Should -Be 0
			"$UserTestDrive\test.txt" | Should -Exist
			"$UserTestDrive\test.txt" | Should -FileContentMatch 'Hello World'
		}
		It 'Should run a .cmd file with arguments' {
			$ScriptContent = 'echo %1 > "%LOCALAPPDATA%\Temp\Execute-ProcessAsUser\test.txt"'
			Set-Content -Path "$UserTestDrive\test.cmd" -Value $ScriptContent -Encoding ASCII
			$ProcessExitCode = Execute-ProcessAsUser -Path 'cmd.exe' -Parameters '/c test.cmd "Hello World"' -WorkingDirectory $UserTestDrive -Wait -PassThru
			$ProcessExitCode | Should -Be 0
			"$UserTestDrive\test.txt" | Should -Exist
			"$UserTestDrive\test.txt" | Should -FileContentMatch 'Hello World'
		}
		It 'Should run a .cmd file with a space in the name' {
			$ScriptContent = 'echo Hello World > "%LOCALAPPDATA%\Temp\Execute-ProcessAsUser\test.txt"'
			Set-Content -Path "$UserTestDrive\test 1.cmd" -Value $ScriptContent -Encoding ASCII
			$ProcessExitCode = Execute-ProcessAsUser -Path 'cmd.exe' -Parameters '/c "test 1.cmd"' -WorkingDirectory $UserTestDrive -Wait -PassThru
			$ProcessExitCode | Should -Be 0
			"$UserTestDrive\test.txt" | Should -Exist
			"$UserTestDrive\test.txt" | Should -FileContentMatch 'Hello World'
		}
		It 'Should allow metacharacters through unescaped when quoted' {
			Set-Content -Path "$UserTestDrive\test.txt" -Value 'Hello World'
			# ()%!^& are all escaped with ^ here. All escapes must be placed by the user, not PSADT
			$ProcessExitCode = Execute-ProcessAsUser -Path 'cmd.exe' -Parameters "/c copy `"$UserTestDrive\test.txt`" `"$UserTestDrive\test()%!^&.txt`"" -Wait -PassThru
			$ProcessExitCode | Should -Be 0
			"$UserTestDrive\test()%!^&.txt" | Should -Exist
		}
		It 'Should allow escaped metacharacters through when unquoted' {
			Set-Content -Path "$UserTestDrive\test.txt" -Value 'Hello World'
			# ()%!^& are all escaped with ^ here. All escapes must be placed by the user, not PSADT
			$ProcessExitCode = Execute-ProcessAsUser -Path 'cmd.exe' -Parameters "/c copy `"$UserTestDrive\test.txt`" $UserTestDrive\test^(^)^%^!^^^&.txt" -Wait -PassThru
			$ProcessExitCode | Should -Be 0
			"$UserTestDrive\test()%!^&.txt" | Should -Exist
		}
		It 'Should run a .cmd file with ()%!^& metacharacters in the name, with escapes and quotes' {
			$ScriptContent = 'echo Hello World > "%LOCALAPPDATA%\Temp\Execute-ProcessAsUser\test.txt"'
			Set-Content -Path "$UserTestDrive\test()%!^&.cmd" -Value $ScriptContent -Encoding ASCII
			$ProcessExitCode = Execute-ProcessAsUser -Path 'cmd.exe' -Parameters '/c "test^(^)^%^!^^^&.cmd"' -WorkingDirectory $UserTestDrive -Wait -PassThru
			$ProcessExitCode | Should -Be 0
			"$UserTestDrive\test.txt" | Should -Exist
			"$UserTestDrive\test.txt" | Should -FileContentMatch 'Hello World'
		}
		It 'Should run a .cmd file with ()%!^& metacharacters in the name, with escapes nad no quotes' {
			$ScriptContent = 'echo Hello World > "%LOCALAPPDATA%\Temp\Execute-ProcessAsUser\test.txt"'
			Set-Content -Path "$UserTestDrive\test()%!^&.cmd" -Value $ScriptContent -Encoding ASCII
			$ProcessExitCode = Execute-ProcessAsUser -Path 'cmd.exe' -Parameters '/c test^(^)^%^!^^^&.cmd' -WorkingDirectory $UserTestDrive -Wait -PassThru
			$ProcessExitCode | Should -Be 0
			"$UserTestDrive\test.txt" | Should -Exist
			"$UserTestDrive\test.txt" | Should -FileContentMatch 'Hello World'
		}
		It 'Should run a .cmd file with a space in the name' {
			$ScriptContent = 'echo Hello World > "%LOCALAPPDATA%\Temp\Execute-ProcessAsUser\test.txt"'
			Set-Content -Path "$UserTestDrive\test 1.cmd" -Value $ScriptContent -Encoding ASCII
			$ProcessExitCode = Execute-ProcessAsUser -Path 'cmd.exe' -Parameters '/c "test 1.cmd"' -WorkingDirectory $UserTestDrive -Wait -PassThru
			$ProcessExitCode | Should -Be 0
			"$UserTestDrive\test.txt" | Should -Exist
			"$UserTestDrive\test.txt" | Should -FileContentMatch 'Hello World'
		}
	}

	Context 'powershell.exe' {
		It 'Should run powershell.exe' {
			$ProcessExitCode = Execute-ProcessAsUser -Path 'powershell.exe' -Parameters '-ExecutionPolicy Bypass -NoProfile -Command exit 0' -Wait -PassThru
			$ProcessExitCode | Should -Be 0
		}
		It 'Should return exit codes' {
			$ProcessExitCode = Execute-ProcessAsUser -Path 'powershell.exe' -Parameters '-ExecutionPolicy Bypass -NoProfile -Command exit 42' -Wait -PassThru
			$ProcessExitCode | Should -Be 42
		}
		It 'Should run powershell.exe with -Command & {} syntax'{
			$ProcessExitCode = Execute-ProcessAsUser -Path 'powershell.exe' -Parameters '-ExecutionPolicy Bypass -NoProfile -Command "&{ exit 42 }"' -Wait -PassThru
			$ProcessExitCode | Should -Be 42
		}
		It 'Should run powershell.exe with -Command & {} syntax, returning $LastExitCode'{
			$ProcessExitCode = Execute-ProcessAsUser -Path 'powershell.exe' -Parameters '-ExecutionPolicy Bypass -NoProfile -Command "&{ &cmd.exe /c exit 42; exit $LastExitCode }"' -Wait -PassThru
			$ProcessExitCode | Should -Be 42
		}
		It 'Should run powershell.exe with -Command & {} syntax to run a .ps1 file'{
			$ScriptContent = 'exit 0'
			Set-Content -Path "$UserTestDrive\test.ps1" -Value $ScriptContent -Encoding UTF8
			$ProcessExitCode = Execute-ProcessAsUser -Path 'powershell.exe' -Parameters '-ExecutionPolicy Bypass -NoProfile -Command "&{ & .\Test.ps1 }"' -WorkingDirectory $UserTestDrive -Wait -PassThru
			$ProcessExitCode | Should -Be 0
		}
		It 'Should run powershell.exe with -Command & {} syntax to run a .ps1 file, returning $LastExitCode' {
			$ScriptContent = 'exit 42'
			Set-Content -Path "$UserTestDrive\test.ps1" -Value $ScriptContent -Encoding UTF8
			$ProcessExitCode = Execute-ProcessAsUser -Path 'powershell.exe' -Parameters "-ExecutionPolicy Bypass -NoProfile -Command `"&{ & .\Test.ps1; exit `$LastExitCode }`"" -WorkingDirectory $UserTestDrive -Wait -PassThru
			$ProcessExitCode | Should -Be 42
		}
		It 'Should run powershell.exe with -File syntax to run a .ps1 file' {
			$ScriptContent = 'exit 42'
			Set-Content -Path "$UserTestDrive\test.ps1" -Value $ScriptContent -Encoding UTF8
			$ProcessExitCode = Execute-ProcessAsUser -Path 'powershell.exe' -Parameters '-ExecutionPolicy Bypass -NoProfile -File .\Test.ps1' -WorkingDirectory $UserTestDrive -Wait -PassThru
			$ProcessExitCode | Should -Be 42
		}
		It 'Should run powershell.exe with -File syntax to run a .ps1 file with arguments' {
			$ScriptContent = @'
param ($ExitCode)
exit $ExitCode
'@
			Set-Content -Path "$UserTestDrive\test.ps1" -Value $ScriptContent -Encoding UTF8
			$ProcessExitCode = Execute-ProcessAsUser -Path 'powershell.exe' -Parameters '-ExecutionPolicy Bypass -NoProfile -File .\Test.ps1 -ExitCode 42' -WorkingDirectory $UserTestDrive -Wait -PassThru
			$ProcessExitCode | Should -Be 42
		}
	}

	Context 'No Users Logged On' {
		It 'Should not produce an error when no users are logged on and ContinueOnError=$true' {
			$BackupRunAsActiveUser = $RunAsActiveUser
			$RunAsActiveUser = $null
			try {
				Execute-ProcessAsUser -Path 'C:\Windows\System32\cmd.exe' -Parameters '/c echo Hello World' -ContinueOnError $true
				$? | Should -BeTrue
			}
			catch {
				$_ | Should -BeNullOrEmpty
			}
			finally {
				$RunAsActiveUser = $BackupRunAsActiveUser
			}
		}
		It 'Should produce an error when no users are logged on and ContinueOnError=$false' {
			$BackupRunAsActiveUser = $RunAsActiveUser
			$RunAsActiveUser = $null
			try {
				Execute-ProcessAsUser -Path 'C:\Windows\System32\cmd.exe' -Parameters '/c echo Hello World' -ContinueOnError $false
				$? | Should -BeFalse
			} catch {
				$_ | Should -Not -BeNullOrEmpty
			} finally {
				$RunAsActiveUser = $BackupRunAsActiveUser
			}
		}
	}
}
