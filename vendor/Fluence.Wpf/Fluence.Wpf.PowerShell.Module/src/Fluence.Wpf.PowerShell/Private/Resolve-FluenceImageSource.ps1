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
        [string]$Image
    )

    if ([string]::IsNullOrWhiteSpace($Image))
    {
        throw '-Image must be a file path, a file: URI, or a pack: URI.'
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
                        throw "-Image file not found: $($uri.LocalPath)"
                    }
                    return $uri.AbsoluteUri
                }
                default
                {
                    throw "-Image '$Image' uses the unsupported scheme '$($uri.Scheme)'. Use a file path, a file: URI, or a pack: URI."
                }
            }
        }
    }

    $resolved = Resolve-Path -LiteralPath $Image -ErrorAction SilentlyContinue
    if ($null -eq $resolved -or -not (Test-Path -LiteralPath $resolved.ProviderPath -PathType Leaf))
    {
        throw "-Image file not found: $Image"
    }
    return ([System.Uri]$resolved.ProviderPath).AbsoluteUri
}
