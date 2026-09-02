BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}
Describe 'Complete-ADTFunction' {
    Context 'Functionality' {
        It 'Returns nothing' {
            # It pairs with Initialize-ADTFunction at the end of every module function, so anything it
            # emitted would land in that function's own output.
            function Test-Probe
            {
                [CmdletBinding()]
                param
                (
                )

                return Complete-ADTFunction -Cmdlet $PSCmdlet
            }
            Test-Probe | Should -BeNullOrEmpty
        }

        It 'Writes its debug entry only when debug messages are being logged' {
            # The entry is a debug message, so it is suppressed unless the caller asked for them, which is
            # what keeps an ordinary deployment log readable.
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'CompleteProbe' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
            try
            {
                function Test-Probe
                {
                    [CmdletBinding()]
                    param
                    (
                    )

                    Complete-ADTFunction -Cmdlet $PSCmdlet
                }
                Test-Probe

                $log = Get-ChildItem -LiteralPath (Get-ADTConfig).Toolkit.LogPath -Recurse -File -Filter '*CompleteProbe*' | Select-Object -First 1
                [System.IO.File]::ReadAllText($log.FullName) | Should -Not -BeLike '*Function End*'
            }
            finally
            {
                Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
            }
        }

        It 'Requires a cmdlet to work against' {
            Test-ADTMandatoryParameter -Command (Get-Command Complete-ADTFunction) -Parameter Cmdlet | Should -BeTrue
        }

        It 'Does not object to being called without a session' {
            # Module functions call it in their end block whether or not a deployment is running.
            function Test-Probe
            {
                [CmdletBinding()]
                param
                (
                )

                Complete-ADTFunction -Cmdlet $PSCmdlet
            }
            { Test-Probe } | Should -Not -Throw
        }
    }
}
