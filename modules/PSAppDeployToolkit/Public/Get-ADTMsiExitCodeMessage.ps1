#-----------------------------------------------------------------------------
#
# MARK: Get-ADTMsiExitCodeMessage
#
#-----------------------------------------------------------------------------

function Get-ADTMsiExitCodeMessage
{
    <#
    .SYNOPSIS
        Get message for MSI exit code.

    .DESCRIPTION
        Get message for MSI exit code by reading it from msimsg.dll.

    .PARAMETER MsiExitCode
        MSI exit code.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.String

        Returns the message for the MSI exit code.

    .EXAMPLE
        Get-ADTMsiExitCodeMessage -MsiExitCode 1618

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        http://msdn.microsoft.com/en-us/library/aa368542(v=vs.85).aspx

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTMsiExitCodeMessage

    .LINK
        https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/main/src/PSAppDeployToolkit/Public/Get-ADTMsiExitCodeMessage.ps1
    #>

    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.UInt32]]$MsiExitCode
    )

    begin
    {
        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                # The underlying Win32Exception always contains a valid message for a given msiexec.exe code.
                return [PSADT.WindowsInstaller.MsiUtilities]::GetExceptionForMsiExitCode($MsiExitCode).Message
            }
            catch
            {
                # Re-writing the ErrorRecord with Write-Error ensures the correct PositionMessage is used.
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            # Process the caught error, log it and throw depending on the specified ErrorAction.
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
