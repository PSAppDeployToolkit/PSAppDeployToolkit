#-----------------------------------------------------------------------------
#
# MARK: Get-ADTMsiExitCodeMessage
#
#-----------------------------------------------------------------------------

function Get-ADTMsiExitCodeMessage
{
    <#
    .SYNOPSIS
        Get message for MSI error code.

    .DESCRIPTION
        Get message for MSI error code by reading it from msimsg.dll.

    .PARAMETER MsiExitCode
        MSI error code.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.String

        Returns the message for the MSI error code.

    .EXAMPLE
        Get-ADTMsiExitCodeMessage -MsiErrorCode 1618

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        http://msdn.microsoft.com/en-us/library/aa368542(v=vs.85).aspx

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$MsiExitCode
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
                # Only return the output if we receive something from the library.
                if (![System.String]::IsNullOrWhiteSpace(($msg = [PSADT.Installer.Msi]::GetMessageFromMsiExitCode($MsiExitCode).Trim())))
                {
                    return $msg
                }
            }
            catch
            {
                # Re-writing the ErrorRecord with Write-Object ensures the correct PositionMessage is used.
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
