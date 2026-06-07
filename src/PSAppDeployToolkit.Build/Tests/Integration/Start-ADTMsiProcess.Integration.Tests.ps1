<#

.SYNOPSIS
Integration tests for Start-ADTMsiProcess: a full install -> verify -> uninstall -> verify-removed
cycle against a real, genuinely-installing MSI authored by New-ADTTestInstallMsi.

.DESCRIPTION
These tests mutate the real machine (they install and uninstall an MSI under msiexec, writing a
registry value under HKLM and a file under %ProgramData%), so the whole Describe is skipped unless
the session is elevated. All artifacts live under a dedicated 'PSADT.Test' namespace and AfterAll
performs best-effort cleanup (msiexec /x by ProductCode plus manual artifact removal) so a mid-run
failure cannot leave the machine in a dirty state.

This file is tagged 'Integration' and is collected only by Invoke-ADTPesterIntegrationTesting
(Run.Path = Tests\Integration). The unit runner points at Tests\Unit and never collects it.

PSAppDeployToolkit is licensed under the GNU LGPLv3 License - © 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).

This program is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the
Free Software Foundation, either version 3 of the License, or any later version. This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License
for more details. You should have received a copy of the GNU Lesser General Public License along with this program. If not, see <http://www.gnu.org/licenses/>.

.LINK
https://psappdeploytoolkit.com

#>

BeforeDiscovery {
    # Determine elevation at discovery time so the Describe-level -Skip can evaluate. We avoid the
    # module here (it may not be imported yet) and use the framework directly.
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'IsElevated', Justification = 'This variable is consumed by the Describe -Skip expression, which PSScriptAnalyzer has no visibility of.')]
    $IsElevated = ([System.Security.Principal.WindowsPrincipal]::new([System.Security.Principal.WindowsIdentity]::GetCurrent())).IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)
}

