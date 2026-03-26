BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Remove-ADTContentFromCache' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'MockSession', Justification = 'Used in It blocks.')]
        $MockSession = [PSCustomObject]@{ DirFiles = $null; DirSupportFiles = $null }
        Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $MockSession }

        # Fixed script-dir in TestDrive — used as the revert target for DirFiles/DirSupportFiles.
        $script:ADTTestRemoveCacheScriptDir = Join-Path -Path $TestDrive -ChildPath 'ScriptDir'
        $null = New-Item -Path $script:ADTTestRemoveCacheScriptDir -ItemType Directory
        Mock -ModuleName PSAppDeployToolkit Get-ADTSessionCacheScriptDirectory { return $script:ADTTestRemoveCacheScriptDir }
    }

    AfterAll {
        Remove-Variable -Name ADTTestRemoveCacheScriptDir -Scope Script -ErrorAction SilentlyContinue
    }

    BeforeEach {
        $MockSession.DirFiles = $null
        $MockSession.DirSupportFiles = $null
    }

    Context 'Folder does not exist' {
        It 'Does not throw when the cache folder does not exist' {
            $nonExistentDir = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
            { Remove-ADTContentFromCache -LiteralPath $nonExistentDir } | Should -Not -Throw
        }

        It 'Returns no output when the cache folder does not exist' {
            $nonExistentDir = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
            $result = Remove-ADTContentFromCache -LiteralPath $nonExistentDir
            $result | Should -BeNull
        }
    }

    Context 'Folder removal' {
        It 'Removes the cache folder when it exists' {
            $cacheDir = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
            $null = New-Item -Path $cacheDir -ItemType Directory
            Remove-ADTContentFromCache -LiteralPath $cacheDir
            Test-Path -LiteralPath $cacheDir | Should -BeFalse
        }

        It 'Does not throw when removing an existing folder' {
            $cacheDir = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
            $null = New-Item -Path $cacheDir -ItemType Directory
            { Remove-ADTContentFromCache -LiteralPath $cacheDir } | Should -Not -Throw
        }

        It 'Does not remove the folder under -WhatIf' {
            $cacheDir = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
            $null = New-Item -Path $cacheDir -ItemType Directory
            Remove-ADTContentFromCache -LiteralPath $cacheDir -WhatIf
            Test-Path -LiteralPath $cacheDir | Should -BeTrue
        }
    }

    Context 'Session property revert' {
        It 'Sets DirFiles on the session after removal' {
            $cacheDir = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
            $null = New-Item -Path $cacheDir -ItemType Directory
            Remove-ADTContentFromCache -LiteralPath $cacheDir
            $MockSession.DirFiles | Should -Not -BeNull
        }

        It 'Reverts DirFiles to the Files subdirectory of the script dir' {
            $cacheDir = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
            $null = New-Item -Path $cacheDir -ItemType Directory
            Remove-ADTContentFromCache -LiteralPath $cacheDir
            $MockSession.DirFiles | Should -Be (Join-Path -Path $script:ADTTestRemoveCacheScriptDir -ChildPath 'Files')
        }

        It 'Does not update session properties under -WhatIf' {
            $cacheDir = Join-Path -Path $TestDrive -ChildPath ([System.Guid]::NewGuid().ToString('N'))
            $null = New-Item -Path $cacheDir -ItemType Directory
            Remove-ADTContentFromCache -LiteralPath $cacheDir -WhatIf
            $MockSession.DirFiles | Should -BeNull
        }
    }

    Context 'Error handling' {
        It 'Throws when Get-ADTSession fails' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { throw [System.InvalidOperationException]::new('No active session.') }
            { Remove-ADTContentFromCache -LiteralPath (Join-Path -Path $TestDrive -ChildPath 'ErrorTest') } | Should -Throw
        }
    }
}
