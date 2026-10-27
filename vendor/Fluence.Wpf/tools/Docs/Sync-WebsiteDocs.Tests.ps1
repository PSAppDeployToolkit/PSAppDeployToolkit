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
CAUSES AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#>
Describe 'Website documentation sync' {
    BeforeAll {
        $sync = Join-Path $PSScriptRoot 'Sync-WebsiteDocs.ps1'
        function Write-Fixture([string]$path,[string]$text) {
            [IO.Directory]::CreateDirectory((Split-Path -Parent $path)) | Out-Null
            [IO.File]::WriteAllText($path,$text)
        }
        function Write-ApiFixture([string]$repo,[string[]]$types=@('Widget')) {
            $api = Join-Path $repo 'artifacts/docs/api'
            Write-Fixture (Join-Path $api 'index.md') "# C# API reference`n`nFor guides, see [controls](../controls.md) and [theming](../theming.md).`n`n## Namespaces`n`n- [Example](Example/index.md) ($($types.Count) types)`n"
            $lines = @('# Example','','## Types','')
            foreach ($type in $types) {
                $lines += "- [$type]($type.md)"
                Write-Fixture (Join-Path $api "Example/$type.md") "# $type"
            }
            Write-Fixture (Join-Path $api 'Example/index.md') ($lines -join "`n")
        }
        function Get-TreeState([string]$root) {
            return (@(Get-ChildItem -LiteralPath $root -Recurse -File | Sort-Object FullName | ForEach-Object {
                "$([IO.Path]::GetRelativePath($root,$_.FullName))=$((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash)"
            }) -join "`n")
        }
        function Write-CaptureFixture([string]$repo,[string]$siteDocs) {
            Write-Fixture (Join-Path $repo 'tools/Docs/VisualCaptureMap.psd1') "@{ SchemaVersion=1; Captures=@(@{ Id='gallery/button.png'; Path='docs/screenshots/gallery/button.png'; SourceRelativePath='screenshots/gallery/button.png'; Inputs=@('Fluence.Wpf.Demo/Pages/Button.xaml'); Theme='light'; State='default'; Mode='WpfRender' }) }"
            Write-Fixture (Join-Path $repo 'Fluence.Wpf.Demo/Pages/Button.xaml') '<Button />'
            Write-Fixture (Join-Path $siteDocs 'screenshots/gallery/button.png') 'website image'
            Write-Fixture (Join-Path $repo 'artifacts/docs/screenshots/gallery/button.png') 'candidate image'
            Write-Fixture (Join-Path $siteDocs 'screenshots/gallery/manifest.json') '{"captureMethod":"WPF RenderTargetBitmap","limitation":"preserve","entries":[{"route":"button","kind":"route","theme":"light","state":"default","file":"button.png","title":"old"},{"route":"other","kind":"route","theme":"light","state":"default","file":"other.png","title":"keep"}]}'
        }
        function Write-Approval([string]$path,[string]$repo,[string]$siteDocs,[string]$decision,[object[]]$galleryEntries=@()) {
            $imageBytes = if ($decision -eq 'material-change') {
                [IO.File]::ReadAllBytes((Join-Path $repo 'artifacts/docs/screenshots/gallery/button.png'))
            } else {
                [IO.File]::ReadAllBytes((Join-Path $siteDocs 'screenshots/gallery/button.png'))
            }
            $sha = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($imageBytes)).ToLowerInvariant()
            $inputHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes("Fluence.Wpf.Demo/Pages/Button.xaml`0<Button />"))).ToLowerInvariant()
            $capture = [ordered]@{
                id='gallery/button.png'; path='docs/screenshots/gallery/button.png'
                sourceRelativePath='screenshots/gallery/button.png'
                sha256=$sha; pngSha256=$sha; checkedVisualInputSha256=$inputHash
                inputs=@('Fluence.Wpf.Demo/Pages/Button.xaml'); theme='light'; state='default'
                mode='WpfRender'; decision=$decision; provenance='reviewed-example'
            }
            $approved = [ordered]@{ schemaVersion=1; captures=@($capture) }
            if ($galleryEntries.Count -gt 0) { $approved.galleryEntries = $galleryEntries }
            Write-Fixture $path (($approved | ConvertTo-Json -Depth 20))
        }
    }
    BeforeEach {
        $caseRoot = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        $repo = Join-Path $caseRoot 'source'
        $siteDocs = Join-Path $caseRoot 'website/docs'
        [IO.Directory]::CreateDirectory($siteDocs) | Out-Null
        Write-Fixture (Join-Path $repo 'Fluence.Wpf.PowerShell.Module/src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1') "@{ FunctionsToExport = @('Get-FluenceWidget') }"
        Write-ApiFixture $repo
        Write-Fixture (Join-Path $repo 'artifacts/docs/powershell/reference/Get-FluenceWidget.mdx') '# Function'
        foreach ($name in 'README.md','input-types.md','result-objects.md') {
            Write-Fixture (Join-Path $repo "docs/powershell/reference/$name") "# $name"
        }
        foreach ($name in 'controls.md','theming.md','winui-parity.md') {
            Write-Fixture (Join-Path $repo "docs/$name") "# $name"
        }
    }
    It 'preflights complete references then leaves a second sync byte identical' {
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        $first = Get-TreeState (Split-Path -Parent $siteDocs)
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $first
        @(Get-ChildItem -LiteralPath (Join-Path $repo 'artifacts/docs') -Directory -Filter 'sync-recovery-*').Count | Should -Be 0
        $json = Get-Content -Raw (Join-Path (Split-Path -Parent $siteDocs) 'source-docs-manifest.json') | ConvertFrom-Json
        @($json.files).Count | Should -Be 10
    }
    It 'does not mutate the website when API index is missing' {
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/api/index.md')
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -DryRun } | Should -Throw
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'does not mutate the website when an exported function is missing' {
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/powershell/reference/Get-FluenceWidget.mdx')
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs } | Should -Throw
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'blocks dirty owned targets before any mutation' {
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        Write-Fixture (Join-Path $siteDocs 'api/index.md') '# local edit'
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs } | Should -Throw
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'keeps an unowned website image' {
        Write-Fixture (Join-Path $siteDocs 'screenshots/gallery/unowned.png') 'website bytes'
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        [IO.File]::ReadAllText((Join-Path $siteDocs 'screenshots/gallery/unowned.png')) | Should -BeExactly 'website bytes'
    }
    It 'removes only obsolete reference pages owned by the prior manifest' {
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/api/Example/Widget.md')
        Write-ApiFixture $repo @('Old','Gadget')
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        Write-Fixture (Join-Path $siteDocs 'api/unowned.md') '# keep'
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/api/Example/Old.md')
        Write-ApiFixture $repo @('Gadget')
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        Test-Path -LiteralPath (Join-Path $siteDocs 'api/Example/Old.md') | Should -BeFalse
        [IO.File]::ReadAllText((Join-Path $siteDocs 'api/unowned.md')) | Should -BeExactly '# keep'
    }
    It 'rejects a missing linked namespace before <Mode> changes website bytes' -ForEach @(
        @{ Mode='Sync' }, @{ Mode='DryRun' }, @{ Mode='Initialize' }
    ) {
        Write-ApiFixture $repo
        if ($Mode -ne 'Initialize') { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs }
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/api/Example/index.md')
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        $switches = switch ($Mode) {
            'DryRun' { @{ DryRun=$true } }
            'Initialize' { @{ InitializeManifest=$true } }
            default { @{} }
        }
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs @switches } | Should -Throw '*Missing generated API namespace index*'
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'rejects a missing linked type before <Mode> changes website bytes' -ForEach @(
        @{ Mode='Sync' }, @{ Mode='DryRun' }, @{ Mode='Initialize' }
    ) {
        Write-ApiFixture $repo
        if ($Mode -ne 'Initialize') { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs }
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/api/Example/Widget.md')
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        $switches = switch ($Mode) {
            'DryRun' { @{ DryRun=$true } }
            'Initialize' { @{ InitializeManifest=$true } }
            default { @{} }
        }
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs @switches } | Should -Throw '*Missing generated API type page*'
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'accepts an updated type inventory and removes the formerly owned type only' {
        Write-ApiFixture $repo @('Widget','Gadget')
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/api/Example/Widget.md')
        Write-ApiFixture $repo @('Gadget')
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        Test-Path -LiteralPath (Join-Path $siteDocs 'api/Example/Widget.md') | Should -BeFalse
        Test-Path -LiteralPath (Join-Path $siteDocs 'api/Example/Gadget.md') | Should -BeTrue
        [IO.File]::ReadAllText((Join-Path $siteDocs 'api/Example/index.md')) | Should -Not -Match 'Widget'
    }
    It 'rejects a <Case> root inventory before <Mode> changes website bytes' -ForEach @(
        @{ Case='truncated'; Mode='Sync' }, @{ Case='truncated'; Mode='DryRun' }, @{ Case='truncated'; Mode='Initialize' },
        @{ Case='no-section'; Mode='Sync' }, @{ Case='no-section'; Mode='DryRun' }, @{ Case='no-section'; Mode='Initialize' },
        @{ Case='empty'; Mode='Sync' }, @{ Case='empty'; Mode='DryRun' }, @{ Case='empty'; Mode='Initialize' }
    ) {
        if ($Mode -ne 'Initialize') { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs }
        $api = Join-Path $repo 'artifacts/docs/api'
        switch ($Case) {
            'truncated' {
                Remove-Item -LiteralPath (Join-Path $api 'Example/index.md'),(Join-Path $api 'Example/Widget.md')
                Write-Fixture (Join-Path $api 'index.md') '# API'
            }
            'no-section' {
                Write-Fixture (Join-Path $api 'index.md') "# API`n`nFor guides, see [controls](../controls.md)."
            }
            'empty' {
                Remove-Item -LiteralPath (Join-Path $api 'Example/Widget.md')
                Write-Fixture (Join-Path $api 'index.md') "# API`n`n## Namespaces`n`n- [Example](Example/index.md) (0 types)"
                Write-Fixture (Join-Path $api 'Example/index.md') "# Example`n`n## Types`n"
            }
        }
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        $switches = switch ($Mode) {
            'DryRun' { @{ DryRun=$true } }
            'Initialize' { @{ InitializeManifest=$true } }
            default { @{} }
        }
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs @switches } | Should -Throw '*Generated API * inventory is *'
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'resolves fragments and escaped relative API inventory links' {
        Write-ApiFixture $repo
        $api = Join-Path $repo 'artifacts/docs/api'
        Write-Fixture (Join-Path $api 'Example/index.md') "# Example`n`n## Types`n`n- [Widget](Widget%2Emd#members)"
        Write-Fixture (Join-Path $api 'index.md') "# C# API reference`n`n## Namespaces`n`n- [Example](Example/index\.md#types) (1 types)"
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -DryRun } | Should -Not -Throw
    }
    It 'initializes matching website references and leaves the following sync byte identical' {
        foreach ($name in 'index.md','Example/index.md','Example/Widget.md') {
            $source = Join-Path $repo "artifacts/docs/api/$name"
            Write-Fixture (Join-Path $siteDocs "api/$name") ([IO.File]::ReadAllText($source))
        }
        Write-Fixture (Join-Path $siteDocs 'powershell/reference/Get-FluenceWidget.mdx') '# Function'
        foreach ($name in 'README.md','input-types.md','result-objects.md') {
            Write-Fixture (Join-Path $siteDocs "powershell/reference/$name") "# $name"
        }
        foreach ($name in 'controls.md','theming.md','winui-parity.md') {
            Write-Fixture (Join-Path $siteDocs $name) "# $name"
        }
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -InitializeManifest
        [IO.File]::ReadAllText((Join-Path $siteDocs 'api/index.md')) | Should -BeExactly ([IO.File]::ReadAllText((Join-Path $repo 'artifacts/docs/api/index.md')))
        $state = Get-Content -Raw (Join-Path (Split-Path -Parent $siteDocs) 'source-docs-manifest.json') | ConvertFrom-Json
        (@($state.files | Where-Object path -eq 'docs/api/index.md')[0]).provenance | Should -Be 'source-generated'
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'refuses initialization with a missing API index before website writes' {
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/api/index.md')
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -InitializeManifest } | Should -Throw
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'refuses initialization with a missing exported function before website writes' {
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/powershell/reference/Get-FluenceWidget.mdx')
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -InitializeManifest } | Should -Throw
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'refuses a divergent website reference during initialization' {
        Write-Fixture (Join-Path $siteDocs 'api/index.md') '# website wins'
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -InitializeManifest } | Should -Throw
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'rejects a valid but dirty tracked root manifest' {
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        $website = Split-Path -Parent $siteDocs
        & git -C $website init -q
        & git -C $website add source-docs-manifest.json
        & git -C $website -c user.name=Fixture -c user.email=fixture@example.invalid commit -q -m baseline
        $statePath = Join-Path $website 'source-docs-manifest.json'
        $edited = [IO.File]::ReadAllText($statePath).Replace('source-generated','local-website-edit')
        [IO.File]::WriteAllText($statePath,$edited)
        $before = Get-TreeState $website
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs } | Should -Throw '*Dirty tracked website source docs manifest*'
        (Get-TreeState $website) | Should -BeExactly $before
    }
    It 'rejects wrong-kind prior gallery ownership before website writes' {
        Write-Fixture (Join-Path $siteDocs 'screenshots/gallery/manifest.json') '{"entries":[]}'
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        $statePath = Join-Path (Split-Path -Parent $siteDocs) 'source-docs-manifest.json'
        $state = Get-Content -Raw $statePath | ConvertFrom-Json
        (@($state.files | Where-Object path -eq 'docs/screenshots/gallery/manifest.json')[0]).kind = 'reference'
        Write-Fixture $statePath ($state | ConvertTo-Json -Depth 20)
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs } | Should -Throw '*kind does not match path*'
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'restores website bytes after an injected mid-apply failure and keeps a durable snapshot' {
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        try {
            $env:FLUENCE_DOCS_SYNC_TEST_FAIL_AFTER = '1'
            { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs } | Should -Throw '*website paths were restored*'
        } finally { Remove-Item Env:FLUENCE_DOCS_SYNC_TEST_FAIL_AFTER -ErrorAction SilentlyContinue }
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
        $snapshots = @(Get-ChildItem -LiteralPath (Join-Path $repo 'artifacts/docs') -Directory -Filter 'sync-recovery-*')
        $snapshots.Count | Should -Be 1
        Test-Path -LiteralPath (Join-Path $snapshots[0].FullName 'ledger.json') | Should -BeTrue
    }
    It 'reports a durable snapshot when restoration itself fails' {
        try {
            $env:FLUENCE_DOCS_SYNC_TEST_FAIL_AFTER = '1'
            $env:FLUENCE_DOCS_SYNC_TEST_RESTORE_FAIL_PATH = 'docs/api/index.md'
            { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs } | Should -Throw '*Restoration failed*Recovery snapshot:*'
        } finally {
            Remove-Item Env:FLUENCE_DOCS_SYNC_TEST_FAIL_AFTER -ErrorAction SilentlyContinue
            Remove-Item Env:FLUENCE_DOCS_SYNC_TEST_RESTORE_FAIL_PATH -ErrorAction SilentlyContinue
        }
        $snapshots = @(Get-ChildItem -LiteralPath (Join-Path $repo 'artifacts/docs') -Directory -Filter 'sync-recovery-*')
        $snapshots.Count | Should -Be 1
        Test-Path -LiteralPath (Join-Path $snapshots[0].FullName 'ledger.json') | Should -BeTrue
    }
    It 'retains website capture bytes without approval even if a candidate exists' {
        Write-CaptureFixture $repo $siteDocs
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        [IO.File]::ReadAllText((Join-Path $siteDocs 'screenshots/gallery/button.png')) | Should -BeExactly 'website image'
        $state = Get-Content -Raw (Join-Path (Split-Path -Parent $siteDocs) 'source-docs-manifest.json') | ConvertFrom-Json
        $capture = @($state.files | Where-Object kind -eq 'capture')[0]
        $capture.checkedVisualInputSha256 | Should -BeNullOrEmpty
        $capture.decision | Should -Be 'legacy-unverified'
    }
    It 'applies a reviewed image and merges only the selected gallery entry' {
        Write-CaptureFixture $repo $siteDocs
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        $approval = Join-Path $caseRoot 'approval.json'
        $patch = [pscustomobject]@{ route='button'; kind='route'; theme='light'; state='default'; file='button.png'; title='new' }
        Write-Approval $approval $repo $siteDocs 'material-change' @($patch)
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $approval
        [IO.File]::ReadAllText((Join-Path $siteDocs 'screenshots/gallery/button.png')) | Should -BeExactly 'candidate image'
        $gallery = Get-Content -Raw (Join-Path $siteDocs 'screenshots/gallery/manifest.json') | ConvertFrom-Json
        $gallery.captureMethod | Should -Be 'WPF RenderTargetBitmap'
        $gallery.limitation | Should -Be 'preserve'
        @($gallery.entries).Count | Should -Be 2
        $gallery.entries[0].title | Should -Be 'new'
        $gallery.entries[1].title | Should -Be 'keep'
    }
    It 'advances checked inputs without replacing a visually unchanged image' {
        Write-CaptureFixture $repo $siteDocs
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        $approval = Join-Path $caseRoot 'approval.json'
        Write-Approval $approval $repo $siteDocs 'visually-unchanged'
        $oldImage = (Get-FileHash (Join-Path $siteDocs 'screenshots/gallery/button.png')).Hash
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $approval
        (Get-FileHash (Join-Path $siteDocs 'screenshots/gallery/button.png')).Hash | Should -Be $oldImage
        $state = Get-Content -Raw (Join-Path (Split-Path -Parent $siteDocs) 'source-docs-manifest.json') | ConvertFrom-Json
        $capture = @($state.files | Where-Object kind -eq 'capture')[0]
        $capture.checkedVisualInputSha256 | Should -Be ([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes("Fluence.Wpf.Demo/Pages/Button.xaml`0<Button />"))).ToLowerInvariant())
        $capture.decision | Should -Be 'visually-unchanged'
    }
    It 'rejects a partial approval before touching any website bytes' {
        Write-CaptureFixture $repo $siteDocs
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        $approval = Join-Path $caseRoot 'approval.json'
        Write-Approval $approval $repo $siteDocs 'material-change'
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/screenshots/gallery/button.png')
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $approval } | Should -Throw
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'rejects a dirty selected gallery image before touching other paths' {
        Write-CaptureFixture $repo $siteDocs
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        $approval = Join-Path $caseRoot 'approval.json'
        Write-Approval $approval $repo $siteDocs 'material-change'
        Write-Fixture (Join-Path $siteDocs 'screenshots/gallery/button.png') 'local edit'
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $approval } | Should -Throw
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'rejects an unknown gallery patch before touching any website bytes' {
        Write-CaptureFixture $repo $siteDocs
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        $approval = Join-Path $caseRoot 'approval.json'
        $patch = [pscustomobject]@{ route='unknown'; kind='route'; theme='light'; state='default'; file='unknown.png'; title='invalid' }
        Write-Approval $approval $repo $siteDocs 'material-change' @($patch)
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $approval } | Should -Throw
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
}
