function Show-ADTDialogBox
{
    <#

    .SYNOPSIS
    Display a custom dialog box with optional title, buttons, icon and timeout.

    Show-ADTInstallationPrompt is recommended over this function as it provides more customization and uses consistent branding with the other UI components.

    .DESCRIPTION
    Display a custom dialog box with optional title, buttons, icon and timeout. The default button is "OK", the default Icon is "None", and the default Timeout is None

    .PARAMETER Text
    Text in the message dialog box

    .PARAMETER Title
    Title of the message dialog box

    .PARAMETER Buttons
    Buttons to be included on the dialog box. Options: OK, OKCancel, AbortRetryIgnore, YesNoCancel, YesNo, RetryCancel, CancelTryAgainContinue. Default: OK.

    .PARAMETER DefaultButton
    The Default button that is selected. Options: First, Second, Third. Default: First.

    .PARAMETER Icon
    Icon to display on the dialog box. Options: None, Stop, Question, Exclamation, Information. Default: None

    .PARAMETER Timeout
    Timeout period in seconds before automatically closing the dialog box with the return message "Timeout". Default: UI timeout value set in the config XML file.

    .PARAMETER TopMost
    Specifies whether the message box is a system modal message box and appears in a topmost window. Default: $true.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.String. Returns the text of the button that was clicked.

    .EXAMPLE
    Show-ADTDialogBox -Title 'Installed Complete' -Text 'Installation has completed. Please click OK and restart your computer.' -Icon 'Information'

    .EXAMPLE
    Show-ADTDialogBox -Title 'Installation Notice' -Text 'Installation will take approximately 30 minutes. Do you wish to proceed?' -Buttons 'OKCancel' -DefaultButton 'Second' -Icon 'Exclamation' -Timeout 600 -Topmost $false

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, Position = 0, HelpMessage = 'Enter a message for the dialog box.')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Text,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Title = (Get-ADTSession).GetPropertyValue('InstallTitle'),

        [Parameter(Mandatory = $false)]
        [ValidateSet('OK', 'OKCancel', 'AbortRetryIgnore', 'YesNoCancel', 'YesNo', 'RetryCancel', 'CancelTryAgainContinue')]
        [System.String]$Buttons = 'OK',

        [Parameter(Mandatory = $false)]
        [ValidateSet('First', 'Second', 'Third')]
        [System.String]$DefaultButton = 'First',

        [Parameter(Mandatory = $false)]
        [ValidateSet('Exclamation', 'Information', 'None', 'Stop', 'Question')]
        [System.String]$Icon = 'None',

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$Timeout = (Get-ADTConfig).UI.DefaultTimeout,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        try
        {
            $adtSession = Get-ADTSession
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }
    }

    process
    {
        # Bypass if in silent mode.
        if ($adtSession.IsSilent())
        {
            Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) [Mode: $($adtSession.GetPropertyValue('deployMode'))]. Text: $Text"
            return
        }

        Write-ADTLogEntry -Message "Displaying Dialog Box with message: $Text..."
        try
        {
            try
            {
                $result = switch ([System.Activator]::CreateInstance([System.Type]::GetTypeFromProgID('WScript.Shell')).Popup($Text, $Timeout, $Title, ($Script:DialogBox.Buttons.$Buttons + $Script:DialogBox.Icons.$Icon + $Script:DialogBox.DefaultButtons.$DefaultButton + (4096 * !$NotTopMost))))
                {
                    1 {'OK'; break}
                    2 {'Cancel'; break}
                    3 {'Abort'; break}
                    4 {'Retry'; break}
                    5 {'Ignore'; break}
                    6 {'Yes'; break}
                    7 {'No'; break}
                    10 {'Try Again'; break}
                    11 {'Continue'; break}
                    -1 {'Timeout'; break}
                    default {'Unknown'; break}
                }

                Write-ADTLogEntry -Message "Dialog Box Response: $result"
                return $result
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
