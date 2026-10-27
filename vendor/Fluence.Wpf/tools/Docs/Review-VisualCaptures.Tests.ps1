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

Describe 'Review-VisualCaptures' {
    BeforeAll {
        Add-Type -AssemblyName PresentationCore
        $script:review = Join-Path $PSScriptRoot 'Review-VisualCaptures.ps1'
        $script:selector = Join-Path $PSScriptRoot 'Select-VisualCaptures.ps1'
        $script:sync = Join-Path $PSScriptRoot 'Sync-WebsiteDocs.ps1'
        function Write-FixtureFile {
            param([string]$Path, [string]$Content)
            [System.IO.Directory]::CreateDirectory((Split-Path -Parent $Path)) | Out-Null
            [System.IO.File]::WriteAllText($Path, $Content)
        }
        function Write-TestPng {
            param([string]$Path, [byte]$Blue = 0, [string]$Comment)
            $pixels = [byte[]]@($Blue, 0, 0, 255)
            $bitmap = [System.Windows.Media.Imaging.BitmapSource]::Create(1,1,96,96,[System.Windows.Media.PixelFormats]::Bgra32,$null,$pixels,4)
            $metadata = [System.Windows.Media.Imaging.BitmapMetadata]::new('png')
            if ($Comment) { $metadata.SetQuery('/tEXt/{str=Comment}', $Comment) }
            $frame = [System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap,$null,$metadata,$null)
            $encoder = [System.Windows.Media.Imaging.PngBitmapEncoder]::new()
            $encoder.Frames.Add($frame)
            $stream = [System.IO.File]::Create($Path)
            try { $encoder.Save($stream) } finally { $stream.Dispose() }
        }
        function Invoke-Review {
            param([string]$DecisionsPath, [string]$OutputPath)
            $args = @{ CandidatesPath=$script:candidatesPath; WebsiteRoot=$script:website; CaptureOutputRoot=$script:captures; WebsiteManifestPath=$script:manifestPath; MapPath=$script:mapPath }
            if ($DecisionsPath) { $args.DecisionsPath = $DecisionsPath }
            if ($OutputPath) { $args.OutputPath = $OutputPath }
            & $script:review @args
        }
    }

    BeforeEach {
        $script:website = Join-Path $TestDrive 'website'
        $script:captures = Join-Path $TestDrive 'captures'
        $script:published = Join-Path $website 'docs/screenshots/controls'
        $script:captured = Join-Path $captures 'screenshots/controls'
        New-Item -ItemType Directory -Path $published,$captured -Force | Out-Null
        $script:baselinePng = Join-Path $published 'page-light.png'
        $script:candidatePng = Join-Path $captured 'page-light.png'
        $script:candidatesPath = Join-Path $TestDrive 'candidates.json'
        $script:manifestPath = Join-Path $website 'source-docs-manifest.json'
        $script:decisionsPath = Join-Path $TestDrive 'decisions.json'
        $script:approvedPath = Join-Path $TestDrive 'approved.json'
        $script:mapPath = Join-Path $TestDrive 'map.psd1'
        "@{ SchemaVersion=1; Captures=@(@{ Id='controls/page-light.png'; Path='docs/screenshots/controls/page-light.png'; SourceRelativePath='screenshots/controls/page-light.png'; Inputs=@('page.xaml'); Family='controls/page'; Theme='light'; State='default'; Mode='WpfRender' }) }" |
            Set-Content -LiteralPath $mapPath -Encoding utf8
        if (Test-Path -LiteralPath $approvedPath) { Remove-Item -LiteralPath $approvedPath }
        Write-TestPng -Path $baselinePng
        Write-TestPng -Path $candidatePng
        @{schemaVersion=1; files=@(@{path='docs/screenshots/controls/page-light.png'; mode='WpfRender'; pngSha256=(Get-FileHash $baselinePng).Hash.ToLowerInvariant()})} |
            ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8
        @{schemaVersion=1; candidates=@(@{id='controls/page-light.png'; path='docs/screenshots/controls/page-light.png'; sourceRelativePath='screenshots/controls/page-light.png'; checkedVisualInputSha256=('a'*64); inputs=@('page.xaml'); theme='light'; state='default'; mode='WpfRender'})} |
            ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $candidatesPath -Encoding utf8
    }

    It 'keeps published bytes when PNG metadata differs but decoded pixels match' {
        Write-TestPng -Path $candidatePng -Comment 'new metadata'
        (Get-FileHash $candidatePng).Hash | Should -Not -Be (Get-FileHash $baselinePng).Hash
        $result = Invoke-Review -OutputPath $approvedPath
        $result.pending.Count | Should -Be 0
        $result.captures[0].decision | Should -Be 'visually-unchanged'
        $result.captures[0].pngSha256 | Should -Be (Get-FileHash $baselinePng).Hash.ToLowerInvariant()
        (Get-Content $approvedPath -Raw | ConvertFrom-Json).captures.Count | Should -Be 1
    }

    It 'requires explicit material approval for changed pixels' {
        Write-TestPng -Path $candidatePng -Blue 255
        $result = Invoke-Review
        $result.pending[0].reason | Should -Be 'material-review-required'
        @{decisions=@(@{id='controls/page-light.png'; decision='material-change'; provenance='visual-review-approved'; candidatePngSha256=(Get-FileHash $candidatePng).Hash.ToLowerInvariant()})} |
            ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $decisionsPath -Encoding utf8
        $approved = Invoke-Review -DecisionsPath $decisionsPath -OutputPath $approvedPath
        $approved.captures[0].pngSha256 | Should -Be (Get-FileHash $candidatePng).Hash.ToLowerInvariant()
    }

    It 'keeps changed pixels pending without a reviewer decision' {
        Write-TestPng -Path $candidatePng -Blue 255
        { Invoke-Review -OutputPath $approvedPath } | Should -Throw '*incomplete*'
        Test-Path $approvedPath | Should -BeFalse
    }

    It 'keeps a missing selected capture pending' {
        Remove-Item -LiteralPath $candidatePng
        (Invoke-Review).pending[0].reason | Should -Be 'missing-capture-or-baseline'
    }

    It 'requires an explicit hash-bound review for a genuinely new capture' {
        Remove-Item -LiteralPath $baselinePng
        @{schemaVersion=1; files=@()} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8
        (Invoke-Review).pending[0].reason | Should -Be 'new-capture-review-required'
        @{decisions=@(@{id='controls/page-light.png'; decision='new-capture'; provenance='visual-review'; candidatePngSha256=('0'*64)})} |
            ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $decisionsPath -Encoding utf8
        (Invoke-Review -DecisionsPath $decisionsPath).pending[0].reason | Should -Be 'new-capture-review-required'
        @{decisions=@(@{id='controls/page-light.png'; decision='new-capture'; provenance='visual-review'; candidatePngSha256=(Get-FileHash $candidatePng).Hash.ToLowerInvariant()})} |
            ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $decisionsPath -Encoding utf8
        $approved = Invoke-Review -DecisionsPath $decisionsPath -OutputPath $approvedPath
        $approved.captures[0].decision | Should -Be 'new-capture'
        $approved.captures[0].pngSha256 | Should -Be (Get-FileHash $candidatePng).Hash.ToLowerInvariant()
    }

    It 'does not treat a recorded but missing baseline as a new capture' {
        Remove-Item -LiteralPath $baselinePng
        @{decisions=@(@{id='controls/page-light.png'; decision='new-capture'; provenance='visual-review'; candidatePngSha256=(Get-FileHash $candidatePng).Hash.ToLowerInvariant()})} |
            ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $decisionsPath -Encoding utf8
        (Invoke-Review -DecisionsPath $decisionsPath).pending[0].reason | Should -Be 'missing-capture-or-baseline'
    }

    It 'does not approve new gallery captures' {
        $newDir = Join-Path $captures 'screenshots/gallery'
        New-Item -ItemType Directory -Path $newDir -Force | Out-Null
        Copy-Item -LiteralPath $candidatePng -Destination (Join-Path $newDir 'page-light.png')
        (Get-Content -LiteralPath $mapPath -Raw).Replace('controls/page','gallery/page').Replace('screenshots/controls','screenshots/gallery') |
            Set-Content -LiteralPath $mapPath -Encoding utf8
        (Get-Content -LiteralPath $candidatesPath -Raw).Replace('controls/page','gallery/page').Replace('screenshots/controls','screenshots/gallery') |
            Set-Content -LiteralPath $candidatesPath -Encoding utf8
        @{schemaVersion=1; files=@()} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8
        @{decisions=@(@{id='gallery/page-light.png'; decision='new-capture'; provenance='visual-review'; candidatePngSha256=(Get-FileHash $candidatePng).Hash.ToLowerInvariant()})} |
            ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $decisionsPath -Encoding utf8
        (Invoke-Review -DecisionsPath $decisionsPath).pending[0].reason | Should -Be 'missing-capture-or-baseline'
    }

    It 'rejects a submitted capture when its declared family is incomplete' {
        "@{ SchemaVersion=1; Captures=@(@{ Id='controls/page-light.png'; Path='docs/screenshots/controls/page-light.png'; SourceRelativePath='screenshots/controls/page-light.png'; Inputs=@('page.xaml'); Family='controls/page'; Theme='light'; State='default'; Mode='WpfRender' }, @{ Id='controls/page-dark.png'; Path='docs/screenshots/controls/page-dark.png'; SourceRelativePath='screenshots/controls/page-dark.png'; Inputs=@('page.xaml'); Family='controls/page'; Theme='dark'; State='default'; Mode='WpfRender' }) }" |
            Set-Content -LiteralPath $mapPath -Encoding utf8
        { Invoke-Review -OutputPath $approvedPath } | Should -Throw '*Incomplete capture family*'
        Test-Path -LiteralPath $approvedPath | Should -BeFalse
    }

    It 'rejects a website baseline reached through a junction when supported' {
        $outside = Join-Path $TestDrive 'outside-baseline'
        New-Item -ItemType Directory -Path $outside -Force | Out-Null
        Copy-Item -LiteralPath $baselinePng -Destination (Join-Path $outside 'page-light.png')
        $link = Join-Path $website 'docs/screenshots/linked'
        try { New-Item -ItemType Junction -Path $link -Target $outside -ErrorAction Stop | Out-Null }
        catch { Set-ItResult -Skipped -Because 'Junction creation unavailable'; return }
        $candidateData = Get-Content -LiteralPath $candidatesPath -Raw | ConvertFrom-Json
        $candidateData.candidates[0].path = 'docs/screenshots/linked/page-light.png'
        $candidateData | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $candidatesPath -Encoding utf8
        $mapText = Get-Content -LiteralPath $mapPath -Raw
        $mapText.Replace('docs/screenshots/controls/page-light.png','docs/screenshots/linked/page-light.png') |
            Set-Content -LiteralPath $mapPath -Encoding utf8
        try { { Invoke-Review } | Should -Throw '*reparse point*' }
        finally { Remove-Item -LiteralPath $link -Force }
    }

    It 'rejects a candidate reached through a junction when supported' {
        $outside = Join-Path $TestDrive 'outside-candidate'
        New-Item -ItemType Directory -Path $outside -Force | Out-Null
        Copy-Item -LiteralPath $candidatePng -Destination (Join-Path $outside 'page-light.png')
        $link = Join-Path $captures 'screenshots/linked'
        try { New-Item -ItemType Junction -Path $link -Target $outside -ErrorAction Stop | Out-Null }
        catch { Set-ItResult -Skipped -Because 'Junction creation unavailable'; return }
        $candidateData = Get-Content -LiteralPath $candidatesPath -Raw | ConvertFrom-Json
        $candidateData.candidates[0].sourceRelativePath = 'screenshots/linked/page-light.png'
        $candidateData | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $candidatesPath -Encoding utf8
        $mapText = Get-Content -LiteralPath $mapPath -Raw
        $mapText.Replace("SourceRelativePath='screenshots/controls/page-light.png'","SourceRelativePath='screenshots/linked/page-light.png'") |
            Set-Content -LiteralPath $mapPath -Encoding utf8
        try { { Invoke-Review } | Should -Throw '*reparse point*' }
        finally { Remove-Item -LiteralPath $link -Force }
    }

    It 'advances the checked input after a no-visible decision so a second selection skips' {
        $source = Join-Path $TestDrive 'source'
        New-Item -ItemType Directory -Path $source -Force | Out-Null
        '<Root>Page</Root>' | Set-Content -LiteralPath (Join-Path $source 'page.xaml') -Encoding utf8
        $first = & $selector -RepositoryRoot $source -MapPath $mapPath -WebsiteManifestPath $manifestPath
        $first.candidates | ConvertTo-Json -Depth 5 | Out-Null
        $first | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $candidatesPath -Encoding utf8
        $reviewed = Invoke-Review
        $checked = $reviewed.captures[0]
        @{schemaVersion=1;files=@(@{path=$checked.path;checkedVisualInputSha256=$checked.checkedVisualInputSha256})} |
            ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8
        $second = & $selector -RepositoryRoot $source -MapPath $mapPath -WebsiteManifestPath $manifestPath
        $second.candidates.Count | Should -Be 0
    }

    It 'round trips an initialized path keyed manifest through review and sync' {
        $case = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        $repo = Join-Path $case 'source'
        $site = Join-Path $case 'website'
        $siteDocs = Join-Path $site 'docs'
        [System.IO.Directory]::CreateDirectory($siteDocs) | Out-Null
        Write-FixtureFile (Join-Path $repo 'tools/Docs/VisualCaptureMap.psd1') "@{ SchemaVersion=1; Captures=@(@{ Id='controls/page-light.png'; Path='docs/screenshots/controls/page-light.png'; SourceRelativePath='screenshots/controls/page-light.png'; Inputs=@('page.xaml'); Family='controls/page'; Theme='light'; State='default'; Mode='WpfRender' }) }"
        Write-FixtureFile (Join-Path $repo 'page.xaml') '<Root>Page</Root>'
        Write-FixtureFile (Join-Path $repo 'Fluence.Wpf.PowerShell.Module/src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1') "@{ FunctionsToExport=@('Get-FluenceWidget') }"
        $references = @{
            'api/index.md' = "# C# API reference`n`n## Namespaces`n`n- [Example](Example/index.md) (1 types)"
            'api/Example/index.md' = "# Example`n`n## Types`n`n- [Widget](Widget.md)"
            'api/Example/Widget.md' = '# Widget'
            'powershell/reference/Get-FluenceWidget.mdx' = '# Function'
            'powershell/reference/README.md' = '# README'
            'powershell/reference/input-types.md' = '# Inputs'
            'powershell/reference/result-objects.md' = '# Results'
            'controls.md' = '# Controls'
            'theming.md' = '# Theming'
            'winui-parity.md' = '# Parity'
        }
        foreach ($name in $references.Keys) {
            $sourcePath = if ($name.StartsWith('api/', [System.StringComparison]::Ordinal) -or $name.EndsWith('.mdx', [System.StringComparison]::Ordinal)) {
                Join-Path $repo "artifacts/docs/$name"
            } else { Join-Path $repo "docs/$name" }
            Write-FixtureFile $sourcePath $references[$name]
            Write-FixtureFile (Join-Path $siteDocs $name) $references[$name]
        }
        $sitePng = Join-Path $siteDocs 'screenshots/controls/page-light.png'
        $integratedCandidate = Join-Path $repo 'artifacts/docs/screenshots/controls/page-light.png'
        [System.IO.Directory]::CreateDirectory((Split-Path -Parent $sitePng)) | Out-Null
        [System.IO.Directory]::CreateDirectory((Split-Path -Parent $integratedCandidate)) | Out-Null
        Copy-Item -LiteralPath $baselinePng -Destination $sitePng
        Copy-Item -LiteralPath $script:candidatePng -Destination $integratedCandidate

        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -InitializeManifest
        $siteManifest = Join-Path $site 'source-docs-manifest.json'
        $legacy = Get-Content -LiteralPath $siteManifest -Raw | ConvertFrom-Json
        @($legacy.files | Where-Object path -eq 'docs/screenshots/controls/page-light.png')[0].PSObject.Properties.Name | Should -Not -Contain 'id'
        $first = & $selector -RepositoryRoot $repo -MapPath (Join-Path $repo 'tools/Docs/VisualCaptureMap.psd1') -WebsiteManifestPath $siteManifest
        $first.candidates.Count | Should -Be 1
        $first.candidates[0].reason | Should -Be 'legacy-unverified'
        $first | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $candidatesPath -Encoding utf8
        $approved = & $review -CandidatesPath $candidatesPath -WebsiteRoot $site -CaptureOutputRoot (Join-Path $repo 'artifacts/docs') -WebsiteManifestPath $siteManifest -MapPath (Join-Path $repo 'tools/Docs/VisualCaptureMap.psd1') -OutputPath $approvedPath
        $approved.captures[0].decision | Should -Be 'visually-unchanged'
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $approvedPath
        $second = & $selector -RepositoryRoot $repo -MapPath (Join-Path $repo 'tools/Docs/VisualCaptureMap.psd1') -WebsiteManifestPath $siteManifest
        $second.candidates.Count | Should -Be 0

        # Add a dark companion with no published baseline, then review the complete family.
        $integratedMap = Join-Path $repo 'tools/Docs/VisualCaptureMap.psd1'
        "@{ SchemaVersion=1; Captures=@(@{ Id='controls/page-light.png'; Path='docs/screenshots/controls/page-light.png'; SourceRelativePath='screenshots/controls/page-light.png'; Inputs=@('page.xaml'); Family='controls/page'; Theme='light'; State='default'; Mode='WpfRender' }, @{ Id='controls/page-dark.png'; Path='docs/screenshots/controls/page-dark.png'; SourceRelativePath='screenshots/controls/page-dark.png'; Inputs=@('page.xaml'); Family='controls/page'; Theme='dark'; State='default'; Mode='WpfRender' }) }" |
            Set-Content -LiteralPath $integratedMap -Encoding utf8
        $newPng = Join-Path $repo 'artifacts/docs/screenshots/controls/page-dark.png'
        Write-TestPng -Path $newPng -Blue 255
        $third = & $selector -RepositoryRoot $repo -MapPath $integratedMap -WebsiteManifestPath $siteManifest -CaptureId 'controls/page-dark.png'
        $third | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $candidatesPath -Encoding utf8
        @{decisions=@(@{id='controls/page-dark.png'; decision='new-capture'; provenance='visual-review'; candidatePngSha256=(Get-FileHash $newPng).Hash.ToLowerInvariant()})} |
            ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $decisionsPath -Encoding utf8
        & $review -CandidatesPath $candidatesPath -WebsiteRoot $site -CaptureOutputRoot (Join-Path $repo 'artifacts/docs') -WebsiteManifestPath $siteManifest -MapPath $integratedMap -DecisionsPath $decisionsPath -OutputPath $approvedPath | Out-Null
        $newTarget = Join-Path $siteDocs 'screenshots/controls/page-dark.png'
        Write-TestPng -Path $newTarget
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $approvedPath } | Should -Throw '*New capture baseline unexpectedly exists*'
        Remove-Item -LiteralPath $newTarget
        $manifestBefore = (Get-FileHash -LiteralPath $siteManifest).Hash
        try {
            $env:FLUENCE_DOCS_SYNC_TEST_CREATE_RACE_PATH = 'docs/screenshots/controls/page-dark.png'
            { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $approvedPath } | Should -Throw
        } finally { Remove-Item Env:FLUENCE_DOCS_SYNC_TEST_CREATE_RACE_PATH -ErrorAction SilentlyContinue }
        (Get-Content -LiteralPath $newTarget -Raw) | Should -BeExactly 'concurrent sentinel'
        (Get-FileHash -LiteralPath $siteManifest).Hash | Should -Be $manifestBefore
        Remove-Item -LiteralPath $newTarget
        try {
            $env:FLUENCE_DOCS_SYNC_TEST_FAIL_AFTER = '1'
            { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $approvedPath } | Should -Throw '*Injected sync apply failure*'
        } finally { Remove-Item Env:FLUENCE_DOCS_SYNC_TEST_FAIL_AFTER -ErrorAction SilentlyContinue }
        Test-Path -LiteralPath $newTarget | Should -BeFalse
        (Get-FileHash -LiteralPath $siteManifest).Hash | Should -Be $manifestBefore
        try {
            $env:FLUENCE_DOCS_SYNC_TEST_PARTIAL_CREATE_PATH = 'docs/screenshots/controls/page-dark.png'
            { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $approvedPath } | Should -Throw '*Restoration failed*Recovery snapshot*'
        } finally { Remove-Item Env:FLUENCE_DOCS_SYNC_TEST_PARTIAL_CREATE_PATH -ErrorAction SilentlyContinue }
        (Get-Item -LiteralPath $newTarget).Length | Should -Be 1
        (Get-FileHash -LiteralPath $siteManifest).Hash | Should -Be $manifestBefore
        Remove-Item -LiteralPath $newTarget
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $approvedPath
        (Get-FileHash $newTarget).Hash | Should -Be (Get-FileHash $newPng).Hash
        $updated = Get-Content -LiteralPath $siteManifest -Raw | ConvertFrom-Json
        @($updated.files | Where-Object path -eq 'docs/screenshots/controls/page-dark.png')[0].decision | Should -Be 'new-capture'
    }
}
