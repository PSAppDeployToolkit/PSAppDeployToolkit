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

    # The `-Force` parameter reports the link's own attributes rather than following it to the target,
    # which is the distinction being drawn here. A path that isn't there at all cannot be one itself.
    if (($item = Get-Item -LiteralPath $LiteralPath -Force -ErrorAction Ignore))
    {
        if ($item.Attributes.HasFlag([System.IO.FileAttributes]::ReparsePoint))
        {
            return $true
        }
    }

    # It can still sit beneath one. An elevated write is redirected just as thoroughly by a junction above
    # the path as by one at its end, and a standard user able to leave one behind can leave it at either.
    for ($parent = [System.IO.Directory]::GetParent($LiteralPath); $null -ne $parent; $parent = $parent.Parent)
    {
        if ($parent.Exists -and $parent.Attributes.HasFlag([System.IO.FileAttributes]::ReparsePoint))
        {
            return $true
        }
    }
    return $false
}
