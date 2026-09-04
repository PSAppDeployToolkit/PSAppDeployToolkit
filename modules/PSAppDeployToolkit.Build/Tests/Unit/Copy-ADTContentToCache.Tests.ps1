BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}
Describe 'Copy-ADTContentToCache' {
    Context 'With no session open' {
        It 'Tells the caller to open one first' {
            { Copy-ADTContentToCache } | Should -Throw -ErrorId 'ADTSessionBufferEmpty,Get-ADTSession'
        }
    }

    Context 'Caching a deployment' {
        BeforeEach {
            $script:Deploy = "$TestDrive\Deploy$([System.Guid]::NewGuid().ToString('N'))"
            $null = New-Item -Path "$script:Deploy\Files" -ItemType Directory -Force
            $null = New-Item -Path "$script:Deploy\SupportFiles" -ItemType Directory -Force
            Set-Content -LiteralPath "$script:Deploy\Files\payload.txt" -Value 'payload'
            Set-Content -LiteralPath "$script:Deploy\SupportFiles\support.txt" -Value 'support'
            Set-Content -LiteralPath "$script:Deploy\Invoke-AppDeployToolkit.ps1" -Value '# toolkit'
            $script:Session = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'CacheCopy' -DeployMode Silent -ScriptDirectory $script:Deploy -PassThru -InformationAction SilentlyContinue
            $script:Cache = "$((Get-ADTConfig).Toolkit.CachePath)\$($script:Session.InstallName)"
        }

        AfterEach {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
            Remove-Item -LiteralPath $script:Cache -Recurse -Force -ErrorAction Ignore
        }

        It 'Copies the deployment content into the cache' {
            Copy-ADTContentToCache
            Test-Path -LiteralPath "$script:Cache\Files\payload.txt" -PathType Leaf | Should -BeTrue
            Test-Path -LiteralPath "$script:Cache\SupportFiles\support.txt" -PathType Leaf | Should -BeTrue
        }

        It 'Copies the toolkit content alongside it' {
            Copy-ADTContentToCache
            Test-Path -LiteralPath "$script:Cache\Invoke-AppDeployToolkit.ps1" -PathType Leaf | Should -BeTrue
        }

        It 'Repoints the session at the cached copy' {
            # This is the point of caching: the rest of the deployment reads from the cache, so that it
            # survives the original location going away.
            Copy-ADTContentToCache
            $script:Session.DirFiles | Should -BeExactly "$script:Cache\Files"
            $script:Session.DirSupportFiles | Should -BeExactly "$script:Cache\SupportFiles"
        }

        It 'Copies only what it was asked for' {
            Copy-ADTContentToCache -Content Files
            Test-Path -LiteralPath "$script:Cache\Files" -PathType Container | Should -BeTrue
            Test-Path -LiteralPath "$script:Cache\SupportFiles" | Should -BeFalse
        }

        It 'Leaves the session pointing at the original for content it did not copy' {
            Copy-ADTContentToCache -Content Files
            $script:Session.DirSupportFiles | Should -BeExactly "$script:Deploy\SupportFiles"
        }

        It 'Clears out whatever was in the cache folder already' {
            # A previous run's content mixing with this one is how a deployment ends up installing a
            # version nobody asked for.
            $null = New-Item -Path $script:Cache -ItemType Directory -Force
            Set-Content -LiteralPath "$script:Cache\stale.txt" -Value 'stale'
            Copy-ADTContentToCache
            Test-Path -LiteralPath "$script:Cache\stale.txt" | Should -BeFalse
        }

        It 'Copies the toolkit content on its own' {
            # Toolkit means everything that is not one of the two content folders, so the deployment script
            # itself comes across and nothing else does.
            Copy-ADTContentToCache -Content Toolkit
            Test-Path -LiteralPath "$script:Cache\Invoke-AppDeployToolkit.ps1" -PathType Leaf | Should -BeTrue
            Test-Path -LiteralPath "$script:Cache\Files" | Should -BeFalse
            Test-Path -LiteralPath "$script:Cache\SupportFiles" | Should -BeFalse
        }

        It 'Copies the support files on their own' {
            Copy-ADTContentToCache -Content SupportFiles
            Test-Path -LiteralPath "$script:Cache\SupportFiles\support.txt" -PathType Leaf | Should -BeTrue
            Test-Path -LiteralPath "$script:Cache\Files" | Should -BeFalse
            $script:Session.DirSupportFiles | Should -BeExactly "$script:Cache\SupportFiles"
            $script:Session.DirFiles | Should -BeExactly "$script:Deploy\Files"
        }

        It 'Reports a cache folder it could not clear' {
            # Something holding a file open in the cache is the usual reason, and copying over the top of a
            # folder that could not be cleared would mix the last run's content in with this one's.
            $null = New-Item -Path $script:Cache -ItemType Directory -Force
            $stream = [System.IO.File]::Open("$script:Cache\held-open.txt", 'Create', 'Write', 'None')

            try
            {
                { Copy-ADTContentToCache } | Should -Throw -ErrorId 'RemoveFileSystemItemIOError,Copy-ADTContentToCache'
            }
            finally
            {
                $stream.Dispose()
            }
        }
        It 'Refuses the root cache folder' {
            # The destination is erased before copying, so allowing the root would wipe every other
            # package's cache along with it.
            { Copy-ADTContentToCache -LiteralPath (Get-ADTConfig).Toolkit.CachePath } | Should -Throw -ErrorId 'CachePathIsRootDirectory,Copy-ADTContentToCache'
        }

        It 'Does nothing when it is already running from the cache' {
            # Erasing the destination here would destroy the source it was about to copy.
            Copy-ADTContentToCache -LiteralPath $script:Deploy
            Test-Path -LiteralPath "$script:Deploy\Files\payload.txt" -PathType Leaf | Should -BeTrue
        }

        It 'Copies nothing with -WhatIf' {
            Copy-ADTContentToCache -WhatIf
            Test-Path -LiteralPath $script:Cache | Should -BeFalse
        }

        It 'Refuses a content type it does not know' {
            { Copy-ADTContentToCache -Content 'Everything' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Refuses the same content type twice' {
            { Copy-ADTContentToCache -Content Files, Files } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Copy-ADTContentToCache'
        }
    }
}
