BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

    $script:Notepad = "$env:SystemRoot\System32\notepad.exe"
}

Describe 'Get-ADTExecutableInfo' {
    Context 'Functionality' {
        BeforeAll {
            $script:Info = Get-ADTExecutableInfo -LiteralPath $script:Notepad
        }

        It 'Returns executable information' {
            $script:Info | Should -BeOfType ([PSADT.FileSystem.ExecutableInfo])
        }

        It 'Points back at the file it read' {
            $script:Info.FileInfo.FullName | Should -BeExactly $script:Notepad
        }

        It 'Reports the machine architecture' {
            # The same header field Get-ADTPEFileArchitecture reads, so the two have to agree.
            $script:Info.Machine | Should -Be (Get-ADTPEFileArchitecture -LiteralPath $script:Notepad)
        }

        It 'Reports the subsystem' {
            # Notepad is a windowed application rather than a console one, which is what the toolkit uses to
            # decide whether a process needs a window hidden.
            $script:Info.Subsystem | Should -Be ([PSADT.Interop.IMAGE_SUBSYSTEM]::IMAGE_SUBSYSTEM_WINDOWS_GUI)
        }

        It 'Says whether the executable is managed' {
            # Notepad is native, so this is the negative case for the .NET detection.
            $script:Info.IsDotNetExecutable | Should -BeFalse
        }

        It 'Detects a managed executable' {
            # One of the toolkit's own client binaries, which is a managed assembly. pwsh.exe is not: it is
            # a native apphost that loads the runtime, so it reads as unmanaged here.
            $managed = Get-ADTExecutableInfo -LiteralPath "$PSScriptRoot\..\..\..\PSAppDeployToolkit\lib\net472\PSADT.ClientServer.Client.exe"
            $managed.IsDotNetExecutable | Should -BeTrue
        }

        It 'Reads more than one file at a time' {
            @(Get-ADTExecutableInfo -LiteralPath $script:Notepad, "$env:SystemRoot\System32\cmd.exe").Count | Should -Be 2
        }

        It 'Accepts a wildcard through -Path' {
            @(Get-ADTExecutableInfo -Path "$env:SystemRoot\System32\notepad.*").Count | Should -BeGreaterThan 0
        }

        It 'Accepts a file from the pipeline' {
            (Get-Item -LiteralPath $script:Notepad | Get-ADTExecutableInfo).FileInfo.Name | Should -BeExactly 'notepad.exe'
        }

        It 'Errors on a file that is not an executable' {
            $plain = "$TestDrive\plain.txt"
            Set-Content -LiteralPath $plain -Value 'not an executable'
            { Get-ADTExecutableInfo -LiteralPath $plain -ErrorAction Stop } | Should -Throw
        }
    }
}
