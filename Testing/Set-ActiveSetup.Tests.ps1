BeforeAll {
	$DeployMode = 'NonInteractive'
	$null = . "$PSScriptRoot\..\Toolkit\AppDeployToolkit\AppDeployToolkitMain.ps1" *> $null
	if (-not $SessionZero -or -not $RunAsActiveUser) {
		throw 'This test should be run under the system account with another user logged in.'
	}
	Mock Write-Host {}
	$DebugPreference = 'Continue'
}

Describe 'Set-ActiveSetup' {
	BeforeAll {
		$Key = 'Set-ActiveSetup'
		$UserTestDrive = "$runasUserProfile\AppData\Local\Temp\Set-ActiveSetup"
	}
	BeforeEach {
		Set-ActiveSetup -Key $Key -PurgeActiveSetupKey
		Remove-Item -Path $UserTestDrive -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
		New-Item -Path $UserTestDrive -ItemType Directory -Force -ErrorAction SilentlyContinue | Out-Null
	}
	AfterAll {
		$Key = 'Set-ActiveSetup'
		$UserTestDrive = "$runasUserProfile\AppData\Local\Temp\Set-ActiveSetup"
		Set-ActiveSetup -Key $Key -PurgeActiveSetupKey
		Remove-Item -Path $UserTestDrive -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
	}

	Context 'cmd.exe' {
		It 'Should run cmd.exe to copy a file' {
			Set-Content -Path "$UserTestDrive\test.txt" -Value 'Hello World'
			Set-ActiveSetup -Key $Key -StubExePath "$env:SystemRoot\System32\cmd.exe" -Arguments "/c copy `"$UserTestDrive\test.txt`" `"$UserTestDrive\test 2.txt`""
			"$UserTestDrive\test 2.txt" | Should -Exist
		}
	}

	Context 'powershell.exe' {
		It 'Should run powershell.exe to copy a file' {
			Set-Content -Path "$UserTestDrive\test.txt" -Value 'Hello World'
			Set-ActiveSetup -Key $Key -StubExePath "$PSHOME\powershell.exe" -Arguments "-ExecutionPolicy Bypass -NoProfile -Command Copy-Item '$UserTestDrive\test.txt' '$UserTestDrive\test 2.txt'"
			"$UserTestDrive\test 2.txt" | Should -Exist
		}
	}

	Context '.CMD file' {
		It 'Should run a .CMD file' {
			$ScriptContent = "echo Hello World > `"$UserTestDrive\test.txt`""
			Set-Content -Path "$UserTestDrive\test.cmd" -Value $ScriptContent -Encoding ASCII
			Set-ActiveSetup -Key $Key -StubExePath "$UserTestDrive\test.cmd"
			"$UserTestDrive\test.txt" | Should -Exist
			"$UserTestDrive\test.txt" | Should -FileContentMatch 'Hello World'
		}
		It 'Should run a .CMD file with arguments' {
			$ScriptContent = "echo %1...%2...%3 > `"$UserTestDrive\test.txt`""
			Set-Content -Path "$UserTestDrive\test.cmd" -Value $ScriptContent -Encoding ASCII
			Set-ActiveSetup -Key $Key -StubExePath "$UserTestDrive\test.cmd" -Arguments "Hello `"To The`" World"
			"$UserTestDrive\test.txt" | Should -Exist
			"$UserTestDrive\test.txt" | Should -FileContentMatch 'Hello..."To The"...World'
		}
		It 'Should run a .CMD file with metacharacters in the name 1' {
			$ScriptContent = "echo Hello World > `"$UserTestDrive\test.txt`""
			Set-Content -Path "$UserTestDrive\test()%!^&.cmd" -Value $ScriptContent -Encoding ASCII
			Set-ActiveSetup -Key $Key -StubExePath "$UserTestDrive\test()%!^&.cmd"
			"$UserTestDrive\test.txt" | Should -Exist
			"$UserTestDrive\test.txt" | Should -FileContentMatch 'Hello World'
		}
		It 'Should run a .CMD file with metacharacters in the name 2' {
			$ScriptContent = "echo Hello World > `"$UserTestDrive\test.txt`""
			Set-Content -Path "$UserTestDrive\test(1).cmd" -Value $ScriptContent -Encoding ASCII
			Set-ActiveSetup -Key $Key -StubExePath "$UserTestDrive\test(1).cmd"
			"$UserTestDrive\test.txt" | Should -Exist
			"$UserTestDrive\test.txt" | Should -FileContentMatch 'Hello World'
		}
		It 'Should run a .CMD file with metacharacters in the name 3' {
			$ScriptContent = "echo Hello World > `"$UserTestDrive\test.txt`""
			Set-Content -Path "$UserTestDrive\test (1).cmd" -Value $ScriptContent -Encoding ASCII
			Set-ActiveSetup -Key $Key -StubExePath "$UserTestDrive\test (1).cmd"
			"$UserTestDrive\test.txt" | Should -Exist
			"$UserTestDrive\test.txt" | Should -FileContentMatch 'Hello World'
		}
		It 'Should run a .CMD file from %ProgramData%' {
			# Note this does not currently work for per-user variables like %APPDATA% since they are expanded by the script in system context
			$ScriptContent = "echo Hello World > `"$UserTestDrive\test.txt`""
			Set-Content -Path "$env:ProgramData\Set-ActiveSetup_Test.cmd" -Value $ScriptContent -Encoding ASCII
			Set-ActiveSetup -Key $Key -StubExePath '%ProgramData%\Set-ActiveSetup_Test.cmd'
			"$UserTestDrive\test.txt" | Should -Exist
			"$UserTestDrive\test.txt" | Should -FileContentMatch 'Hello World'
			Remove-File -Path "$env:ProgramData\Set-ActiveSetup_Test.cmd"
		}
	}

	Context '.PS1 file' {
		It 'Should run a .PS1 file' {
			$ScriptContent = "Set-Content -Path `"$UserTestDrive\test.txt`" -Value 'Hello World'"
			Set-Content -Path "$UserTestDrive\test.ps1" -Value $ScriptContent -Encoding UTF8
			Set-ActiveSetup -Key $Key -StubExePath "$UserTestDrive\test.ps1"
			"$UserTestDrive\test.txt" | Should -Exist
			"$UserTestDrive\test.txt" | Should -FileContentMatch 'Hello World'
		}
		It 'Should run a .PS1 file with arguments' {
			$ScriptContent = @"
param (`$FirstWord, `$SecondWord, `$ThirdWord)
Set-Content -Path `"$UserTestDrive\test.txt`" -Value "`$FirstWord...`$SecondWord...`$ThirdWord"
"@
			Set-Content -Path "$UserTestDrive\test.ps1" -Value $ScriptContent -Encoding UTF8
			Set-ActiveSetup -Key $Key -StubExePath "$UserTestDrive\test.ps1" -Arguments '-FirstWord "Hello" -SecondWord "To The" -ThirdWord "World"'
			"$UserTestDrive\test.txt" | Should -Exist
			"$UserTestDrive\test.txt" | Should -FileContentMatch 'Hello...To The...World'
		}
		It 'Should run a .PS1 file with a space in the name' {
			$ScriptContent = "Set-Content -Path `"$UserTestDrive\test.txt`" -Value 'Hello World'"
			Set-Content -Path "$UserTestDrive\test 1.ps1" -Value $ScriptContent -Encoding UTF8
			Set-ActiveSetup -Key $Key -StubExePath "$UserTestDrive\test 1.ps1"
			"$UserTestDrive\test.txt" | Should -Exist
			"$UserTestDrive\test.txt" | Should -FileContentMatch 'Hello World'
		}
	}

	Context '.VBS file' {
		It 'Should run a .VBS file' {
			$ScriptContent = @"
Set fso = CreateObject(`"Scripting.FileSystemObject`")
Set file = fso.OpenTextFile(`"$UserTestDrive\test.txt`", 2, True)
file.WriteLine `"Hello...To The...World`"
file.Close
"@
			Set-Content -Path "$UserTestDrive\test.vbs" -Value $ScriptContent -Encoding ASCII
			Set-ActiveSetup -Key $Key -StubExePath "$UserTestDrive\test.vbs"
			"$UserTestDrive\test.txt" | Should -Exist
			"$UserTestDrive\test.txt" | Should -FileContentMatch 'Hello...To The...World'
		}
		It 'Should run a .VBS file with arguments' {
			$ScriptContent = @"
Set fso = CreateObject(`"Scripting.FileSystemObject`")
Set file = fso.OpenTextFile(`"$UserTestDrive\test.txt`", 2, True)
file.WriteLine WScript.Arguments(0) & `"...`" & WScript.Arguments(1) & `"...`" & WScript.Arguments(2)
file.Close
"@
			Set-Content -Path "$UserTestDrive\test.vbs" -Value $ScriptContent -Encoding ASCII
			Set-ActiveSetup -Key $Key -StubExePath "$UserTestDrive\test.vbs" -Arguments 'Hello "To The" World'
			"$UserTestDrive\test.txt" | Should -Exist
			"$UserTestDrive\test.txt" | Should -FileContentMatch 'Hello...To The...World'
		}
	}
}
