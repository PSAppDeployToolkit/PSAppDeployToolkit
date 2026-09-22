function Close-FluenceWindow
{
    <#
    .SYNOPSIS
        Closes a Fluence-hosted window on its UI thread, optionally stashing a result for the host.
    .DESCRIPTION
        Runs on the UI (STA) thread that owns the window. When -Result is supplied it is stored on
        the window's Tag so the window host can return it, then the window is closed.
    .PARAMETER Window
        The System.Windows.Window to close.
    .PARAMETER Result
        An optional value to stash on the window's Tag before closing.
    .EXAMPLE
        Close-FluenceWindow -Window $window
    .EXAMPLE
        Close-FluenceWindow -Window $window -Result 'Saved'
    .NOTES
        Stashes -Result on the window's Tag for the window host to return, then closes on the UI
        thread. A window implies a running Application, so no application is created here.
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Closes a window the caller explicitly named; makes no persistent or destructive system change, so ShouldProcess prompting is not appropriate.')]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [System.Windows.Window]$Window,

        [Parameter()]
        [object]$Result
    )

    $null = Invoke-OnFluenceUi -Script {
        param($window, $result, $hasResult)

        if ($hasResult)
        {
            $window.Tag = $result
        }

        $window.Close()
    } -ArgumentList @($Window, $Result, $PSBoundParameters.ContainsKey('Result'))
}
