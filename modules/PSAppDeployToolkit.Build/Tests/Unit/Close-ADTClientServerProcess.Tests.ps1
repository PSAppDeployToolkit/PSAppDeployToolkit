BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}
Describe 'Close-ADTClientServerProcess' {
    # Only the refusal is covered. A silent deployment creates and disposes the client per operation
    # rather than keeping one, and the only way to leave one running is to put a dialog on screen and
    # keep it there, which is left to the user interface effort.
    Context 'With no client running' {
        BeforeAll {
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'CloseClientProbe' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Reports that there is nothing to close' {
            # Callers guard on the module's own state before calling this, so being asked to close a
            # client that was never started is a caller mistake rather than something to shrug off.
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Close-ADTClientServerProcess } | Should -Throw -ErrorId 'ClientServerProcessNull'
            }
        }

        It 'Leaves the module holding no client either way' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                try { Close-ADTClientServerProcess } catch { $null = $_ }
                $ADT.ClientServerProcess | Should -BeNullOrEmpty
            }
        }
    }
}
