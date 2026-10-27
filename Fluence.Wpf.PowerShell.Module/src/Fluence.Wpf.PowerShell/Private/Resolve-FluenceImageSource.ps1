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


function Resolve-FluenceImageSource
{
    <#
    .SYNOPSIS
        Validates a dialog -Image value and returns it as an absolute file: or pack: URI string.
    .DESCRIPTION
        Accepts a filesystem path (relative paths resolve against the current location and the file
        must exist), a file: URI, or a pack: URI (pack://application:,,,/Assembly;component/path).
        Anything else, such as an http: or data: URI, is rejected at spec-build time with a clear
        error. This validates the source location only: image decoding, pack resource existence,
        and WPF pack authority resolution occur on the UI thread and can still fail there.
    .PARAMETER Image
        The path or URI the caller passed.
    .PARAMETER ParameterName
        The public parameter name used in validation errors.
    .OUTPUTS
        System.String
    .NOTES
        Does not require a host application; pure logic helper for the dialog cmdlets.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [Parameter(Mandatory = $true)]
        [string]$Image,

        [Parameter()]
        [string]$ParameterName = 'Image'
    )

    if ([string]::IsNullOrWhiteSpace($Image))
    {
        throw "-$ParameterName must be a file path, a file: URI, or a pack: URI."
    }

    $uri = $null
    if ([System.Uri]::TryCreate($Image, [System.UriKind]::Absolute, [ref]$uri) -and -not [string]::IsNullOrWhiteSpace($uri.Scheme))
    {
        # A Windows drive letter parses as an absolute URI with a one-letter scheme; treat it as a path.
        if ($uri.Scheme.Length -gt 1)
        {
            switch ($uri.Scheme)
            {
                'pack'
                {
                    return $uri.AbsoluteUri
                }
                'file'
                {
                    if (-not (Test-Path -LiteralPath $uri.LocalPath -PathType Leaf))
                    {
                        throw "-$ParameterName file not found: $($uri.LocalPath)"
                    }
                    return $uri.AbsoluteUri
                }
                default
                {
                    throw "-$ParameterName '$Image' uses the unsupported scheme '$($uri.Scheme)'. Use a file path, a file: URI, or a pack: URI."
                }
            }
        }
    }

    $resolved = Resolve-Path -LiteralPath $Image -ErrorAction SilentlyContinue
    if ($null -eq $resolved -or -not (Test-Path -LiteralPath $resolved.ProviderPath -PathType Leaf))
    {
        throw "-$ParameterName file not found: $Image"
    }
    return ([System.Uri]$resolved.ProviderPath).AbsoluteUri
}
