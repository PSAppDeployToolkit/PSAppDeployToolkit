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
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
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
        try
        {
            try
            {
                # Test whether the device is Autopilot-enrolled.
                if (![Microsoft.Win32.Registry]::GetValue("HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Provisioning\Diagnostics\AutoPilot", "CloudAssignedTenantId", $null))
                {
                    return $false
                }

                # Test whether wwahost.exe is running. This is responsible for displaying the ESP.
                if (!($wwaHostProcess = $([System.Diagnostics.Process]::GetProcessesByName('wwahost'))))
                {
                    return $false
                }

                # Return early if the device is actively within the OOBE according to the system.
                if (!(Test-ADTOobeCompleted))
                {
                    return $true
                }

                # Confirm that there's an actively logged on user.
                if (!($runAsActiveUser = Get-ADTClientServerUser))
                {
                    return $false
                }

                # Return early if the wwahost process is not owned by the currently logged on user.
                if ($wwaHostProcess.SessionId -ne $runAsActiveUser.SessionId)
                {
                    return $false
                }

                # Test whether there's active ESP data for the current user.
                if (!($espData = Get-ItemProperty -Path "Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Enrollments\*\FirstSync\$($runAsActiveUser.SID)"))
                {
                    return $false
                }

                # Locate the IsSyncDone property and coerce it into a bool for return. If the value is null, we return null here to indicate indetermination.
                if (($isSyncDone = $espData | Select-Object -ExpandProperty IsSyncDone -ErrorAction Ignore) -is [System.Int32])
                {
                    return !!$isSyncDone
                }
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
