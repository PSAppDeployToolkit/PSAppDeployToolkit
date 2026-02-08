#-----------------------------------------------------------------------------
#
# MARK: Update-ADTGroupPolicy
#
#-----------------------------------------------------------------------------

function Update-ADTGroupPolicy
{
    <#
    .SYNOPSIS
        Performs a gpupdate command to refresh Group Policies on the local machine.

    .DESCRIPTION
        This function performs a gpupdate command to refresh Group Policies on the local machine.

    .PARAMETER Target
        Specifies that only User or only Computer policy settings are updated. By default, both User and Computer policy settings are updated.

    .PARAMETER Force
        Reapplies all policy settings. By default, only policy settings that have changed are applied.

    .PARAMETER NoWait
        Starts the underlying gpupdate.exe call without waiting for it to finish.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any objects.

    .EXAMPLE
        Update-ADTGroupPolicy

        Performs a gpupdate command to refresh Group Policies on the local machine.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Update-ADTGroupPolicy
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateSet('Computer', 'User')]
        [System.String[]]$Target = ('Computer', 'User'),

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SwitchParameter]$Force,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SwitchParameter]$NoWait
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
    }

    process
    {
        # Handle the Computer target first.
        if ($Target.Contains('Computer'))
        {
            # Set up the parameters for Start-ADTProcess.
            $sapParams = @{
                FilePath = "$([System.Environment]::SystemDirectory)\gpupdate.exe"
                ArgumentList = $('/Target:Computer'; if ($Force) { '/Force' })
                InformationAction = [System.Management.Automation.ActionPreference]::SilentlyContinue
                CreateNoWindow = $true
                StandardInput = 'N'
            }
            if (!$NoWait)
            {
                Write-ADTLogEntry -Message "$(($msg = "Updating Group Policies for the Computer"))."
                try
                {
                    try
                    {
                        if (($result = Start-ADTProcess @sapParams -IgnoreExitCodes * -PassThru).ExitCode -ne 0)
                        {
                            $naerParams = @{
                                Exception = [System.Runtime.InteropServices.ExternalException]::new("$msg failed with exit code [$result.ExitCode].", $result.ExitCode)
                                Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                                ErrorId = 'GpUpdateComputerFailure'
                                TargetObject = $result
                                RecommendedAction = "Please review the result in this error's TargetObject property and try again."
                            }
                            throw (New-ADTErrorRecord @naerParams)
                        }
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
            else
            {
                Write-ADTLogEntry -Message "Updating Group Policies for the Computer without waiting."
                Start-ADTProcess @sapParams -NoWait
            }
        }

        # Handle the User target if specified.
        if ($Target.Contains('User'))
        {
            # Return early if there's no logged on user.
            if (!($runAsActiveUser = Get-ADTClientServerUser))
            {
                Write-ADTLogEntry -Message "Bypassing Group Policy update for the User as there is no active user logged onto the system."
            }

            # Set up the parameters for Invoke-ADTClientServerOperation.
            $iacsoParams = @{
                GroupPolicyUpdate = $true
                User = $runAsActiveUser
                Force = $Force
            }
            if (!$NoWait)
            {
                Write-ADTLogEntry -Message "$(($msg = "Updating Group Policies for the User"))."
                try
                {
                    try
                    {
                        if (($result = Invoke-ADTClientServerOperation @iacsoParams).ExitCode -ne 0)
                        {
                            $naerParams = @{
                                Exception = [System.Runtime.InteropServices.ExternalException]::new("$msg failed with exit code [$result.ExitCode].", $result.ExitCode)
                                Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                                ErrorId = 'GpUpdateUserFailure'
                                TargetObject = $result
                                RecommendedAction = "Please review the result in this error's TargetObject property and try again."
                            }
                            throw (New-ADTErrorRecord @naerParams)
                        }
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
            else
            {
                Write-ADTLogEntry -Message "Updating Group Policies for the Computer without waiting."
                Invoke-ADTClientServerOperation @iacsoParams -NoWait
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
