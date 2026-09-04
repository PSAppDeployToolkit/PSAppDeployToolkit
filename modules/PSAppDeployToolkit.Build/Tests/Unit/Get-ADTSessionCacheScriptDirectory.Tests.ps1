BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive

    function Get-Probe
    {
        return InModuleScope -ModuleName PSAppDeployToolkit { Get-ADTSessionCacheScriptDirectory }
    }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Get-ADTSessionCacheScriptDirectory' {
    Context 'With no session open' {
        It 'Tells the caller to open one first' {
            { Get-Probe } | Should -Throw -ErrorId 'ADTSessionBufferEmpty,Get-ADTSession'
        }
    }

    Context 'With a script directory holding a Files folder' {
        BeforeAll {
            $script:Deploy = "$TestDrive\Deploy"
            $null = New-Item -Path "$script:Deploy\Files" -ItemType Directory -Force
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'CacheDirProbe' -DeployMode Silent -ScriptDirectory $script:Deploy -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Returns the directory holding the deployment content' {
            # What gets copied to the cache is the script directory, not the Files folder itself, so the
            # parent is what comes back.
            Get-Probe | Should -BeExactly $script:Deploy
        }

        It 'Returns a directory that exists' {
            Test-Path -LiteralPath (Get-Probe) -PathType Container | Should -BeTrue
        }
    }

    Context 'With a script directory holding no content folders' {
        BeforeAll {
            $script:Bare = "$TestDrive\Bare"
            $null = New-Item -Path $script:Bare -ItemType Directory -Force
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'BareProbe' -DeployMode Silent -ScriptDirectory $script:Bare -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Refuses rather than caching a directory with nothing to cache' {
            # Copy-ADTContentToCache depends on this, and silently caching an empty directory would leave a
            # deployment pointing at content that is not there.
            { Get-Probe } | Should -Throw -ErrorId 'ScriptDirectoryInvalid'
        }
    }
}
