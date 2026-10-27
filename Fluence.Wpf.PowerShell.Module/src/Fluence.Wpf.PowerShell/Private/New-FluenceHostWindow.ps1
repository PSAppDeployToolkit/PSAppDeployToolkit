<#
Copyright (c) 2026, Dan Cunningham. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#>


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
    Set-FluenceWindowIcon -Window $window -IconSource $Spec.TitleBarIcon

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
