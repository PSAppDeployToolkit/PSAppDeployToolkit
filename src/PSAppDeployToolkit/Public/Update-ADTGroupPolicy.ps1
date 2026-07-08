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
        The `Update-ADTGroupPolicy` function performs a gpupdate command to refresh Group Policies on the local machine.

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

    .LINK
        https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/main/src/PSAppDeployToolkit/Public/Update-ADTGroupPolicy.ps1
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateSet('Computer', 'User')]
        [PSAppDeployToolkit.Attributes.ValidateUnique()]
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

        # Internal implementation for repeated usage.
        function Update-ADTGroupPolicyImpl
        {
            [CmdletBinding()]
            param
            (
                [Parameter(Mandatory = $true)]
                [ValidateSet('Computer', 'User')]
                [ValidateNotNullOrEmpty()]
                [System.String]$Target,

                [Parameter(Mandatory = $false)]
                [ValidateNotNullOrEmpty()]
                [System.Management.Automation.SwitchParameter]$Force,

                [Parameter(Mandatory = $false)]
                [ValidateNotNullOrEmpty()]
                [System.Management.Automation.SwitchParameter]$NoWait
            )

            dynamicparam
            {
                # Return early when targeting Computer because no dynamic RunAsActiveUser parameter is needed.
                if ($PSBoundParameters.Target -eq 'Computer')
                {
                    return
                }

                # Define parameter dictionary for returning at the end.
                $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()

                # Add in the mandatory RunAsActiveUser parameter.
                $paramDictionary.Add('RunAsActiveUser', [System.Management.Automation.RuntimeDefinedParameter]::new(
                        'RunAsActiveUser', [PSADT.Foundation.RunAsActiveUser], $(
                            [System.Management.Automation.ParameterAttribute]@{ Mandatory = $true }
                            [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                        )
                    ))

                # Return the populated dictionary.
                return $paramDictionary
            }

            begin
            {
                # Set up the parameters for Start-ADTProcess.
                $sapParams = @{
                    FilePath = "$([System.Environment]::SystemDirectory)\gpupdate.exe"
                    ArgumentList = $("/Target:$Target"; if ($Force) { '/Force' })
                    InformationAction = [System.Management.Automation.ActionPreference]::SilentlyContinue
                    CreateNoWindow = $true
                    StandardInput = 'N'
                }
                if ($PSBoundParameters.ContainsKey('RunAsActiveUser'))
                {
                    $sapParams.Add('RunAsActiveUser', $PSBoundParameters.RunAsActiveUser)
                }
            }

            end
            {
                # Perform the underlying call as required.
                if (!$NoWait)
                {
                    Write-ADTLogEntry -Message "$(($msg = "Updating Group Policies for the $Target"))."
                    try
                    {
                        try
                        {
                            if (($result = Start-ADTProcess @sapParams -ErrorAction SilentlyContinue -PassThru).ExitCode -ne 0)
                            {
                                $naerParams = @{
                                    Exception = [PSADT.ProcessManagement.ProcessException]::new("$msg failed with exit code [$($result.ExitCode)].", $result)
                                    Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                                    ErrorId = "GpUpdate$($Target)Failure"
                                    TargetObject = $result
                                    RecommendedAction = "Please review the result in this error's TargetObject property and try again."
                                }
                                throw (New-ADTErrorRecord @naerParams)
                            }
                            $result.Dispose()
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
                    Write-ADTLogEntry -Message "Updating Group Policies for the $Target without waiting."
                    Start-ADTProcess @sapParams -NoWait
                }
            }
        }
    }

    process
    {
        # Handle the Computer target first.
        if ($Target.Contains('Computer'))
        {
            Update-ADTGroupPolicyImpl -Target Computer -Force:$Force -NoWait:$NoWait
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
            Update-ADTGroupPolicyImpl -Target User -Force:$Force -NoWait:$NoWait -RunAsActiveUser $runAsActiveUser
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
