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

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    param
    (
    )

    begin
    {
        # Make this function continue on error.
        & $Script:CommandTable.'Initialize-ADTFunction' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
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
                    & $Script:CommandTable.'Write-ADTLogEntry' -Message "$(($msg = "Updating Group Policies for the $target"))."
                    $gpUpdateResult = & "$([System.Environment]::SystemDirectory)\cmd.exe" /c "echo N | gpupdate.exe /Target:$target /Force" 2>&1
                    if (!$LASTEXITCODE)
                    {
                        return
                    }

                    # If we're here, we had a bad exit code.
                    & $Script:CommandTable.'Write-ADTLogEntry' -Message ($msg = "$msg failed with exit code [$LASTEXITCODE].") -Severity 3
                    $naerParams = @{
                        Exception = [System.ApplicationException]::new($msg)
                        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                        ErrorId = 'GpUpdateFailure'
                        TargetObject = $gpUpdateResult
                        RecommendedAction = "Please review the result in this error's TargetObject property and try again."
                    }
                    throw (& $Script:CommandTable.'New-ADTErrorRecord' @naerParams)
                }
                catch
                {
                    & $Script:CommandTable.'Write-Error' -ErrorRecord $_
                }
            }
            catch
            {
                & $Script:CommandTable.'Invoke-ADTFunctionErrorHandler' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
            }
        }
    }

    end
    {
        & $Script:CommandTable.'Complete-ADTFunction' -Cmdlet $PSCmdlet
    }
}
