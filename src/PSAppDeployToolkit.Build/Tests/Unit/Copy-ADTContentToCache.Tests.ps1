BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Copy-ADTContentToCache' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
        Mock -ModuleName PSAppDeployToolkit Copy-ADTFile { }

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'MockSession', Justification = 'Used in It blocks.')]
        $MockSession = [PSCustomObject]@{ DirFiles = $null; DirSupportFiles = $null }
        Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $MockSession }

        # Fixed script-dir in TestDrive that always exists — serves as the source.
        $script:ADTTestCopyCacheScriptDir = Join-Path -Path $TestDrive -ChildPath 'ScriptDir'
        $null = New-Item -Path $script:ADTTestCopyCacheScriptDir -ItemType Directory
        Mock -ModuleName PSAppDeployToolkit Get-ADTSessionCacheScriptDirectory { return $script:ADTTestCopyCacheScriptDir }
    }

    AfterAll {
        Remove-Variable -Name ADTTestCopyCacheScriptDir -Scope Script -ErrorAction SilentlyContinue
    }

    BeforeEach {
        $MockSession.DirFiles = $null
        $MockSession.DirSupportFiles = $null
    }

    Context 'Folder creation' {
        It 'Creates the cache folder when it does not exist' {
            $cacheDir = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
            Copy-ADTContentToCache -LiteralPath $cacheDir
            Test-Path -LiteralPath $cacheDir -PathType Container | Should -BeTrue
        }

        It 'Does not throw when the cache folder already exists' {
            $cacheDir = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
            $null = New-Item -Path $cacheDir -ItemType Directory
            { Copy-ADTContentToCache -LiteralPath $cacheDir } | Should -Not -Throw
        }

        It 'Does not create the cache folder under -WhatIf' {
            $cacheDir = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
            Copy-ADTContentToCache -LiteralPath $cacheDir -WhatIf
            Test-Path -LiteralPath $cacheDir -PathType Container | Should -BeFalse
        }
    }

    Context 'Copy operation' {
        It 'Calls Copy-ADTFile when source and destination differ' {
            $cacheDir = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
            $null = New-Item -Path $cacheDir -ItemType Directory
            Copy-ADTContentToCache -LiteralPath $cacheDir
            Should -Invoke Copy-ADTFile -ModuleName PSAppDeployToolkit -Scope It
        }

        It 'Does not call Copy-ADTFile under -WhatIf' {
            $cacheDir = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
            $null = New-Item -Path $cacheDir -ItemType Directory
            Copy-ADTContentToCache -LiteralPath $cacheDir -WhatIf
            Should -Not -Invoke Copy-ADTFile -ModuleName PSAppDeployToolkit -Scope It
        }

        It 'Skips Copy-ADTFile when source and destination are the same path' {
            Copy-ADTContentToCache -LiteralPath $script:ADTTestCopyCacheScriptDir
            Should -Not -Invoke Copy-ADTFile -ModuleName PSAppDeployToolkit -Scope It
        }
    }

    Context 'Session property update' {
        It 'Sets DirFiles on the session after a successful operation' {
            $cacheDir = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
            $null = New-Item -Path $cacheDir -ItemType Directory
            $null = New-Item -Path (Join-Path $cacheDir 'Files') -ItemType Directory
            Copy-ADTContentToCache -LiteralPath $cacheDir
            $MockSession.DirFiles | Should -Not -BeNull
        }

        It 'Sets DirSupportFiles on the session after a successful operation' {
            $cacheDir = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
            $null = New-Item -Path $cacheDir -ItemType Directory
            $null = New-Item -Path (Join-Path $cacheDir 'SupportFiles') -ItemType Directory
            Copy-ADTContentToCache -LiteralPath $cacheDir
            $MockSession.DirSupportFiles | Should -Not -BeNull
        }

        It 'Sets DirFiles to the Files subdirectory of the cache path' {
            $cacheDir = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
            $null = New-Item -Path $cacheDir -ItemType Directory
            $null = New-Item -Path (Join-Path $cacheDir 'Files') -ItemType Directory
            Copy-ADTContentToCache -LiteralPath $cacheDir
            $MockSession.DirFiles | Should -Be (Join-Path -Path $cacheDir -ChildPath 'Files')
        }
    }

    Context 'Error handling' {
        It 'Throws when Get-ADTSession fails' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { throw [System.InvalidOperationException]::new('No active session.') }
            { Copy-ADTContentToCache -LiteralPath (Join-Path -Path $TestDrive -ChildPath 'ErrorTest') } | Should -Throw
        }
    }
}
