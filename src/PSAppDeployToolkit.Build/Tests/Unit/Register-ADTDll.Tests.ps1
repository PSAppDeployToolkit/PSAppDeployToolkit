BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Register-ADTDll' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Mock Invoke-ADTRegSvr32 so no real regsvr32 invocation occurs.
        Mock -ModuleName PSAppDeployToolkit Invoke-ADTRegSvr32 { }

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'DllPath', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $DllPath = Join-Path $TestDrive 'test.dll'
        [System.IO.File]::WriteAllBytes($DllPath, [System.Byte[]]@())
    }

    Context 'Functionality' {
        It 'Invokes Invoke-ADTRegSvr32 with Action Register' {
            Register-ADTDll -FilePath $DllPath
            Should -Invoke -CommandName Invoke-ADTRegSvr32 -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Action -eq 'Register'
            }
        }

        It 'Forwards the FilePath to Invoke-ADTRegSvr32' {
            Register-ADTDll -FilePath $DllPath
            Should -Invoke -CommandName Invoke-ADTRegSvr32 -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $FilePath -eq $DllPath
            }
        }

        It 'Produces no output' {
            $result = Register-ADTDll -FilePath $DllPath
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Should have a mandatory FilePath parameter' {
            (Get-Command Register-ADTDll).Parameters['FilePath'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Throws when the DLL file does not exist' {
            { Register-ADTDll -FilePath (Join-Path $TestDrive 'nonexistent.dll') } | Should -Throw
        }
    }
}
