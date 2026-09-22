function Show-FluenceWindowCore
{
    <#
    .SYNOPSIS
        Ensures the WPF Application, builds and populates the window, shows it modally, and returns the
        stashed result.
    .DESCRIPTION
        Seeds the three theme slots and accent through Initialize-FluenceApplication, builds the window
        and its content via Set-FluenceWindowContent (rethrowing any user-block error), optionally
        watches OS theme changes for the window's lifetime, and shows the window with ShowDialog. The
        value stashed on the window's Tag (for example by Close-FluenceWindow) is returned wrapped in a
        hashtable so the caller's collection-unwrap is unambiguous.
    .PARAMETER Spec
        The normalized window specification hashtable from Show-FluenceWindow.
    .OUTPUTS
        System.Collections.Hashtable
    .NOTES
        Must run on a UI (STA) thread; call it through Invoke-OnFluenceUi. Blocks until the window
        closes.
    #>
    [CmdletBinding()]
    [OutputType([hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [hashtable]$Spec
    )

    Initialize-FluenceApplication -Theme $Spec.Theme -Backdrop $Spec.Backdrop -Accent $Spec.Accent

    $state = @{ Error = $null }
    $window = Set-FluenceWindowContent -Spec $Spec -State $state

    try
    {
        if ($null -ne $state.Error)
        {
            throw $state.Error
        }

        if ($Spec.WatchSystemTheme)
        {
            [Fluence.Wpf.SystemThemeWatcher]::Watch($window)
            $unwatch = {
                [Fluence.Wpf.SystemThemeWatcher]::UnWatch($window)
            }.GetNewClosure()
            $window.add_Closed($unwatch)
        }

        $null = $window.ShowDialog()

        if ($null -ne $state.Error)
        {
            throw $state.Error
        }

        # Wrap in a hashtable so the caller's IList-unwrap is unambiguous even when the user's stashed
        # result is itself an array.
        return @{ Result = $window.Tag }
    }
    finally
    {
        # A callback can fail before ShowDialog; WPF still registers the constructed window.
        $window.Close()
    }
}
