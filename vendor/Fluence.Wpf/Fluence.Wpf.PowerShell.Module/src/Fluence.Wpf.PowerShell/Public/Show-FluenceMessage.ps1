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


function Show-FluenceMessage
{
    <#
    .SYNOPSIS
        Shows a themed Fluent message dialog and returns the name of the button the user clicked.
    .DESCRIPTION
        A thin wrapper around Show-FluenceDialog that maps a named button preset (OK, OKCancel,
        YesNo, YesNoCancel) to the correct button objects, renders the message as wrapping
        TextBlocks with an optional leading severity FontIcon, and returns the clicked button name
        as a string instead of the full DialogResult. For cancel-less presets (OK, YesNo), closing
        the dialog with the title-bar X or Esc returns the safe button name (OK or No) rather than
        $null, so a guard such as `if ($answer -ne 'No')` does not run the affirmative branch on a
        dismiss. A timeout returns -DefaultButton when it is given, and the same safe name otherwise.
    .PARAMETER Message
        One or more message lines displayed in the dialog.
    .PARAMETER Title
        The window title. -TitleBarText is an alias.
    .PARAMETER TitleBarIcon
        Title bar icon from a local file path, file: URI, or pack: URI. Other schemes and missing
        local files are rejected before UI dispatch. This does not change a message severity icon.
    .PARAMETER Icon
        The severity icon to display to the left of the message text.
        Info (default), Success, Warning, Error, Question, or None. Each severity maps to a distinct
        Segoe Fluent glyph and themed brush: Success, Warning, and Error use the InfoBar severity
        glyphs; Info uses the Informational glyph; Question uses the Segoe Fluent Help glyph with
        the neutral brush. None draws no glyph, which is what an image-led dialog wants. The message
        renders as wrapping TextBlocks, not inside an InfoBar.
    .PARAMETER Buttons
        Named button set: OK (default), OKCancel, YesNo, or YesNoCancel.
    .PARAMETER DefaultButton
        The button (by name, for example 'No') that Enter activates, that carries the countdown
        caption, and that a timeout resolves to. Must be one of the names in the chosen preset.
        Defaults to the preset's first button.
    .PARAMETER Timeout
        Seconds after which the dialog closes on its own. Between 1 and 86400.
    .PARAMETER Countdown
        Show the remaining seconds in the default button's caption. Requires -Timeout.
    .PARAMETER Image
        An image shown above the message at up to 120 device-independent pixels high: a file path,
        a file: URI, or a pack: URI. Other schemes are rejected before the dialog opens.
    .PARAMETER MessageAlignment
        Left (default) or Center. Applies to the image and the message lines.
    .PARAMETER Position
        Center (default), TopRight, or BottomRight of the primary work area.
    .PARAMETER Theme
        Auto, Light, Dark, or HighContrast. Omit it to keep the theme already applied to the process;
        the first Fluence call in a process applies Auto.
    .PARAMETER Backdrop
        Mica, Acrylic, Tabbed, None, or Auto. Omit it to keep the backdrop already applied to the
        process; the first Fluence call in a process applies Mica.
    .EXAMPLE
        Show-FluenceMessage -Message 'Proceed?' -Icon Question -Buttons YesNo
    .EXAMPLE
        $answer = Show-FluenceMessage -Message 'Save changes?' -Buttons OKCancel -Icon Warning
        if ($answer -eq 'OK') { Save-Data }
    .EXAMPLE
        $answer = Show-FluenceMessage -Message 'Restart now?' -Buttons YesNo -DefaultButton No -Timeout 30 -Countdown

        Counts down on the No button and returns 'No' if nobody answers within thirty seconds.
    .OUTPUTS
        System.String
    .NOTES
        Uses the calling STA thread or a module-owned STA runspace. An existing host application
        is supported only when the command already runs on its dispatcher thread. Blocks until the dialog closes.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [string[]]$Message,

        [Parameter()]
        [Alias('TitleBarText')]
        [string]$Title = 'Fluence',

        [Parameter()]
        [string]$TitleBarIcon,

        [Parameter()]
        [ValidateSet('None', 'Info', 'Success', 'Warning', 'Error', 'Question')]
        [string]$Icon = 'Info',

        [Parameter()]
        [ValidateSet('OK', 'OKCancel', 'YesNo', 'YesNoCancel')]
        [string]$Buttons = 'OK',

        [Parameter()]
        [ValidateSet('OK', 'Cancel', 'Yes', 'No')]
        [string]$DefaultButton,

        [Parameter()]
        [ValidateRange(1, 86400)]
        [int]$Timeout,

        [Parameter()]
        [switch]$Countdown,

        [Parameter()]
        [string]$Image,

        [Parameter()]
        [ValidateSet('Left', 'Center')]
        [string]$MessageAlignment,

        [Parameter()]
        [ValidateSet('Center', 'TopRight', 'BottomRight')]
        [string]$Position,

        [Parameter()]
        [ValidateSet('Auto', 'Light', 'Dark', 'HighContrast')]
        [string]$Theme,

        [Parameter()]
        [ValidateSet('Mica', 'Acrylic', 'Tabbed', 'None', 'Auto')]
        [string]$Backdrop
    )

    $presetButtons = Get-FluenceButtonPreset -Preset $Buttons

    $resolvedDefault = $null
    if ($PSBoundParameters.ContainsKey('DefaultButton'))
    {
        $names = @($presetButtons | ForEach-Object { $_.Name })
        if ($names -notcontains $DefaultButton)
        {
            throw "-DefaultButton '$DefaultButton' is not a button of the '$Buttons' preset (expected one of: $($names -join ', '))."
        }
        # Move the default flag onto the chosen button so Enter activates it and the countdown
        # caption sits on it. The Fluence.Button objects are the module's own specification objects.
        foreach ($button in $presetButtons)
        {
            $button.IsDefault = ($button.Name -eq $DefaultButton)
        }
        $resolvedDefault = $DefaultButton
    }

    $dialogParams = @{
        Message = $Message
        Title   = $Title
        Icon    = $Icon
        Buttons = $presetButtons
    }

    foreach ($name in @('Theme', 'Backdrop', 'Timeout', 'Countdown', 'Image', 'MessageAlignment', 'Position', 'TitleBarIcon'))
    {
        if ($PSBoundParameters.ContainsKey($name))
        {
            $dialogParams[$name] = $PSBoundParameters[$name]
        }
    }

    $result = Show-FluenceDialog @dialogParams

    $resolveParams = @{ Result = $result; Buttons = $presetButtons }
    if ($null -ne $resolvedDefault)
    {
        $resolveParams['DefaultButton'] = $resolvedDefault
    }
    return Resolve-FluenceClickedButton @resolveParams
}
