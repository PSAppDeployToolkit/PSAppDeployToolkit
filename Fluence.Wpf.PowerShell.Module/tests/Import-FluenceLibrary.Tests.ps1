#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

# Import-FluenceLibrary reuses the Fluence.Wpf assembly a host has already loaded. The test process
# has loaded the module's own staged build, so the "already loaded" branch is the one that runs
# here; the version comparison is exercised by pointing -ModuleRoot at a lib folder holding a stub
# Fluence.Wpf.dll compiled with a deliberately higher assembly version.

BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

    $script:Invoke = {
        param($root)
        & (Get-Module Fluence.Wpf.PowerShell) { param($r) Import-FluenceLibrary -ModuleRoot $r -WarningVariable w 3>$null; $w } $root
    }

    if ($PSEdition -eq 'Core') { $script:Tfm = 'net8.0-windows10.0.26100.0' } else { $script:Tfm = 'net472' }

    # A stub assembly named Fluence.Wpf with version 99.0.0.0, used only for GetAssemblyName.
    $script:NewerRoot = Join-Path $TestDrive 'newer'
    $newerLib = Join-Path $script:NewerRoot "lib/$script:Tfm"
    $null = New-Item -ItemType Directory -Path $newerLib -Force
    $stubSource = '[assembly: System.Reflection.AssemblyVersion("99.0.0.0")] public static class FluenceStub { }'
    Add-Type -TypeDefinition $stubSource -OutputAssembly (Join-Path $newerLib 'Fluence.Wpf.dll') -OutputType Library

    # A lib folder holding a copy of the staged build: same version as the loaded assembly.
    $script:SameRoot = Join-Path $TestDrive 'same'
    $sameLib = Join-Path $script:SameRoot "lib/$script:Tfm"
    $null = New-Item -ItemType Directory -Path $sameLib -Force
    $staged = Join-Path $PSScriptRoot "../src/Fluence.Wpf.PowerShell/lib/$script:Tfm/Fluence.Wpf.dll"
    Copy-Item -LiteralPath $staged -Destination $sameLib

    # No lib folder at all: the hosted-without-lib shape.
    $script:EmptyRoot = Join-Path $TestDrive 'empty'
    $null = New-Item -ItemType Directory -Path $script:EmptyRoot -Force
}

Describe 'Import-FluenceLibrary version check' {
    It 'warns when the loaded Fluence.Wpf is older than the staged build' {
        $warnings = & $script:Invoke $script:NewerRoot
        @($warnings).Count | Should -Be 1
        [string]$warnings[0] | Should -Match 'older than the 99\.0\.0\.0 build'
    }
    It 'stays silent when the loaded and staged versions match' {
        $warnings = & $script:Invoke $script:SameRoot
        @($warnings).Count | Should -Be 0
    }
    It 'stays silent when the module ships no staged build' {
        $warnings = & $script:Invoke $script:EmptyRoot
        @($warnings).Count | Should -Be 0
    }
}
