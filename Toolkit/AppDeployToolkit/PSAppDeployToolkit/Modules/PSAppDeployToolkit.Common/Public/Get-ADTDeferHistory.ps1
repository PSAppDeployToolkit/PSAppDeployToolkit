function Get-ADTDeferHistory
{
    <#

    .SYNOPSIS
    Get the history of deferrals from the registry for the current application, if it exists.

    .DESCRIPTION
    Get the history of deferrals from the registry for the current application, if it exists.

    .PARAMETER DeferTimesRemaining
    Specify the number of deferrals remaining.

    .PARAMETER DeferDeadline
    Specify the deadline for the deferral.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.String. Returns the history of deferrals from the registry for the current application, if it exists.

    .EXAMPLE
    Get-ADTDeferHistory

    .NOTES
    This is an internal script function and should typically not be called directly.

    .LINK
    https://psappdeploytoolkit.com

    #>

    begin {
        Write-ADTDebugHeader
    }

    process {
        Write-ADTLogEntry -Message 'Getting deferral history...'
        return Get-RegistryKey -Key (Get-ADTSession).GetPropertyValue('RegKeyDeferHistory') -ContinueOnError $true
    }

    end {
        Write-ADTDebugFooter
    }
}
