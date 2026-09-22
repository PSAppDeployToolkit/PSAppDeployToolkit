function Set-FluenceProgressState
{
    <#
    .SYNOPSIS
        Applies a progress state (message, detail, percent, mode) to the progress window's controls.
    .DESCRIPTION
        Writes the four state values onto the window's template parts: the message and detail text
        blocks (an empty detail collapses its line) and the progress bar, which switches between the
        determinate and indeterminate modes and takes a percentage clamped to 0 to 100.
    .PARAMETER Parts
        The hashtable returned by New-FluenceProgressWindow (Window, MessageText, DetailText, Bar).
    .PARAMETER State
        The progress state hashtable (Message, Detail, PercentComplete, Indeterminate).
    .NOTES
        Must run on the UI thread that owns the window. Sets in-memory control properties only.
    #>
    [CmdletBinding()]
    [OutputType([void])]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Sets in-memory WPF control properties; changes no external system state.')]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.Collections.IDictionary]$Parts,

        [Parameter(Mandatory = $true)]
        [System.Collections.IDictionary]$State
    )

    $Parts.MessageText.Text = [string]$State.Message

    $detail = [string]$State.Detail
    $Parts.DetailText.Text = $detail
    if ([string]::IsNullOrWhiteSpace($detail))
    {
        $Parts.DetailText.Visibility = [System.Windows.Visibility]::Collapsed
    }
    else
    {
        $Parts.DetailText.Visibility = [System.Windows.Visibility]::Visible
    }

    if ([bool]$State.Indeterminate)
    {
        $Parts.Bar.ProgressMode = [Fluence.Wpf.ProgressBarMode]::Indeterminate
    }
    else
    {
        $Parts.Bar.ProgressMode = [Fluence.Wpf.ProgressBarMode]::Standard
        $Parts.Bar.Value = [double]$State.PercentComplete
    }
}
