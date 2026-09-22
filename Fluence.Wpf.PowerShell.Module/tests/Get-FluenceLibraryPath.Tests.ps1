# Imported at discovery time, not in BeforeAll: InModuleScope below needs the module loaded
# while Pester is still discovering the cases.
Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

# The subject is a module-private helper. AGENTS.md forbids dot-sourcing a copy of one, so the
# cases run inside the imported module instead, where the private functions already resolve.
InModuleScope 'Fluence.Wpf.PowerShell' {
    BeforeAll {
        $script:root = Join-Path $TestDrive 'modroot'
        New-Item -ItemType Directory -Path (Join-Path $script:root 'lib/net472') -Force | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $script:root 'lib/net8.0-windows10.0.26100.0') -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $script:root 'lib/net472/Fluence.Wpf.dll') -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $script:root 'lib/net8.0-windows10.0.26100.0/Fluence.Wpf.dll') -Force | Out-Null
    }

    Describe 'Get-FluenceLibraryPath' {
        Context 'Edition selection' {
            It 'returns the net8.0-windows path for Core' {
                $p = Get-FluenceLibraryPath -ModuleRoot $script:root -Edition 'Core'
                $p | Should -Match 'net8\.0-windows'
            }
            It 'returns the net472 path for Desktop' {
                $p = Get-FluenceLibraryPath -ModuleRoot $script:root -Edition 'Desktop'
                $p | Should -Match 'net472'
            }
        }
        Context 'Missing assembly' {
            It 'throws when the dll is absent' {
                $empty = Join-Path $TestDrive 'empty'
                New-Item -ItemType Directory -Path $empty -Force | Out-Null
                { Get-FluenceLibraryPath -ModuleRoot $empty -Edition 'Core' } | Should -Throw '*not found*'
            }
        }
    }

}
