function Update-FluenceProgress
{
    <#
    .SYNOPSIS
        Changes the message, detail, or percentage of an open progress window.
    .DESCRIPTION
        Only the values you pass change; the rest keep their current text. Passing -PercentComplete
        makes the bar determinate (clamped to 0..100); -Indeterminate switches it back. On the inline
        STA host this call also lets the window repaint, so call it at least every few seconds during
        long work even when nothing changed.
    .PARAMETER Handle
        The Fluence.ProgressHandle returned by Show-FluenceProgress.
    .PARAMETER Message
        The new main status line.
    .PARAMETER Detail
        The new detail line. An empty string hides it.
    .PARAMETER PercentComplete
        The new percentage; the bar becomes determinate.
    .PARAMETER Indeterminate
        Switch the bar back to indeterminate.
    .EXAMPLE
        Update-FluenceProgress -Handle $progress -Message 'Installing components' -PercentComplete 40
    .EXAMPLE
        Update-FluenceProgress -Handle $progress -Detail 'Waiting for the service to start' -Indeterminate
    .NOTES
        Runs on the UI thread that owns the window; blocks only for the duration of the update.
    #>
    [CmdletBinding()]
    [OutputType([void])]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Updates a transient in-process window the caller opened; makes no persistent or destructive system change.')]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [PSTypeName('Fluence.ProgressHandle')]
        [object]$Handle,

        [Parameter()]
        [string]$Message,

        [Parameter()]
        [string]$Detail,

        [Parameter()]
        [double]$PercentComplete,

        [Parameter()]
        [switch]$Indeterminate
    )

    if (-not $Handle.IsOpen)
    {
        throw 'The progress window has already been closed.'
    }

    $next = Resolve-FluenceProgressState -Current $Handle.State -Bound $PSBoundParameters
    foreach ($key in @('Message', 'Detail', 'PercentComplete', 'Indeterminate'))
    {
        $Handle.State[$key] = $next[$key]
    }

    $null = Invoke-OnFluenceUi -Script {
        param($h)
        Set-FluenceProgressState -Parts $h.Parts -State $h.State
        if ($h.Mode -eq 'Inline')
        {
            Invoke-FluenceDispatcherPump
        }
    } -ArgumentList @($Handle)
}
