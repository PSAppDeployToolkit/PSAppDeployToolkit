BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTConfig' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality — initialized module' {
        BeforeAll {
            Initialize-ADTModule
        }

        It 'Returns a non-null value without throwing' {
            $result = Get-ADTConfig
            $result | Should -Not -BeNull
        }

        It 'Returns a Hashtable' {
            $result = Get-ADTConfig
            $result | Should -BeOfType ([System.Collections.Hashtable])
        }

        It 'Returned hashtable is non-empty' {
            $result = Get-ADTConfig
            $result.Count | Should -BeGreaterThan 0
        }

        It 'Contains the Toolkit configuration section' {
            $result = Get-ADTConfig
            $result.ContainsKey('Toolkit') | Should -BeTrue
        }

        It 'Contains the UI configuration section' {
            $result = Get-ADTConfig
            $result.ContainsKey('UI') | Should -BeTrue
        }

        It 'Contains the MSI configuration section' {
            $result = Get-ADTConfig
            $result.ContainsKey('MSI') | Should -BeTrue
        }

        It 'Contains the Assets configuration section' {
            $result = Get-ADTConfig
            $result.ContainsKey('Assets') | Should -BeTrue
        }

        It 'Returns the same object on successive calls (referential consistency)' {
            $first  = Get-ADTConfig
            $second = Get-ADTConfig
            # Both calls should return the same underlying hashtable reference.
            [System.Object]::ReferenceEquals($first, $second) | Should -BeTrue
        }
    }

    Context 'Functionality — uninitialized module' {
        BeforeEach {
            # Drive Config to null so Get-ADTConfig thinks the module is uninitialized.
            $m = Get-Module PSAppDeployToolkit
            $script:SavedConfig = & $m { $Script:ADT.Config }
            & $m { $Script:ADT.Config = $null }
        }

        AfterEach {
            # Restore original config so other tests are unaffected.
            $savedConfig = $script:SavedConfig
            $m = Get-Module PSAppDeployToolkit
            & $m { $Script:ADT.Config = $args[0] } $savedConfig
        }

        It 'Throws InvalidOperationException with ErrorId ADTConfigNotLoaded when config is null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.InvalidOperationException]
                ErrorId       = 'ADTConfigNotLoaded,Get-ADTConfig'
            }
            { Get-ADTConfig } | Should @shouldParams
        }

        It 'Error message mentions Initialize-ADTModule' {
            $err = $null
            try
            {
                Get-ADTConfig
            }
            catch
            {
                $err = $_
            }
            $err.Exception.Message | Should -Match 'Initialize-ADTModule'
        }
    }

    Context 'Input Validation' {
        It 'Has no non-common parameters' {
            (Get-Command Get-ADTConfig).Parameters.Keys |
                Where-Object { $_ -notin [System.Management.Automation.Cmdlet]::CommonParameters } |
                Should -BeNullOrEmpty
        }
    }

    Context 'Metadata' {
        It 'Is exported from the module' {
            (Get-Module PSAppDeployToolkit).ExportedFunctions.Keys | Should -Contain 'Get-ADTConfig'
        }

        It 'Declares OutputType of System.Collections.Hashtable' {
            $outputTypes = (Get-Command Get-ADTConfig).OutputType.Type
            $outputTypes | Should -Contain ([System.Collections.Hashtable])
        }
    }
}
