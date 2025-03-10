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
        This function performs a gpupdate command to refresh Group Policies on the local machine. It updates both Computer and User policies by forcing a refresh using the gpupdate.exe utility.

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
        Copyright: © 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Update-ADTGroupPolicy
    #>

    [CmdletBinding()]
    param
    (
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
    }

    process
    {
        # Handle each target separately so we can report on it.
        foreach ($target in ('Computer', 'User'))
        {
            try
            {
                try
                {
                    # Invoke gpupdate.exe and cache the results. An exit code of 0 is considered successful.
                    Write-ADTLogEntry -Message "$(($msg = "Updating Group Policies for the $target"))."
                    $gpUpdateResult = & "$([System.Environment]::SystemDirectory)\cmd.exe" /c "echo N | gpupdate.exe /Target:$target /Force" 2>&1
                    if (!$Global:LASTEXITCODE)
                    {
                        continue
                    }

                    # If we're here, we had a bad exit code.
                    Write-ADTLogEntry -Message ($msg = "$msg failed with exit code [$Global:LASTEXITCODE].") -Severity 3
                    $naerParams = @{
                        Exception = [System.Runtime.InteropServices.ExternalException]::new($msg, $Global:LASTEXITCODE)
                        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                        ErrorId = 'GpUpdateFailure'
                        TargetObject = $gpUpdateResult
                        RecommendedAction = "Please review the result in this error's TargetObject property and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
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
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
