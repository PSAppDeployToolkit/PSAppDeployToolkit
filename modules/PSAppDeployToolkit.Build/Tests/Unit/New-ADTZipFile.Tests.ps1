BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'New-ADTZipFile' {
    BeforeAll {
        function Get-ArchiveEntry
        {
            [CmdletBinding()]
            [OutputType([System.String])]
            param
            (
                [Parameter(Mandatory = $true)]
                [System.String]$Path
            )

            $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
            try
            {
                return $archive.Entries.FullName
            }
            finally
            {
                $archive.Dispose()
            }
        }
    }

    Context 'Creating an archive' {
        BeforeEach {
            $script:Source = "$TestDrive\Source"
            $script:Destination = "$TestDrive\archive.zip"
            Remove-Item -LiteralPath $script:Source, $script:Destination -Recurse -Force -ErrorAction Ignore
            $null = New-Item -Path $script:Source -ItemType Directory -Force
            Set-Content -LiteralPath "$script:Source\one.txt" -Value 'first'
            Set-Content -LiteralPath "$script:Source\two.txt" -Value 'second'
        }

        It 'Writes the archive it was asked for' {
            New-ADTZipFile -LiteralPath $script:Source -DestinationPath $script:Destination
            Test-Path -LiteralPath $script:Destination -PathType Leaf | Should -BeTrue
        }

        It 'Puts the source content inside it' {
            New-ADTZipFile -Path "$script:Source\*" -DestinationPath $script:Destination
            Get-ArchiveEntry -Path $script:Destination | Should -Contain 'one.txt'
            Get-ArchiveEntry -Path $script:Destination | Should -Contain 'two.txt'
        }

        It 'Replaces an existing archive when forced' {
            New-ADTZipFile -Path "$script:Source\*" -DestinationPath $script:Destination
            Remove-Item -LiteralPath "$script:Source\two.txt" -Force
            New-ADTZipFile -Path "$script:Source\*" -DestinationPath $script:Destination -Force
            Get-ArchiveEntry -Path $script:Destination | Should -Not -Contain 'two.txt'
        }

        It 'Refuses to overwrite an existing archive otherwise' {
            # Silently replacing an archive would lose whatever a previous step had put in it.
            New-ADTZipFile -Path "$script:Source\*" -DestinationPath $script:Destination
            { New-ADTZipFile -Path "$script:Source\*" -DestinationPath $script:Destination } | Should -Throw
        }

        It 'Adds to an existing archive when updating' {
            New-ADTZipFile -Path "$script:Source\one.txt" -DestinationPath $script:Destination
            New-ADTZipFile -Path "$script:Source\two.txt" -DestinationPath $script:Destination -Update
            @(Get-ArchiveEntry -Path $script:Destination).Count | Should -Be 2
        }

        It 'Honours the compression level it was given' {
            # The level is passed straight through to Compress-Archive, so an uncompressed archive of
            # highly compressible content has to come out substantially larger.
            $bulky = "$script:Source\bulky.txt"
            [System.IO.File]::WriteAllText($bulky, [System.String]::new('A', 200kb))
            New-ADTZipFile -Path $bulky -DestinationPath "$TestDrive\optimal.zip" -CompressionLevel Optimal
            New-ADTZipFile -Path $bulky -DestinationPath "$TestDrive\none.zip" -CompressionLevel NoCompression
            (Get-Item -LiteralPath "$TestDrive\none.zip").Length | Should -BeGreaterThan (Get-Item -LiteralPath "$TestDrive\optimal.zip").Length
        }
    }

    Context 'Removing the source afterwards' {
        BeforeEach {
            $script:Source = "$TestDrive\Consumed"
            $script:Destination = "$TestDrive\consumed.zip"
            Remove-Item -LiteralPath $script:Source, $script:Destination -Recurse -Force -ErrorAction Ignore
            $null = New-Item -Path $script:Source -ItemType Directory -Force
            Set-Content -LiteralPath "$script:Source\one.txt" -Value 'first'
        }

        It 'Leaves the source alone by default' {
            New-ADTZipFile -LiteralPath $script:Source -DestinationPath $script:Destination
            Test-Path -LiteralPath $script:Source | Should -BeTrue
        }

        It 'Deletes the source when asked to' {
            # Used when archiving a staging folder that has no reason to survive the archive.
            New-ADTZipFile -LiteralPath $script:Source -DestinationPath $script:Destination -RemoveSourceAfterArchiving
            Test-Path -LiteralPath $script:Source | Should -BeFalse
        }

        It 'Keeps the archive it just wrote' {
            New-ADTZipFile -LiteralPath $script:Source -DestinationPath $script:Destination -RemoveSourceAfterArchiving
            Test-Path -LiteralPath $script:Destination -PathType Leaf | Should -BeTrue
        }
    }

    Context 'WhatIf' {
        It 'Writes nothing' {
            $source = "$TestDrive\Untouched"
            $null = New-Item -Path $source -ItemType Directory -Force
            Set-Content -LiteralPath "$source\one.txt" -Value 'first'
            New-ADTZipFile -LiteralPath $source -DestinationPath "$TestDrive\untouched.zip" -WhatIf
            Test-Path -LiteralPath "$TestDrive\untouched.zip" | Should -BeFalse
        }

        It 'Leaves the source alone even when told to remove it' {
            $source = "$TestDrive\StillHere"
            $null = New-Item -Path $source -ItemType Directory -Force
            Set-Content -LiteralPath "$source\one.txt" -Value 'first'
            New-ADTZipFile -LiteralPath $source -DestinationPath "$TestDrive\stillhere.zip" -RemoveSourceAfterArchiving -WhatIf
            Test-Path -LiteralPath $source | Should -BeTrue
        }
    }

    Context 'Input Validation' {
        It 'Refuses the same source twice' {
            { New-ADTZipFile -LiteralPath "$TestDrive\a", "$TestDrive\a" -DestinationPath "$TestDrive\dupe.zip" } | Should -Throw -ErrorId 'ParameterArgumentValidationError,New-ADTZipFile'
        }

        It 'Refuses a blank destination' {
            { New-ADTZipFile -LiteralPath $TestDrive -DestinationPath '   ' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,New-ADTZipFile'
        }

        It 'Refuses a compression level it cannot pass on' {
            { New-ADTZipFile -LiteralPath $TestDrive -DestinationPath "$TestDrive\bad.zip" -CompressionLevel 'Maximum' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Refuses a wildcard source and a literal one together' {
            { New-ADTZipFile -Path "$TestDrive\a" -LiteralPath "$TestDrive\b" -DestinationPath "$TestDrive\both.zip" } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
