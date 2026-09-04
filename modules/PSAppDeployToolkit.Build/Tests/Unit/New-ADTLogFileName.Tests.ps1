BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'New-ADTLogFileName' {
    Context 'With no session open' {
        It 'Tells the caller to open one first' {
            # The name is built from the session's own details, so there is nothing to build one from.
            { New-ADTLogFileName -Discriminator 'Anything' } | Should -Throw -ErrorId 'ADTSessionBufferEmpty,New-ADTLogFileName'
        }
    }

    Context 'With a session open' {
        BeforeAll {
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppVendor 'Vend' -AppName 'Prod' -AppVersion '1.2' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Returns a string' {
            New-ADTLogFileName -Discriminator 'Disc' | Should -BeOfType ([System.String])
        }

        It 'Builds the name from the install name, the discriminator and the deployment type' {
            # This is how a function that writes its own log alongside the session's names the file, so the
            # composition is the contract.
            New-ADTLogFileName -Discriminator 'Disc' -FileNameOnly | Should -BeExactly 'Vend_Prod_1.2_Disc_Install.log'
        }

        It 'Returns a full path unless only the name is asked for' {
            $full = New-ADTLogFileName -Discriminator 'Disc'
            $full | Should -BeExactly ([System.IO.Path]::Combine((Get-ADTConfig).Toolkit.LogPath, 'Vend_Prod_1.2_Disc_Install.log'))
            [System.IO.Path]::IsPathRooted($full) | Should -BeTrue
        }

        It 'Puts the log beside the session log' {
            [System.IO.Path]::GetDirectoryName((New-ADTLogFileName -Discriminator 'Disc')) | Should -BeExactly (Get-ADTConfig).Toolkit.LogPath
        }

        It 'Gives different discriminators different names' {
            New-ADTLogFileName -Discriminator 'One' -FileNameOnly | Should -Not -BeExactly (New-ADTLogFileName -Discriminator 'Two' -FileNameOnly)
        }

        It 'Rejects an empty discriminator' {
            { New-ADTLogFileName -Discriminator '' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
