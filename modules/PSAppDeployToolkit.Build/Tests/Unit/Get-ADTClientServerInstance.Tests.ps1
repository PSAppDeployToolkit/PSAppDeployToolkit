BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Get-ADTClientServerInstance' {
    # Only the refusal is covered. Holding an instance open means keeping a dialog on screen, which a
    # silent deployment never does and which is left to the user interface effort.
    Context 'With no client running' {
        It 'Refuses rather than handing back nothing' {
            # Callers guard on Test-ADTClientServerActive first, so arriving here with no instance is a
            # caller that skipped the guard rather than an ordinary state to report.
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Get-ADTClientServerInstance } | Should -Throw -ErrorId 'ClientServerInstanceNotFoundError,Get-ADTClientServerInstance'
            }
        }

        It 'Says this is the toolkit team''s problem rather than the caller''s' {
            # It is unreachable from outside the module, so anyone seeing it has found a bug rather than
            # made a mistake of their own.
            InModuleScope -ModuleName PSAppDeployToolkit {
                $record = try { Get-ADTClientServerInstance } catch { $_ }
                $record.Exception.Message | Should -BeLike '*no active client/server instance*'
            }
        }

        It 'Refuses whether or not the module has been initialized' {
            # The instance lives beside the module state rather than inside it, so initializing does not
            # produce one and tearing down does not remove one.
            Initialize-ADTTestModule -Path $TestDrive
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Get-ADTClientServerInstance } | Should -Throw -ErrorId 'ClientServerInstanceNotFoundError,Get-ADTClientServerInstance'
            }
        }
    }
}
