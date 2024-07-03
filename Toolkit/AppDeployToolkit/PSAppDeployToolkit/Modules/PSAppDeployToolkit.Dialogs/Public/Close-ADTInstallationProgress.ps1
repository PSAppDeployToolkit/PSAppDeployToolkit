function Close-ADTInstallationProgress
{
    <#

    .SYNOPSIS
    Closes the dialog created by Show-ADTInstallationProgress.

    .DESCRIPTION
    Closes the dialog created by Show-ADTInstallationProgress.

    This function is called by the Close-ADTSession function to close a running instance of the progress dialog if found.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not generate any output.

    .EXAMPLE
    Close-ADTInstallationProgress

    .NOTES
    This is an internal script function and should typically not be called directly.

    .LINK
    https://psappdeploytoolkit.com

    #>

    begin {
        Initialize-ADTFunction -Cmdlet $PSCmdlet
    }

    process {
        & (Get-ADTDialogFunction)
    }

    end {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
