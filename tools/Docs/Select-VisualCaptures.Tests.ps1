<#
Copyright (c) 2026, Dan Cunningham. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#>

Describe 'Select-VisualCaptures' {
    BeforeAll {
        $selector = Join-Path $PSScriptRoot 'Select-VisualCaptures.ps1'
        $fixture = Join-Path $TestDrive 'source'
        New-Item -ItemType Directory -Path $fixture -Force | Out-Null
        $script:page = Join-Path $fixture 'page.xaml'
        $script:other = Join-Path $fixture 'other.xaml'
        $script:shared = Join-Path $fixture 'shared.xaml'
        $script:mapPath = Join-Path $TestDrive 'map.psd1'
        $script:manifestPath = Join-Path $TestDrive 'manifest.json'
        $script:root = $fixture
        @'
@{ SchemaVersion = 1; Captures = @(
  @{ Id='gallery/page-light.png'; Path='docs/screenshots/gallery/page-light.png'; SourceRelativePath='screenshots/gallery/page-light.png'; Inputs=@('page.xaml','shared.xaml'); Family='gallery/page'; Theme='light'; State='default'; Mode='WpfRender' },
  @{ Id='gallery/page-dark.png'; Path='docs/screenshots/gallery/page-dark.png'; SourceRelativePath='screenshots/gallery/page-dark.png'; Inputs=@('page.xaml','shared.xaml'); Family='gallery/page'; Theme='dark'; State='default'; Mode='WpfRender' },
  @{ Id='gallery/page-light-disabled.png'; Path='docs/screenshots/gallery/page-light-disabled.png'; SourceRelativePath='screenshots/gallery/page-light-disabled.png'; Inputs=@('page.xaml','shared.xaml'); Family='gallery/page'; Theme='light'; State='disabled'; Mode='WpfRender' },
  @{ Id='gallery/other-light.png'; Path='docs/screenshots/gallery/other-light.png'; SourceRelativePath='screenshots/gallery/other-light.png'; Inputs=@('other.xaml','shared.xaml'); Family='gallery/other'; Theme='light'; State='default'; Mode='WpfRender' }
) }
'@ | Set-Content -LiteralPath $mapPath -Encoding utf8
        function Get-Candidates { & $selector -RepositoryRoot $script:root -MapPath $script:mapPath -WebsiteManifestPath $script:manifestPath }
        function Save-CheckedManifest {
            $result = Get-Candidates
            @{ schemaVersion = 1; files = @($result.candidates | ForEach-Object { @{ path = $_.path; checkedVisualInputSha256 = $_.checkedVisualInputSha256 } }) } |
                ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $script:manifestPath -Encoding utf8
        }
    }

    BeforeEach {
        '<Root><TextBlock>Page</TextBlock></Root>' | Set-Content -LiteralPath $page -Encoding utf8
        '<Root><TextBlock>Other</TextBlock></Root>' | Set-Content -LiteralPath $other -Encoding utf8
        '<Root><Style TargetType="Button" /></Root>' | Set-Content -LiteralPath $shared -Encoding utf8
        if (Test-Path $manifestPath) { Remove-Item -LiteralPath $manifestPath }
    }

    It 'selects exactly one page pair when that page changes' {
        Save-CheckedManifest
        '<Root><TextBlock>Changed</TextBlock></Root>' | Set-Content -LiteralPath $page -Encoding utf8
        @((Get-Candidates).candidates.id | Sort-Object) | Should -Be @('gallery/page-dark.png','gallery/page-light-disabled.png','gallery/page-light.png')
    }

    It 'selects bounded shared template dependents' {
        Save-CheckedManifest
        '<Root><Style TargetType="Button" /><Setter /></Root>' | Set-Content -LiteralPath $shared -Encoding utf8
        (Get-Candidates).candidates.Count | Should -Be 4
    }

    It 'skips unchanged content and a timestamp-only touch' {
        Save-CheckedManifest
        (Get-Candidates).candidates.Count | Should -Be 0
        [System.IO.File]::SetLastWriteTimeUtc($page, [DateTime]::UtcNow.AddMinutes(5))
        (Get-Candidates).candidates.Count | Should -Be 0
    }

    It 'selects changed content with the same modified time' {
        Save-CheckedManifest
        $before = [System.IO.File]::GetLastWriteTimeUtc($page)
        '<Root><TextBlock>Changed</TextBlock></Root>' | Set-Content -LiteralPath $page -Encoding utf8
        [System.IO.File]::SetLastWriteTimeUtc($page, $before)
        (Get-Candidates).candidates.Count | Should -Be 3
    }

    It 'ignores XML comments and attribute formatting' {
        Save-CheckedManifest
        '<Root><!-- safe --><TextBlock>Page</TextBlock></Root>' | Set-Content -LiteralPath $page -Encoding utf8
        '<Root><Style TargetType = "Button" /></Root>' | Set-Content -LiteralPath $shared -Encoding utf8
        (Get-Candidates).candidates.Count | Should -Be 0
    }

    It 'preserves xml:space and inline whitespace changes' {
        '<Root xml:space="preserve"><TextBlock><Run>A</Run> <Run>B</Run></TextBlock></Root>' | Set-Content -LiteralPath $page -Encoding utf8
        Save-CheckedManifest
        '<Root xml:space="preserve"><TextBlock><Run>A</Run>  <Run>B</Run></TextBlock></Root>' | Set-Content -LiteralPath $page -Encoding utf8
        (Get-Candidates).candidates.Count | Should -Be 3
    }

    It 'expands a single requested ID to its Light Dark and declared state family' {
        $result = & $selector -RepositoryRoot $root -MapPath $mapPath -WebsiteManifestPath $manifestPath -CaptureId 'gallery/page-light.png'
        @($result.candidates).Count | Should -Be 3
        @($result.candidates.id) | Should -Contain 'gallery/page-dark.png'
        @($result.candidates.id) | Should -Contain 'gallery/page-light-disabled.png'
    }

    It 'includes actual shared Generic and Typography XAML in every authored WPF capture' {
        $realMap = Import-PowerShellDataFile -LiteralPath (Join-Path $PSScriptRoot 'VisualCaptureMap.psd1') -SkipLimitCheck
        @($realMap.Captures | Where-Object { $_.Inputs -notcontains 'Fluence.Wpf/Themes/Generic.xaml' -or
            $_.Inputs -notcontains 'Fluence.Wpf/Themes/Typography/Typography.xaml' }).Count | Should -Be 0
        $realMap.Captures.Count | Should -Be 419
        @($realMap.Captures | Where-Object { $_.Id -like 'controls/*' -and
            $_.Inputs -notcontains 'Fluence.Wpf.Tests/Tools/GalleryScreenshotHarness.cs' }).Count | Should -Be 0
        foreach ($family in @('auto-suggest-box', 'combo-box', 'date-picker', 'drop-down-button', 'menu', 'split-button', 'time-picker', 'toggle-split-button')) {
            foreach ($theme in @('light', 'dark')) {
                @($realMap.Captures.Id) | Should -Contain "controls/$family-$theme-open.png"
            }
        }
        foreach ($case in @(@('navigation-view', 'compact'), @('tree-view', 'expanded'))) {
            foreach ($theme in @('light', 'dark')) {
                @($realMap.Captures.Id) | Should -Contain "controls/$($case[0])-$theme-$($case[1]).png"
            }
        }
    }

    It 'nominates a mapped family after a shared XAML edit' {
        $sharedRoot = Join-Path $root 'Fluence.Wpf/Themes'
        New-Item -ItemType Directory -Path $sharedRoot -Force | Out-Null
        $generic = Join-Path $sharedRoot 'Generic.xaml'
        '<Root><Style /></Root>' | Set-Content -LiteralPath $generic -Encoding utf8
        $mapText = Get-Content -LiteralPath $mapPath -Raw
        $mapText.Replace("'page.xaml','shared.xaml'", "'Fluence.Wpf/Themes/Generic.xaml','page.xaml','shared.xaml'") |
            Set-Content -LiteralPath $mapPath -Encoding utf8
        Save-CheckedManifest
        '<Root><Style /><Setter /></Root>' | Set-Content -LiteralPath $generic -Encoding utf8
        @((Get-Candidates).candidates | Where-Object family -eq 'gallery/page').Count | Should -Be 3
        @((Get-Candidates).candidates | Where-Object family -eq 'gallery/other').Count | Should -Be 0
    }

    It 'rejects an input path through a junction when supported' {
        $outside = Join-Path $TestDrive 'outside'
        New-Item -ItemType Directory -Path $outside -Force | Out-Null
        Copy-Item -LiteralPath $page -Destination (Join-Path $outside 'page.xaml')
        try { New-Item -ItemType Junction -Path (Join-Path $root 'linked') -Target $outside -ErrorAction Stop | Out-Null }
        catch { Set-ItResult -Skipped -Because 'Junction creation unavailable'; return }
        $mapText = Get-Content -LiteralPath $mapPath -Raw
        $mapText.Replace("'page.xaml'", "'linked/page.xaml'") | Set-Content -LiteralPath $mapPath -Encoding utf8
        { Get-Candidates } | Should -Throw '*reparse point*'
    }
}
