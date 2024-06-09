function Set-ADTDeferHistory
{
    <#

    .SYNOPSIS
    Set the history of deferrals in the registry for the current application.

    .DESCRIPTION
    Set the history of deferrals in the registry for the current application.

    .PARAMETER DeferTimesRemaining
    Specify the number of deferrals remaining.

    .PARAMETER DeferDeadline
    Specify the deadline for the deferral.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not return any objects.

    .EXAMPLE
    Set-ADTDeferHistory

    .NOTES
    This is an internal script function and should typically not be called directly.

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.Int32]]$DeferTimesRemaining,

        [AllowEmptyString()]
        [System.String]$DeferDeadline
    )

    begin {
        Write-ADTDebugHeader
        $regKeyDeferHistory = (Get-ADTSession).GetPropertyValue('RegKeyDeferHistory')
    }

    process {
        if ($DeferTimesRemaining -and ($DeferTimesRemaining -ge 0))
        {
            Write-ADTLogEntry -Message "Setting deferral history: [DeferTimesRemaining = $DeferTimesRemaining]."
            Set-RegistryKey -Key $regKeyDeferHistory -Name 'DeferTimesRemaining' -Value $DeferTimesRemaining -ContinueOnError $true
        }
        if (![System.String]::IsNullOrWhiteSpace($DeferDeadline))
        {
            Write-ADTLogEntry -Message "Setting deferral history: [DeferDeadline = $DeferDeadline]."
            Set-RegistryKey -Key $regKeyDeferHistory -Name 'DeferDeadline' -Value $DeferDeadline -ContinueOnError $true
        }
    }

    end {
        Write-ADTDebugFooter
    }
}
