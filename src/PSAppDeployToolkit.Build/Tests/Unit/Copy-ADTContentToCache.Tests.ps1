BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Copy-ADTContentToCache' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Input Validation' {
        It 'Should have a non-mandatory LiteralPath parameter' {
            (Get-Command Copy-ADTContentToCache).Parameters['LiteralPath'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }

        It 'Should have a non-mandatory Exclude parameter' {
            (Get-Command Copy-ADTContentToCache).Parameters['Exclude'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }

        It 'Should throw ParameterArgumentValidationError when LiteralPath is null, empty or whitespace' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Copy-ADTContentToCache'
            }
            { Copy-ADTContentToCache -LiteralPath $null } | Should @shouldParams
            { Copy-ADTContentToCache -LiteralPath '' } | Should @shouldParams
            { Copy-ADTContentToCache -LiteralPath " `f`n`r`t`v" } | Should @shouldParams
        }

        It 'Should throw ParameterArgumentValidationError when Exclude contains an invalid value' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return [PSCustomObject]@{ InstallName = 'TestApp'; DirFiles = $null; DirSupportFiles = $null; ScriptDirectory = @() } }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig { return [PSCustomObject]@{ Toolkit = [PSCustomObject]@{ CachePath = "$TestDrive\Cache" } } }
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Copy-ADTContentToCache'
            }
            { Copy-ADTContentToCache -LiteralPath "$TestDrive\Cache\TestApp" -Exclude 'InvalidValue' } | Should @shouldParams
        }

        It 'Should throw when all possible Exclude values are specified (nothing to copy)' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return [PSCustomObject]@{ InstallName = 'TestApp'; DirFiles = $null; DirSupportFiles = $null; ScriptDirectory = @() } }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig { return [PSCustomObject]@{ Toolkit = [PSCustomObject]@{ CachePath = "$TestDrive\Cache" } } }
            { Copy-ADTContentToCache -LiteralPath "$TestDrive\Cache\TestApp" -Exclude Files, SupportFiles, Toolkit } | Should -Throw
        }
    }

    Context 'Session requirement' {
        It 'Throws InvalidOperationException when no ADT session is active' {
            { Copy-ADTContentToCache -LiteralPath "$TestDrive\CacheNoSession" } | Should -Throw -ExceptionType ([System.InvalidOperationException])
        }
    }

    Context 'Behavioural - copy to cache' {
        BeforeAll {
            # Build a fake scriptDir under TestDrive with Files and SupportFiles sub-folders.
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'FakeScriptDir', Justification = 'Used inside It blocks.')]
            $FakeScriptDir = "$TestDrive\FakeScript"
            New-Item -Path "$FakeScriptDir\Files" -ItemType Directory -Force | Out-Null
            New-Item -Path "$FakeScriptDir\SupportFiles" -ItemType Directory -Force | Out-Null
            New-Item -Path "$FakeScriptDir\Files\sample.txt" -ItemType File -Force | Out-Null
            Set-Content -Path "$FakeScriptDir\Files\sample.txt" -Value 'hello'

            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'FakeCachePath', Justification = 'Used inside It blocks.')]
            $FakeCachePath = "$TestDrive\Cache\TestApp"

            Mock -ModuleName PSAppDeployToolkit Get-ADTSession {
                return [PSCustomObject]@{
                    InstallName      = 'TestApp'
                    DirFiles         = "$FakeScriptDir\Files"
                    DirSupportFiles  = "$FakeScriptDir\SupportFiles"
                    ScriptDirectory  = @($FakeScriptDir)
                }
            }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig {
                return [PSCustomObject]@{
                    Toolkit = [PSCustomObject]@{ CachePath = "$TestDrive\Cache" }
                }
            }
            # Mock Copy-ADTFile to intercept copies without touching the filesystem.
            Mock -ModuleName PSAppDeployToolkit Copy-ADTFile { }
        }

        It 'Does not throw when invoked with a valid LiteralPath and an active session' {
            { Copy-ADTContentToCache -LiteralPath $FakeCachePath } | Should -Not -Throw
        }

        It 'Creates the cache folder when it does not exist' {
            $newCache = "$TestDrive\Cache\BrandNew"
            Copy-ADTContentToCache -LiteralPath $newCache
            Test-Path -LiteralPath $newCache -PathType Container | Should -BeTrue
        }

        It 'Invokes Copy-ADTFile when the cache and script directories differ' {
            $distinctCache = "$TestDrive\Cache\DistinctRun"
            Copy-ADTContentToCache -LiteralPath $distinctCache
            Should -Invoke -CommandName Copy-ADTFile -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Destination -eq $distinctCache
            }
        }

        It 'Invokes Copy-ADTFile only for Files when Exclude contains SupportFiles and Toolkit' {
            $excludeCache = "$TestDrive\Cache\ExcludeRun"
            Copy-ADTContentToCache -LiteralPath $excludeCache -Exclude SupportFiles, Toolkit
            Should -Invoke -CommandName Copy-ADTFile -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $LiteralPath -like '*\Files'
            }
            Should -Invoke -CommandName Copy-ADTFile -ModuleName PSAppDeployToolkit -Times 0 -ParameterFilter {
                $LiteralPath -like '*\SupportFiles'
            }
        }

        It 'Produces no output' {
            $result = Copy-ADTContentToCache -LiteralPath "$TestDrive\Cache\NoOut"
            $result | Should -BeNullOrEmpty
        }
    }
}
