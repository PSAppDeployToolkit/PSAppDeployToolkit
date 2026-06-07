BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Remove-ADTContentFromCache' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Input Validation' {
        It 'Should have a non-mandatory LiteralPath parameter' {
            (Get-Command Remove-ADTContentFromCache).Parameters['LiteralPath'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }

        It 'Should throw ParameterArgumentValidationError when LiteralPath is null, empty or whitespace' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Remove-ADTContentFromCache'
            }
            { Remove-ADTContentFromCache -LiteralPath $null } | Should @shouldParams
            { Remove-ADTContentFromCache -LiteralPath '' } | Should @shouldParams
            { Remove-ADTContentFromCache -LiteralPath " `f`n`r`t`v" } | Should @shouldParams
        }
    }

    Context 'Session requirement' {
        It 'Throws InvalidOperationException when no ADT session is active' {
            { Remove-ADTContentFromCache -LiteralPath "$TestDrive\NoSession" } | Should -Throw -ExceptionType ([System.InvalidOperationException])
        }
    }

    Context 'Behavioural - cache folder does not exist' {
        BeforeAll {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'FakeScriptDir', Justification = 'Used inside It blocks.')]
            $FakeScriptDir = "$TestDrive\FakeScriptRemove"
            New-Item -Path "$FakeScriptDir\Files" -ItemType Directory -Force | Out-Null
            New-Item -Path "$FakeScriptDir\SupportFiles" -ItemType Directory -Force | Out-Null

            Mock -ModuleName PSAppDeployToolkit Get-ADTSession {
                return [PSCustomObject]@{
                    InstallName     = 'TestApp'
                    DirFiles        = "$FakeScriptDir\Files"
                    DirSupportFiles = "$FakeScriptDir\SupportFiles"
                    ScriptDirectory = @($FakeScriptDir)
                }
            }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig {
                return [PSCustomObject]@{
                    Toolkit = [PSCustomObject]@{ CachePath = "$TestDrive\CacheRemove" }
                }
            }
        }

        It 'Does not throw when the cache folder does not exist' {
            { Remove-ADTContentFromCache -LiteralPath "$TestDrive\NonExistentCache" } | Should -Not -Throw
        }

        It 'Logs a message and returns when the cache folder does not exist' {
            Remove-ADTContentFromCache -LiteralPath "$TestDrive\NonExistentCache2"
            Should -Invoke -CommandName Write-ADTLogEntry -ModuleName PSAppDeployToolkit -ParameterFilter {
                $Message -like '*does not exist*'
            }
        }

        It 'Produces no output when the cache folder does not exist' {
            $result = Remove-ADTContentFromCache -LiteralPath "$TestDrive\NonExistentCache3"
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Behavioural - cache folder exists' {
        BeforeAll {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'FakeScriptDir2', Justification = 'Used inside It blocks.')]
            $FakeScriptDir2 = "$TestDrive\FakeScriptRemove2"
            New-Item -Path "$FakeScriptDir2\Files" -ItemType Directory -Force | Out-Null
            New-Item -Path "$FakeScriptDir2\SupportFiles" -ItemType Directory -Force | Out-Null

            Mock -ModuleName PSAppDeployToolkit Get-ADTSession {
                return [PSCustomObject]@{
                    InstallName     = 'TestApp'
                    DirFiles        = "$FakeScriptDir2\Files"
                    DirSupportFiles = "$FakeScriptDir2\SupportFiles"
                    ScriptDirectory = @($FakeScriptDir2)
                }
            }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig {
                return [PSCustomObject]@{
                    Toolkit = [PSCustomObject]@{ CachePath = "$TestDrive\CacheRemoveExists" }
                }
            }
        }

        It 'Removes the cache folder when it exists' {
            $cacheToRemove = "$TestDrive\CacheExists\ToRemove"
            New-Item -Path $cacheToRemove -ItemType Directory -Force | Out-Null
            New-Item -Path "$cacheToRemove\dummy.txt" -ItemType File -Force | Out-Null

            Remove-ADTContentFromCache -LiteralPath $cacheToRemove
            Test-Path -LiteralPath $cacheToRemove -PathType Container | Should -BeFalse
        }

        It 'Produces no output when the cache folder is removed' {
            $cacheToRemove2 = "$TestDrive\CacheExists\ToRemove2"
            New-Item -Path $cacheToRemove2 -ItemType Directory -Force | Out-Null

            $result = Remove-ADTContentFromCache -LiteralPath $cacheToRemove2
            $result | Should -BeNullOrEmpty
        }

        It 'Reverts session DirFiles to the script directory path after removal' {
            $cacheToRemove3 = "$TestDrive\CacheExists\ToRemove3"
            New-Item -Path $cacheToRemove3 -ItemType Directory -Force | Out-Null

            $fakeSession = [PSCustomObject]@{
                InstallName     = 'TestApp'
                DirFiles        = $cacheToRemove3
                DirSupportFiles = $cacheToRemove3
                ScriptDirectory = @($FakeScriptDir2)
            }
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $fakeSession }

            Remove-ADTContentFromCache -LiteralPath $cacheToRemove3
            $fakeSession.DirFiles | Should -Be "$FakeScriptDir2\Files"
        }
    }
}
