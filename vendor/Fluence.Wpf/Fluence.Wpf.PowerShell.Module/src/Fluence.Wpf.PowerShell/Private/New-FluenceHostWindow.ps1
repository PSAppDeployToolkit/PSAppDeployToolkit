function New-FluenceHostWindow
{
    <#
    .SYNOPSIS
        Builds a FluenceWindow with module baseline defaults and the caller's explicitly-bound chrome.
    .DESCRIPTION
        Creates a FluenceWindow, seeds the module baseline (title, sizing, startup location, backdrop),
        applies the explicitly-bound chrome over those defaults, and parents the window to an owner when
        one is supplied. The host window is the surface a -Content block fills or a non-Window parsed
        XAML payload is hosted in.
    .PARAMETER Spec
        The normalized window specification hashtable from Show-FluenceWindow.
    .OUTPUTS
        Fluence.Wpf.Controls.FluenceWindow
    .NOTES
        Must run on a UI (STA) thread; call it through Show-FluenceWindowCore. Builds an in-memory
        window object only and changes no external state.
    #>
    [CmdletBinding()]
    [OutputType([Fluence.Wpf.Controls.FluenceWindow])]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Builds a WPF window object in memory; changes no external system state.')]
    param
    (
        [Parameter(Mandatory = $true)]
        [hashtable]$Spec
    )

    $window = [Fluence.Wpf.Controls.FluenceWindow]::new()
    # Module baseline defaults; explicitly-bound chrome below overrides these.
    $window.Title = 'Fluence'
    $window.SizeToContent = [System.Windows.SizeToContent]::Manual
    $window.WindowStartupLocation = [System.Windows.WindowStartupLocation]::CenterScreen
    # An absent Backdrop in the spec means the caller did not ask for one, so the window follows the
    # backdrop already applied to the process instead of overriding a Set-FluenceBackdrop pin.
    $window.SystemBackdropType = if ([string]::IsNullOrWhiteSpace($Spec.Backdrop))
    {
        [Fluence.Wpf.ApplicationThemeManager]::CurrentBackdrop
    }
    else
    {
        ConvertTo-FluenceBackdropType -Backdrop $Spec.Backdrop
    }

    Set-FluenceWindowChrome -Window $window -ChromeBound $Spec.ChromeBound

    if ($null -ne $Spec.Owner)
    {
        # A WPF window's Owner must live on the same thread as the window. On an MTA host the window
        # is built on the module's UI runspace, so an Owner created on another thread cannot parent it
        # and assigning it would throw. Parent only when the Owner shares this thread; otherwise show
        # the window unparented rather than failing the whole call.
        if ($Spec.Owner.Dispatcher.CheckAccess())
        {
            $window.Owner = $Spec.Owner
            $window.WindowStartupLocation = [System.Windows.WindowStartupLocation]::CenterOwner
        }
        else
        {
            Write-Warning "The -Owner window belongs to a different thread than the Fluence UI thread; modal owner parenting is not available here. Showing the window without an owner."
        }
    }
    return $window
}
