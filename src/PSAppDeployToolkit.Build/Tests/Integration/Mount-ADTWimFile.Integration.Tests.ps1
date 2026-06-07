<#

.SYNOPSIS
Integration tests for Mount-ADTWimFile / Dismount-ADTWimFile against a real .wim image.

.DESCRIPTION
Builds a tiny real .wim (via New-ADTTestWim -> New-WindowsImage) from a temp payload folder
containing a marker file, mounts it with Mount-ADTWimFile, asserts the marker is readable at the
mount point and that the mount is reported, then dismounts (discarding changes) and asserts the
image is no longer mounted.

Capturing and mounting a Windows image requires Dism and an elevated session, so the whole Describe
is skipped unless the session is elevated. AfterAll force-dismounts any leftover mount (best-effort)
so an interrupted run cannot leave a stale mount on the machine.

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
    # Determine elevation at discovery time so the Describe-level -Skip can evaluate.
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'IsElevated', Justification = 'This variable is consumed by the Describe -Skip expression, which PSScriptAnalyzer has no visibility of.')]
    $IsElevated = ([System.Security.Principal.WindowsPrincipal]::new([System.Security.Principal.WindowsIdentity]::GetCurrent())).IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)
}

Describe 'Mount-ADTWimFile' -Tag Integration -Skip:(-not $IsElevated) {
    BeforeAll {
        # Import the BUILT module (compiled output under Artifacts\ModuleOnly), mirroring the
        # integration sample. Fall back to the source manifest when no build output exists so the
        # test remains runnable on a dev box without a prior Build step.
        $ModuleName = 'PSAppDeployToolkit'
        $builtManifest = [System.IO.Path]::Combine($PSScriptRoot, '..', '..', '..', 'Artifacts', 'ModuleOnly', $ModuleName, "$ModuleName.psd1")
        $sourceManifest = [System.IO.Path]::Combine($PSScriptRoot, '..', '..', '..', $ModuleName, "$ModuleName.psd1")
        $manifest = if (Test-Path -LiteralPath $builtManifest) { $builtManifest } else { $sourceManifest }
        Get-Module -Name $ModuleName -ErrorAction SilentlyContinue | Remove-Module -Force
        Import-Module -Name $manifest -Force
        $script:AdtModule = Get-Module -Name $ModuleName

        # Import the shared fixture toolkit (authors the .wim).
        Import-Module -Name ([System.IO.Path]::Combine($PSScriptRoot, '..', 'Support', 'TestFixtures.psm1')) -Force

        # Author a tiny .wim from a payload folder containing a marker file, all under a namespaced
        # temp root.
        $script:TestRoot = [System.IO.Path]::Combine($env:TEMP, 'PSADT.Test', "Wim_$([System.Guid]::NewGuid().ToString('N'))")
        $script:PayloadDir = [System.IO.Path]::Combine($script:TestRoot, 'payload')
        $script:MountDir = [System.IO.Path]::Combine($script:TestRoot, 'mount')
        $null = [System.IO.Directory]::CreateDirectory($script:PayloadDir)
        $null = [System.IO.Directory]::CreateDirectory($script:MountDir)
        $script:MarkerName = 'marker.txt'
        $script:MarkerContent = 'PSADT WIM MARKER'
        [System.IO.File]::WriteAllText([System.IO.Path]::Combine($script:PayloadDir, $script:MarkerName), $script:MarkerContent)
        $script:WimPath = [System.IO.Path]::Combine($script:TestRoot, 'image.wim')
        $null = New-ADTTestWim -SourceFolder $script:PayloadDir -Path $script:WimPath
    }

    AfterAll {
        # Best-effort: force-dismount any leftover mount and remove the temp tree. Every step is
        # wrapped and ignores errors so cleanup can never fail the run.
        if ($script:MountDir)
        {
            try { Dismount-ADTWimFile -Path $script:MountDir -ErrorAction SilentlyContinue } catch { $null = $_ }
            try { $null = Get-WindowsImage -Mounted -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $script:MountDir } | ForEach-Object { Dismount-WindowsImage -Path $_.Path -Discard -ErrorAction SilentlyContinue } } catch { $null = $_ }
        }
        try { if ($script:TestRoot -and (Test-Path -LiteralPath $script:TestRoot)) { Remove-Item -LiteralPath $script:TestRoot -Recurse -Force -ErrorAction SilentlyContinue } } catch { $null = $_ }
    }

    Context 'Mount then dismount (ordered lifecycle)' {
        It 'authored a real .wim file on disk' {
            Test-Path -LiteralPath $script:WimPath | Should -BeTrue
            (Get-Item -LiteralPath $script:WimPath).Length | Should -BeGreaterThan 0
        }

        It 'mounts the image without throwing' {
            { Mount-ADTWimFile -ImagePath $script:WimPath -Path $script:MountDir -Index 1 } | Should -Not -Throw
        }

        It 'exposes the marker file (readable) at the mount point' {
            $mounted = [System.IO.Path]::Combine($script:MountDir, $script:MarkerName)
            Test-Path -LiteralPath $mounted | Should -BeTrue
            (Get-Content -LiteralPath $mounted -Raw).Trim() | Should -Be $script:MarkerContent
        }

        It 'reports the mount via Get-WindowsImage -Mounted' {
            $entry = Get-WindowsImage -Mounted | Where-Object { $_.Path -eq $script:MountDir }
            $entry | Should -Not -BeNullOrEmpty
            $entry.ImagePath | Should -Be $script:WimPath
        }

        It 'reports the mount via the private Get-ADTMountedWimFile' {
            # Get-ADTMountedWimFile is a private (non-exported) function; reach it within module scope.
            $mountPath = $script:MountDir
            $entry = & $script:AdtModule { param($p) Get-ADTMountedWimFile -Path $p } $mountPath
            $entry | Should -Not -BeNullOrEmpty
        }

        It 'dismounts the image without throwing' {
            { Dismount-ADTWimFile -Path $script:MountDir } | Should -Not -Throw
        }

        It 'no longer reports the image as mounted after dismount' {
            $entry = Get-WindowsImage -Mounted | Where-Object { $_.Path -eq $script:MountDir }
            $entry | Should -BeNullOrEmpty
        }
    }
}
