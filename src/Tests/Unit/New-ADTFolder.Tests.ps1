BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'New-ADTFolder' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should create a new folder if the LiteralPath provided does not exist' {
            New-ADTFolder -LiteralPath "$TestDrive\NewFolder"
            Test-Path -LiteralPath "$TestDrive\NewFolder" -PathType Container | Should -BeTrue
        }
        It 'Does not overwrite an existing folder' {
            New-Item -Path "$TestDrive\NewFolder\test.txt" -ItemType File -Force | Out-Null
            Set-Content -Path "$TestDrive\NewFolder\test.txt" -Value 'original content'

            New-ADTFolder -LiteralPath "$TestDrive\NewFolder"

            Test-Path -LiteralPath "$TestDrive\NewFolder" -PathType Container | Should -BeTrue

            "$TestDrive\NewFolder\test.txt" | Should -FileContentMatchExactly '^original content$'
        }
        It 'Should pass through the new folder' {
            # When the folder doesn't already exist
            $item = New-ADTFolder -LiteralPath "$TestDrive\NewNewFolder" -PassThru
            $item | Should -BeOfType ([System.IO.DirectoryInfo])
            $item.FullName | Should -Be "$TestDrive\NewNewFolder"
            $item.Exists | Should -BeTrue

            # When the folder already exists
            $item2 = New-ADTFolder -LiteralPath "$TestDrive\NewNewFolder" -PassThru
            $item2 | Should -BeOfType ([System.IO.DirectoryInfo])
            $item2.FullName | Should -Be "$TestDrive\NewNewFolder"
            $item2.Exists | Should -BeTrue
        }
    }

    Context 'Input Validation' {
        It 'Should verify that LiteralPath is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,New-ADTFolder'
            }
            { New-ADTFolder -LiteralPath $null } | Should @shouldParams
            { New-ADTFolder -LiteralPath '' } | Should @shouldParams
            { New-ADTFolder -LiteralPath " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should verify that LiteralPath is a valid path' {
            $invalidFileName = [System.IO.Path]::GetInvalidFileNameChars() -join ''
            { New-ADTFolder -LiteralPath $invalidFileName } | Should -Throw -ExceptionType ([System.Management.Automation.DriveNotFoundException]) -ErrorId 'DriveNotFound,New-ADTFolder'
            { New-ADTFolder -LiteralPath "$Env:SystemDrive\$invalidFileName" } | Should -Throw -ExceptionType ([System.ArgumentException]) -ExpectedMessage 'Illegal characters in path.' -ErrorId 'System.ArgumentException,Microsoft.PowerShell.Commands.TestPathCommand'
        }
        It 'Should accept pipeline input for the LiteralPath parameter' {
            { [PSCustomObject]@{ LiteralPath = "$TestDrive\NewFolder" } | New-ADTFolder } | Should -Not -Throw
            { [PSCustomObject]@{ Path = "$TestDrive\NewFolder" } | New-ADTFolder } | Should -Not -Throw
            { [PSCustomObject]@{ PSPath = "$TestDrive\NewFolder" } | New-ADTFolder } | Should -Not -Throw
        }
    }
}
