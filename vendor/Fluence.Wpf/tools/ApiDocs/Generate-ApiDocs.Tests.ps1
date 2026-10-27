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

BeforeAll {
    $script:generator = Join-Path $PSScriptRoot 'Generate-ApiDocs.ps1'
    $script:repository = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
    $script:assembly = Join-Path $script:repository 'Fluence.Wpf/bin/Release/net10.0-windows10.0.26100.0/Fluence.Wpf.dll'
    $script:xml = Join-Path $script:repository 'Fluence.Wpf/bin/Release/net10.0-windows10.0.26100.0/Fluence.Wpf.xml'
    if (-not [System.IO.File]::Exists($script:assembly) -or -not [System.IO.File]::Exists($script:xml)) {
        throw 'Build Fluence.Wpf Release net10.0-windows10.0.26100.0 before running API generator tests.'
    }
}

Describe 'API reference output contract' {
    BeforeEach {
        $script:outputRoot = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        $script:coveragePath = Join-Path $TestDrive ([guid]::NewGuid().ToString('N') + '.md')
    }

    It 'writes a complete output, detects missing and extra pages, and keeps a second Write byte stable' {
        & $script:generator -Configuration Release -OutputRoot $script:outputRoot -CoveragePath $script:coveragePath | Out-Null
        [System.IO.File]::Exists((Join-Path $script:outputRoot 'index.md')) | Should -BeTrue
        [System.IO.File]::Exists($script:coveragePath) | Should -BeTrue
        [System.IO.File]::Exists((Join-Path $script:outputRoot '.fluence-api-docs-output')) | Should -BeTrue
        { & $script:generator -Configuration Release -OutputRoot $script:outputRoot -CoveragePath $script:coveragePath -Check | Out-Null } | Should -Not -Throw

        $index = Join-Path $script:outputRoot 'index.md'
        Remove-Item -LiteralPath $script:coveragePath
        $coverageFailure = { & $script:generator -Configuration Release -OutputRoot $script:outputRoot -CoveragePath $script:coveragePath -Check | Out-Null } | Should -Throw -PassThru
        $coverageFailure.Exception.Message | Should -Match ([regex]::Escape($script:coveragePath))
        & $script:generator -Configuration Release -OutputRoot $script:outputRoot -CoveragePath $script:coveragePath | Out-Null

        Remove-Item -LiteralPath $index
        { & $script:generator -Configuration Release -OutputRoot $script:outputRoot -CoveragePath $script:coveragePath -Check | Out-Null } | Should -Throw '*index.md*'
        & $script:generator -Configuration Release -OutputRoot $script:outputRoot -CoveragePath $script:coveragePath | Out-Null

        $obsolete = Join-Path $script:outputRoot 'obsolete.md'
        [System.IO.File]::WriteAllText($obsolete, 'obsolete')
        { & $script:generator -Configuration Release -OutputRoot $script:outputRoot -CoveragePath $script:coveragePath -Check | Out-Null } | Should -Throw '*obsolete.md*'
        & $script:generator -Configuration Release -OutputRoot $script:outputRoot -CoveragePath $script:coveragePath | Out-Null
        [System.IO.File]::Exists($obsolete) | Should -BeFalse

        $before = @(Get-ChildItem -LiteralPath $script:outputRoot -Recurse -File | Sort-Object FullName | ForEach-Object {
            '{0}:{1}' -f ([System.IO.Path]::GetRelativePath($script:outputRoot, $_.FullName)), (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        })
        $coverageBefore = (Get-FileHash -LiteralPath $script:coveragePath -Algorithm SHA256).Hash
        $past = [datetime]::SpecifyKind([datetime]'2020-01-02T03:04:05', [System.DateTimeKind]::Utc)
        [System.IO.File]::SetLastWriteTimeUtc($index, $past)
        [System.IO.File]::SetLastWriteTimeUtc($script:coveragePath, $past)
        $indexTimeBefore = [System.IO.File]::GetLastWriteTimeUtc($index)
        $coverageTimeBefore = [System.IO.File]::GetLastWriteTimeUtc($script:coveragePath)
        & $script:generator -Configuration Release -OutputRoot $script:outputRoot -CoveragePath $script:coveragePath | Out-Null
        $after = @(Get-ChildItem -LiteralPath $script:outputRoot -Recurse -File | Sort-Object FullName | ForEach-Object {
            '{0}:{1}' -f ([System.IO.Path]::GetRelativePath($script:outputRoot, $_.FullName)), (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        })
        $after | Should -BeExactly $before
        (Get-FileHash -LiteralPath $script:coveragePath -Algorithm SHA256).Hash | Should -BeExactly $coverageBefore
        [System.IO.File]::GetLastWriteTimeUtc($index) | Should -Be $indexTimeBefore
        [System.IO.File]::GetLastWriteTimeUtc($script:coveragePath) | Should -Be $coverageTimeBefore
    }

    It 'refuses an authored output directory before writing any page or coverage bytes' {
        $null = New-Item -ItemType Directory -Path $script:outputRoot
        $authored = Join-Path $script:outputRoot 'README.md'
        [System.IO.File]::WriteAllText($authored, 'authored page')
        [System.IO.File]::WriteAllText($script:coveragePath, 'authored coverage')
        $readmeBefore = (Get-FileHash -LiteralPath $authored -Algorithm SHA256).Hash
        $coverageBefore = (Get-FileHash -LiteralPath $script:coveragePath -Algorithm SHA256).Hash

        { & $script:generator -Configuration Release -OutputRoot $script:outputRoot -CoveragePath $script:coveragePath | Out-Null } |
            Should -Throw '*not a verified API generator output*'

        @((Get-ChildItem -LiteralPath $script:outputRoot -Recurse -File)).Count | Should -Be 1
        [System.IO.File]::Exists((Join-Path $script:outputRoot 'index.md')) | Should -BeFalse
        (Get-FileHash -LiteralPath $authored -Algorithm SHA256).Hash | Should -BeExactly $readmeBefore
        (Get-FileHash -LiteralPath $script:coveragePath -Algorithm SHA256).Hash | Should -BeExactly $coverageBefore
    }

    It 'accepts the same Windows output root with different path casing' -Skip:(-not $IsWindows) {
        & $script:generator -Configuration Release -OutputRoot $script:outputRoot -CoveragePath $script:coveragePath | Out-Null
        $marker = Join-Path $script:outputRoot '.fluence-api-docs-output'
        $markerBefore = (Get-FileHash -LiteralPath $marker -Algorithm SHA256).Hash
        $caseVariant = $script:outputRoot.ToUpperInvariant()
        $caseVariant | Should -Not -BeExactly $script:outputRoot

        { & $script:generator -Configuration Release -OutputRoot $caseVariant -CoveragePath $script:coveragePath | Out-Null } | Should -Not -Throw
        { & $script:generator -Configuration Release -OutputRoot $caseVariant -CoveragePath $script:coveragePath -Check | Out-Null } | Should -Not -Throw
        (Get-FileHash -LiteralPath $marker -Algorithm SHA256).Hash | Should -BeExactly $markerBefore
    }

    It 'rejects a copied output marker in a different directory before writes' {
        & $script:generator -Configuration Release -OutputRoot $script:outputRoot -CoveragePath $script:coveragePath | Out-Null
        $otherRoot = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        $null = New-Item -ItemType Directory -Path $otherRoot
        Copy-Item -LiteralPath (Join-Path $script:outputRoot '.fluence-api-docs-output') -Destination $otherRoot
        $coverageBefore = (Get-FileHash -LiteralPath $script:coveragePath -Algorithm SHA256).Hash

        { & $script:generator -Configuration Release -OutputRoot $otherRoot -CoveragePath $script:coveragePath | Out-Null } |
            Should -Throw '*Invalid API output ownership marker*'

        @((Get-ChildItem -LiteralPath $otherRoot -File)).Count | Should -Be 1
        [System.IO.File]::Exists((Join-Path $otherRoot 'index.md')) | Should -BeFalse
        (Get-FileHash -LiteralPath $script:coveragePath -Algorithm SHA256).Hash | Should -BeExactly $coverageBefore
    }
}
