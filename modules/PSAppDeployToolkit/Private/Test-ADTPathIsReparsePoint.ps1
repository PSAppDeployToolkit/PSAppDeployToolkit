#-----------------------------------------------------------------------------
#
# MARK: Test-ADTPathIsReparsePoint
#
#-----------------------------------------------------------------------------

function Private:Test-ADTPathIsReparsePoint
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$LiteralPath
    )

    # -Force reports the link's own attributes rather than following it to the target, which is the
    # distinction being drawn here. A path that isn't there at all cannot be one.
    if (($item = Get-Item -LiteralPath $LiteralPath -Force -ErrorAction Ignore))
    {
        return $item.Attributes.HasFlag([System.IO.FileAttributes]::ReparsePoint)
    }
    return $false
}
