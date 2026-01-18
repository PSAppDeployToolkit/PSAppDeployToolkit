#-----------------------------------------------------------------------------
#
# MARK: Test-ADTEspActive
#
#-----------------------------------------------------------------------------

function Test-ADTEspActive
{
    <#
    .SYNOPSIS
        Checks if the device is currently within a device or user Enrollment Status Page (ESP) phase.

    .DESCRIPTION
        This function checks if the device is currently within a device or user Enrollment Status Page (ESP) phase.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Boolean

        Returns $true if the device is actively within a device or user ESP phase, otherwise $false.

    .EXAMPLE
        Test-ADTEspActive

        Checks if the device is actively within a device or user ESP phase or not and returns true or false.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Test-ADTEspActive
    #>

    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
    )

    begin
    {
        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        # Perform the device ESP tests, followed by the user ESP tests.
        Write-ADTLogEntry -Message "Testing whether Windows is currently in a device or user ESP state."
        try
        {
            try
            {
                # Test whether wwahost.exe is running. This is responsible for displaying the ESP.
                if (!($wwaHostProcess = [System.Diagnostics.Process]::GetProcessesByName('wwahost')).Length)
                {
                    Write-ADTLogEntry -Message "Current ESP state is [$false]. Reason: [There is no wwahost process currently running]."
                    return $false
                }

                # Return early if the device is actively within the OOBE according to the system.
                if (!(Test-ADTOobeCompleted))
                {
                    Write-ADTLogEntry -Message "Current ESP state is [$true]. Reason: [Device is still within the OOBE phase]."
                    return $true
                }

                # Confirm that there's an actively logged on user.
                if (!($runAsActiveUser = Get-ADTClientServerUser))
                {
                    Write-ADTLogEntry -Message "Current ESP state is [$false]. Reason: [There is actively logged on user]."
                    return $false
                }

                # Return early if the wwahost process is not owned by the currently logged on user.
                if (!($wwaHostProcess | & { process { if ($_.SessionId -eq $runAsActiveUser.SessionId) { return $_ } } } | Select-Object -First 1))
                {
                    Write-ADTLogEntry -Message "Current ESP state is [$false]. Reason: [The current wwahost process is not running as the actively logged on user]."
                    return $false
                }

                # Test whether there's active ESP data for the current user.
                if (!($espData = Get-ItemProperty -Path "Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Enrollments\*\FirstSync\$($runAsActiveUser.SID)"))
                {
                    Write-ADTLogEntry -Message "Current ESP state is [$false]. Reason: [There is no ESP data for the actively logged on user]."
                    return $false
                }

                # Locate the IsSyncDone property and coerce it into a bool for return. If the value is null, we consider that false.
                $isEspActive = !($espData | Select-Object -ExpandProperty IsSyncDone -ErrorAction Ignore)
                Write-ADTLogEntry -Message "Current ESP state is [$isEspActive]. Reason: [Based on IsSyncDone flag within the registry]."
                return $isEspActive
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
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Error determining whether an Enrollment Status Page is active."
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
