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


function Get-FluenceInput
{
    <#
    .SYNOPSIS
        Shows a themed Fluent input dialog and returns the captured value, or $null on cancel.
    .DESCRIPTION
        Wraps a single-prompt Show-FluenceDialog with an OK (default) and Cancel button. Returns
        the captured input value when the user clicks OK, or $null when the user cancels or the
        dialog times out. Password input returns SecureString unless -AsPlainText is specified.
        Dispose the returned SecureString when the caller no longer needs it.
    .PARAMETER Message
        The prompt label shown in the dialog (Mandatory).
    .PARAMETER Title
        The window title. -TitleBarText is an alias.
    .PARAMETER TitleBarIcon
        Title bar icon from a local file path, file: URI, or pack: URI. Other schemes and missing
        local files are rejected before UI dispatch. This does not change a message severity icon.
    .PARAMETER AsPlainText
        Password prompts only. Return a plain string instead of the default SecureString.
        Assign an explicitly requested plaintext value to a variable to avoid printing it.
        The DialogResult default formatting does not apply to this raw string.
    .PARAMETER DefaultValue
        The initial value pre-filled in the input control. Password defaults must be strings and
        remain plaintext in the specification; omit the default when collecting a secret.
    .PARAMETER InputType
        The input control type. One of: Text (default), Multiline, Password, Number, Checkbox,
        Toggle, Choice, Date, Time, FileOpen, FileSave, FolderOpen, Link. A List prompt is not
        offered here because it picks from a set rather than capturing a typed value; use
        Show-FluenceListSelection for that.
    .PARAMETER ValidateSet
        The allowed values for a Choice prompt. Required when -InputType is Choice.
    .PARAMETER As
        How a Choice prompt renders its values: Combo (default) or Radio.
    .PARAMETER Timeout
        Seconds after which the dialog closes on its own and $null is returned. Between 1 and 86400.
    .PARAMETER Countdown
        Show the remaining seconds in the OK button's caption. Requires -Timeout.
    .PARAMETER Theme
        Auto, Light, Dark, or HighContrast. Omit it to keep the theme already applied to the process;
        the first Fluence call in a process applies Auto.
    .PARAMETER Backdrop
        Mica, Acrylic, Tabbed, None, or Auto. Omit it to keep the backdrop already applied to the
        process; the first Fluence call in a process applies Mica.
    .EXAMPLE
        $name = Get-FluenceInput -Message 'Enter your name'
    .EXAMPLE
        $age = Get-FluenceInput -Message 'Enter your age' -InputType Number -DefaultValue 25
    .EXAMPLE
        $edition = Get-FluenceInput -Message 'Edition' -InputType Choice -ValidateSet 'Standard', 'Pro' -As Radio
    .EXAMPLE
        $server = Get-FluenceInput -Message 'Server name' -DefaultValue 'localhost' -Timeout 20 -Countdown
        if ($null -eq $server) { $server = 'localhost' }

        Falls back to the default after twenty unattended seconds.
    .OUTPUTS
        System.Object
    .NOTES
        Uses the calling STA thread or a module-owned STA runspace. An existing host application
        is supported only when the command already runs on its dispatcher thread. Blocks until the dialog closes.
    #>
    [CmdletBinding()]
    [OutputType([object])]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Message,

        [Parameter()]
        [Alias('TitleBarText')]
        [string]$Title = 'Fluence',

        [Parameter()]
        [string]$TitleBarIcon,

        [Parameter()]
        [object]$DefaultValue,

        [Parameter()]
        [switch]$AsPlainText,

        [Parameter()]
        [ValidateSet('Text', 'Multiline', 'Password', 'Number', 'Checkbox', 'Toggle',
            'Choice', 'Date', 'Time', 'FileOpen', 'FileSave', 'FolderOpen', 'Link')]
        [string]$InputType = 'Text',

        [Parameter()]
        [string[]]$ValidateSet,

        [Parameter()]
        [ValidateSet('Combo', 'Radio')]
        [string]$As,

        [Parameter()]
        [ValidateRange(1, 86400)]
        [int]$Timeout,

        [Parameter()]
        [switch]$Countdown,

        [Parameter()]
        [ValidateSet('Auto', 'Light', 'Dark', 'HighContrast')]
        [string]$Theme,

        [Parameter()]
        [ValidateSet('Mica', 'Acrylic', 'Tabbed', 'None', 'Auto')]
        [string]$Backdrop
    )

    $promptParams = @{
        Name      = 'Input'
        Message   = $Message
        InputType = $InputType
    }

    foreach ($name in @('DefaultValue', 'ValidateSet', 'As', 'AsPlainText'))
    {
        if ($PSBoundParameters.ContainsKey($name))
        {
            $promptParams[$name] = $PSBoundParameters[$name]
        }
    }

    $prompt = New-FluencePrompt @promptParams

    $buttons = @(
        New-FluenceButton 'OK' -IsDefault
        New-FluenceButton 'Cancel' -IsCancel
    )

    $dialogParams = @{
        Title   = $Title
        Prompts = @($prompt)
        Buttons = $buttons
    }

    foreach ($name in @('Theme', 'Backdrop', 'Timeout', 'Countdown', 'TitleBarIcon'))
    {
        if ($PSBoundParameters.ContainsKey($name))
        {
            $dialogParams[$name] = $PSBoundParameters[$name]
        }
    }

    $result = Show-FluenceDialog @dialogParams

    if ($result.Cancelled -or $result.TimedOut)
    {
        if ($result.Input -is [System.Security.SecureString])
        {
            $result.Input.Dispose()
        }
        return $null
    }

    return $result.Input
}
