BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'New-ADTZipFile' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    BeforeEach {
        # Fresh source directory and unique destination path per test.
        $script:SrcDir = Join-Path $TestDrive (New-Guid)
        $null = New-Item -Path $script:SrcDir -ItemType Directory
        Set-Content -Path (Join-Path $script:SrcDir 'data.txt')  -Value 'hello'
        Set-Content -Path (Join-Path $script:SrcDir 'extra.txt') -Value 'world'
        $script:ZipPath = Join-Path $TestDrive "$((New-Guid).ToString('N')).zip"
    }

    Context 'LiteralPath Parameter Set' {
        It 'Creates a zip file at DestinationPath' {
            New-ADTZipFile -LiteralPath (Join-Path $script:SrcDir 'data.txt') -DestinationPath $script:ZipPath
            Test-Path -LiteralPath $script:ZipPath | Should -BeTrue
        }

        It 'Zip file contains the source file' {
            New-ADTZipFile -LiteralPath (Join-Path $script:SrcDir 'data.txt') -DestinationPath $script:ZipPath
            $zip = [System.IO.Compression.ZipFile]::OpenRead($script:ZipPath)
            try { $zip.Entries.Name | Should -Contain 'data.txt' }
            finally { $zip.Dispose() }
        }

        It 'Produces no pipeline output' {
            $result = New-ADTZipFile -LiteralPath (Join-Path $script:SrcDir 'data.txt') -DestinationPath $script:ZipPath
            $result | Should -BeNullOrEmpty
        }

        It 'Does not throw for a valid source file and destination' {
            { New-ADTZipFile -LiteralPath (Join-Path $script:SrcDir 'data.txt') -DestinationPath $script:ZipPath } | Should -Not -Throw
        }

        It 'Source file still exists when -RemoveSourceAfterArchiving is not specified' {
            $src = Join-Path $script:SrcDir 'data.txt'
            New-ADTZipFile -LiteralPath $src -DestinationPath $script:ZipPath
            Test-Path -LiteralPath $src | Should -BeTrue
        }
    }

    Context 'Path Parameter Set' {
        It 'Creates a zip file when using -Path' {
            New-ADTZipFile -Path (Join-Path $script:SrcDir 'data.txt') -DestinationPath $script:ZipPath
            Test-Path -LiteralPath $script:ZipPath | Should -BeTrue
        }
    }

    Context '-Force Switch' {
        It '-Force overwrites an existing zip file without throwing' {
            New-ADTZipFile -LiteralPath (Join-Path $script:SrcDir 'data.txt') -DestinationPath $script:ZipPath
            { New-ADTZipFile -LiteralPath (Join-Path $script:SrcDir 'extra.txt') -DestinationPath $script:ZipPath -Force } | Should -Not -Throw
        }

        It '-Force results in the new source file being in the zip' {
            New-ADTZipFile -LiteralPath (Join-Path $script:SrcDir 'data.txt')  -DestinationPath $script:ZipPath
            New-ADTZipFile -LiteralPath (Join-Path $script:SrcDir 'extra.txt') -DestinationPath $script:ZipPath -Force
            $zip = [System.IO.Compression.ZipFile]::OpenRead($script:ZipPath)
            try { $zip.Entries.Name | Should -Contain 'extra.txt' }
            finally { $zip.Dispose() }
        }
    }

    Context '-WhatIf Support' {
        It 'Does not create the zip file when -WhatIf is specified' {
            New-ADTZipFile -LiteralPath (Join-Path $script:SrcDir 'data.txt') -DestinationPath $script:ZipPath -WhatIf
            Test-Path -LiteralPath $script:ZipPath | Should -BeFalse
        }
    }

    Context 'Input Validation' {
        It 'Throws when LiteralPath is null' {
            { New-ADTZipFile -LiteralPath $null -DestinationPath $script:ZipPath } | Should -Throw
        }

        It 'Throws when DestinationPath is null' {
            { New-ADTZipFile -LiteralPath (Join-Path $script:SrcDir 'data.txt') -DestinationPath $null } | Should -Throw
        }
    }
}
