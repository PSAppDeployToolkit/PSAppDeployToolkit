#-----------------------------------------------------------------------------
#
# MARK: Test-ADTMicrophoneInUse
#
#-----------------------------------------------------------------------------

function Test-ADTMicrophoneInUse
{
    <#
    .SYNOPSIS
        Tests whether the device's microphone is in use.

    .DESCRIPTION
        Tests whether someone is using the microphone on their device. This could be within Teams, Zoom, a game, or any other app that uses a microphone.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Boolean

        Returns $true if the microphone is in use, otherwise returns $false.

    .EXAMPLE
        Test-ADTMicrophoneInUse

        Checks if the microphone is in use and returns true or false.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Test-ADTMicrophoneInUse
    #>

    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        Write-ADTLogEntry -Message "Checking whether the device's microphone is in use..."
        try
        {
            try
            {
                if (($microphoneInUse = [PSADT.Utilities.DeviceUtilities]::IsMicrophoneInUse()))
                {
                    Write-ADTLogEntry -Message "The device's microphone is currently in use."
                }
                else
                {
                    Write-ADTLogEntry -Message "The device's microphone is currently not in use."
                }
                return $microphoneInUse
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
