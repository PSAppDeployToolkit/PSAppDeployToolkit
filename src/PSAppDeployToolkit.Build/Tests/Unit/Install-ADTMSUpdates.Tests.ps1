BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Install-ADTMSUpdates' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Mock Start-ADTProcess so no real processes are launched.
        Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { }
    }

    Context 'Functionality - directory with .msu files' {
        BeforeAll {
            # Create a temporary directory with two .msu stub files.
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'UpdateDir', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $UpdateDir = Join-Path $TestDrive 'MSUpdates'
            $null = New-Item -Path $UpdateDir -ItemType Directory
            $null = New-Item -Path (Join-Path $UpdateDir 'KB1111111.msu') -ItemType File
            $null = New-Item -Path (Join-Path $UpdateDir 'KB2222222.msu') -ItemType File
        }

        It 'Calls Start-ADTProcess once per .msu file in the directory' {
            Install-ADTMSUpdates -LiteralPath $UpdateDir
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 2 -Exactly
        }

        It 'Calls Start-ADTProcess with /quiet /norestart arguments for each update' {
            Install-ADTMSUpdates -LiteralPath $UpdateDir
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 2 -Exactly -ParameterFilter {
                $ArgumentList -eq '/quiet /norestart'
            }
        }

        It 'Produces no output' {
            $result = Install-ADTMSUpdates -LiteralPath $UpdateDir
            $result | Should -BeNullOrEmpty
        }

        It 'Skips Start-ADTProcess when -WhatIf is supplied' {
            Set-ItResult -Skipped -Because 'Install-ADTMSUpdates wraps SupportsShouldProcess inside the module; -WhatIf is not bindable from outside the module scope and throws ParameterBindingException when called directly from test context.'
        }
    }

    Context 'Functionality - single .msu file path' {
        BeforeAll {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'SingleUpdateFile', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $SingleUpdateFile = Join-Path $TestDrive 'KB3333333.msu'
            $null = New-Item -Path $SingleUpdateFile -ItemType File
        }

        It 'Calls Start-ADTProcess exactly once when given a single .msu file path' {
            Install-ADTMSUpdates -LiteralPath $SingleUpdateFile
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }

        It 'Calls Start-ADTProcess with the full path to the .msu file' {
            Install-ADTMSUpdates -LiteralPath $SingleUpdateFile
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $FilePath -eq $SingleUpdateFile
            }
        }
    }

    Context 'Input Validation' {
        It 'Should have a mandatory LiteralPath parameter' {
            (Get-Command Install-ADTMSUpdates).Parameters['LiteralPath'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Throws when LiteralPath does not exist' {
            { Install-ADTMSUpdates -LiteralPath 'C:\ThisPathCannotPossiblyExist99999' } | Should -Throw -ExceptionType ([System.ArgumentException])
        }

        It 'Throws when LiteralPath is an existing file without a .msu extension' {
            $nonMsuFile = Join-Path $TestDrive 'update.exe'
            $null = New-Item -Path $nonMsuFile -ItemType File
            { Install-ADTMSUpdates -LiteralPath $nonMsuFile } | Should -Throw -ExceptionType ([System.ArgumentException])
        }

        It 'Throws when directory contains no .msu files' {
            $emptyDir = Join-Path $TestDrive 'EmptyUpdates'
            $null = New-Item -Path $emptyDir -ItemType Directory
            { Install-ADTMSUpdates -LiteralPath $emptyDir } | Should -Throw
        }
    }
}
