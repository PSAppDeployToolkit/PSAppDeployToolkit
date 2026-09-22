function Invoke-FluenceWindow
{
    <#
    .SYNOPSIS
        Ensures the WPF Application, seeds the Fluence theme slots, builds the dialog window, and shows it.
    .DESCRIPTION
        The UI-thread half of Show-FluenceDialog. Seeds the application and theming through
        Initialize-FluenceApplication, builds the window with New-FluenceDialogWindow, shows it
        modally, and returns the result hashtable the window's handlers filled in.
    .PARAMETER Spec
        The dialog specification hashtable built on the caller thread by Show-FluenceDialog.
    .NOTES
        Must run on a UI (STA) thread; call it through Invoke-OnFluenceUi. Returns the result hashtable.
    #>
    [CmdletBinding()]
    [OutputType([hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [hashtable]$Spec
    )

    # Ensure the Application exists and seed the three theme slots and accent. Mandatory before
    # showing a FluenceWindow; shared with the window host.
    Initialize-FluenceApplication -Theme $Spec.Theme -Backdrop $Spec.Backdrop -Accent $Spec.AccentColor

    $state = @{ Result = @{}; Window = $null }
    $window = New-FluenceDialogWindow -Spec $Spec -State $state
    $state.Window = $window

    $null = $window.ShowDialog()
    return $state.Result
}
