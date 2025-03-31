#-----------------------------------------------------------------------------
#
# MARK: Show-ADTInstallationPromptFluent
#
#-----------------------------------------------------------------------------

function Private:Show-ADTInstallationPromptFluent
{
    <#
    .SYNOPSIS
        Internal function to display a custom prompt using the Fluent UI.

    .DESCRIPTION
        Called by Show-ADTInstallationPrompt. Uses the UnifiedAdtApplication C# class
        to display a Custom dialog. Handles parameter mapping and result translation.

    .PARAMETER Title
        Dialog title.

    .PARAMETER Subtitle
        Dialog subtitle.

    .PARAMETER Message
        The message text to display.

    .PARAMETER ButtonRightText
        Text for the right button.

    .PARAMETER ButtonLeftText
        Text for the left button.

    .PARAMETER ButtonMiddleText
        Text for the middle button.

    .PARAMETER Timeout
        Timeout duration in seconds.

    .PARAMETER NotTopMost
        Switch to prevent the dialog from being topmost.

    .OUTPUTS
        String
        Returns the text of the button clicked, or 'Timeout'.
    #>
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'UnboundArguments', Justification = "This parameter is just to trap any superfluous input at the end of the function's call.")]
    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Title,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Subtitle,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Message,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ButtonRightText,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ButtonLeftText,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ButtonMiddleText,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost,

        [Parameter(Mandatory = $false, ValueFromRemainingArguments = $true, DontShow = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Generic.List[System.Object]]$UnboundArguments
    )

    # Perform initial setup
    $adtConfig = Get-ADTConfig

    # Map parameters for the C# ShowCustomDialog method.
    $dialogParams = @{
        dialogExpiryDuration = [System.TimeSpan]::FromMinutes((Get-ADTConfig).UI.DialogStyleFluentOptions.ExpiryDuration)
        dialogAccentColor    = (Get-ADTConfig).UI.DialogStyleFluentOptions.AccentColor
        dialogPosition       = (Get-ADTConfig).UI.DialogStyleFluentOptions.Position
        dialogTopMost        = !$NotTopMost
        dialogAllowMove      = (Get-ADTConfig).UI.DialogStyleFluentOptions.AllowMove
        appTitle             = $Title
        subtitle             = $Subtitle
        appIconImage         = $adtConfig.Assets.Logo
        customMessage        = $Message
        ButtonLeftText       = $ButtonLeftText
        ButtonMiddleText     = $ButtonMiddleText
        ButtonRightText      = $ButtonRightText
    }

    # Call the C# method with positional parameters
    $result = [PSADT.UserInterface.UnifiedADTApplication]::ShowCustomDialog(
        $dialogParams.dialogExpiryDuration,
        $dialogParams.dialogAccentColor,
        $dialogParams.dialogPosition,
        $dialogParams.dialogTopMost,
        $dialogParams.dialogAllowMove,
        $dialogParams.appTitle,
        $dialogParams.subtitle,
        $dialogParams.appIconImage,
        $dialogParams.customMessage,
        $dialogParams.ButtonLeftText,
        $dialogParams.ButtonMiddleText,
        $dialogParams.ButtonRightText
    )

    # Return the result directly (e.g., the text of the button clicked, or "Cancel" on timeout/close)
    # Handle potential errors or unexpected results
    switch ($result)
    {
        'Cancel'
        {
            # Dialog timed out or was closed unexpectedly
            Write-Warning "Installation prompt timed out or was closed."
            return 'Timeout' # Maintain compatibility with expected return values
        }
        'Error'
        {
            # An error occurred within the C# dialog code
            Write-Error "An error occurred while displaying the installation prompt (Fluent)."
            return 'Timeout' # Treat errors like timeouts for safety
        }
        'Disposed'
        {
            # The application was disposed before the dialog could be shown
            Write-Warning "The UI application was disposed before the installation prompt could be shown."
            return 'Timeout' # Treat as timeout
        }
        default
        {
            # Return the actual button text clicked
            # Remove accelerator key underscore if present
            return $result.Replace('_', '')
        }
    }
}
