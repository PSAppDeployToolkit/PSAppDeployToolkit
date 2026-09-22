function Set-FluenceWindowChrome
{
    <#
    .SYNOPSIS
        Applies a hashtable of FluenceWindow property names to a window.
    .DESCRIPTION
        Assigns each entry of the chrome hashtable onto the matching property of the window. The
        hashtable holds only the chrome parameters the caller explicitly bound, so unset properties
        keep the window's own defaults. A null hashtable is a no-op.
    .PARAMETER Window
        The window to apply the chrome to.
    .PARAMETER ChromeBound
        A hashtable mapping FluenceWindow property names to values; null is treated as no chrome.
    .NOTES
        Runs on the UI (STA) thread. Sets in-memory window properties only; changes no external state.
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Sets in-memory WPF window properties the caller explicitly bound; changes no external system state.')]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.Windows.Window]$Window,

        [Parameter()]
        [hashtable]$ChromeBound
    )

    if ($null -eq $ChromeBound) { return }
    foreach ($key in $ChromeBound.Keys)
    {
        $Window.$key = $ChromeBound[$key]
    }
}
