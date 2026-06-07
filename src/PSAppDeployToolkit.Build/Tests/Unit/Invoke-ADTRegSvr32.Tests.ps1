BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Invoke-ADTRegSvr32' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Mock Start-ADTProcess so that no real regsvr32 invocation occurs.
        Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { }

        # Mock Get-ADTPEFileArchitecture to return AMD64 so the bitness path is deterministic.
        Mock -ModuleName PSAppDeployToolkit Get-ADTPEFileArchitecture {
            return [PSADT.Interop.IMAGE_FILE_MACHINE]::IMAGE_FILE_MACHINE_AMD64
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'DllPath', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $DllPath = Join-Path $TestDrive 'test.dll'
        [System.IO.File]::WriteAllBytes($DllPath, [System.Byte[]]@())
    }

    Context 'Functionality' {
        It 'Invokes Start-ADTProcess when registering a DLL' {
            Invoke-ADTRegSvr32 -FilePath $DllPath -Action Register
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }

        It 'Passes /s without /u and targets regsvr32.exe when registering' {
            Invoke-ADTRegSvr32 -FilePath $DllPath -Action Register
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                ($ArgumentList -match '/s') -and ($ArgumentList -notmatch '/u') -and ($FilePath -match 'regsvr32\.exe$') -and ($SuccessExitCodes -contains 0)
            }
        }

        It 'Passes /s and /u and targets regsvr32.exe when unregistering' {
            Invoke-ADTRegSvr32 -FilePath $DllPath -Action Unregister
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                ($ArgumentList -match '/s') -and ($ArgumentList -match '/u') -and ($FilePath -match 'regsvr32\.exe$') -and ($SuccessExitCodes -contains 0)
            }
        }

        It 'Includes the DLL file path in the argument list for Register' {
            Invoke-ADTRegSvr32 -FilePath $DllPath -Action Register
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $ArgumentList -match [System.Text.RegularExpressions.Regex]::Escape($DllPath)
            }
        }

        It 'Includes the DLL file path in the argument list for Unregister' {
            Invoke-ADTRegSvr32 -FilePath $DllPath -Action Unregister
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $ArgumentList -match [System.Text.RegularExpressions.Regex]::Escape($DllPath)
            }
        }

        It 'Produces no output' {
            $result = Invoke-ADTRegSvr32 -FilePath $DllPath -Action Register
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Should have a mandatory FilePath parameter' {
            (Get-Command Invoke-ADTRegSvr32).Parameters['FilePath'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Should have a mandatory Action parameter' {
            (Get-Command Invoke-ADTRegSvr32).Parameters['Action'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Throws InvalidFilePathParameterValue when the DLL file does not exist' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.ArgumentException]
                ErrorId       = 'InvalidFilePathParameterValue,Invoke-ADTRegSvr32'
            }
            { Invoke-ADTRegSvr32 -FilePath (Join-Path $TestDrive 'nonexistent.dll') -Action Register } | Should @shouldParams
        }

        It 'Throws ParameterArgumentValidationError when Action is not Register or Unregister' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Invoke-ADTRegSvr32'
            }
            { Invoke-ADTRegSvr32 -FilePath $DllPath -Action 'Install' } | Should @shouldParams
        }
    }
}
