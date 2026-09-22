<#
.SYNOPSIS
    Normalizes a text file to UTF-8 with BOM and LF line endings, and fails on em/en dashes.
.NOTES
    Repo text policy. Does not require a host application.
#>
[CmdletBinding()]
param
(
    [Parameter(Mandatory = $true)]
    [string[]]$Path
)

foreach ($item in $Path)
{
    $text = [System.IO.File]::ReadAllText($item)
    if ($text.Contains([char]0x2014) -or $text.Contains([char]0x2013))
    {
        throw "Em/en dash found in: $item"
    }
    $text = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    $enc = [System.Text.UTF8Encoding]::new($true)
    [System.IO.File]::WriteAllText($item, $text, $enc)
}
