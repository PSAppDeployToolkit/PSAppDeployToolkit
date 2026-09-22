# Imported at discovery time, not in BeforeAll: InModuleScope below needs the module loaded
# while Pester is still discovering the cases.
Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }
# The subject is a module-private helper. AGENTS.md forbids dot-sourcing a copy of one, so the
# cases run inside the imported module instead, where the private functions already resolve.
InModuleScope 'Fluence.Wpf.PowerShell' {
    BeforeAll {
        $script:Png = Join-Path $TestDrive 'logo.png'
        [System.IO.File]::WriteAllBytes($script:Png, [byte[]](0x89, 0x50, 0x4E, 0x47))
    }

    Describe 'Resolve-FluenceImageSource' {
        It 'turns an existing file path into a file: URI' {
            $r = Resolve-FluenceImageSource -Image $script:Png
            $r | Should -Match '^file:///'
            ([System.Uri]$r).LocalPath | Should -Be $script:Png
        }
        It 'accepts an existing file: URI' {
            $r = Resolve-FluenceImageSource -Image ([System.Uri]$script:Png).AbsoluteUri
            ([System.Uri]$r).LocalPath | Should -Be $script:Png
        }
        It 'passes a pack: URI through' {
            $pack = 'pack://application:,,,/Contoso.Branding;component/Assets/logo.png'
            Resolve-FluenceImageSource -Image $pack | Should -Be $pack
        }
        It 'rejects a missing file' {
            { Resolve-FluenceImageSource -Image (Join-Path $TestDrive 'missing.png') } | Should -Throw '*not found*'
        }
        It 'rejects the <Scheme> scheme' -ForEach @(
            @{ Scheme = 'https'; Value = 'https://example.com/logo.png' }
            @{ Scheme = 'http'; Value = 'http://example.com/logo.png' }
            @{ Scheme = 'data'; Value = 'data:image/png;base64,iVBORw0KGgo=' }
            @{ Scheme = 'ftp'; Value = 'ftp://example.com/logo.png' }
        ) {
            { Resolve-FluenceImageSource -Image $Value } | Should -Throw "*unsupported scheme '$Scheme'*"
        }
        It 'rejects an empty value' {
            { Resolve-FluenceImageSource -Image ' ' } | Should -Throw '*must be a file path*'
        }
    }

}
