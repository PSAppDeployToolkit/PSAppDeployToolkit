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
Describe 'Source docs manifest' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'SourceDocsManifest.ps1')
        $hash = 'a' * 64
        $image = 'docs/screenshots/gallery/button.png'
    }
    It 'serializes sorted records with BOM and LF and stays byte identical' {
        $manifest = [pscustomobject]@{ schemaVersion=1; files=@(
            [pscustomobject]@{ path=$image; kind='capture'; sha256=$hash; provenance='legacy-unverified'; theme='light'; state='default'; mode='WpfRender'; inputs=@('b.xaml','a.xaml'); checkedVisualInputSha256=$null; pngSha256=$hash; decision='legacy-unverified' },
            [pscustomobject]@{ path='docs/api/index.md'; kind='reference'; sha256=$hash; provenance='source-generated' }
        ) }
        $first = ConvertTo-SourceDocsManifestByteArray $manifest @($image)
        $second = ConvertTo-SourceDocsManifestByteArray $manifest @($image)
        [Convert]::ToHexString($first) | Should -BeExactly ([Convert]::ToHexString($second))
        [Convert]::ToHexString($first[0..2]) | Should -BeExactly 'EFBBBF'
        $json = [Text.Encoding]::UTF8.GetString($first[3..($first.Length-1)])
        $json.Contains("`r") | Should -BeFalse
        $json.IndexOf('docs/api/index.md') | Should -BeLessThan $json.IndexOf($image)
        $json.IndexOf('a.xaml') | Should -BeLessThan $json.IndexOf('b.xaml')
        ($json | ConvertFrom-Json).files[1].checkedVisualInputSha256 | Should -BeNullOrEmpty
    }
    It 'ignores source revision changes that are absent from managed records' {
        $manifest = [pscustomobject]@{ schemaVersion=1; sourceRevision='first'; files=@(
            [pscustomobject]@{ path='docs/api/index.md'; kind='reference'; sha256=$hash; provenance='source-generated' }
        ) }
        $first = ConvertTo-SourceDocsManifestByteArray $manifest
        $manifest.sourceRevision = 'second'
        [Convert]::ToHexString((ConvertTo-SourceDocsManifestByteArray $manifest)) | Should -BeExactly ([Convert]::ToHexString($first))
    }
    It 'rejects duplicate, parent, absolute, colon, and unmanaged paths' {
        $base = [pscustomobject]@{ schemaVersion=1; files=@(
            [pscustomobject]@{ path='docs/api/index.md'; kind='reference'; sha256=$hash; provenance='source-generated' }
        ) }
        foreach ($bad in 'docs/api/../escape.md','/docs/api/index.md','C:/docs/api/index.md','docs/other.md') {
            $base.files[0].path = $bad
            { ConvertTo-SourceDocsManifestByteArray $base } | Should -Throw
        }
        $base.files[0].path = 'docs/api/index.md'
        $base.files = @($base.files[0],$base.files[0])
        { ConvertTo-SourceDocsManifestByteArray $base } | Should -Throw
    }
    It 'rejects wrong kinds for a gallery manifest, mapped PNG, and reference' {
        foreach ($case in @(
            @{ path='docs/screenshots/gallery/manifest.json'; kind='reference' },
            @{ path='docs/api/index.md'; kind='gallery-manifest' },
            @{ path='docs/screenshots/gallery/button.png'; kind='reference' }
        )) {
            $manifest = [pscustomobject]@{ schemaVersion=1; files=@(
                [pscustomobject]@{ path=$case.path; kind=$case.kind; sha256=$hash; provenance='test' }
            ) }
            { ConvertTo-SourceDocsManifestByteArray $manifest @($image) } | Should -Throw
        }
    }
    It 'rejects reparse points when supported' {
        $root = Join-Path $TestDrive 'root'
        $outside = Join-Path $TestDrive 'outside'
        [IO.Directory]::CreateDirectory($root) | Out-Null
        [IO.Directory]::CreateDirectory($outside) | Out-Null
        try { New-Item -ItemType SymbolicLink -Path (Join-Path $root 'link') -Target $outside -ErrorAction Stop | Out-Null }
        catch { Set-ItResult -Skipped -Because 'Symbolic links unavailable'; return }
        { Get-DocsContainedPath $root 'link/file.md' } | Should -Throw
    }
}
