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
Describe 'Remove-ADTContentFromCache' {
    Context 'With no session open' {
        It 'Tells the caller to open one first' {
            { Remove-ADTContentFromCache } | Should -Throw -ErrorId 'ADTSessionBufferEmpty,Get-ADTSession'
        }
    }

    Context 'Clearing a cached deployment' {
        BeforeEach {
            $script:Deploy = "$TestDrive\Deploy$([System.Guid]::NewGuid().ToString('N'))"
            $null = New-Item -Path "$script:Deploy\Files" -ItemType Directory -Force
            $null = New-Item -Path "$script:Deploy\SupportFiles" -ItemType Directory -Force
            Set-Content -LiteralPath "$script:Deploy\Files\payload.txt" -Value 'payload'
            $script:Session = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'CacheRemove' -DeployMode Silent -ScriptDirectory $script:Deploy -PassThru -InformationAction SilentlyContinue
            $script:Cache = "$((Get-ADTConfig).Toolkit.CachePath)\$($script:Session.InstallName)"
        }

        AfterEach {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
            Remove-Item -LiteralPath $script:Cache -Recurse -Force -ErrorAction Ignore
        }

        It 'Removes the cache folder' {
            Copy-ADTContentToCache
            Remove-ADTContentFromCache
            Test-Path -LiteralPath $script:Cache | Should -BeFalse
        }

        It 'Points the session back at where it came from' {
            # The deployment carries on after the cache is cleared, so it has to be left reading from the
            # original script directory again.
            Copy-ADTContentToCache
            Remove-ADTContentFromCache
            $script:Session.DirFiles | Should -BeExactly "$script:Deploy\Files"
            $script:Session.DirSupportFiles | Should -BeExactly "$script:Deploy\SupportFiles"
        }

        It 'Says so when there is no cache folder to remove' {
            Remove-ADTContentFromCache
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*does not exist*' }
        }

        It 'Does not object when there is nothing to remove' {
            { Remove-ADTContentFromCache } | Should -Not -Throw
        }

        It 'Leaves the cache alone with -WhatIf' {
            Copy-ADTContentToCache
            Remove-ADTContentFromCache -WhatIf
            Test-Path -LiteralPath $script:Cache -PathType Container | Should -BeTrue
        }

        It 'Refuses a blank path' {
            { Remove-ADTContentFromCache -LiteralPath '   ' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Remove-ADTContentFromCache'
        }
    }
}