Describe 'Start-ADTMsiProcess' -Tag Integration -Skip:(-not $IsElevated) {
    BeforeAll {
        # Import the BUILT module the same way the integration sample documents (the compiled module
        # under Artifacts). Mirror the sample's relative-to-Artifacts approach, but resolve to the
        # real build output location (Artifacts\ModuleOnly\PSAppDeployToolkit). If no build output
        # exists (e.g. running this file directly on a dev box without a prior Build step), fall back
        # to the source manifest so the integration test is still runnable.
        $ModuleName = 'PSAppDeployToolkit'
        $builtManifest = [System.IO.Path]::Combine($PSScriptRoot, '..', '..', '..', 'Artifacts', 'ModuleOnly', $ModuleName, "$ModuleName.psd1")
        $sourceManifest = [System.IO.Path]::Combine($PSScriptRoot, '..', '..', '..', $ModuleName, "$ModuleName.psd1")
        $manifest = if (Test-Path -LiteralPath $builtManifest) { $builtManifest } else { $sourceManifest }
        Get-Module -Name $ModuleName -ErrorAction SilentlyContinue | Remove-Module -Force
        Import-Module -Name $manifest -Force

        # Import the shared fixture toolkit (authors the installing MSI).
        Import-Module -Name ([System.IO.Path]::Combine($PSScriptRoot, '..', 'Support', 'TestFixtures.psm1')) -Force

        # Author the installing MSI into a dedicated namespaced temp folder.
        $script:TestRoot = [System.IO.Path]::Combine($env:TEMP, 'PSADT.Test', "MsiProcess_$([System.Guid]::NewGuid().ToString('N'))")
        $null = [System.IO.Directory]::CreateDirectory($script:TestRoot)
        $script:MsiPath = [System.IO.Path]::Combine($script:TestRoot, 'PSADTTestApp.msi')
        $script:Msi = New-ADTTestInstallMsi -Path $script:MsiPath

        # Best-effort: make sure no stale registration from a previous interrupted run exists.
        $null = Start-Process -FilePath msiexec.exe -ArgumentList '/x', $script:Msi.ProductCode, '/qn' -Wait -PassThru -ErrorAction SilentlyContinue
    }

    AfterAll {
        # Best-effort cleanup so a mid-run failure cannot poison the machine. Every step is wrapped
        # and ignores errors.
        if ($script:Msi)
        {
            try { $null = Start-Process -FilePath msiexec.exe -ArgumentList '/x', $script:Msi.ProductCode, '/qn' -Wait -PassThru -ErrorAction SilentlyContinue } catch { $null = $_ }
            try { if (Test-Path -LiteralPath $script:Msi.RegistryKey) { Remove-Item -LiteralPath $script:Msi.RegistryKey -Recurse -Force -ErrorAction SilentlyContinue } } catch { $null = $_ }
            try { if (Test-Path -LiteralPath $script:Msi.InstallFolder) { Remove-Item -LiteralPath $script:Msi.InstallFolder -Recurse -Force -ErrorAction SilentlyContinue } } catch { $null = $_ }
        }
        try { if ($script:TestRoot -and (Test-Path -LiteralPath $script:TestRoot)) { Remove-Item -LiteralPath $script:TestRoot -Recurse -Force -ErrorAction SilentlyContinue } } catch { $null = $_ }
        # Remove the namespaced parent keys/folders only if now empty (do not clobber concurrent runs' siblings).
        try { foreach ($k in 'HKLM:\SOFTWARE\PSADT.Test', 'HKLM:\SOFTWARE\WOW6432Node\PSADT.Test') { if ((Test-Path -LiteralPath $k) -and -not (Get-ChildItem -LiteralPath $k -ErrorAction SilentlyContinue)) { Remove-Item -LiteralPath $k -Force -ErrorAction SilentlyContinue } } } catch { $null = $_ }
    }

    Context 'Install then uninstall (ordered lifecycle)' {
        It 'installs the MSI with a success (zero) exit code' {
            $result = Start-ADTMsiProcess -Action Install -FilePath $script:MsiPath -PassThru
            $result.ExitCode | Should -Be 0
        }

        It 'creates the HKLM registry marker value on install' {
            Test-Path -LiteralPath $script:Msi.RegistryKey | Should -BeTrue
            (Get-ItemProperty -LiteralPath $script:Msi.RegistryKey).($script:Msi.RegistryValueName) | Should -Be '1'
        }

        It 'creates the namespaced file on disk on install' {
            Test-Path -LiteralPath $script:Msi.InstalledFile | Should -BeTrue
        }

        It 'is discoverable via Get-ADTApplication after install' {
            $app = Get-ADTApplication -ProductCode $script:Msi.ProductCode
            $app | Should -Not -BeNullOrEmpty
            $app.DisplayName | Should -Be $script:Msi.ProductName
        }

        It 'uninstalls the MSI with a success (zero) exit code' {
            $result = Start-ADTMsiProcess -Action Uninstall -FilePath $script:MsiPath -PassThru
            $result.ExitCode | Should -Be 0
        }

        It 'removes the HKLM registry marker on uninstall' {
            Test-Path -LiteralPath $script:Msi.RegistryKey | Should -BeFalse
        }

        It 'removes the namespaced file and folder on uninstall' {
            Test-Path -LiteralPath $script:Msi.InstalledFile | Should -BeFalse
            Test-Path -LiteralPath $script:Msi.InstallFolder | Should -BeFalse
        }

        It 'is no longer discoverable via Get-ADTApplication after uninstall' {
            Get-ADTApplication -ProductCode $script:Msi.ProductCode | Should -BeNullOrEmpty
        }
    }

    Context 'SIMULATEFAIL=1 forces a non-zero result and leaves no artifacts' {
        It 'returns a non-zero exit code when -PassThru -ErrorAction SilentlyContinue is used' {
            $result = Start-ADTMsiProcess -Action Install -FilePath $script:MsiPath -AdditionalArgumentList 'SIMULATEFAIL=1' -PassThru -ErrorAction SilentlyContinue
            $result.ExitCode | Should -Not -Be 0
        }

        It 'throws a terminating error under the default -ErrorAction' {
            { Start-ADTMsiProcess -Action Install -FilePath $script:MsiPath -AdditionalArgumentList 'SIMULATEFAIL=1' } | Should -Throw
        }

        It 'leaves no registry marker behind after a simulated failure' {
            Test-Path -LiteralPath $script:Msi.RegistryKey | Should -BeFalse
        }

        It 'leaves no file behind after a simulated failure' {
            Test-Path -LiteralPath $script:Msi.InstalledFile | Should -BeFalse
        }

        It 'does not register the product after a simulated failure' {
            Get-ADTApplication -ProductCode $script:Msi.ProductCode | Should -BeNullOrEmpty
        }
    }
}
