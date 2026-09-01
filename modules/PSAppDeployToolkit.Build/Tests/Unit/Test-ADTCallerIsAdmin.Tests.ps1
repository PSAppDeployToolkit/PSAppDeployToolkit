BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest
}

Describe 'Test-ADTCallerIsAdmin' {
    Context 'Functionality' {
        It 'Answers with a boolean' {
            Test-ADTCallerIsAdmin | Should -BeOfType ([System.Boolean])
        }

        It 'Agrees with the token Windows reports for this process' {
            # Asked of whoami rather than of the same WindowsPrincipal call the function makes, so that
            # the answer comes from somewhere else. An unelevated process still carries the
            # Administrators SID in its token, but for deny only, so the SID alone is not the question.
            $adminsGroup = whoami /groups /fo csv | ConvertFrom-Csv | & {
                process
                {
                    if ($_.SID.Equals('S-1-5-32-544'))
                    {
                        return $_
                    }
                }
            }
            Test-ADTCallerIsAdmin | Should -Be ($adminsGroup -and $adminsGroup.Attributes.Contains('Enabled group'))
        }

        It 'Agrees with what this process can actually do' {
            # The strongest oracle available: an operation only an administrator can perform. If the
            # function says yes then this has to work, and if it says no then it must not, so an
            # implementation that simply returned one answer would fail here whichever answer it chose.
            $privilegedRead = { $null = Get-ChildItem -LiteralPath "$([System.Environment]::SystemDirectory)\config" -ErrorAction Stop }
            if (Test-ADTCallerIsAdmin)
            {
                $privilegedRead | Should -Not -Throw
            }
            else
            {
                $privilegedRead | Should -Throw
            }
        }
    }
}
